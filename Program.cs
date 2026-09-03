
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
//using PaletsWebApp.Utilites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Utilites;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();



builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

//axty desabilitamos endpoint y usamos mvc
builder.Services.AddMvc().AddMvcOptions(Opcion => { Opcion.EnableEndpointRouting = false; });

//axty session
builder.Services.AddDistributedMemoryCache(); //This way ASP.NET Core will use a Memory Cache to store session variables
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(10); // It depends on user requirements.
});




builder.Services.Configure<IdentityOptions>(options =>
{
    // Default Password settings.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});


builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddNotyf(config => { config.DurationInSeconds = 10; config.IsDismissable = true; config.Position = NotyfPosition.BottomRight; });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/AccessDenied";
});


var app = builder.Build();
DataSeeding();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseNotyf();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseCookiePolicy();
app.UseSession();


//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseMvc(routes =>
{
    routes.MapRoute(
        name: "default",
        template: "{controller=Home}/{action=Index}/{id?}");
    //routes.MapAreaRoute("Usuario", "Usuario", "{controller=Admin}/{action=''}/{id?}");
    //routes.MapAreaRoute("Dashboard", "Dashboard", "{controller=Dashboard}/{action=''}/{id?}");
    //routes.MapAreaRoute("Customers", "Customers", "{controller=Customers}/{action=''}/{id?}");
});



app.Run();


void DataSeeding()
{
    using (var scope = app.Services.CreateScope())
    {
        var DbInitialize = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        DbInitialize.Initialize();
    }
}


