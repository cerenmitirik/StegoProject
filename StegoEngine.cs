using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using OpenCvSharp;

namespace StegoApi.Services
{
    public static class StegoEngine
    {
        private const int SALT_SIZE = 16;
        private const int IV_SIZE = 16;
        private const int PBKDF2_ITERATIONS = 100000;

        public static byte[] HideMessage(byte[] originalImageBytes, string secretMessage, string password)
        {
            using Mat image = Cv2.ImDecode(originalImageBytes, ImreadModes.Color); // imreadle okumuyor, bellekte var olan bir bayt dizisini kullanıyor. memorystraam ile okunan,
            if (image.Empty())
                throw new ArgumentException("Görsel çözümlenemedi veya geçersiz format.");

            byte[] encryptedPayload = EncryptAES(secretMessage, password);
// mesajı şifreleme fonksiyonuna gönderiyor., geri şifreli anlamsız byte dizisi kalıyor.

            byte[] lengthBytes = BitConverter.GetBytes(encryptedPayload.Length);
            byte[] fullPayload = new byte[4 + encryptedPayload.Length];
            Buffer.BlockCopy(lengthBytes, 0, fullPayload, 0, 4);
            Buffer.BlockCopy(encryptedPayload, 0, fullPayload, 4, encryptedPayload.Length);
//şifreli verinin ne kadar uzun(kaç bayt) , BitConverter.GetBytes — bir int sayıyı 4 baytlık bir diziye çeviren, sonra bu "uzunluk bilgisini" şifreli verinin başına ekliyoruz. 
// fotoğrafta 1 milyon bit varsa şifrelendikten sonra 5000 varsaçhepsini okur ya da gerekli yeri okuamaz.
// //bu kadar bayt okuyacaksın 32 bayt=32 bit göümüyotuzi sonra da asıl veriyi
          
            long totalPixels = (long)image.Width * image.Height;
            long totalAvailableBytes = (totalPixels * 3) / 8;

            if (fullPayload.Length > totalAvailableBytes)
                throw new InvalidOperationException($"Mesaj çok büyük! Görsel en fazla {totalAvailableBytes} bayt taşıyabilir.");

// !!!! Kapasite hesabı: Her piksel 3 kanaldan oluşuyor. er kanalın sadece 1 biti (en anlamsız biti, LSB) kullanılıyor
// Toplam piksel × 3 kanal = kaç bit taşıyabiliriz
// (Toplam piksel × 3) / 8 = kaç bayt taşıyabiliriz (çünkü 1 bayt = 8 bit)
// bu işlemi yapınca sonuçtaki kapasite mesakın içeriğinde büyük , eğer mesaj büyükse hata fırlat

            int totalBytesInImage = image.Rows * (int)image.Step();
            // gorüntünün toplam bellek boyutunu bayt cinsinden buluyoruz (genişlikx3)
            byte[] pixelBuffer = new byte[totalBytesInImage];
            Marshal.Copy(image.Data, pixelBuffer, 0, totalBytesInImage);
//toplu kopyalama. image.Data görüntüyü tuttuğu ham bellek adresi. Marshal.Copy,bellekteki tüm veriyi tek seferde, C#' yönetebileceği bir byte[] dizisine (pixelBuffer) kopyalıyor. piksel piksel gezmek yerine, tüm görüntüyü tek bir "blok kopyalama" işlemiyle al
            
            // GÖMME İŞLEMİ LSB MATEMATİĞİ
            int totalBitsToEmbed = fullPayload.Length * 8;
            int bitCounter = 0;

            for (int i = 0; i < totalBytesInImage && bitCounter < totalBitsToEmbed; i++)
            {
                int byteIdx = bitCounter / 8;
                int bitIdx = bitCounter % 8;
                // GÜMÜLECEK VERİNİN (FULLPAYLOAD) ŞU ANKİ BİTİNİN HANGİ BAYTIN İÇİNDE VE O BAYTIN KAÇINCI BİTİ OLDUĞUNU BULUYOR.
                // bitCounter = 10 ise, byteIdx = 1 (2. bayt), bitIdx = 2 (o baytın 3. biti, 0'dan sayarak).
                int bitToEmbed = (fullPayload[byteIdx] >> bitIdx) & 1;
                //o baytın, o pozisyondaki bitini oku, VE 1 : en sağdaki biti izole edip diğer tüm bitleri sıfırlıyor. elimizde 0 ya 1 kalıyor.
                pixelBuffer[i] = (byte)((pixelBuffer[i] & 0xFE) | bitToEmbed);
               // 0xFE = 11111110 (ikilik tabanda) , bu and işlemi pikselin son bitini sıfırlıyor. dieğr 7 bit aynı , OR işlemi aiz sıfırladığımız bit yerine gömmek istediğimiz biti yerleştiriyor.
               // pixelbuffer her baytında yano her renk kanalında çalışıyor. i doprudan totalbytesImage üzerinde artıyor. her pikselde 3 kanal , her kanalda 2 bit 
                bitCounter++; 
            }

            Marshal.Copy(pixelBuffer, 0, image.Data, totalBytesInImage);
            // değiştiridiğimiz pixelbufferi tekrar image.data ya geri kopyalıyoruz. bu değiştirilmiş görüntüyü PNG formatında bir bayt dizisine sıkıştırıp döndürüyoruz
            return image.ToBytes(".png");
        }
// gizli mesajı geri çıkaran fonksiyon
        public static string ExtractMessage(byte[] stegoImageBytes, string password)
        {
            using Mat image = Cv2.ImDecode(stegoImageBytes, ImreadModes.Color);
            if (image.Empty())
                throw new ArgumentException("Stego görseli okunamadı.");

            int totalBytesInImage = image.Rows * (int)image.Step();
            byte[] pixelBuffer = new byte[totalBytesInImage];
            Marshal.Copy(image.Data, pixelBuffer, 0, totalBytesInImage);

            long maxPayloadBytes = ((long)image.Width * image.Height * 3) / 8;

            byte[] lengthBytes = new byte[4];
            for (int i = 0; i < 32; i++)
            {
                int bit = pixelBuffer[i] & 1; // her baytın (kanalın) son bitini okuyoruz
                lengthBytes[i / 8] |= (byte)(bit << (i % 8)); // okuduğumuz biti, doğru pozisyonuna geri koyuyoruz , gömmede >> sağa kaydırmıştık burda sola kaydırıyoruz <<. 
                //   |= (OR ile birleştirme), bu biti mevcut baytın üzerine ekliyor, üzerine yazmıyor
            }

            int payloadLength = BitConverter.ToInt32(lengthBytes, 0); // 4 baytlık bu diziyi, tekrar bir tam sayıya çeviriyor
            // ilk 32 biti (4 baytı okuyoruz)- HideMessage'ın en başta gömdüğü "uzunluk etiketi".

            if (payloadLength <= 0 || payloadLength > (maxPayloadBytes - 4))
                throw new InvalidDataException("Görsel içinde geçerli bir gizli mesaj başlığı bulunamadı.");
// içinde gizli mesaj olmayan fotoğraf çöz moduna verilirse o fotonun piksellerinin son bitleri rastgeledir. uzunluk okursan büyük sayı çıkar. o yüzden kontrolet
           
            byte[] encryptedPayload = new byte[payloadLength];
            int totalBitsToRead = payloadLength * 8;

            for (int i = 0; i < totalBitsToRead; i++)
            {
                int bufferIndex = 32 + i;
                int bit = pixelBuffer[bufferIndex] & 1;
                encryptedPayload[i / 8] |= (byte)(bit << (i % 8));
            }
        //gerçek uzunluğu biliyoruz. il 32 uzunluk etiketi asıl veri odan sonra başlıyor payloadlenght kadar bayt okuyoruz (sabit 32 değil değişken uzunlukta )

            return DecryptAES(encryptedPayload, password);
            // elde ettiğimiz hala şifreli olan bayt dizisini AES gönderip, okunabilir metni geri al
        }

