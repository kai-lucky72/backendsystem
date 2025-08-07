-- Clean up any existing "Unknown Location" records
UPDATE attendance 
SET location = 'Kigali' 
WHERE location = 'Unknown Location'; 