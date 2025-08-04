-- Update attendance table to replace latitude/longitude with location
ALTER TABLE attendance 
DROP COLUMN latitude,
DROP COLUMN longitude,
ADD COLUMN location VARCHAR(255) NOT NULL DEFAULT 'Kigali' AFTER timestamp; 