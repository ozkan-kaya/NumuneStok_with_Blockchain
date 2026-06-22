using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Services;

var syncRequested =
    args.Any(arg => string.Equals(arg, "--sync-blockchain", StringComparison.OrdinalIgnoreCase)) ||
    Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, "--sync-blockchain", StringComparison.OrdinalIgnoreCase));

if (syncRequested && !string.Equals(Environment.GetEnvironmentVariable("BLOCKCHAIN_SYNC_SOURCE"), "start_and_sync", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Blockchain başlangıç stoğu yalnızca Blockchain/scripts/start_and_sync.sh üzerinden senkronize edilebilir.");
    Environment.ExitCode = 1;
    return;
}

var builder = WebApplication.CreateBuilder(args);


/// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

// Blockchain servisini DI container'a kaydet
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddSingleton<IBlockchainStartupStockSyncService, BlockchainStartupStockSyncService>();


builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriş yapılmamışsa buraya yönlendir
        options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisiz erişim için yönlendirme
    });

builder.Services.AddAuthorization();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseSchemaInitializer.EnsureSupplyChainSchemaAsync(context);
}

if (syncRequested)
{
    using var scope = app.Services.CreateScope();
    var syncService = scope.ServiceProvider.GetRequiredService<IBlockchainStartupStockSyncService>();
    var result = await syncService.SynchronizeAsync(force: true);

    if (!result.Succeeded)
    {
        Console.Error.WriteLine($"❌ Blockchain başlangıç stoğu senkronize edilemedi: {result.ErrorMessage}");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine(
        $"✅ Blockchain başlangıç stoğu senkronize edildi. Lot: {result.LotCount}, yeni başlangıç: {result.InitializedCount}, tamamlama: {result.CompletedCount}");
    return;
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseDeveloperExceptionPage(); // Bu satır geçici olarak eklendiğinde hata detaylarını görebilirsin.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Giriş sayfasına yönlendirme

app.Run();
