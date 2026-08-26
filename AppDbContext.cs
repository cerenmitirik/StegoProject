using Microsoft.EntityFrameworkCore;
using StegoApi.Entities;

namespace StegoApi.Data
{
    public class AppDbContext : DbContext
    {
        //dbcontext sql serverla konuşmayı bilen yapı.. appdbcontext kendisi yapmıyor üst sınıfa veriyor yönet diye.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StegoRecord> StegoRecords => Set<StegoRecord>();
   // stegorecords özelliğine her erişildiğinde set<stego çağrılıp tabloya erişim sağlanıyor.
    }
}