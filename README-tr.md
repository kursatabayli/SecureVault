<div align="center">
  <p>
    <a href="README.md">English Version</a>
  </p>
</div>

<div align="center">
  
  <h1 style="border-bottom: none;">Secure Vault</h1>
  
  **.NET 9, MAUI ve mikroservis mimarisi ile geliştirilmiş, güvenlik ve gizlilik odaklı, modern ve platformlar arası parola yöneticisi.**

</div>

<p align="center">
<a href="https://github.com/kursatabayli/SecureVault/blob/master/LICENSE"><img src="https://img.shields.io/github/license/kursatabayli/SecureVault?style=for-the-badge&color=blue" alt="Lisans"></a>
  <img src="https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 9">
  <img src="https://img.shields.io/badge/MAUI-Multi--Platform-5C2D91?style=for-the-badge&logo=c-sharp" alt="MAUI">
  <img src="https://img.shields.io/badge/Architecture-Microservices-orange?style=for-the-badge" alt="Mikroservis Mimarisi">
</p>

> **⚠️ Proje Durumu: Geliştirme Aşamasında**
>
> Bu proje aktif olarak geliştirilme aşamasındadır ve öncelikli amacı teknik konseptleri sergilemektir. Yazılım, herhangi bir garanti olmaksızın "olduğu gibi" sunulmaktadır. Henüz **production (canlı) kullanım için kararlı kabul edilmemektedir**. Lütfen bu aşamada gerçek ve hassas kimlik bilgilerinizi saklamak için kullanmayınız.

Bu proje, güvenlik ve gizlilik odaklı modern bir parola ve 2FA kod yöneticisi uygulamasıdır. Proje, **Sıfır Bilgi (Zero-Knowledge)** mimarisi temel alınarak tasarlanmıştır. Bu, sunucunun kullanıcının ana parolasından veya kasada saklanan verilerden hiçbir şekilde haberdar olmaması anlamına gelir. Tüm şifreleme ve şifre çözme işlemleri istemci tarafında gerçekleştirilir.

---

### ✨ Temel Özellikler

* **Sıfır Bilgi Mimarisi:** Sunucular, ana parolanızı veya kasa verilerinizi asla göremez.

* **Uçtan Uca Şifreleme (E2EE):** Tüm veriler, cihazdan ayrılmadan önce AES-256 (GCM) ile şifrelenir.

* **Güçlü Kriptografi:** Parola türetme için Argon2id, kimlik doğrulama için ECDSA ve güvenli anahtar değişimi için ECDH kullanılır.

* **Platformlar Arası Destek:** .NET MAUI Blazor ile Windows ve Android için tek bir kod tabanı.

* **Gerçek Zamanlı Senkronizasyon:** Bir cihazda yapılan değişiklikler, anında diğer cihazlarınıza yansıtılır (SignalR ile).

* **Güvenli QR Kod ile Oturum Açma:** Parolanızı girmeden, mobil cihazınızla QR kod okutarak güvenli ve şifreli bir şekilde oturum açın.

* **Çevrimdışı Erişim:** Kasa verileriniz SQLite kullanılarak yerel olarak saklanır, böylece internet bağlantınız olmasa bile erişebilirsiniz.

* **Açık Kaynak:** Tamamen şeffaf ve topluluk tarafından denetlenebilir kod.

---

### 🏛️ Mimari ve Güvenlik Modeli

Mimari, güvenlik, ölçeklenebilirlik ve esneklik sağlamak amacıyla modern tasarım desenleri ve teknolojiler üzerine kurulmuştur.

| Özellik | Uygulama Detayları |
| :--- | :--- |
| 🛡️ **Sıfır Bilgi Kimlik Doğrulama** | Kimlik doğrulama, **ECDSA imzaları** kullanılarak bir "challenge-response" mekanizması ile gerçekleştirilir. Kullanıcının ana parolası istemci cihazını asla terk etmez. |
| ✍️ **Uçtan Uca Şifreleme (E2EE)** | Tüm kasa verileri, sunucuya gönderilmeden önce istemci tarafında **AES-256 (GCM)** ile şifrelenir. Bu, sunucu ele geçirilse bile verilerin gizli kalmasını sağlar. |
| 🔑 **QR Kod ile Güvenli Oturum Açma** | Cihazlar arasında **ECDH (Elliptic Curve Diffie-Hellman)** anahtar değişimi kullanılarak geçici ve güvenli bir şifreleme kanalı oluşturulur. Oturum bilgileri bu kanal üzerinden E2EE ile iletilir. |
| 🌐 **Platformlar Arası İstemci** | **.NET MAUI Blazor** kullanılarak geliştirilen tek bir kod tabanı, hem mobil (Android) hem de masaüstü (Windows) platformlarını hedefleyerek tutarlı bir arayüz ve yerel performans sunar. |
| 🧩 **Mikroservis Tabanlı Backend** | Arka uç, birbirinden bağımsız servislerden (Identity, Vault, Interaction) oluşur. Servisler arası iletişim **Ocelot API Gateway**, servis keşfi için **Consul** ve asenkron mesajlaşma için **RabbitMQ** ile yönetilir. |
| 🔄 **Gerçek Zamanlı Veri Akışı** | **SignalR**, istemciler ve sunucu arasında çift yönlü, kalıcı bir bağlantı kurarak veri değişikliklerinin anında tüm cihazlara iletilmesini sağlar. |
| 💾 **Hibrit Veri Depolama** | İstemci tarafında veriler, çevrimdışı erişim için bir **SQLite** veritabanında saklanır. Sunucu tarafında ise servis ihtiyacına göre **PostgreSQL** ve **MongoDB** kullanılır. |

