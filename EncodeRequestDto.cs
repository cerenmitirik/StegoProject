using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace StegoApi.DTOs
{
    public class EncodeRequestDto
    {
        [Required]
        // oğrulama (validation) niteliği (attribute), gelen HTTP isteğini doğruluyor
        // eğer Image, SecretMessage ya da Password boş/eksik gelirse, Controller kodun hiç çalışmadan, otomatik olarak 400 Bad Request
        public IFormFile Image { get; set; } = null!;
        //bilerek null başlatıyorum uyarı verme. neden gerekli ? ıformfile da null olamayacak tip tanımlanmış ama nesne oluştuğunda geçici boş olacağı için kontrol altına al

        [Required]
        [MinLength(1)]
        public string SecretMessage { get; set; } = string.Empty;
// string empty : boş stringle başlat
        [Required]
        [MinLength(6)] //en az 6 karakter. 6 dan azsa controllere girmeden 400 bad request
        public string Password { get; set; } = string.Empty;
    }
}