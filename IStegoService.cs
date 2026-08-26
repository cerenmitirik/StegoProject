using System.Threading.Tasks;
using StegoApi.DTOs;
using StegoApi.Entities;

namespace StegoApi.Services
{
    public interface IStegoService // iş ilanı gibi  , işin yapılma şekli
    {
        Task<(byte[] ImageBytes, StegoRecord Record)> EncodeAndSaveAsync(EncodeRequestDto dto);
        //EncodeAndSaveAsync — bir DTO alıp, karşılığında bir fotoğraf + kayıt döndürebilmeli
        Task<string> DecodeAsync(DecodeRequestDto dto);
        //DecodeAsync — bir DTO alıp, karşılığında bir metin döndürebilmeli
    // şifreli görseli ve parolayı alıri çözülen ham gizli metni string olarak döner.
    }
}