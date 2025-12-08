using System;
using System.Linq;
using Microsoft.AspNetCore.Rewrite;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Console.WriteLine("CONTENT ROOT: " + builder.Environment.ContentRootPath);
Console.WriteLine("WEB ROOT: " + builder.Environment.WebRootPath);
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();


var defaultCulture = "en";
var supportedCultures = new[] { "en", "ru", "ua" };

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";

    if (path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    if (path.Contains('.'))
    {
        await next();
        return;
    }

    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    if (segments.Length > 0 &&
        supportedCultures.Contains(segments[0], StringComparer.OrdinalIgnoreCase))
    {
        await next();
        return; 
    }


    var newPath = "/" + defaultCulture + (path == "/" ? "" : path);

    var query = context.Request.QueryString.HasValue
        ? context.Request.QueryString.Value
        : "";

    var newUrl = newPath + query;

    context.Response.Redirect(newUrl, permanent: false);
});

var rewriteOptions = new RewriteOptions()
    // /download/callcenterV7 -> внутренне /download
    .AddRewrite(@"^download/([A-Za-z0-9_\-]+)$", "download", skipRemainingRules: true)
    // /ru/download/callcenterV7 -> /ru/download
    .AddRewrite(@"^(ru|ua|en)/download/([A-Za-z0-9_\-]+)$", "$1/download", skipRemainingRules: true)
    .AddRewrite(@"^thankyou/([A-Za-z0-9_\-]+)$", "thankyou", skipRemainingRules: true)
    .AddRewrite(@"^(ru|ua|en)/thankyou/([A-Za-z0-9_\-]+)$", "$1/thankyou", skipRemainingRules: true);

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