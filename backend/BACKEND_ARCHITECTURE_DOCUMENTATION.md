# Prime Management Backend - Complete Architecture Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Database Schema & Relationships](#database-schema--relationships)
3. [Authentication Flow](#authentication-flow)
4. [API Endpoints & Flows](#api-endpoints--flows)
5. [Service Layer Architecture](#service-layer-architecture)
6. [Data Flow Examples](#data-flow-examples)
7. [Security & Authorization](#security--authorization)
8. [Error Handling](#error-handling)

---

## Project Overview

The Prime Management Backend is an ASP.NET Core Web API that manages a hierarchical organization structure with:
- **Admins**: System administrators who manage managers
- **Managers**: Supervise agents and groups
- **Agents**: Sales/Individual agents who collect clients and track attendance
- **Groups**: Teams of agents led by team leaders

### Key Technologies
- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Authentication**: JWT Bearer Tokens
- **Architecture**: Repository Pattern + Service Layer

---

## Database Schema & Relationships

### Core Entity Relationships

```
User (1) ←→ (1) Manager
User (1) ←→ (1) Agent
Manager (1) ←→ (N) Agent
Manager (1) ←→ (N) Group
Group (1) ←→ (N) Agent
Agent (1) ←→ (N) Client
Agent (1) ←→ (N) Attendance
Agent (1) ←→ (N) ClientsCollected
User (1) ←→ (N) Notification (as Sender)
User (1) ←→ (N) Notification (as Recipient)
```

### Entity Details

#### 1. User Entity
```csharp
// Core user information
- Id (Primary Key)
- FirstName, LastName, Email, WorkId
- PasswordHash (encrypted)
- Role (ADMIN, MANAGER, AGENT)
- Active (boolean)
- LastLogin (DateTime?)
- CreatedAt

// Navigation Properties
- Agent (1:1 relationship)
- Manager (1:1 relationship)
- SentNotifications (1:N)
- ReceivedNotifications (1:N)
- AuditLogs (1:N)
- CreatedManagers (1:N - for admins)
```

#### 2. Manager Entity
```csharp
// Manager-specific data
- UserId (Primary Key, Foreign Key to User)
- CreatedById (Foreign Key to User - who created this manager)
- Department

// Navigation Properties
- User (1:1)
- CreatedBy (1:1 - the admin who created this manager)
- Agents (1:N)
- Groups (1:N)
- AttendanceTimeframes (1:N)
```

#### 3. Agent Entity
```csharp
// Agent-specific data
- UserId (Primary Key, Foreign Key to User)
- ManagerId (Foreign Key to Manager)
- GroupId (Nullable Foreign Key to Group)
- AgentType (SALES, INDIVIDUAL)
- Sector (work location)

// Navigation Properties
- User (1:1)
- Manager (1:1)
- Group (1:1, nullable)
- Clients (1:N)
- Attendances (1:N)
- ClientsCollectedRecords (1:N)
- LedGroups (1:N - if agent is a team leader)
```

#### 4. Group Entity
```csharp
// Group information
- Id (Primary Key)
- ManagerId (Foreign Key to Manager)
- Name, Description
- LeaderId (Nullable Foreign Key to Agent)

// Navigation Properties
- Manager (1:1)
- Leader (1:1, nullable)
- Agents (1:N)
```

#### 5. Client Entity
```csharp
// Client information
- Id (Primary Key)
- FullName, NationalId, PhoneNumber, Email
- Location, DateOfBirth
- InsuranceType, PayingAmount, PayingMethod
- ContractYears
- AgentId (Nullable Foreign Key to Agent)
- CollectedByName, CollectedAt
- CreatedAt, Active

// Navigation Properties
- Agent (1:1, nullable)
```

#### 6. Notification Entity
```csharp
// Notification data
- Id (Primary Key)
- SenderId (Foreign Key to User)
- RecipientId (Nullable Foreign Key to User)
- Title, Message
- Priority (LOW, MEDIUM, HIGH, URGENT)
- Category (SYSTEM, ATTENDANCE, PERFORMANCE, TASK, OTHER)
- Status, SentAt, ReadStatus
- ViaEmail

// Navigation Properties
- Sender (1:1)
- Recipient (1:1, nullable)
```

---

## Authentication Flow

### Login Process Flow

```
1. Frontend sends POST /api/auth/login
   {
     "workId": "MGR001",
     "email": "manager@company.com",
     "role": "manager"
   }

2. AuthController.Login() processes request:
   a. Validates request model
   b. Calls UserService.GetUserByWorkIdAsync(workId)
   c. Validates email match (case-sensitive)
   d. Validates role match (if provided)
   e. Updates LastLogin timestamp
   f. Generates JWT token via JwtService
   g. Returns AuthResponse with token and user info

3. Database Operations:
   - SELECT * FROM users WHERE work_id = @workId
   - UPDATE users SET last_login = @timestamp WHERE id = @userId

4. JWT Token Generation:
   - Contains user claims: Id, Email, Role, WorkId, etc.
   - Signed with secret key
   - Expires in 30 days

5. Response:
   {
     "token": "eyJhbGciOiJIUzI1NiIs...",
     "user": {
       "id": "35",
       "firstName": "John",
       "lastName": "Doe",
       "email": "manager@company.com",
       "workId": "MGR001",
       "role": "manager",
       "agentType": null,
       "groupName": null
     }
   }
```

### Authorization Flow

```
1. Frontend includes JWT token in Authorization header:
   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

2. JWT Middleware validates token:
   a. Verifies signature
   b. Checks expiration
   c. Extracts claims

3. Authorization attributes check roles:
   [Authorize(Roles = "admin,manager,agent")]
   [Authorize(Roles = "admin")]

4. User context available in controllers via:
   User.FindFirstValue(ClaimTypes.NameIdentifier) // User ID
   User.FindFirstValue("role") // User role
```

---

## API Endpoints & Flows

### 1. Authentication Endpoints

#### POST /api/auth/login
**Flow:**
```
Request → AuthController.Login() → UserService.GetUserByWorkIdAsync() → Database → JwtService.GenerateToken() → Response
```

**Database Operations:**
- `SELECT * FROM users WHERE work_id = @workId`
- `UPDATE users SET last_login = @timestamp WHERE id = @userId`

### 2. Admin Endpoints

#### GET /api/admin/users
**Flow:**
```
Request → AdminController.GetAllUsers() → UserService.GetAllUsersAsync() → Database → UserService.MapToDTO() → Response
```

**Database Operations:**
- `SELECT * FROM users WHERE active = 1`
- Joins with agents, managers for additional data

#### POST /api/admin/managers
**Flow:**
```
Request → AdminController.CreateManager() → ManagerService.CreateManagerAsync() → UserService.CreateUserAsync() → Database → Response
```

**Database Operations:**
- `INSERT INTO users (first_name, last_name, email, work_id, password_hash, role, created_at, active) VALUES (...)`
- `INSERT INTO managers (user_id, created_by) VALUES (...)`

#### GET /api/admin/managers
**Flow:**
```
Request → AdminController.GetAllManagers() → ManagerService.GetAllManagersAsync() → Database → Map to DTO → Response
```

**Database Operations:**
- `SELECT m.*, u.* FROM managers m JOIN users u ON m.user_id = u.id WHERE u.active = 1`
- Count agents for each manager
- Get last login information

#### PUT /api/admin/managers/{id}
**Flow:**
```
Request → AdminController.UpdateManager() → ManagerService.UpdateManagerAsync() → Database → Response
```

**Database Operations:**
- `UPDATE users SET first_name = @firstName, last_name = @lastName, ... WHERE id = @userId`
- `UPDATE managers SET ... WHERE user_id = @userId`

#### POST /api/admin/notifications
**Flow:**
```
Request → AdminController.SendNotification() → NotificationService.SendNotificationAsync() → Database → Response
```

**Database Operations:**
- `INSERT INTO notifications (sender_id, recipient_id, title, message, priority, category, sent_at, read_status) VALUES (...)`

### 3. Manager Endpoints

#### GET /api/manager/dashboard
**Flow:**
```
Request → ManagerController.GetDashboard() → ManagerService.GetDashboardAsync() → Multiple Services → Response
```

**Database Operations:**
- Get manager's agents: `SELECT * FROM agents WHERE manager_id = @managerId`
- Get attendance data: `SELECT * FROM attendance WHERE agent_id IN (...)`
- Get client data: `SELECT * FROM clients WHERE agent_id IN (...)`
- Get group data: `SELECT * FROM agent_groups WHERE manager_id = @managerId`

#### GET /api/manager/agents
**Flow:**
```
Request → ManagerController.GetAgents() → AgentService.GetAgentsByManagerAsync() → Database → Map to DTO → Response
```

**Database Operations:**
- `SELECT a.*, u.* FROM agents a JOIN users u ON a.user_id = u.id WHERE a.manager_id = @managerId AND u.active = 1`

#### POST /api/manager/agents
**Flow:**
```
Request → ManagerController.CreateAgent() → AgentService.CreateAgentAsync() → UserService.CreateUserAsync() → Database → Response
```

**Database Operations:**
- `INSERT INTO users (...) VALUES (...)`
- `INSERT INTO agents (user_id, manager_id, agent_type, sector) VALUES (...)`

#### PUT /api/manager/agents/{id}
**Flow:**
```
Request → ManagerController.UpdateAgent() → AgentService.UpdateAgentAsync() → Database → Response
```

**Database Operations:**
- `UPDATE users SET ... WHERE id = @userId`
- `UPDATE agents SET ... WHERE user_id = @userId`

#### GET /api/manager/performance
**Flow:**
```
Request → ManagerController.GetPerformance() → ManagerService.GetPerformanceOverviewAsync() → Multiple Services → Response
```

**Database Operations:**
- Get all agents under manager
- Get clients for each agent in date range
- Get attendance data for each agent
- Calculate performance metrics

### 4. Agent Endpoints

#### GET /api/agent/dashboard
**Flow:**
```
Request → AgentController.GetDashboard() → Multiple Services → Response
```

**Database Operations:**
- Check attendance: `SELECT * FROM attendance WHERE agent_id = @agentId AND DATE(timestamp) = CURDATE()`
- Get client count: `SELECT COUNT(*) FROM clients WHERE agent_id = @agentId`
- Get recent activities: `SELECT * FROM clients WHERE agent_id = @agentId ORDER BY created_at DESC LIMIT 5`

#### POST /api/agent/attendance
**Flow:**
```
Request → AgentController.MarkAttendance() → AttendanceService.MarkAttendanceAsync() → Database → Response
```

**Database Operations:**
- `INSERT INTO attendance (agent_id, timestamp, location, sector) VALUES (...)`

#### GET /api/agent/attendance/history
**Flow:**
```
Request → AgentController.GetAttendanceHistory() → AttendanceService.GetAttendanceByAgentAsync() → Database → Response
```

**Database Operations:**
- `SELECT * FROM attendance WHERE agent_id = @agentId ORDER BY timestamp DESC`

#### GET /api/agent/group-performance
**Flow:**
```
Request → AgentController.GetGroupPerformance() → BuildGroupPerformanceDto() → Multiple Services → Response
```

**Database Operations:**
- Get agent's group: `SELECT * FROM agent_groups WHERE id = @groupId`
- Get group members: `SELECT * FROM agents WHERE group_id = @groupId`
- Get performance data for all group members

#### POST /api/agent/clients
**Flow:**
```
Request → AgentController.CreateClient() → ClientService.CreateClientAsync() → Database → Response
```

**Database Operations:**
- `INSERT INTO clients (full_name, national_id, phone_number, ..., agent_id, created_at) VALUES (...)`

---

## Service Layer Architecture

### Service Responsibilities

#### 1. UserService
- User CRUD operations
- Password hashing/validation
- User status management
- DTO mapping

#### 2. ManagerService
- Manager CRUD operations
- Performance calculations
- Dashboard data aggregation
- Agent management

#### 3. AgentService
- Agent CRUD operations
- Performance calculations
- Attendance tracking
- Client management

#### 4. NotificationService
- Notification CRUD operations
- Recipient filtering
- Priority/category handling
- Read status management

#### 5. ClientService
- Client CRUD operations
- Search functionality
- Date range filtering
- DTO mapping

#### 6. AttendanceService
- Attendance tracking
- Time calculations
- Status determination (present/late)
- Date range queries

### Repository Pattern
Each service uses repositories for data access:
- `IUserRepository`
- `IManagerRepository`
- `IAgentRepository`
- `INotificationRepository`
- `IClientRepository`
- `IAttendanceRepository`

---

## Data Flow Examples

### Example 1: Manager Creating an Agent

```
1. Frontend sends POST /api/manager/agents
   {
     "firstName": "John",
     "lastName": "Doe",
     "email": "john.doe@company.com",
     "workId": "AGT001",
     "agentType": "sales",
     "sector": "Kigali"
   }

2. ManagerController.CreateAgent()
   - Extracts manager ID from JWT token
   - Validates request data
   - Calls AgentService.CreateAgentAsync()

3. AgentService.CreateAgentAsync()
   - Creates User entity
   - Calls UserService.CreateUserAsync()
   - Creates Agent entity
   - Saves to database
   - Returns agent DTO

4. Database Operations:
   INSERT INTO users (first_name, last_name, email, work_id, password_hash, role, created_at, active)
   VALUES ('John', 'Doe', 'john.doe@company.com', 'AGT001', 'hashed_password', 'agent', NOW(), 1);

   INSERT INTO agents (user_id, manager_id, agent_type, sector)
   VALUES (LAST_INSERT_ID(), @managerId, 'SALES', 'Kigali');

5. Response:
   {
     "id": "agt-001",
     "firstName": "John",
     "lastName": "Doe",
     "email": "john.doe@company.com",
     "workId": "AGT001",
     "role": "agent",
     "type": "sales",
     "sector": "Kigali",
     "status": "active"
   }
```

### Example 2: Agent Marking Attendance

```
1. Frontend sends POST /api/agent/attendance
   {
     "location": "Kigali Office",
     "sector": "Sales"
   }

2. AgentController.MarkAttendance()
   - Extracts agent ID from JWT token
   - Gets agent entity
   - Calls AttendanceService.MarkAttendanceAsync()

3. AttendanceService.MarkAttendanceAsync()
   - Creates Attendance entity
   - Sets timestamp to current time
   - Saves to database
   - Returns attendance info

4. Database Operations:
   INSERT INTO attendance (agent_id, timestamp, location, sector)
   VALUES (@agentId, NOW(), 'Kigali Office', 'Sales');

5. Response:
   {
     "message": "Attendance marked successfully at 09:15",
     "attendance": {
       "date": "2025-08-07",
       "time": "09:15",
       "status": "Present"
     }
   }
```

### Example 3: Admin Sending Notification

```
1. Frontend sends POST /api/admin/notifications
   {
     "title": "Monthly Meeting",
     "message": "Team meeting scheduled for tomorrow at 10 AM",
     "recipient": "All Managers",
     "priority": "high",
     "category": "system"
   }

2. AdminController.SendNotification()
   - Extracts admin info from JWT token
   - Validates notification data
   - Calls NotificationService.SendNotificationAsync()

3. NotificationService.SendNotificationAsync()
   - Determines recipients based on "All Managers"
   - Creates Notification entities for each recipient
   - Saves to database
   - Returns notification response

4. Database Operations:
   INSERT INTO notifications (sender_id, recipient_id, title, message, priority, category, sent_at, read_status)
   VALUES (@adminId, @managerId1, 'Monthly Meeting', 'Team meeting...', 'HIGH', 'SYSTEM', NOW(), 0);

   INSERT INTO notifications (sender_id, recipient_id, title, message, priority, category, sent_at, read_status)
   VALUES (@adminId, @managerId2, 'Monthly Meeting', 'Team meeting...', 'HIGH', 'SYSTEM', NOW(), 0);

5. Response:
   {
     "id": "notif-123",
     "title": "Monthly Meeting",
     "message": "Team meeting scheduled for tomorrow at 10 AM",
     "recipient": "All Managers",
     "priority": "high",
     "category": "system",
     "status": "sent",
     "sentAt": "2025-08-07T09:15:00Z",
     "readBy": 0,
     "totalRecipients": 5
   }
```

---

## Security & Authorization

### JWT Token Structure
```json
{
  "nameid": "35",
  "email": "manager@company.com",
  "unique_name": "John Doe",
  "FirstName": "John",
  "LastName": "Doe",
  "WorkId": "MGR001",
  "role": "manager",
  "Active": "True",
  "nbf": 1754574041,
  "exp": 1757166041,
  "iat": 1754574041,
  "iss": "PrimeManagementApp",
  "aud": "PrimeManagementAppUsers"
}
```

### Role-Based Access Control
- **Admin**: Full system access, can manage managers
- **Manager**: Can manage agents and groups under their supervision
- **Agent**: Can access their own data and perform assigned tasks

### Authorization Attributes
```csharp
[Authorize(Roles = "admin,manager,agent")]  // Multiple roles
[Authorize(Roles = "admin")]                // Admin only
[Authorize(Roles = "manager")]              // Manager only
[AllowAnonymous]                            // No authentication required
```

---

## Error Handling

### Global Exception Handling
- Custom exception middleware
- Structured error responses
- Logging of all errors

### Error Response Format
```json
{
  "error": "Error type",
  "message": "Human-readable error message",
  "details": "Additional error details (optional)"
}
```

### Common Error Scenarios
1. **Authentication Errors**: Invalid credentials, expired tokens
2. **Authorization Errors**: Insufficient permissions
3. **Validation Errors**: Invalid request data
4. **Database Errors**: Constraint violations, connection issues
5. **Business Logic Errors**: Invalid operations

---

## Performance Considerations

### Database Optimization
- Proper indexing on frequently queried columns
- Efficient joins and queries
- Connection pooling
- Query result caching where appropriate

### API Optimization
- Pagination for large datasets
- Selective field loading
- Efficient DTO mapping
- Response compression

### Security Best Practices
- JWT token expiration
- Password hashing with salt
- Input validation and sanitization
- SQL injection prevention via parameterized queries
- CORS configuration
- Rate limiting

---

This documentation provides a comprehensive overview of the Prime Management Backend architecture, helping developers understand the system's structure, data flow, and implementation details.
