using NETPhotoGallery.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // Added for ILogger
using Microsoft.Extensions.Configuration; // Added for IConfiguration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddControllersWithViews();

// Add Application Insights telemetry services
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddSingleton<IAzureBlobConnectionFactory, AzureBlobConnectionFactory>();
builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
builder.Services.AddSingleton<IImageLikeService, ImageLikeService>();

var app = builder.Build();

// Initialize Azure Tables
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var tableStorageService = services.GetRequiredService<ITableStorageService>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var folderTableName = configuration.GetValue<string>("StorageAccount:FolderTableName") ?? "Folders";
        var tagTableName = configuration.GetValue<string>("StorageAccount:TagTableName") ?? "Tags";
        var imageTagTableName = configuration.GetValue<string>("StorageAccount:ImageTagTableName") ?? "ImageTags";
        // Photos table might also be needed if we are storing image metadata there too
        // var photoMetadataTableName = configuration.GetValue<string>("StorageAccount:PhotoTableName") ?? "Photos";


        await tableStorageService.InitializeTableAsync(folderTableName);
        await tableStorageService.InitializeTableAsync(tagTableName);
        await tableStorageService.InitializeTableAsync(imageTagTableName);
        // await tableStorageService.InitializeTableAsync(photoMetadataTableName);


        // Optionally, ensure the root folder concept/partition exists if needed by FolderService logic
        var folderService = services.GetRequiredService<IFolderService>();
        await folderService.EnsureRootPartitionKeyAsync(); // Call the helper
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred initializing the Azure Tables.");
    }
}

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
