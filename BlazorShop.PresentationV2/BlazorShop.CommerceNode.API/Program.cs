using BlazorShop.CommerceNode.API.Configuration;
using BlazorShop.CommerceNode.API.Endpoints;
using BlazorShop.CommerceNode.API.Middleware;
using BlazorShop.Infrastructure.Data.CommerceNode;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<CommerceNodeOptions>()
    .Bind(builder.Configuration.GetSection(CommerceNodeOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<CommerceNodeOptions>, CommerceNodeOptionsValidator>();
builder.Services.AddCommerceNodeInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/commerce"),
    branch => branch.UseMiddleware<CommerceNodeCredentialMiddleware>());
app.MapCommerceHealthEndpoints();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
