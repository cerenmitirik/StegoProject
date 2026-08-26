using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StegoApi.Data;
using StegoApi.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext Bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// ppsettings.json'daki DefaultConnection bağlantı dizesini okuyup AppDbContext'i DI konteynerine kaydediyor.


// DI Servisleri
builder.Services.AddScoped<IStegoService, StegoService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Mobil bağlantı için CORS: web tarayıcısı gibi ortamlara) "özel header'larımı (X-PSNR gibi) okumana izin veriyorum
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("X-Stego-Id", "X-PSNR");
    });
});



var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5046");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Statik dosyaları (wwwroot/uploads) dış dünyaya açar
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();