using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Homevault.Application.Homes;
using Homevault_api.ExceptionHandling;
using Homevault.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateHomeValidator>();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<CreateHome>();
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

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
