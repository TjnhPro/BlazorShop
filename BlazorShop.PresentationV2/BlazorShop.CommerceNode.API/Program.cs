using BlazorShop.CommerceNode.API.Configuration;
using BlazorShop.CommerceNode.API.Deployment;
using BlazorShop.CommerceNode.API.Endpoints;
using BlazorShop.CommerceNode.API.Middleware;
using BlazorShop.CommerceNode.API.Tasks;
using BlazorShop.CommerceNode.API.Workers;
using BlazorShop.Application.CommerceNode.Tasks;
using BlazorShop.Infrastructure.Data.CommerceNode;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<CommerceNodeOptions>()
    .Bind(builder.Configuration.GetSection(CommerceNodeOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<CommerceNodeOptions>, CommerceNodeOptionsValidator>();
builder.Services.AddOptions<CommerceNodeRuntimeOptions>()
    .Bind(builder.Configuration.GetSection(CommerceNodeRuntimeOptions.SectionName));
builder.Services.AddOptions<CommerceTaskWorkerOptions>()
    .Bind(builder.Configuration.GetSection(CommerceTaskWorkerOptions.SectionName));
builder.Services.AddOptions<StorefrontDeploymentOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontDeploymentOptions.SectionName));
builder.Services.AddOptions<NginxDeploymentOptions>()
    .Bind(builder.Configuration.GetSection(NginxDeploymentOptions.SectionName));
builder.Services.AddCommerceNodeInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IStorefrontDockerDeploymentService, StorefrontDockerDeploymentService>();
builder.Services.AddSingleton<INginxDeploymentService, NginxDeploymentService>();
builder.Services.AddScoped<ICommerceTaskHandler, CompleteTestCommerceTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, FailTestCommerceTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, WaitTestCommerceTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, StoreCreateAndDeployTaskHandler>();
builder.Services.AddHostedService<CommerceTaskWorker>();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(CreateUploadsStaticFileOptions(uploadsPath));

app.MapGet("/", () => Results.Redirect("/swagger"));
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/commerce"),
    branch => branch.UseMiddleware<CommerceNodeCredentialMiddleware>());
app.UseAuthentication();
app.UseAuthorization();
app.MapCommerceHealthEndpoints();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

static StaticFileOptions CreateUploadsStaticFileOptions(string uploadsPath)
{
    var contentTypeProvider = new FileExtensionContentTypeProvider();
    contentTypeProvider.Mappings.Clear();
    contentTypeProvider.Mappings[".jpg"] = "image/jpeg";
    contentTypeProvider.Mappings[".jpeg"] = "image/jpeg";
    contentTypeProvider.Mappings[".png"] = "image/png";
    contentTypeProvider.Mappings[".webp"] = "image/webp";
    contentTypeProvider.Mappings[".gif"] = "image/gif";
    contentTypeProvider.Mappings[".bmp"] = "image/bmp";

    return new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        ContentTypeProvider = contentTypeProvider,
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        },
    };
}
