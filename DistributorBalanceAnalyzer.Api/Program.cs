using DistributorBalanceAnalyzer.Core.Repositories;
using DistributorBalanceAnalyzer.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Distributor Balance Analyzer API",
        Version = "v1",
        Description = "API for analyzing distributor balance transactions and predicting depletion dates"
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
var connectionString = builder.Configuration.GetConnectionString("OracleDb");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Oracle connection string not found in configuration");
}

builder.Services.AddSingleton(new DistributorBalanceRepository(connectionString));
builder.Services.AddScoped<DistributorBalanceService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Distributor Balance Analyzer API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
