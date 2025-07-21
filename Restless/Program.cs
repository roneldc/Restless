using Restless.Config;
using Restless.Interfaces;
using Restless.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("restless-settings.json", optional: false);

builder.Services.Configure<List<PingTarget>>(builder.Configuration.GetSection("Targets"));
builder.Services.Configure<MailjetSettings>(builder.Configuration.GetSection("MailJet"));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMailjetClientWrapper>(sp =>
{
    var mailjetSettings = builder.Configuration.GetSection("MailJet").Get<MailjetSettings>();
    return new MailjetClientWrapper(mailjetSettings!.ApiKey, mailjetSettings.ApiSecret);
});

builder.Services.AddSingleton<IEmailAlertService, EmailAlertService>();
builder.Services.AddHostedService<HealthCheckService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Restless is running"));
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();