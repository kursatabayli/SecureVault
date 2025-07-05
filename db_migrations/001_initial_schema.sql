CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(100) UNIQUE NOT NULL,
    public_key TEXT NOT NULL,
    user_info JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vault_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    -- Bu kaydın türünü ayırt etmemizi sağlayan bir "ayırıcı" (discriminator) kolon.
    -- 1: Giriş Bilgisi (Password)
    -- 2: 2FA Sırrı (Two-Factor Secret)
    -- 3: Güvenli Not (ileride eklemek isterseniz)
    -- 4: Kredi Kartı (ileride eklemek isterseniz)
    item_type SMALLINT NOT NULL,

    -- Şifrelenmiş veri paketi. İçeriği item_type'a göre değişir.
    encrypted_data BYTEA NOT NULL,

    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);


CREATE TABLE user_sessions (
    -- Oturumun benzersiz kimliği
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Hangi kullanıcıya ait olduğu
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    -- Bu oturuma ait Refresh Token'ın GÜVENLİ HASH'i.
    -- Asla token'ın kendisini saklamayız!
    refresh_token_hash TEXT NOT NULL,
	
	refresh_token_salt TEXT NOT NULL,
	
    -- Oturumun hangi IP adresinden başlatıldığı
    ip_address VARCHAR(45),

    -- Oturumun hangi cihazdan/uygulamadan başlatıldığı bilgisi
    -- Örn: "SecureVault v1.2 on Android 14"
    user_agent TEXT,

    -- Oturumun ne zaman başlatıldığı
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Refresh Token'ın ne zaman sona ereceği
    expires_at TIMESTAMPTZ NOT NULL,

    -- Kullanıcının bu oturumu sonlandırıp sonlandırmadığı bilgisi
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,

    -- Oturumun son kullanılma tarihi (isteğe bağlı ama faydalı)
    last_used_at TIMESTAMPTZ
);
