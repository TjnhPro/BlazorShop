using BlazorShop.Application.ControlPlane;
using BlazorShop.ControlPlane.API.Authorization;
using BlazorShop.Infrastructure;
using BlazorShop.Infrastructure.Data.ControlPlane;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration
    .GetSection("ControlPlane:Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControlPlaneAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ControlPlaneWeb",
        policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
            else
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
});
builder.Services.AddControlPlaneApplication(builder.Configuration);
builder.Services.AddSharedAuthenticationInfrastructure(builder.Configuration);
builder.Services.AddControlPlaneInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("ControlPlaneWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
