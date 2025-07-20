DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'user_sessions' AND column_name = 'refresh_token_hash'
    ) THEN
        ALTER TABLE user_sessions
        DROP COLUMN refresh_token_hash;
        RAISE NOTICE 'Kolon kaldırıldı: refresh_token_hash';
    ELSE
        RAISE NOTICE 'Kolon bulunamadı, zaten kaldırılmış olabilir: refresh_token_hash';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'user_sessions' AND column_name = 'refresh_token_salt'
    ) THEN
        ALTER TABLE user_sessions
        DROP COLUMN refresh_token_salt;
        RAISE NOTICE 'Kolon kaldırıldı: refresh_token_salt';
    ELSE
        RAISE NOTICE 'Kolon bulunamadı, zaten kaldırılmış olabilir: refresh_token_salt';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'user_sessions' AND column_name = 'token_identifier'
    ) THEN
        ALTER TABLE user_sessions
        ADD COLUMN token_identifier TEXT;
        RAISE NOTICE 'Kolon eklendi: token_identifier';
    ELSE
        RAISE NOTICE 'Kolon zaten mevcut: token_identifier';
    END IF;

    ALTER TABLE user_sessions ALTER COLUMN token_identifier SET NOT NULL;

END
$$;