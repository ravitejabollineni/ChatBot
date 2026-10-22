--liquibase formatted sql

--changeset chatbot:005-enable-pgvector-extension

CREATE EXTENSION IF NOT EXISTS vector;
