CREATE TYPE conversation_title_status AS ENUM (
    'not_generated',
    'generating',
    'generated'
);

CREATE TABLE conversations
(
    id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    last_updated_at TIMESTAMPTZ NOT NULL,

    title VARCHAR(200) NOT NULL,

    title_status conversation_title_status NOT NULL,

    preview VARCHAR(140),

    CONSTRAINT pk_conversations
        PRIMARY KEY (id)
);