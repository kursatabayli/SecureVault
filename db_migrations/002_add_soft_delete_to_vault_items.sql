DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'vault_items' AND column_name = 'is_deleted'
    ) THEN
        ALTER TABLE vault_items
        ADD COLUMN is_deleted BOOLEAN NOT NULL DEFAULT FALSE;

        CREATE INDEX IF NOT EXISTS idx_vault_items_is_deleted ON vault_items (is_deleted);
    END IF;
END
$$;
