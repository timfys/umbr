using System;
using System.Linq;
using Microsoft.AspNetCore.Rewrite;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Console.WriteLine("CONTENT ROOT: " + builder.Environment.ContentRootPath);
Console.WriteLine("WEB ROOT: " + builder.Environment.WebRootPath);
builder.Services.AddControllers();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();


WebApplication app = builder.Build();
await app.BootUmbracoAsync();

app.UseRouting();              
app.MapControllers();     

await app.BootUmbracoAsync();


var defaultCulture = "en";
var supportedCultures = new[] { "en", "ru", "ua", "he" };

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";

    // 🔥 НЕ ТРОГАЕМ API И НЕ-GET
    if (!HttpMethods.IsGet(context.Request.Method))
    {
        await next();
        return;
    }

    if (path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/download", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/thankyou", StringComparison.OrdinalIgnoreCase) ||
        path.Contains('.'))
    {
        await next();
        return;
    }

    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    if (segments.Length > 0 &&
        !supportedCultures.Contains(segments[0], StringComparer.OrdinalIgnoreCase))
    {
        var newPath = "/" + defaultCulture + path;
        context.Response.Redirect(newPath, permanent: false);
        return;
    }

    await next();
});


var rewriteOptions = new RewriteOptions()
    // /download/callcenterV7 -> внутренне /download
    .AddRewrite(@"^download/(?!submit$)[A-Za-z0-9_\-]+$", "download", true)
    // /ru/download/callcenterV7 -> /ru/download
    .AddRewrite(@"^(ru|ua|en|he)/download/([A-Za-z0-9_\-]+)$", "$1/download", skipRemainingRules: true)
    .AddRewrite(@"^thankyou/([A-Za-z0-9_\-]+)$", "thankyou", skipRemainingRules: true)
    .AddRewrite(@"^(ru|ua|en|he)/thankyou/([A-Za-z0-9_\-]+)$", "$1/thankyou", skipRemainingRules: true);
app.Use(async (context, next) =>
{
    // твой culture middleware — ок
    await next();
});

app.UseRewriter(rewriteOptions);

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseInstallerEndpoints();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();