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
| 🛡️ **Sender-Constrained Token'lar** | Standart Bearer Token'lar yerine, token hırsızlığını ve yeniden oynatma saldırılarını önleyen **DPoP (Demonstration of Proof-of-Possession)** kullanılır. Her token, kriptografik olarak onu oluşturan istemciye bağlanır. |
| ✍️ **Uçtan Uca Şifreleme (E2EE)** | Tüm kasa verileri, sunucuya gönderilmeden önce istemci tarafında **AES-256 (GCM)** ile şifrelenir. Bu, sunucu ele geçirilse bile verilerin gizli kalmasını sağlar. |
| 🔑 **QR Kod ile Güvenli Oturum Açma** | Cihazlar arasında **ECDH (Elliptic Curve Diffie-Hellman)** anahtar değişimi kullanılarak geçici ve güvenli bir şifreleme kanalı oluşturulur. Oturum bilgileri bu kanal üzerinden E2EE ile iletilir. |
| 🌐 **Platformlar Arası İstemci** | **.NET MAUI Blazor** kullanılarak geliştirilen tek bir kod tabanı, hem mobil (Android) hem de masaüstü (Windows) platformlarını hedefleyerek tutarlı bir arayüz ve yerel performans sunar. |
| 🧩 **Mikroservis Tabanlı Backend** | Arka uç, birbirinden bağımsız servislerden (Identity, Vault, Interaction) oluşur. Geliştirme ortamında servis orkestrasyonu, servis keşfi ve telemetri **.NET Aspire** ile sağlanır. API Gateway işlevselliği ise **YARP (Yet Another Reverse Proxy)** kullanılarak yönetilir. |
| 🔄 **Gerçek Zamanlı Veri Akışı** | **SignalR**, istemciler ve sunucu arasında çift yönlü, kalıcı bir bağlantı kurarak veri değişikliklerinin anında tüm cihazlara iletilmesini sağlar. |
| 💾 **Hibrit Veri Depolama** | İstemci tarafında veriler, çevrimdışı erişim için **şifreli bir Realm veritabanında** saklanır. Sunucu tarafında ise servis ihtiyacına göre **PostgreSQL** ve **MongoDB** kullanılır. |

---

### 🔐 Mimarinin Derinlemesine İncelenmesi

## Proje Yapısı
Proje, her biri belirli bir işlevden sorumlu olan bağımsız bileşenlerden oluşur:

* **MAUI Blazor Client:** Kullanıcının etkileşimde bulunduğu, Windows ve Android üzerinde çalışan platformlar arası uygulama. Tüm kriptografik işlemler burada gerçekleşir.
* **YARP API Gateway:** Dış dünyadan gelen istekleri karşılayan, kimlik doğrulama ve yetkilendirme kontrolleri yapan ve istekleri ilgili mikroservise yönlendiren merkezi bir ağ geçididir.
* **Identity Service:** Kullanıcı kaydı, kimlik doğrulama, oturum yönetimi ve genel kullanıcı verilerinden sorumludur. Veritabanı olarak PostgreSQL kullanır.
* **Vault Service:** Kullanıcının şifrelenmiş kasa verilerini (parolalar, 2FA kodları vb.) depolamaktan sorumludur. Veritabanı olarak MongoDB kullanır.
* **Interaction Service:** Cihazlar arası gerçek zamanlı iletişimi (SignalR ile) yönetir. QR kod ile giriş ve anlık veri senkronizasyonu bu servis üzerinden sağlanır.

## 🔐 Güvenlik Modeli ve Kriptografik Akışlar
Güvenlik modeli, yalnızca kullanıcının kendi verilerine erişebilmesini sağlamak ve oturum güvenliğini en üst düzeye çıkarmak üzere tasarlanmıştır.

### DPoP (Demonstration of Proof-of-Possession)
Proje, oturum güvenliğini sağlamak için standart Bearer Token'lar yerine **DPoP** (RFC 9449) standardını kullanır. Bu yaklaşım, token'ın çalınması ve başka bir istemcide yeniden kullanılması riskini etkili bir şekilde ortadan kaldırır.

