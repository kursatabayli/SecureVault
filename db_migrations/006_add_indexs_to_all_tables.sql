DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
        AND tablename = 'vault_items'
        AND indexname = 'idx_vault_items_on_user_id'
    ) THEN
        CREATE INDEX idx_vault_items_on_user_id ON vault_items (user_id);
        RAISE NOTICE 'Index oluşturuldu: idx_vault_items_on_user_id';
    ELSE
        RAISE NOTICE 'Index zaten mevcut: idx_vault_items_on_user_id';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
        AND tablename = 'user_sessions'
        AND indexname = 'idx_user_sessions_on_user_id'
    ) THEN
        CREATE INDEX idx_user_sessions_on_user_id ON user_sessions (user_id);
        RAISE NOTICE 'Index oluşturuldu: idx_user_sessions_on_user_id';
    ELSE
        RAISE NOTICE 'Index zaten mevcut: idx_user_sessions_on_user_id';
    END IF;


    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
        AND tablename = 'user_sessions'
        AND indexname = 'idx_user_sessions_on_token_identifier'
    ) THEN
        CREATE INDEX idx_user_sessions_on_token_identifier ON user_sessions (token_identifier);
        RAISE NOTICE 'Index oluşturuldu: idx_user_sessions_on_token_identifier';
    ELSE
        RAISE NOTICE 'Index zaten mevcut: idx_user_sessions_on_token_identifier';
    END IF;

END
$$;
