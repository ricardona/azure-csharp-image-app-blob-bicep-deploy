using NETPhotoGallery.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Application Insights telemetry services
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddSingleton<IAzureBlobConnectionFactory, AzureBlobConnectionFactory>();
builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
builder.Services.AddSingleton<IImageLikeService, ImageLikeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
