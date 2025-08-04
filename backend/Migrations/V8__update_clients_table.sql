-- Add missing columns to clients table
ALTER TABLE clients
DROP COLUMN first_name,
DROP COLUMN last_name,
ADD COLUMN full_name VARCHAR(200) NOT NULL AFTER agent_id,
ADD COLUMN location VARCHAR(200) NOT NULL,
ADD COLUMN date_of_birth DATE NOT NULL,
ADD COLUMN paying_amount DECIMAL(10,2) NOT NULL,
ADD COLUMN paying_method VARCHAR(50) NOT NULL,
ADD COLUMN contract_years INT NOT NULL; 