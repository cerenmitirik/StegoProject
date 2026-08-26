using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace StegoApi.DTOs
{
    // dto data transfer object: dışarıdan http isteğinden gelen veriyi tek bir kutuda topluyor
    public class DecodeRequestDto
    {
        [Required]
        //doğrulama niteliği (validation attribute). gelen HTTP isteğini doğruluyor
        
        public IFormFile StegoImage { get; set; } = null!;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // controlleri doğrudan almıyor. birden fazla alanı tek bir dto altında topluyorsun. 
}