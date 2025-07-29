<div align="center">
  <p>
    <a href="README.md">English Version</a>
  </p>
</div>

<div align="center">

  <!-- Proje Logosu -->
  <!-- <img src="#" alt="SecureVault Logo" width="150"/> -->
  
  <h1 style="border-bottom: none;">SecureVault</h1>
  
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

### 🏛️ Temel Mimari Özellikleri

Mimari, güvenlik ve ölçeklenebilirliği sağlamak amacıyla birkaç temel teknik prensip üzerine inşa edilmiştir.

| Özellik | Uygulama Detayları |
| :--- | :--- |
| 🛡️ **Sıfır Bilgi Kimlik Doğrulama** | Kimlik doğrulama, **ECDSA imzaları** kullanılarak bir "challenge-response" mekanizması ile gerçekleştirilir. Kullanıcının ana parolası istemci cihazını asla terk etmez. |
| ✍️ **Uçtan Uca Şifreleme (E2EE)** | Tüm kasa verileri, sunucuya gönderilmeden önce istemci tarafında **AES-256 (GCM)** ile şifrelenir. Bu, sunucu ele geçirilse bile verilerin gizli kalmasını sağlar. |
| 🌐 **Platformlar Arası İstemci** | **.NET MAUI Blazor** kullanılarak geliştirilen tek bir kod tabanı, hem mobil (Android) hem de masaüstü (Windows) platformlarını hedefleyerek tutarlı bir arayüz ve yerel performans sunar. |
| 🧩 **Mikroservis Tabanlı Backend** | Arka uç, birbirinden bağımsız servislerden (Identity, Vault) oluşur. Servisler arası iletişim **Ocelot API Gateway** ve servis keşfi için **Consul** ile yönetilir, bu da esnekliği ve ölçeklenebilirliği artırır. |

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
| | **Entity Framework Core** | ORM; veritabanı ile nesneye yönelik, güvenli etkileşim. |
| | **Redis** | Yüksek performanslı cache servisi; "challenge" ve geçici oturum verileri için. |
| **DevOps** | **Docker, Docker Compose** | Konteynerleştirme; tutarlı geliştirme ve dağıtım ortamları. |
| | **Consul** | Service Discovery; mikroservislerin dinamik bir ortamda birbirini bulması. |
| **Güvenlik** | **BouncyCastle** | Gelişmiş kriptografi işlemleri (özellikle ECDSA imzalama). |
| | **Argon2id** | Güçlü parola hashleme; kaba kuvvet saldırılarına dayanıklı Master Key türetme. |
| | **AES-256 (GCM)** | Uçtan uca veri şifrelemesi için doğrulanmış, modern simetrik şifreleme. |
| | **JWT (JSON Web Token)** | Güvenli ve stateless oturum yönetimi (Access & Refresh Token). |

---

### 🔐 Güvenlik Mimarisi Detayları

Güvenlik modeli, yalnızca kullanıcının kendi verilerine erişebilmesini sağlamak üzere tasarlanmıştır.

#### 1. Kayıt ve Anahtar Üretim Akışı
1.  İstemci tarafında benzersiz bir `salt` üretilir.
2.  Kullanıcının ana parolası ve `salt`, **Argon2id** kullanılarak 32 byte'lık bir `Master Key`'e dönüştürülür.
3.  `Master Key`, **HKDF** ile iki ayrı anahtar türetmek için kullanılır:
    * `Authentication Key (Private Key)`: Kimlik doğrulama "challenge"larını imzalamak için.
    * `Encryption Key`: Kasa verilerini şifrelemek ve çözmek için.
4.  `Authentication Key` kullanılarak **ECDSA (secp256k1)** ile bir `Public Key` (açık anahtar) hesaplanır.
5.  Sunucuya sadece `Public Key` ve `salt` gönderilir. Ana parola ve türetilen diğer anahtarlar istemci cihazından asla ayrılmaz.

#### 2. Kimlik Doğrulama Akışı
1.  İstemci, sunucudan rastgele bir `challenge` metni talep eder.
2.  İstemci tarafında, saklanan parola ve sunucudan alınan `salt` kullanılarak `Master Key` ve `Authentication Key` anlık olarak yeniden oluşturulur.
3.  `challenge`, `Authentication Key` ile imzalanır.
4.  Oluşturulan imza sunucuya gönderilir. Sunucu, kullanıcının saklanan `Public Key`'i ile bu imzayı doğrular. Geçerli bir imza, erişim izni verir.

---

### 🚀 Proje Yol Haritası

| Özellik | Açıklama | Durum |
| :--- | :--- | :--- |
| Sıfır Bilgi Kimlik Doğrulama | Parolasız, kanıta dayalı giriş sistemi. | ✅ **Tamamlandı** |
| Uçtan Uca Şifreleme | Kasa verilerinin cihazda şifrelenmesi. | ✅ **Tamamlandı** |
| 2FA Kasa (TOTP/HOTP) | QR kod ile ve manuel olarak 2FA kodları ekleme/listeleme. | ✅ **Tamamlandı** |
| Gelişmiş Oturum Yönetimi | Aktif oturumları listeleme ve uzaktan sonlandırma. | ✅ **Tamamlandı** |
| Kasa Verilerini Düzenleme | Mevcut parola ve 2FA kayıtlarını güncelleme. | 🚧 **Üzerinde Çalışılıyor** |
| QR Kod ile Oturum Açma | Web veya diğer cihazlarda hızlı ve güvenli oturum açma. | 📋 **Planlandı** |
| Kurtarma Anahtarı | Parola unutulması durumunda veri kurtarma için güvenli bir mekanizma. | 📋 **Planlandı** |
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
    Projenin ana dizininde `.env` adında bir dosya oluşturun. Bu dosya, `docker-compose.yml` tarafından kullanılacak olan hassas bilgileri içerir.

    **.env dosyası içeriği örneği:**
    ```env
    # PostgreSQL Ayarları(Identity servisi için)
    DB_USER=postgre_user
    DB_PASSWORD=postgre_secret_password
    DB_NAME=securevault_identity_dev
    DB_HOST=postgres-db

    # MongoDB Ayarları (Vault servisi için)
    MONGO_INITDB_ROOT_USERNAME=mongo_user
    MONGO_INITDB_ROOT_PASSWORD=mongo_secret_password
    MONGO_DB_NAME=securevault_vault_dev
    MONGO_HOST=mongo-db


    # JWT Ayarları (En az 32 rastgele karakter olmalı)
    JWT_KEY=YourSuperSecretKeyForJwtTokens32CharactersLong
    JWT_REFRESH_KEY=AnotherSuperSecretKeyForRefreshTokens32CharsLong

    # Consul Ayarları
    CONSUL_HOST=consul

    # Docker Network Ayarları
    FORWARDED_HEADERS_KNOWN_NETWORKS=172.22.0.0/16
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
