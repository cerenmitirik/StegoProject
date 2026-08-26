using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OpenCvSharp;
using StegoApi.Data;
using StegoApi.DTOs;
using StegoApi.Entities;

namespace StegoApi.Services
{
    public class StegoService : IStegoService // işi yapan kişi , ben bu ilana başv
    {
        private readonly AppDbContext _context;
        private readonly string _uploadDirectory;

        public StegoService(AppDbContext context)
        {
            _context = context;
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(_uploadDirectory))
                Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<(byte[] ImageBytes, StegoRecord Record)> EncodeAndSaveAsync(EncodeRequestDto dto)
        {
            //encodeansaveasync
            using var ms = new MemoryStream(); // gelen ıformfile nesnesini belleğe kopyalar
            await dto.Image.CopyToAsync(ms);
            byte[] originalBytes = ms.ToArray();

           
            // 1. LSB + AES ile gizleme
            byte[] stegoBytes = StegoEngine.HideMessage(originalBytes, dto.SecretMessage, dto.Password);
// Elindeki fotoğrafı, gizli mesajı ve parolayı, asıl işi yapan uzmana (StegoEngine) veriyor — "bunu şifrele ve göm" diyor
            
            // 2. orijinal ve mesaj gömülmüş fotoğrafı opencvnın anlayacağı formata Mat çevirip ikisini karşılaştırarak ne kadar benziyolar diye bakar 
            using Mat originalMat = Cv2.ImDecode(originalBytes, ImreadModes.Color);
            using Mat stegoMat = Cv2.ImDecode(stegoBytes, ImreadModes.Color);
            double psnr = StegoEngine.CalculatePSNR(originalMat, stegoMat);

            // 3. Dosyayı diske kaydetme
            string fileName = $"stego_{Guid.NewGuid()}.png";
            string filePath = Path.Combine(_uploadDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, stegoBytes);

            // 4. Veritabanı kaydı (Yerel Saat)
            var record = new StegoRecord
            {
                OriginalFileName = dto.Image.FileName,
                StegoImagePath = $"/uploads/{fileName}",
                PsnrValue = Math.Round(psnr, 2),
                FileSizeBytes = stegoBytes.Length,
                CreatedAt = DateTime.Now
            };

            _context.StegoRecords.Add(record);
            await _context.SaveChangesAsync();

            return (stegoBytes, record); // üretilen fotoğrafın kendisini (baytlarını ) hem de veri tabanına yazdığı kaydı geri döndür.
            // controllerin ikisine de ihtiyacı var i fotoğrafı telefona göndermek için , kaydı ise -stego-id headerine koymak için)
        }

        public async Task<string> DecodeAsync(DecodeRequestDto dto)
        {
            try
            {
                using var ms = new MemoryStream();
                await dto.StegoImage.CopyToAsync(ms);
                byte[] stegoBytes = ms.ToArray();

                return StegoEngine.ExtractMessage(stegoBytes, dto.Password);
            }
            // şifreli mesaj içeren fotoyu belleğe alıyor.stegoengine'e bu fotoğraftaki gizli mesajı çıkar diyor, çözüleni döndür.
            catch (CryptographicException)
            {
                throw new ArgumentException("Parola hatalı veya görselin piksel yapısı bozulmuş/sıkıştırılmış!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Stego Decoda Hata:]"+ex);
                throw new ArgumentException("Mesaj çözülemedi: " + ex.Message);
            }
        }
    }
}