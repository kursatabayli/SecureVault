DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'users' AND column_name = 'salt'
    ) THEN
        ALTER TABLE users
        ADD COLUMN salt BYTEA NOT NULL;
    END IF;
END
$$;