* **Nasıl Çalışır:** İstemci, her API isteğinde, sahip olduğu JWT'ye (Access Token) ek olarak, o anki isteğe (HTTP metodu ve URL) ve token'a bağlı özel bir "kanıt" (proof) imzalar.
* **Avantajı:** API Gateway (YARP), bu kanıtı doğrulayarak token'ı *sadece* onu talep eden ve ilgili kriptografik anahtara sahip olan *orijinal* istemcinin kullanabildiğinden emin olur.

### Temel Kriptografik Akışlar

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

---

### ⚙️ Teknoloji Yığını

Bu proje, hedeflerine ulaşmak için modern ve sağlam bir teknoloji yığını kullanmaktadır.

| Kategori | Teknoloji / Kütüphane | Amaç |
| :--- | :--- | :--- |
| **Backend** | **.NET 9, ASP.NET Core** | Yüksek performanslı, modern API'ler oluşturmak için kullanılan ana çatı. |
| | **MediatR** | CQRS desenini uygulamak ve iş mantığını komut/sorgulara ayırmak. |
| | **YARP (Yet Another Reverse Proxy)** | API Gateway; istek yönlendirme, rate limiting ve merkezi yönetim. |
| **Frontend**| **.NET MAUI Blazor** | Platformlar arası (Windows, Android) istemci uygulaması çatısı. |
| | **MudBlazor** | Temiz ve duyarlı bir arayüz oluşturmak için zengin bileşen kütüphanesi. |
| **Veri & Cache**| **PostgreSQL** | İlişkisel ve JSONB verileri için ana veritabanı. |
| | **MongoDB** | Vault servisi için NoSQL belge veritabanı. |
| | **Realm** | İstemci tarafında şifreli, yerel ve reaktif veritabanı. |
| | **Entity Framework Core** | ORM; veritabanı ile nesneye yönelik, güvenli etkileşim. |
| | **Redis** | Yüksek performanslı cache servisi. Ayrıca **SignalR Backplane** olarak ölçeklenebilir, gerçek zamanlı iletişim sağlar. |
| **Mesajlaşma & Real-time** | **SignalR** | Cihazlar arası gerçek zamanlı veri senkronizasyonu ve mesajlaşma. |
| **API İletişimi** | **Refit** | .NET için tip güvenli REST istemcisi, API çağrılarını basitleştirir. |
| **Gözlemlenebilirlik** | **OpenTelemetry** | Servisler arası telemetri verilerini (log, trace, metric) toplamak ve iletmek için standart. |
| | **Serilog** | Esnek ve yapılandırılabilir, yapısal loglama kütüphanesi. |
| | **Seq** | Serilog ve OpenTelemetry ile entegre çalışan, merkezi log toplama ve analiz sunucusu. |
| | **Prometheus** | Servislerden metrikleri (ölçümleri) toplamak için zaman serisi veritabanı. |
| | **Grafana** | Prometheus ve diğer kaynaklardan gelen metrikleri görselleştirmek için dashboard arayüzü. |
| **DevOps & Orkestrasyon** | **.NET Aspire** | Geliştirme ortamı için servis orkestrasyonu, keşfi ve telemetri. |
| | **Docker** | Konteynerleştirme; servisleri izole bir ortamda çalıştırma. |
| **Güvenlik** | **DPoP (JWT ile)** | Token'ın istemciye "bağlandığı" (sender-constrained) gelişmiş oturum yönetimi (RFC 9449). Klasik Bearer Token'lar yerine kullanılır. |
| | **BouncyCastle** | Gelişmiş kriptografi işlemleri (özellikle ECDSA imzalama). |
| | **Argon2id** | Güçlü parola hashleme; kaba kuvvet saldırılarına dayanıklı Master Key türetme. |
| | **AES-256 (GCM)** | Uçtan uca veri şifrelemesi için doğrulanmış, modern simetrik şifreleme. |
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
* [Dev Tunnels CLI](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
* [.NET Aspire CLI](https://learn.microsoft.com/en-us/dotnet/aspire/cli/install) (Opsiyonel, Kubernetes manifestlerini oluşturmak için gereklidir)

#### Kurulum Adımları
1.  **Projeyi Klonlayın:**
    ```bash
    git clone https://github.com/kursatabayli/SecureVault.git
    cd securevault
    ```

2.  **AppHost Gizli Anahtarlarını (User Secrets) Ayarlayın:**
* Visual Studio'da `src/aspire/SecureVault.AppHost` projesine sağ tıklayın.
* **Manage User Secrets** (Kullanıcı Gizli Anahtarlarını Yönet) seçeneğini seçin.
* Açılan `secrets.json` dosyasına aşağıdaki içeriği kendiniz için değiştirerek ekleyin:

  ```json
    {
      "Parameters:vault-db-password": "your-secure-mongo-password-123",
      "Parameters:seqpassword": "your-seq-admin-password",
      "Parameters:redispassword": "your-main-redis-password",
      "Parameters:redis-cache-password": "your-cache-redis-password",
      "Parameters:postgresusername": "your-postgres-user",
      "Parameters:postgrespassword": "your-postgres-password",
      "Parameters:jwtrefreshkey": "your-super-secret-64-byte-refresh-key",
      "Parameters:jwtkey": "your-super-secret-64-byte-jwt-key"
    }
    ```

3.  **İstemci (MAUI) Ayarlarını Yapılandırın:**
* `src/client/SecureVault.App` dizinindeki `appsettings.json` dosyasını açın. Bu dosyayı bir sonraki adımlarda alacağınız **Dev Tunnels** URL'si ile güncelleyeceksiniz.
* Dosyanın içeriği aşağıdaki gibi olmalıdır:

    ```json
    {
      "ApiSettings": {
        "BaseUrl": "https://replace-this-with-your-api-gateway-devtunnel/"
      }
    }
    ```

4.  **Dev Tunnels'a Giriş Yapın ve Uygulamayı Başlatın:**
* Ön gereksinimlerde kurduğunuz Dev Tunnels CLI'a bir Microsoft veya GitHub hesabı ile giriş yapın:
    ```bash
    devtunnel user login
    ```
* Projenin `AppHost`'unu Visual Studio veya komut satırı ile başlatın:
    ```bash
    cd src/aspire/SecureVault.AppHost
    dotnet run
    ```

5.  **Dev Tunnel URL'sini Alın ve Ayarlayın:**
* .NET Aspire Dashboard'u otomatik olarak başlayacak ve varsayılan tarayıcınızda açılacaktır.
* Dashboard'da, `public-gateway`  servisinizi bulun. Dev Tunnels entegrasyonu sayesinde bu servis için **genel (public) bir URL** oluşturulduğunu göreceksiniz.
* Bu **`https://...` ile başlayan URL'yi** kopyalayın.
* 3. Adım'da açtığınız `src/client/SecureVault.App/appsettings.json` dosyasına geri dönün ve `BaseUrl` değerini bu kopyaladığınız URL ile değiştirin.

6.  **MAUI Client'ı Çalıştırın:** `src/client/SecureVault.App` projesini Visual Studio'da açıp istediğiniz platform (Windows veya Android) için çalıştırın. İstemci, 3. adımda yaptığınız ayarlar sayesinde Aspire tarafından yönetilen servislere otomatik olarak bağlanacaktır.

---

### 🚀 Kubernetes için Yayınlama (Deployment)

Bu proje, .NET Aspire'in dağıtım özelliklerini kullanarak bir Kubernetes cluster'ına kolayca yayınlanacak şekilde yapılandırılmıştır.

`SecureVault.AppHost` projesi içerisinde yer alan `builder.AddKubernetesEnvironment("k8s");` satırı, Aspire'e Kubernetes'i hedefleyen bir yapılandırma olduğunu bildirir ve buna uygun manifestler üretmesini sağlar.

Tüm mikroservisler, veritabanları ve bağımlılıklar için gerekli olan Kubernetes manifest dosyalarını (Deployment, Service, ConfigMap, Secret vb. YAML dosyaları) tek bir komutla oluşturabilirsiniz:

  ```bash
    # aspire komutunu çalıştırarak manifestleri oluşturun
    aspire publish -o ./kubernetes-manifests
  ```
Bu komut, projenizin ana dizininde kubernetes-manifests (veya belirttiğiniz başka bir çıktı klasörü) oluşturur. Bu klasörün içinde, tüm uygulamanızı kubectl apply -f . komutuyla Kubernetes cluster'ınıza dağıtmak için gereken tüm YAML dosyaları bulunur.

---

### 📄 Lisans

Bu proje, [MIT Lisansı](https://github.com/kursatabayli/SecureVault/blob/master/LICENSE) altında lisanslanmıştır.
