DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'user_sessions' AND column_name = 'token_identifier'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'user_sessions' AND column_name = 'refresh_token_jti'
    ) THEN
        ALTER TABLE public.user_sessions
        RENAME COLUMN token_identifier TO refresh_token_jti;
        RAISE NOTICE 'Kolon yeniden adlandırıldı: "token_identifier" -> "refresh_token_jti"';
    ELSE
        RAISE NOTICE 'Yeniden adlandırma işlemi atlandı: "token_identifier" bulunamadı veya "refresh_token_jti" zaten mevcut.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'user_sessions' AND column_name = 'access_token_jti'
    ) THEN
        ALTER TABLE public.user_sessions
        ADD COLUMN access_token_jti TEXT NULL;
        RAISE NOTICE 'Kolon eklendi: "access_token_jti" (TEXT, NULL)';
    ELSE
        RAISE NOTICE 'Kolon ekleme işlemi atlandı: "access_token_jti" zaten mevcut.';
    END IF;

END
$$;