---

### 🔐 Mimarinin Derinlemesine İncelenmesi

## Proje Yapısı
Proje, her biri belirli bir işlevden sorumlu olan bağımsız bileşenlerden oluşur:

* **MAUI Blazor Client:** Kullanıcının etkileşimde bulunduğu, Windows ve Android üzerinde çalışan platformlar arası uygulama. Tüm kriptografik işlemler burada gerçekleşir.
* **Ocelot API Gateway:** Dış dünyadan gelen istekleri karşılayan, kimlik doğrulama ve yetkilendirme kontrolleri yapan ve istekleri ilgili mikroservise yönlendiren merkezi bir ağ geçididir.
* **Identity Service:** Kullanıcı kaydı, kimlik doğrulama, oturum yönetimi ve genel kullanıcı verilerinden sorumludur. Veritabanı olarak PostgreSQL kullanır.
* **Vault Service:** Kullanıcının şifrelenmiş kasa verilerini (parolalar, 2FA kodları vb.) depolamaktan sorumludur. Veritabanı olarak MongoDB kullanır.
* **Interaction Service:** Cihazlar arası gerçek zamanlı iletişimi (SignalR ile) ve asenkron olayları (RabbitMQ üzerinden) yönetir. QR kod ile giriş ve anlık veri senkronizasyonu bu servis üzerinden sağlanır.

## Kriptografik Akışlar
Güvenlik modeli, yalnızca kullanıcının kendi verilerine erişebilmesini sağlamak üzere tasarlanmıştır.

1. **Kayıt ve Anahtar Üretim Akışı**
     * İstemci tarafında benzersiz bir `salt` üretilir.
     * Kullanıcının ana parolası ve `salt`, Argon2id kullanılarak 32 byte'lık bir `Master Key`'e dönüştürülür.
     * `Master Key`, **HKDF** ile iki ayrı anahtar türetmek için kullanılır:
        * `Private Key`: Kimlik doğrulama "challenge"larını imzalamak için.
        * `Encryption Key`: Kasa verilerini şifrelemek ve çözmek için.
     * `Private Key` kullanılarak **ECDSA (secp256k1)** ile bir `Public Key` (açık anahtar) hesaplanır.
     * Sunucuya sadece `Public Key` ve `salt` gönderilir. Ana parola ve türetilen diğer anahtarlar istemci cihazından asla ayrılmaz.

2. **Parola Tabanlı Kimlik Doğrulama Akışı**
   * İstemci, sunucudan rastgele bir `challenge` metni talep eder.
   * İstemci tarafında, saklanan parola ve sunucudan alınan `salt` kullanılarak `Master Key` ve `Private Key` anlık olarak yeniden oluşturulur.
   * `challenge`, `Private Key` ile imzalanır.
   * Oluşturulan imza sunucuya gönderilir. Sunucu, kullanıcının saklanan `Public Key`'i ile bu imzayı doğrular. Geçerli bir imza, erişim izni verir.

3. **QR Kod ile Oturum Açma Akışı (ECDH ile)**
   * Oturum açmak isteyen yeni cihaz (Requester) veya zaten oturumu açık olan yetkili cihaz (Provider), Interaction servisi üzerinden bir kanal oluşturur ve bu kanala ait ID'yi QR kod olarak gösterir.
   * Provider veya Requester, bu QR kodu okur.
   * Her iki cihaz da **ECDH (Elliptic Curve Diffie-Hellman)** kullanarak geçici birer anahtar çifti oluşturur ve açık anahtarlarını SignalR üzerinden birbirlerine gönderir.
   * Her iki taraf da kendi özel anahtarını ve karşı tarafın açık anahtarını kullanarak ortak bir `shared secret` (paylaşılan sır) hesaplar. Bu sır, ağ üzerinden asla iletilmez.
   * Provider cihaz, kullanıcının `Encryption Key` ve `Private Key`'ini bu `shared secret` ile şifreleyerek Requester cihaza gönderir.
   * Requester, şifreli paketi kendi `shared secret`'ı ile çözerek oturum açmak için gerekli anahtarlara sahip olur ve standart kimlik doğrulama akışını tamamlar.

