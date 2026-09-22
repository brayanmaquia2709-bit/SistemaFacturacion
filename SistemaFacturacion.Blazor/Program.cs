using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SistemaFacturacion.API.Data;
using SistemaFacturacion.API.Services;
using SistemaFacturacion.Blazor.Components;
using SistemaFacturacion.Core.Entities;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Puerto para Render
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(renderPort))
{
    builder.WebHost.UseUrls($"http://*:{renderPort}");
}

// Configuración de QuestPDF (Licencia Comunitaria Gratuita)
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuración de EF Core SQLite para la BD compartida
builder.Services.AddDbContext<FacturacionDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=facturacion.db"));

// Registro de servicios de PDF y Correo
builder.Services.AddScoped<PdfFacturaService>();
builder.Services.AddScoped<CorreoService>();

// Habilitar Controllers de API en el mismo servicio Web
builder.Services.AddControllers()
    .AddApplicationPart(typeof(SistemaFacturacion.API.Controllers.ProductosController).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Habilitar CORS para permitir consumo desde .NET MAUI y clientes externos
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var defaultLocalApi = !string.IsNullOrEmpty(renderPort) ? $"http://127.0.0.1:{renderPort}/" : "http://localhost:5145/";

var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") 
    ?? (!string.IsNullOrEmpty(renderPort) ? defaultLocalApi : builder.Configuration["ApiBaseUrl"])
    ?? defaultLocalApi;

if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<SistemaFacturacion.Blazor.Services.FacturacionApiService>();

var app = builder.Build();

// Inicializar y sembrar Base de Datos SQLite en el inicio del servicio Web
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FacturacionDbContext>();
    try
    {
        _ = db.Facturas.Select(f => new { f.UsuarioId, f.CajaSesionId, f.PagaCon }).FirstOrDefault();
    }
    catch
    {
        db.Database.EnsureDeleted();
    }

    db.Database.EnsureCreated();

    if (!db.Impuestos.Any())
    {
        db.Impuestos.AddRange(
            new Impuesto { Tipo = "IVA 19%", Porcentaje = 19.0m, Activo = true },
            new Impuesto { Tipo = "Exento 0%", Porcentaje = 0.0m, Activo = true },
            new Impuesto { Tipo = "IVA 5% Reducido", Porcentaje = 5.0m, Activo = true }
        );
        db.SaveChanges();
    }

    if (!db.Clientes.Any())
    {
        db.Clientes.AddRange(
            new Cliente { Nombre = "Empresa Tecnológica S.A.", Email = "contacto@tecnosolution.com", Telefono = "555-123-4567", DocumentoIdentidad = "NIT-900123456-1", Direccion = "Av. Principal 100" },
            new Cliente { Nombre = "María Fernanda López", Email = "m.lopez@gmail.com", Telefono = "555-987-6543", DocumentoIdentidad = "CC-1020304050", Direccion = "Calle Primavera 45" },
            new Cliente { Nombre = "Distribuidora del Norte", Email = "ventas@delnorte.com", Telefono = "555-456-7890", DocumentoIdentidad = "NIT-800987654-3", Direccion = "Zona Industrial 12" }
        );
        db.SaveChanges();
    }

    if (!db.Productos.Any())
    {
        db.Productos.AddRange(
            new Producto { Nombre = "Laptop Pro 15\"", Precio = 1250.00m, Stock = 25, CodigoBarra = "PROD-001", Categoria = "Electrónica" },
            new Producto { Nombre = "Monitor 4K 27\"", Precio = 380.00m, Stock = 40, CodigoBarra = "PROD-002", Categoria = "Periféricos" },
            new Producto { Nombre = "Teclado Mecánico RGB", Precio = 85.50m, Stock = 100, CodigoBarra = "PROD-003", Categoria = "Accesorios" },
            new Producto { Nombre = "Mouse Inalámbrico Ergonómico", Precio = 45.00m, Stock = 80, CodigoBarra = "PROD-004", Categoria = "Accesorios" },
            new Producto { Nombre = "Impresora Multifuncional Láser", Precio = 290.00m, Stock = 15, CodigoBarra = "PROD-005", Categoria = "Oficina" }
        );
        db.SaveChanges();
    }

    if (!db.Usuarios.Any())
    {
        db.Usuarios.AddRange(
            new Usuario { Nombre = "Administrador del Sistema", Username = "admin", PasswordHash = "admin123", Rol = "Admin", Activo = true },
            new Usuario { Nombre = "Juan Pérez (Cajero 1)", Username = "cajero1", PasswordHash = "123", Rol = "Cajero", Activo = true },
            new Usuario { Nombre = "María Gómez (Cajera 2)", Username = "cajero2", PasswordHash = "123", Rol = "Cajero", Activo = true }
        );
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
});
app.UseCors("AllowAll");

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
