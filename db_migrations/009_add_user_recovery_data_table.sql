DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'user_recovery_dat'
    ) THEN
        CREATE TABLE public.user_recovery_data (
            user_id UUID PRIMARY KEY,
            recovery_data BYTEA NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            
            CONSTRAINT fk_user
                FOREIGN KEY(user_id) 
                REFERENCES public.users(id)
                ON DELETE CASCADE
        );
        RAISE NOTICE 'Tablo oluşturuldu: "user_recovery_data"';
    ELSE
        RAISE NOTICE 'Tablo oluşturma işlemi atlandı: "user_recovery_data" zaten mevcut.';
    END IF;
END
$$;