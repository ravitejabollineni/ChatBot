--liquibase formatted sql

--changeset chatbot:004-add-is-partial-to-conversation-messages

ALTER TABLE conversation_messages
    ADD COLUMN is_partial BOOLEAN NOT NULL DEFAULT FALSE;
