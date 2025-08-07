-- Ensure agent_id and user_id are compatible for the foreign key
ALTER TABLE agents
  MODIFY user_id BIGINT UNSIGNED NOT NULL;

ALTER TABLE clients
  ADD CONSTRAINT fk_client_agent FOREIGN KEY (agent_id) REFERENCES agents(user_id) ON DELETE SET NULL; 