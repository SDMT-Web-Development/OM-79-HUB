using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OM_79_HUB.Data;
using OM79.Models.DB;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OM_79_HUBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection") ?? throw new InvalidOperationException("Connection string 'OM_79_HUBContext' not found.")));

//Second DB
builder.Services.AddDbContext<OM79Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection2") ?? throw new InvalidOperationException("Connection string 'UR MOM' not found.")));

//Second DB
builder.Services.AddDbContext<Pj103Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection3") ?? throw new InvalidOperationException("Connection string 'John' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();

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
