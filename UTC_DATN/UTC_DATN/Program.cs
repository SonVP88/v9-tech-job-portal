using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UTC_DATN.Data;
using UTC_DATN.Services.Implements;
using UTC_DATN.Services.Interfaces;
using UTC_DATN.Services.Background;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("UTC_DATNContext");
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IAiMatchingService, AiMatchingService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Background Services
builder.Services.AddHostedService<JobExpirationService>();

// Đăng ký HttpClientFactory cho các service cần gọi external API
builder.Services.AddHttpClient<IAiMatchingService, AiMatchingService>();
builder.Services.AddHttpClient(); // Cho MasterDataService

// KẾ HOẠCH TỐI ƯU TỐC ĐỘ: Cấu hình HttpClient riêng cho Gemini 
// Giới hạn Timeout 60s để tránh treo luồng hệ thống vĩnh viễn
builder.Services.AddHttpClient("GeminiClient", client => {
    client.Timeout = TimeSpan.FromSeconds(60);
    // Tắt Header Expect: 100-continue để tối ưu hóa việc đẩy Stream cho Google
    client.DefaultRequestHeaders.ExpectContinue = false;
});

// Đăng ký Memory Cache cho caching
builder.Services.AddMemoryCache();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200") 
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); 
        });
});

builder.Services.AddDbContext<UTC_DATNContext>(options =>
    options.UseSqlServer(connectionString));
// Add services to the container.

builder.Services.AddControllers();

// Cấu hình file upload size limit
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Seed initial data (Roles và Admin User)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UTC_DATNContext>();
    await DbInitializer.InitializeAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.MapOpenApi();
}

app.UseCors("AllowAngular");

// Serve static files từ wwwroot (cho file uploads)
app.UseStaticFiles();

app.UseAuthentication(); 
app.UseAuthorization();

// app.UseHttpsRedirection(); // Tắt trong development, chỉ dùng HTTP



app.MapControllers();

app.Run();
