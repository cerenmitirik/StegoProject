using System;

namespace StegoApi.Entities
{
    public class StegoRecord

    // panoramadaki gibi key required maxlengt yok burda. ef core isim kuralına göre ıd alanını otomatik primary key yapıyo. maxlegnt holmayınca nvarchar (max) yapıyor ama.
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StegoImagePath { get; set; } = string.Empty;
        public double PsnrValue { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}