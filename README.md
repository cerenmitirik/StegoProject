Full-Stack Steganography Studio (LSB + AES-256)
Uçtan uca güvenli veri gizleme ve çıkarma sağlayan tam kapsamlı bir steganografi sistemidir. Sistem, gizlenecek metni AES-256-CBC ile şifreleyip En Önemsiz Bit (LSB - Least Significant Bit) manipülasyonu yöntemiyle piksellere gömer ve görüntü kalitesini PSNR (Peak Signal-to-Noise Ratio) metriğiyle doğrular.
Mimari ve Teknolojiler
Backend: C# .NET 8 Web API

Görüntü İşleme & Kriptografi: OpenCVSharp4, System.Security.Cryptography (AES-256, PBKDF2 with SHA-256)

Veritabanı & ORM: SQL Server, Entity Framework Core 8

Mobil İstemci: React Native (Expo SDK 54), JavaScript

Dosya / Medya Yönetimi: Android Storage Access Framework (SAF), Expo FileSystem, Expo Sharing

⚙️ Matematiksel ve Kriptografik YaklaşımAES-256-CBC & PBKDF2:Kullanıcı parolası doğrudan anahtar olarak kullanılmaz; 16 baytlık rastgele Salt ve $100.000$ PBKDF2 iterasyonu ile 256-bit anahtar türetilir.Her şifreleme için 16 baytlık dinamik IV (Initialization Vector) üretilir.LSB Gömme:BGR piksellerinin son bitleri sıfırlanıp (pixel & 0xFE), şifreli verinin bitleri enjekte edilir (| bit).PSNR Analizi:Orijinal ve stego görsel arasındaki fark Mean Squared Error ($MSE$) üzerinden logaritmik ölçekte hesaplanır:$$PSNR = 10 \cdot \log_{10}\left(\frac{255^2}{MSE}\right)$$Sistem standart olarak 90+ dB kalite skoru üreterek insan gözüyle fark edilemeyen kayıpsız gizleme sağlar.

Proje Yapısı
Plaintext
StegoProject/
├── StegoApi/               # ASP.NET Core 8 Web API
│   ├── Controllers/        # Encode & Decode REST Endpoint'leri
│   ├── Data/               # AppDbContext ve SQL Server konfigürasyonu
│   ├── DTOs/               # Model validasyon sınıfları
│   ├── Entities/           # StegoRecord veritabanı tablosu
│   ├── Services/           # StegoEngine (Matematik/Kripto) ve StegoService
│   └── wwwroot/uploads/    # Üretilen şifreli PNG dosyaları
└── StegoMobile/            # React Native Expo Mobil Uygulaması
    ├── App.js              # Mobil UI, SAF indirme ve Encode/Decode akışı
    └── package.json        # Bağımlılıklar

    Hata Yönetimi
Yanlış parola girilmesi veya görselin harici platformlarca (örn. JPEG sıkıştırması) bozulması durumunda backend CryptographicException durumunu yakalayarak 400 Bad Request yanıtı üretir; mobil arayüz kullanıcıyı bilgilendirir.
