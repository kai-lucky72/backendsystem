-- V1__consolidated_schema.sql
-- Complete schema definition for Prime Management App

-- 1. USERS
CREATE TABLE users (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(60) NOT NULL,
    work_id VARCHAR(50) NOT NULL UNIQUE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20) NULL,
    national_id VARCHAR(50) NULL,
    profile_image_url VARCHAR(255) NULL,
    role ENUM('ADMIN','MANAGER','AGENT') NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    INDEX idx_user_email (email),
    INDEX idx_user_work_id (work_id),
    INDEX idx_user_role (role)
) ENGINE=InnoDB;

-- 2. MANAGERS
CREATE TABLE managers (
    user_id BIGINT UNSIGNED PRIMARY KEY,
    created_by BIGINT UNSIGNED NOT NULL,
    department VARCHAR(100) NULL,
    CONSTRAINT fk_managers_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_managers_creator FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- 3. AGENTS
CREATE TABLE agents (
    user_id BIGINT UNSIGNED PRIMARY KEY,
    manager_id BIGINT UNSIGNED NOT NULL,
    agent_type ENUM('SALES','INDIVIDUAL') NOT NULL,
    sector VARCHAR(100) NOT NULL,
    CONSTRAINT fk_agents_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_agents_manager FOREIGN KEY (manager_id) REFERENCES managers(user_id) ON DELETE RESTRICT,
    INDEX idx_agent_manager (manager_id),
    INDEX idx_agent_type (agent_type)
) ENGINE=InnoDB;

-- 4. GROUPS (renamed to agent_groups to avoid MySQL reserved keyword)
CREATE TABLE agent_groups (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    manager_id BIGINT UNSIGNED NOT NULL,
    name VARCHAR(100) NOT NULL,
    leader_id BIGINT UNSIGNED NULL,
    CONSTRAINT fk_groups_manager FOREIGN KEY (manager_id) REFERENCES managers(user_id) ON DELETE CASCADE,
    CONSTRAINT fk_groups_leader FOREIGN KEY (leader_id) REFERENCES agents(user_id) ON DELETE SET NULL,
    INDEX idx_group_manager (manager_id),
    INDEX idx_group_leader (leader_id)
) ENGINE=InnoDB;

-- 5. GROUP_MEMBERS
CREATE TABLE group_members (
    group_id BIGINT UNSIGNED NOT NULL,
    agent_id BIGINT UNSIGNED NOT NULL,
    PRIMARY KEY (group_id, agent_id),
    CONSTRAINT fk_gm_group FOREIGN KEY (group_id) REFERENCES agent_groups(id) ON DELETE CASCADE,
    CONSTRAINT fk_gm_agent FOREIGN KEY (agent_id) REFERENCES agents(user_id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 6. ATTENDANCE
CREATE TABLE attendance (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    agent_id BIGINT UNSIGNED NOT NULL,
    timestamp DATETIME NOT NULL,
    latitude DECIMAL(9,6) NOT NULL,
    longitude DECIMAL(9,6) NOT NULL,
    sector VARCHAR(100) NOT NULL,
    status ENUM('CHECKED_IN', 'CHECKED_OUT', 'BREAK_START', 'BREAK_END') NOT NULL DEFAULT 'CHECKED_IN',
    notes TEXT NULL,
    CONSTRAINT fk_att_agent FOREIGN KEY (agent_id) REFERENCES agents(user_id) ON DELETE CASCADE,
    INDEX idx_attendance_agent (agent_id),
    INDEX idx_attendance_timestamp (timestamp)
) ENGINE=InnoDB;

-- 7. ATTENDANCE TIMEFRAMES
CREATE TABLE attendance_timeframes (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    manager_id BIGINT UNSIGNED NOT NULL,
    day_of_week TINYINT NOT NULL COMMENT '0=Sunday, 1=Monday, etc.',
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    break_duration INT NOT NULL DEFAULT 60 COMMENT 'Break duration in minutes',
    applies_to_all_agents BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_timeframe_manager FOREIGN KEY (manager_id) REFERENCES managers(user_id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 8. AGENT TIMEFRAMES
CREATE TABLE agent_timeframes (
    timeframe_id BIGINT UNSIGNED NOT NULL,
    agent_id BIGINT UNSIGNED NOT NULL,
    PRIMARY KEY (timeframe_id, agent_id),
    CONSTRAINT fk_at_timeframe FOREIGN KEY (timeframe_id) REFERENCES attendance_timeframes(id) ON DELETE CASCADE,
    CONSTRAINT fk_at_agent FOREIGN KEY (agent_id) REFERENCES agents(user_id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 9. CLIENTS
CREATE TABLE clients (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    agent_id BIGINT UNSIGNED NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NULL,
    phone_number VARCHAR(20) NOT NULL,
    national_id VARCHAR(50) NOT NULL UNIQUE,
    address TEXT NULL,
    city VARCHAR(100) NULL,
    insurance_type VARCHAR(50) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_client_agent FOREIGN KEY (agent_id) REFERENCES agents(user_id) ON DELETE RESTRICT,
    INDEX idx_clients_agent (agent_id),
    INDEX idx_clients_national_id (national_id),
    INDEX idx_clients_created_at (created_at)
) ENGINE=InnoDB;

-- 10. CLIENTS_COLLECTED (for unstructured client data collection)
CREATE TABLE clients_collected (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    agent_id BIGINT UNSIGNED NOT NULL,
    collected_at DATETIME NOT NULL,
    client_data JSON NOT NULL,
    CONSTRAINT fk_cc_agent FOREIGN KEY (agent_id) REFERENCES agents(user_id) ON DELETE CASCADE,
    INDEX idx_clients_collected_agent (agent_id),
    INDEX idx_clients_collected_date (collected_at)
) ENGINE=InnoDB;

-- 11. NOTIFICATIONS
CREATE TABLE notifications (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    sender_id BIGINT UNSIGNED NOT NULL,
    recipient_id BIGINT UNSIGNED NULL,
    message TEXT NOT NULL,
    sent_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    via_email BOOLEAN NOT NULL DEFAULT FALSE,
    read_status BOOLEAN NOT NULL DEFAULT FALSE,
    priority ENUM('LOW', 'MEDIUM', 'HIGH', 'URGENT') NOT NULL DEFAULT 'MEDIUM',
    category ENUM('SYSTEM', 'ATTENDANCE', 'PERFORMANCE', 'TASK', 'OTHER') NOT NULL DEFAULT 'SYSTEM',
    CONSTRAINT fk_notif_sender FOREIGN KEY (sender_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_notif_recipient FOREIGN KEY (recipient_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_notification_sender (sender_id),
    INDEX idx_notification_recipient (recipient_id),
    INDEX idx_notification_date (sent_at)
) ENGINE=InnoDB;

-- 12. AUDIT_LOG
CREATE TABLE audit_log (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    event_type VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(50) NOT NULL,
    user_id BIGINT UNSIGNED NULL,
    timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    details TEXT NULL,
    CONSTRAINT fk_audit_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_audit_event_type (event_type),
    INDEX idx_audit_entity (entity_type, entity_id),
    INDEX idx_audit_user (user_id),
    INDEX idx_audit_timestamp (timestamp)
) ENGINE=InnoDB;

-- 13. PERFORMANCE METRICS CACHE
CREATE TABLE performance_metrics_cache (
    cache_key VARCHAR(255) PRIMARY KEY,
    data JSON NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME NOT NULL,
    INDEX idx_cache_expiry (expires_at)
) ENGINE=InnoDB;

-- 14. VIEWS

-- Agent Attendance View
CREATE OR REPLACE VIEW agent_attendance_view AS
SELECT 
    a.id as attendance_id,
    a.timestamp,
    a.status,
    ag.user_id as agent_id,
    u.email as agent_email,
    u.work_id as agent_work_id,
    ag.sector,
    m.user_id as manager_id,
    mu.email as manager_email,
    g.id as group_id,
    g.name as group_name
FROM 
    attendance a
JOIN agents ag ON a.agent_id = ag.user_id
JOIN users u ON ag.user_id = u.id
JOIN managers m ON ag.manager_id = m.user_id
JOIN users mu ON m.user_id = mu.id
LEFT JOIN group_members gm ON ag.user_id = gm.agent_id
LEFT JOIN agent_groups g ON gm.group_id = g.id;

-- Agent Performance View
CREATE OR REPLACE VIEW agent_performance_view AS
SELECT 
    a.user_id AS agent_id,
    u.email AS agent_email,
    u.work_id AS agent_work_id,
    a.agent_type,
    a.sector,
    m.user_id AS manager_id,
    mu.email AS manager_email,
    g.id AS group_id,
    g.name AS group_name,
    (SELECT COUNT(*) FROM attendance att WHERE att.agent_id = a.user_id) AS attendance_count,
    (SELECT COUNT(*) FROM clients_collected cc WHERE cc.agent_id = a.user_id) AS clients_collected_count,
    (SELECT COUNT(*) FROM clients c WHERE c.agent_id = a.user_id) AS clients_count,
    (SELECT AVG(CASE WHEN att.status = 'CHECKED_IN' THEN 1 ELSE 0 END) 
     FROM attendance att 
     WHERE att.agent_id = a.user_id) AS attendance_rate,
    (SELECT COUNT(*) FROM attendance att 
     WHERE att.agent_id = a.user_id 
     AND att.timestamp >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AS recent_attendance_count,
    (SELECT COUNT(*) FROM clients_collected cc 
     WHERE cc.agent_id = a.user_id 
     AND cc.collected_at >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AS recent_clients_collected_count,
    (SELECT COUNT(*) FROM clients c 
     WHERE c.agent_id = a.user_id 
     AND c.created_at >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AS recent_clients_count
FROM 
    agents a
JOIN users u ON a.user_id = u.id
JOIN managers m ON a.manager_id = m.user_id
JOIN users mu ON m.user_id = mu.id
LEFT JOIN group_members gm ON a.user_id = gm.agent_id
LEFT JOIN agent_groups g ON gm.group_id = g.id;

-- Group Performance View
CREATE OR REPLACE VIEW group_performance_view AS
SELECT 
    g.id AS group_id,
    g.name AS group_name,
    g.manager_id,
    mu.email AS manager_email,
    g.leader_id,
    lu.email AS leader_email,
    (SELECT COUNT(*) FROM group_members gm WHERE gm.group_id = g.id) AS members_count,
    (SELECT COUNT(*) FROM attendance att 
     JOIN group_members gm ON att.agent_id = gm.agent_id 
     WHERE gm.group_id = g.id) AS total_attendance_count,
    (SELECT COUNT(*) FROM clients_collected cc 
     JOIN group_members gm ON cc.agent_id = gm.agent_id 
     WHERE gm.group_id = g.id) AS total_clients_collected_count,
    (SELECT COUNT(*) FROM clients c 
     JOIN group_members gm ON c.agent_id = gm.agent_id 
     WHERE gm.group_id = g.id) AS total_clients_count,
    (SELECT COUNT(*) FROM attendance att 
     JOIN group_members gm ON att.agent_id = gm.agent_id 
     WHERE gm.group_id = g.id 
     AND att.timestamp >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AS recent_attendance_count,
    (SELECT COUNT(*) FROM clients_collected cc 
     JOIN group_members gm ON cc.agent_id = gm.agent_id 
     WHERE gm.group_id = g.id 
     AND cc.collected_at >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AS recent_clients_collected_count,
    (SELECT COUNT(*) FROM clients c 
     JOIN group_members gm ON c.agent_id = gm.agent_id 
     WHERE gm.group_id = g.id 
     AND c.created_at >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AS recent_clients_count
FROM 
    agent_groups g
JOIN users mu ON g.manager_id = mu.id
LEFT JOIN users lu ON g.leader_id = lu.id;

-- 15. STORED PROCEDURES

-- Cleanup expired cache entries
DELIMITER //
CREATE PROCEDURE cleanup_expired_cache()
BEGIN
    DELETE FROM performance_metrics_cache WHERE expires_at < NOW();
END//
DELIMITER ;

-- 16. EVENTS

-- Cache cleanup event
CREATE EVENT IF NOT EXISTS cache_cleanup_event
ON SCHEDULE EVERY 1 HOUR
DO
    CALL cleanup_expired_cache(); 