4. **Gerçek Zamanlı Senkronizasyon Akışı**
   * Bir kullanıcı cihazında kasa verisi eklediğinde, güncellediğinde veya sildiğinde, bu değişiklik hem yerel SQLite veritabanına kaydedilir hem de şifrelenerek Vault servisine gönderilir.
   * Vault servisi, veriyi kaydettikten sonra **RabbitMQ**'ya "UserActivityOccurred" gibi bir olay (event) yayınlar.
   * **Interaction servisi**, bu olayı RabbitMQ üzerinden dinler.
   * Olayı yakaladığında, ilgili kullanıcının diğer tüm aktif cihazlarına **SignalR** üzerinden "SyncRequired" (Senkronizasyon Gerekli) bildirimi gönderir.
   * Bu bildirimi alan diğer istemciler, sunucudan en son değişiklikleri çekerek yerel veritabanlarını günceller.

---

### ⚙️ Teknoloji Yığını

Bu proje, hedeflerine ulaşmak için modern ve sağlam bir teknoloji yığını kullanmaktadır.

| Kategori | Teknoloji / Kütüphane | Amaç |
| :--- | :--- | :--- |
| **Backend** | **.NET 9, ASP.NET Core** | Yüksek performanslı, modern API'ler oluşturmak için kullanılan ana çatı. |
| | **MediatR** | CQRS desenini uygulamak ve iş mantığını komut/sorgulara ayırmak. |
| | **Ocelot** | API Gateway; istek yönlendirme, rate limiting ve merkezi yönetim. |
| **Frontend**| **.NET MAUI Blazor** | Platformlar arası (Windows, Android) istemci uygulaması çatısı. |
| | **MudBlazor** | Temiz ve duyarlı bir arayüz oluşturmak için zengin bileşen kütüphanesi. |
| **Veri & Cache**| **PostgreSQL** | İlişkisel ve JSONB verileri için ana veritabanı. |
| | **MongoDB** | Vault servisi için NoSQL belge veritabanı. |
| | **SQLite** | İstemci tarafında yerel veritabanı. |
| | **Entity Framework Core** | ORM; veritabanı ile nesneye yönelik, güvenli etkileşim. |
| | **Redis** | Yüksek performanslı cache servisi; "challenge" ve geçici oturum verileri için. |
| **Mesajlaşma & Real-time** | **RabbitMQ** | Mikroservisler arası asenkron, olay tabanlı iletişim. |
| | **SignalR** | Cihazlar arası gerçek zamanlı veri senkronizasyonu ve mesajlaşma. |
| **API İletişimi** | **Refit** | .NET için tip güvenli REST istemcisi, API çağrılarını basitleştirir. |
| **Logging & Monitoring** | **Serilog** | Esnek ve yapılandırılabilir, yapısal loglama kütüphanesi. |
| | **Seq** | SSerilog ile entegre çalışan, merkezi log toplama ve analiz sunucusu. |
| **DevOps** | **Docker, Docker Compose** | Konteynerleştirme; tutarlı geliştirme ve dağıtım ortamları. |
| | **Consul** | Service Discovery; mikroservislerin dinamik bir ortamda birbirini bulması. |
| **Güvenlik** | **BouncyCastle** | Gelişmiş kriptografi işlemleri (özellikle ECDSA imzalama). |
| | **Argon2id** | Güçlü parola hashleme; kaba kuvvet saldırılarına dayanıklı Master Key türetme. |
| | **AES-256 (GCM)** | Uçtan uca veri şifrelemesi için doğrulanmış, modern simetrik şifreleme. |
| | **JWT (JSON Web Token)** | Güvenli ve stateless oturum yönetimi (Access & Refresh Token). |
| **QR Kod İşlemleri** | **ZXing.Net.MAUI** | İstemci tarafında QR kod okuma ve tarama. |
| | **QRCoder & SkiaSharp** | QR kod oluşturma ve render etme. |

---

### 🚀 Proje Yol Haritası

