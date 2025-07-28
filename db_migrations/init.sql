CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(100) UNIQUE NOT NULL,
    public_key BYTEA NOT NULL,
    user_info JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    salt BYTEA NOT NULL
);


CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    ip_address VARCHAR(45),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    last_used_at TIMESTAMPTZ,
    device_details JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_persistent BOOLEAN NOT NULL DEFAULT FALSE,
    refresh_token_jti TEXT NOT NULL,
    access_token_jti TEXT NULL
);


CREATE INDEX idx_user_sessions_on_user_id ON user_sessions (user_id);
CREATE INDEX idx_user_sessions_on_refresh_token_jti ON user_sessions (refresh_token_jti);

RAISE NOTICE 'Veritabanı şeması ve indeksler başarıyla oluşturuldu.';