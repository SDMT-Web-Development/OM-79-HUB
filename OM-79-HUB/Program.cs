using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Data;
using OM79.Models.DB;
using QuestPDF.Infrastructure;
using Microsoft.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add session services
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15); // Set session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});



//**********
//Change this when committing whether to prod or test!!!!!!!!!!!!!!!!!!
var environment = "Production";


if (environment == "Production")
{
    builder.Services.AddDbContext<OM_79_HUBContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("ProdDevConnectionHub")
            ?? throw new InvalidOperationException("Connection string 'ProdDevConnectionHub' not found.")));

    builder.Services.AddDbContext<OM79Context>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("ProdDevConnectionOM79")
            ?? throw new InvalidOperationException("Connection string 'ProdDevConnectionOM79' not found.")));

    builder.Services.AddDbContext<Pj103Context>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("ProdDevConnectionPJ103")
            ?? throw new InvalidOperationException("Connection string 'ProdDevConnectionPJ103' not found.")));
}
else if (environment == "Test")
{
    builder.Services.AddDbContext<OM_79_HUBContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("TestDevConnectionHub")
            ?? throw new InvalidOperationException("Connection string 'TestDevConnectionHub' not found.")));

    builder.Services.AddDbContext<OM79Context>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("TestDevConnectionOM79")
            ?? throw new InvalidOperationException("Connection string 'TestDevConnectionOM79' not found.")));

    builder.Services.AddDbContext<Pj103Context>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("TestDevConnectionPJ103")
            ?? throw new InvalidOperationException("Connection string 'TestDevConnectionPJ103' not found.")));
}
else
{
    throw new InvalidOperationException("Invalid environment specified. Use 'Production' or 'Test'.");
}


// Register the RecyclableMemoryStreamManager service
builder.Services.AddSingleton<RecyclableMemoryStreamManager>();



// Configure Kestrel server limits
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104857600; // Set to 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // Set to 100 MB
});



// Enumber authentication
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.  
    options.FallbackPolicy = options.DefaultPolicy;
});

// Add services to the container
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.Use(async (context, next) =>
{
    // Pass the environment to the ViewData so it can be accessed in Razor views
    context.Items["Environment"] = environment;
    await next.Invoke();
});

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enumber stuff
app.UseAuthentication();
app.UseAuthorization();

// Add session middleware
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
