using SmartContentRecommender.WebClient.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ApiSettings>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return config.GetSection(ApiSettings.SectionName).Get<ApiSettings>() ?? new ApiSettings();
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".SCR.WebClient.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<ITokenStore, SessionTokenStore>();
builder.Services.AddScoped<ScrApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Для self-contained EXE запускаем по HTTP, чтобы не требовать локальный сертификат.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
