using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using ABM.Data;
using ABM.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ABM.Helpers;
using AutoMapper;
using ABM.Models;
using Microsoft.AspNetCore.Identity;
using ABM.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Agregar servicios al contenedor
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<DatabaseExceptionFilter>(); // Agregar el filtro globalmente
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


builder.Services.AddDbContext<AppDBContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CadenaSQL"),
        sqlServerOptions => sqlServerOptions.CommandTimeout(150) // Timeout de 120 segundos
    );
});


builder.Services.AddTransient<IRepositorioCargas, RepositorioCargas>();
builder.Services.AddTransient<IRepositorioExcepciones, RepositorioExcepciones>();
builder.Services.AddTransient<IRepositorioMatrizDiaria, RepositorioMatrizDiaria>();
builder.Services.AddTransient<IRepositorioUsuarios, RepositorioUsuarios>();
builder.Services.AddTransient<IRepositorioAlertaSistema, RepositorioAlertaSistema>();
builder.Services.AddTransient<IRepositorioReportes, RepositorioReportes>();
builder.Services.AddTransient<IRepositorioVision, RepositorioVision>();
builder.Services.AddTransient<ABM.Servicios.IEmailSender, ABM.Servicios.EmailSender>();
builder.Services.AddTransient<IRepositorioGestion, RepositorioGestion>();
builder.Services.AddTransient<IRepositorioMenus, RepositorioMenus>();
builder.Services.AddScoped<IRepositorioAuditoriaFirmas, RepositorioAuditoriaFirmas>();
builder.Services.AddAutoMapper(typeof(Program));


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Acceso/Login";
		options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Ajusta este valor
		options.SlidingExpiration = true; // Esto renueva la cookie si el usuario está activo
	});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
