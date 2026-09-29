using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Homevault.Application.Homes;
using Homevault.Application.Ports;
using Homevault.Application.Shopping;
using Homevault.Application.Weather;
using Homevault_api.ExceptionHandling;
using Homevault_api.Controllers;
using Homevault_api.Weather;
using Homevault.Infrastructure;
using Homevault.Infrastructure.Persistence;
using Homevault.Infrastructure.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("HomevaultWeb", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:FrontendOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateHomeValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateShoppingItemValidator>();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<CreateHome>();
builder.Services.AddScoped<CollectWeather>();
builder.Services.AddScoped<GetWeather>();
var shoppingHomeId = builder.Configuration.GetValue<Guid>("ShoppingList:HomeId");
if (shoppingHomeId == Guid.Empty)
    throw new InvalidOperationException("ShoppingList:HomeId deve conter o UUID da casa configurada.");
builder.Services.AddScoped(provider =>
    new ShoppingList(provider.GetRequiredService<IShoppingRepository>(), shoppingHomeId));
builder.Services.AddOptions<WeatherOptions>()
    .Bind(builder.Configuration.GetSection(WeatherOptions.SectionName))
    .Validate(options => options.CollectionIntervalMinutes > 0,
        "O intervalo de coleta deve ser maior que zero.")
    .Validate(options => options.RequestTimeoutSeconds > 0,
        "O timeout do provedor deve ser maior que zero.")
    .Validate(options => options.RetentionMonths > 0,
        "O período de retenção deve ser maior que zero.")
    .ValidateOnStart();
builder.Services.AddHostedService<WeatherCollectorBackgroundService>();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Homevault")
    ?? "Data Source=homevault.db");
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
// Learn more about configuring Swagger with Swashbuckle at https://aka.ms/aspnet/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Homevault API",
        Version = "v1"
    });
    options.DocInclusionPredicate((documentName, apiDescription) =>
        apiDescription.GroupName == documentName ||
        (apiDescription.GroupName is null && documentName == "v1"));
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HomeDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("HomevaultWeb");
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