        public static double CalculatePSNR(Mat original, Mat stego)
        {
            if (original.Size() != stego.Size() || original.Type() != stego.Type())
                throw new ArgumentException("Görsellerin boyutları aynı olmalıdır.");

            using Mat diff = new Mat();
            Cv2.Absdiff(original, stego, diff);
            // İki görüntü arasındaki piksel piksel mutlak farkı hesaplıyor (|orijinal - stego|
            
            diff.ConvertTo(diff, MatType.CV_32F);
            using Mat diffSq = diff.Mul(diff);
            Scalar s = Cv2.Sum(diffSq);
            // hata kareler toplamının 

            double totalSse = s.Val0 + s.Val1 + s.Val2;
            double mse = totalSse / (original.Channels() * original.Total());

            if (mse <= 1e-10) return 99.99;

            return 10.0 * Math.Log10((255.0 * 255.0) / mse);
            //Toplam hatayı, piksel sayısı × kanal sayısına bölerek ortalama hataya (MSE) çeviriyor sonra standart PSNR formülünü uyguluyor.
        }

        private static byte[] EncryptAES(string plainText, string password)
        {

        //salt NE: aynı parolayI kullanıp mesaj şifrelerseniz, ve şifreleme her zaman aynı anahtarı üretirsE matematiksel örüntü oluşabilir.
        // salt her şifrelemde kullanılan rastgele üretilen ekstra. aynı parolayı kullansanız bile farklı anahtarlar üretir.
            byte[] salt = new byte[SALT_SIZE];
            RandomNumberGenerator.Fill(salt);

            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(32);
//PBKDF2_ITERATIONS = 100000: aynı işlemi 100.000 kez tekrar tekrar yapmak. asıtlı bir "yavaşlatma" tekniği — kriptografide standart bir yöntem (PBKDF2 = Password-Based Key Derivation Function 2
           
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            // IV (Initialization Vector) nedir: AES gibi blok şifreleme algoritmaları, veriyi sabit boyutlu bloklara (16 baytlık parçalara) bölüp şifreliyor.
            // her şifrelemede rastgele bir başlangıç noktası ekleyerek, aynı mesaj+parola kombinasyonu bile her seferinde farklı bir şifreli çıktı üretmesini sağlıyor.

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] result = new byte[SALT_SIZE + IV_SIZE + cipherBytes.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SALT_SIZE);
            Buffer.BlockCopy(aes.IV, 0, result, SALT_SIZE, IV_SIZE);
            Buffer.BlockCopy(cipherBytes, 0, result, SALT_SIZE + IV_SIZE, cipherBytes.Length);
// Paket yapısı: [16 bayt Salt][16 bayt IV][Şifreli veri] — üçünü tek bir bayt dizisinde birleştiriyor. Salt ve IV'nin kendisi gizli değil
// önce bu paketten salt ve IV'yi geri çıkarıp, aynı hesaplamayı tekrar yaparak doğru anahtarı yeniden üretiyoruz.
            return result;
        }

        private static string DecryptAES(byte[] encryptedPayload, string password)
        {
            // EncryptAES'in tersi: [Salt][IV][Şifreli veri] olarak ayrıştırıyor, salt + parola ile aynı anahtarı yeniden üretiyor, bu anahtar ve IV ile şifreyi çözüp orijinal metni geri veriyor.
            if (encryptedPayload.Length < (SALT_SIZE + IV_SIZE))
                throw new CryptographicException("Geçersiz şifreli veri paketi.");

            byte[] salt = new byte[SALT_SIZE];
            byte[] iv = new byte[IV_SIZE];
            byte[] cipherBytes = new byte[encryptedPayload.Length - SALT_SIZE - IV_SIZE];

            Buffer.BlockCopy(encryptedPayload, 0, salt, 0, SALT_SIZE);
            Buffer.BlockCopy(encryptedPayload, SALT_SIZE, iv, 0, IV_SIZE);
            Buffer.BlockCopy(encryptedPayload, SALT_SIZE + IV_SIZE, cipherBytes, 0, cipherBytes.Length);

            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(32);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}