| Özellik | Açıklama | Durum |
| :--- | :--- | :--- |
| Sıfır Bilgi Kimlik Doğrulama | Sunucuya parola göndermeden, kanıta dayalı giriş sistemi. | ✅ **Tamamlandı** |
| Uçtan Uca Şifreleme | Kasa verilerinin sunucuya varmadan cihazda şifrelenmesi. | ✅ **Tamamlandı** |
| 2FA Kasa (TOTP/HOTP) | QR kod tarayarak, resim yükleyerek veya el ile manuel olarak 2FA kodları ekleme, kodları listeleme. | ✅ **Tamamlandı** |
| QR Kod ile Güvenli Oturum Açma | ECDH kullanılarak E2EE kanal üzerinden güvenli oturum açma. | ✅ **Tamamlandı** |
| Gerçek Zamanlı Senkronizasyon | Cihazlar arası anlık veri senkronizasyonu (SignalR ile). | ✅ **Tamamlandı** |
| Çevrimdışı Mod (Lokal DB) | Verilere çevrimdışı erişim için istemci tarafında SQLite kullanımı. | ✅ **Tamamlandı** |
| Gelişmiş Oturum Yönetimi | Aktif oturumları listeleme ve uzaktan sonlandırma. | ✅ **Tamamlandı** |
| Kurtarma Anahtarı | Parola unutulması durumunda veri kurtarma için güvenli bir mekanizma. | 🚧 **Üzerinde Çalışılıyor** |
| Kasa Verilerini Düzenleme | Mevcut parola ve 2FA kayıtlarını güncelleme. | 🚧 **Üzerinde Çalışılıyor** |
| Otomatik Doldurma (Autofill) | Mobil ve tarayıcılarda parola ve kodların otomatik doldurulması. | 💡 **Değerlendiriliyor** |

---

### 🛠️ Yerel Kurulum ve Başlatma

Projeyi yerel makinenizde çalıştırmak için aşağıdaki adımları izleyin.

#### Ön Gereksinimler
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker ve Docker Compose](https://www.docker.com/products/docker-desktop/)
* [Git](https://git-scm.com/)

#### Kurulum Adımları
1.  **Projeyi Klonlayın:**
    ```bash
    git clone https://github.com/kursatabayli/SecureVault.git
    cd securevault
    ```

2.  **Ortam Değişkenlerini Ayarlayın:**
   Projenin ana dizininde bulunan .env.example dosyasını kopyalayarak `.env` adında yeni bir dosya oluşturun. Bu dosya, `docker-compose.yml` tarafından kullanılacak olan veritabanı bağlantı bilgileri ve JWT anahtarları gibi hassas bilgileri içerir.
`.env` dosyasını kendi ayarlarınıza göre düzenleyin.

    ```bash
    cp .env.example .env
    # .env dosyasını bir metin düzenleyici ile açıp düzenleyin
    ```
    **.env dosyası içeriği örneği:**
    ```env
    # PostgreSQL Ayarları (Identity servisi için)
    DB_USER=postgres
    DB_PASSWORD=postgre_super_secret_password
    DB_NAME=securevault_identity_dev
    DB_HOST=postgres-db
    
    # MongoDB Ayarları (Vault servisi için)
    MONGO_INITDB_ROOT_USERNAME=mongoadmin
    MONGO_INITDB_ROOT_PASSWORD=mongo_super_secret_password
    MONGO_DB_NAME=securevault_vault_dev
    MONGO_HOST=mongo-db
    
    # RabbitMQ Ayarları
    RABBITMQ_USER=guest
    RABBITMQ_PASS=guest
    
    # JWT Ayarları (En az 32 rastgele karakter olmalı)
    JWT_KEY=ChangeThisToARandomlyGenerated64ByteBase64Key=
    JWT_REFRESH_KEY=AlsoChangeThisToAnotherRandomlyGenerated64ByteBase64Key=
    
    # Seq Ayarları (Merkenzi Log Sunucusu)
    SEQ_PASSWORD=your_secret_seq_api_key_or_password
    ```

3.  **Docker ile Başlatın:**
    Projenin ana dizininde aşağıdaki komutu çalıştırın.
    ```bash
    docker-compose up -d --build
    ```

4.  **Servislerin Durumunu Kontrol Edin:**
    Tüm servislerin `Up` veya `healthy` durumunda olduğundan emin olun.
    ```bash
    docker-compose ps
    ```

5.  **Uygulamayı Çalıştırın:**
    * **API Gateway:** `https://localhost:7202` adresinden erişilebilir.
    * **MAUI Client:** `src/clients/SecureVault.App` projesini Visual Studio'da açıp istediğiniz platform (Windows veya Android) için çalıştırın.

---

### 📄 Lisans

Bu proje, [MIT Lisansı](https://github.com/kursatabayli/SecureVault/blob/master/LICENSE) altında lisanslanmıştır.
