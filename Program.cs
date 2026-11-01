using ProductosAPI.Data;
using ProductosAPI.Endpoints;
using ProductosAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddSingleton<IConexionDb, ConexionDb>();

builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<IProductoRepository>(sp => sp.GetRequiredService<ProductoRepository>());
builder.Services.AddScoped<IProductoBusqueda>(sp => sp.GetRequiredService<ProductoRepository>());

var app = builder.Build();

try
{
    InicializadorDb.Inicializar(app.Configuration, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogWarning("No se pudo inicializar la base de datos: {Mensaje}", ex.Message);
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ProductosAPI v1");
        options.DocumentTitle = "ProductosAPI - ADO.NET con MySQL | UP230253";
        options.HeadContent = """
            <style>
                body { margin: 0; }
                .swagger-ui .topbar {
                    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
                    padding: 14px 0;
                }
                .swagger-ui .topbar .wrapper { display: flex; align-items: center; }
                .swagger-ui .topbar a { font-size: 0; }
                .swagger-ui .topbar .wrapper::before {
                    content: "ProductosAPI — ADO.NET + MySQL | UP230253";
                    color: #e94560;
                    font-size: 20px;
                    font-weight: 700;
                    font-family: 'Segoe UI', sans-serif;
                    letter-spacing: 0.5px;
                }
                .swagger-ui .topbar .wrapper::after {
                    content: "CRUD con Inyección de Dependencias y Repositorio";
                    color: #a8a8b3;
                    font-size: 13px;
                    margin-left: 18px;
                    font-family: 'Segoe UI', sans-serif;
                }
                .swagger-ui .info .title { color: #1a1a2e; }
                .swagger-ui .info { margin: 30px 0; }
                .swagger-ui .scheme-container { background: #f8f9fa; border-radius: 8px; }
                .swagger-ui .opblock.opblock-get { border-color: #0f3460; background: rgba(15, 52, 96, 0.05); }
                .swagger-ui .opblock.opblock-get .opblock-summary { border-color: #0f3460; }
                .swagger-ui .opblock.opblock-post { border-color: #e94560; background: rgba(233, 69, 96, 0.05); }
                .swagger-ui .opblock.opblock-post .opblock-summary { border-color: #e94560; }
                .swagger-ui .opblock.opblock-put { border-color: #f5a623; background: rgba(245, 166, 35, 0.05); }
                .swagger-ui .opblock.opblock-put .opblock-summary { border-color: #f5a623; }
                .swagger-ui .opblock.opblock-delete { border-color: #ff4757; background: rgba(255, 71, 87, 0.05); }
                .swagger-ui .opblock.opblock-delete .opblock-summary { border-color: #ff4757; }
                .swagger-ui .btn.execute { background: #e94560; border-color: #e94560; }
                .swagger-ui .btn.execute:hover { background: #c73a52; }
                .swagger-ui .opblock-tag { font-size: 18px; font-weight: 600; border-bottom: 2px solid #0f3460; }
            </style>
            """;
    });
}

app.MapGet("/", () => Results.Content("""
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="utf-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <title>ProductosAPI - ADO.NET | UP230253</title>
        <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body {
                font-family: 'Segoe UI', Tahoma, sans-serif;
                background: linear-gradient(135deg, #1a1a2e, #16213e, #0f3460);
                min-height: 100vh; display: flex; align-items: center; justify-content: center;
                color: #fff;
            }
            .card {
                background: rgba(255,255,255,0.07); backdrop-filter: blur(10px);
                border-radius: 20px; padding: 50px 40px; max-width: 520px; width: 90%;
                border: 1px solid rgba(255,255,255,0.12); text-align: center;
                box-shadow: 0 20px 60px rgba(0,0,0,0.4);
            }
            h1 { font-size: 28px; margin-bottom: 8px; color: #e94560; }
            .sub { color: #a8a8b3; font-size: 14px; margin-bottom: 30px; }
            .badge {
                display: inline-block; background: #e94560; color: #fff;
                padding: 4px 14px; border-radius: 20px; font-size: 12px;
                font-weight: 600; margin-bottom: 24px; letter-spacing: 1px;
            }
            .links { display: flex; flex-direction: column; gap: 12px; margin-top: 20px; }
            .links a {
                display: block; padding: 14px; border-radius: 12px; text-decoration: none;
                font-weight: 600; font-size: 15px; transition: all 0.3s;
            }
            .links .primary { background: #e94560; color: #fff; }
            .links .primary:hover { background: #c73a52; transform: translateY(-2px); }
            .links .secondary { background: rgba(255,255,255,0.08); color: #fff; border: 1px solid rgba(255,255,255,0.15); }
            .links .secondary:hover { background: rgba(255,255,255,0.15); transform: translateY(-2px); }
            .tech { margin-top: 30px; display: flex; justify-content: center; gap: 10px; flex-wrap: wrap; }
            .tech span {
                background: rgba(255,255,255,0.1); padding: 6px 14px; border-radius: 8px;
                font-size: 12px; color: #a8a8b3;
            }
            .footer { margin-top: 30px; font-size: 12px; color: #555; }
        </style>
    </head>
    <body>
        <div class="card">
            <div class="badge">UP230253</div>
            <h1>ProductosAPI</h1>
            <p class="sub">Acceso a Base de Datos con ADO.NET y MySQL</p>
            <p style="color:#ccc;font-size:14px;line-height:1.6">
                API REST para administrar productos usando <strong>ADO.NET puro</strong>
                con patrón Repositorio, inyección de dependencias y MySQL.
            </p>
            <div class="links">
                <a href="/swagger" class="primary">Abrir Swagger UI</a>
                <a href="/conexion" class="secondary">Probar Conexión a MySQL</a>
                <a href="/productos" class="secondary">Ver Productos (JSON)</a>
            </div>
            <div class="tech">
                <span>.NET 10</span>
                <span>ADO.NET</span>
                <span>MySQL</span>
                <span>Swagger</span>
                <span>Minimal API</span>
            </div>
            <p class="footer">Práctica de Acceso a Base de Datos — ISC09C</p>
        </div>
    </body>
    </html>
    """, "text/html"))
    .ExcludeFromDescription();

app.MapConexionEndpoints();
app.MapProductoEndpoints();

app.Run();
