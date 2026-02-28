using TalentAI.Configurations;
using TalentAI.Data;
using TalentAI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow frontend origins during development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Seed admin account on startup
using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await authService.SeedAdminAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
