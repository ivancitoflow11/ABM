using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using ABM.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using ABM.Helpers;
using AutoMapper;
using ABM.Models;
using Microsoft.AspNetCore.Identity;
using ABM.Filters;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<DatabaseExceptionFilter>(); // Agrega el filtro globalmente
});

// Registrar IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configuración de la sesión
builder.Services.AddDistributedMemoryCache(); // Opcional: para almacenar en caché en memoria
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Ajusta el tiempo según sea necesario
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Si no usas Entity Framework, ya no es necesario registrar el DbContext
// builder.Services.AddDbContext<AppDBContext>(options =>
// {
//     options.UseSqlServer(
//         builder.Configuration.GetConnectionString("CadenaSQL"),
//         sqlServerOptions => sqlServerOptions.CommandTimeout(150)
//     );
// });

builder.Services.AddTransient<IRepositorioUsuarios, RepositorioUsuarios>();
builder.Services.AddTransient<IRepositorioRoles, RepositorioRoles>();
builder.Services.AddTransient<IRepositorioReportes, RepositorioReportes>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddTransient<IRepositorioMenus, RepositorioMenus>();
builder.Services.AddTransient<IRepositorioConfiguracion, RepositorioConfiguracion>();
builder.Services.AddTransient<ABM.Servicios.IEmailSender, ABM.Servicios.EmailSender>();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Ajusta este valor según tu necesidad
        options.SlidingExpiration = true; // Renueva la cookie si el usuario está activo
    });

var app = builder.Build();

// Configurar la canalización de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// Agrega el middleware de sesión
app.UseSession();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}");

app.Run();

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<ResumenGestionViewModel, GestionViewModel>();
    }
}
