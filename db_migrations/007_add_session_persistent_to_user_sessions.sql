DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'user_sessions' AND column_name = 'is_persistent'
    ) THEN
        ALTER TABLE user_sessions
        ADD COLUMN is_persistent BOOLEAN NOT NULL DEFAULT FALSE;
    END IF;
END
$$;
