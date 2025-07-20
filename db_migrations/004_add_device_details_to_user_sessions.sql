DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'user_sessions' AND column_name = 'device_details'
    ) THEN
        ALTER TABLE user_sessions
        ADD COLUMN device_details JSONB;
    END IF;

    UPDATE user_sessions SET device_details = '{}'::jsonb WHERE device_details IS NULL;
    ALTER TABLE user_sessions ALTER COLUMN device_details SET NOT NULL;
    ALTER TABLE user_sessions ALTER COLUMN device_details SET DEFAULT '{}'::jsonb;

END
$$;