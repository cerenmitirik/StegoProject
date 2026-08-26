# 🖼️ Full-Stack Steganography Studio (LSB + AES-256)

Uçtan uca güvenli veri gizleme ve çıkarma sağlayan tam kapsamlı bir steganografi sistemidir. Sistem, gizlenecek metni **AES-256-CBC** ile şifreleyip **En Önemsiz Bit (LSB - Least Significant Bit)** manipülasyonu yöntemiyle piksellere gömer ve görüntü kalitesini **PSNR (Peak Signal-to-Noise Ratio)** metriğiyle doğrular.

---

## 🚀 Mimari ve Teknolojiler

- **Backend:** C# .NET 8 Web API
- **Görüntü İşleme & Kriptografi:** OpenCVSharp4, System.Security.Cryptography (AES-256, PBKDF2 with SHA-256)
- **Veritabanı & ORM:** SQL Server, Entity Framework Core 8
- **Mobil İstemci:** React Native (Expo SDK 54), JavaScript
- **Dosya / Medya Yönetimi:** Android Storage Access Framework (SAF), Expo FileSystem, Expo Sharing

---

## ⚙️ Matematiksel ve Kriptografik Yaklaşım

1. **AES-256-CBC & PBKDF2:** 
   - Kullanıcı parolası doğrudan anahtar olarak kullanılmaz; 16 baytlık rastgele `Salt` ve $100.000$ PBKDF2 iterasyonu ile 256-bit anahtar türetilir.
   - Her şifreleme için 16 baytlık dinamik `IV` (Initialization Vector) üretilir.
2. **LSB Gömme:** 
   - BGR piksellerinin son bitleri sıfırlanıp (`pixel & 0xFE`), şifreli verinin bitleri enjekte edilir (`| bit`).
3. **PSNR Analizi:** 
   - Orijinal ve stego görsel arasındaki fark Mean Squared Error ($MSE$) üzerinden logaritmik ölçekte hesaplanır:
   $$PSNR = 10 \cdot \log_{10}\left(\frac{255^2}{MSE}\right)$$
   - Sistem standart olarak **90+ dB** kalite skoru üreterek insan gözüyle fark edilemeyen kayıpsız gizleme sağlar.

---

## 📂 Proje Yapısı

```text
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

🛠️ Kurulum ve Çalıştırma
1. Backend (API) Kurulumu
Bash
cd StegoApi
# appsettings.json dosyasında ConnectionStrings alanını düzenleyin
dotnet restore
dotnet run
API varsayılan olarak http://0.0.0.0:5046 üzerinden ayağa kalkar. Swagger arayüzüne http://localhost:5046/swagger adresinden erişebilirsiniz.

2. Mobil Uygulama Kurulumu
Bash
cd StegoMobile
npm install

# App.js dosyasındaki API_BASE_URL sabitini bilgisayarınızın yerel IP'siyle güncelleyin:
# const API_BASE_URL = '[http://192.168.1.](http://192.168.1.)X:5046';

npx expo start
Expo Go uygulaması ile QR kodu okutarak test edebilirsiniz.

🔒 Hata Yönetimi
Yanlış parola girilmesi veya görselin harici platformlarca (örn. JPEG sıkıştırması) bozulması durumunda backend CryptographicException durumunu yakalayarak 400 Bad Request yanıtı üretir; mobil arayüz kullanıcıyı bilgilendirir.
