CREATE TYPE chat_role AS ENUM (
    'system',
    'user',
    'assistant'
);

CREATE TABLE conversation_messages
(
    id UUID NOT NULL,
    conversation_id UUID NOT NULL,

    role chat_role NOT NULL,

    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,

    input_token_count INTEGER,
    output_token_count INTEGER,
    context_limit INTEGER,
    remaining_token_budget INTEGER,
    percentage_used DOUBLE PRECISION,

    CONSTRAINT pk_conversation_messages
        PRIMARY KEY (id),

    CONSTRAINT fk_conversation_messages_conversation
        FOREIGN KEY (conversation_id)
        REFERENCES conversations(id)
        ON DELETE CASCADE
);