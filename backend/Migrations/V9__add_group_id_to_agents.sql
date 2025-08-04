-- Add group_id column to agents table
ALTER TABLE agents ADD COLUMN group_id BIGINT NULL;

-- Add foreign key constraint
ALTER TABLE agents ADD CONSTRAINT fk_agents_group FOREIGN KEY (group_id) REFERENCES agent_groups(id); 