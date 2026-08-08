--liquibase formatted sql

--changeset chatbot:003-create-conversation-indexes

CREATE INDEX ix_conversations_last_updated_at
    ON conversations (last_updated_at DESC);

CREATE INDEX ix_conversation_messages_conversation_id_created_at
    ON conversation_messages (conversation_id, created_at);