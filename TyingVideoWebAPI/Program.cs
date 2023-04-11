using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Text;
using TyingVideoWebAPI.DbContext;
using TyingVideoWebAPI.DTO;
using TyingVideoWebAPI.Model;
using TyingVideoWebAPI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ��־
builder.Services.AddLogging(logConfig =>
{
    //var logger = new LoggerConfiguration()
    //    .ReadFrom.Configuration(builder.Configuration.GetSection("Serilog"))
    //    .CreateLogger();

    Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(
        "Logs/log.txt", 
        rollingInterval: RollingInterval.Day, 
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 100000,
        rollOnFileSizeLimit: true)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

    logConfig.AddSerilog();
});

// ע�� DbContext ����
builder.Services.AddDbContext<MyIdentityDbContext>(opt =>
{
    string? connStr = builder.Configuration.GetSection("ConnStr").Value;
    MySqlServerVersion ver = new MySqlServerVersion(new Version(5, 17, 9));
    opt.UseMySql(connStr, ver);
});

#region Identity
// ע�� Identity ���
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<MyUser>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequiredLength = 6;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    opt.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
});
IdentityBuilder identityBuilder = new IdentityBuilder(typeof(MyUser), typeof(MyRole), builder.Services);
identityBuilder.AddEntityFrameworkStores<MyIdentityDbContext>()
    .AddDefaultTokenProviders()
    .AddRoleManager<RoleManager<MyRole>>()
    .AddUserManager<UserManager<MyUser>>();
#endregion

#region JWT
// ��ȡ���ò��Ұ� JWTSettings ����
builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JWT"));
// ע����֤��������У�� JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwtSettings = builder.Configuration.GetSection("JWT").Get<JWTSettings>();
        byte[] keyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
        var secretKey = new SymmetricSecurityKey(keyBytes);
        opt.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = secretKey
        };

        // WebSocket ��֧���Զ��屨��ͷ�������Ҫ�� JWT ͨ�� url �е� QueryString ����
        // Ȼ���ڷ������˵� OnMessageReceived �У��� QueryString �е� JWT ��������
        // Ȼ��ֵ�� context.Token�������м���ͻ�ȥ������
        //opt.Events = new JwtBearerEvents
        //{
        //    OnMessageReceived = context =>
        //    {
        //        var accessToken = context.Request.Query["access_token"];
        //        var path = context.Request.Path;
        //        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ChatRoomHub"))
        //        {
        //            context.Token = accessToken;
        //        }

        //        return Task.CompletedTask;
        //    }
        //};
    });
#endregion

#region Redis
//Redis �������ݿ����ӷ���ע��
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    string connStr = builder.Configuration.GetValue<string>("Redis");
    var connection = ConnectionMultiplexer.Connect(connStr);
    return connection;
});

builder.Services.AddSingleton<IServer>(sp =>
{
    string connStr = builder.Configuration.GetValue<string>("Redis");
    string curRedisServer = builder.Configuration.GetValue<string>("RedisConn");
    var connection = ConnectionMultiplexer.Connect(connStr);
    return connection.GetServer(curRedisServer);
});
builder.Services.AddSingleton(typeof(RedisHelper<>));
#endregion

builder.Host.UseSerilog();
var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
