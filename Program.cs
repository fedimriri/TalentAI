using TalentAI.Configurations;
using TalentAI.Data;
using TalentAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables into configuration
builder.Configuration.AddEnvironmentVariables();

// Override MongoDB connection string from environment variable if set
var mongoConn = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
if (!string.IsNullOrEmpty(mongoConn))
    builder.Configuration["MongoSettings:ConnectionString"] = mongoConn;

// Override AI settings from environment variables
var groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
if (!string.IsNullOrEmpty(groqApiKey))
    builder.Configuration["AISettings:GroqApiKey"] = groqApiKey;

// Override Email settings from environment variables
var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST");
if (!string.IsNullOrEmpty(emailHost))
    builder.Configuration["EmailSettings:Host"] = emailHost;

var emailPort = Environment.GetEnvironmentVariable("EMAIL_PORT");
if (!string.IsNullOrEmpty(emailPort))
    builder.Configuration["EmailSettings:Port"] = emailPort;

var emailUsername = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
if (!string.IsNullOrEmpty(emailUsername))
    builder.Configuration["EmailSettings:Username"] = emailUsername;

var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
if (!string.IsNullOrEmpty(emailPassword))
    builder.Configuration["EmailSettings:Password"] = emailPassword;

var emailFrom = Environment.GetEnvironmentVariable("EMAIL_FROM");
if (!string.IsNullOrEmpty(emailFrom))
    builder.Configuration["EmailSettings:FromEmail"] = emailFrom;



builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IResumeParserService, ResumeParserService>();
builder.Services.AddScoped<IJobParserService, JobParserService>();
builder.Services.AddHttpClient<IMatchingService, MatchingService>();
builder.Services.AddHttpClient<IAtsScoringService, AtsScoringService>();

// Bind Settings from appsettings.json
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AISettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();


builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

// Seed admin account on startup with connection error handling
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        await authService.SeedAdminAsync();
        logger.LogInformation("Admin seed completed successfully. MongoDB connection verified.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to MongoDB or seed admin account. " +
            "Verify your MONGO_CONNECTION_STRING environment variable or MongoSettings in appsettings.json.");
        throw; // Re-throw to prevent app from starting with a broken DB connection
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseSession();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
