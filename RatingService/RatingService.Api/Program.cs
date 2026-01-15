using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RatingService.Application.Services;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using RatingService.Infrastructure.Context;
using RatingService.Infrastructure.Interfaces.Base;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Infrastructure.Repositories.Base;
using RatingService.Infrastructure.Repositories.Entities;
using RatingService.Shared.Dtos.Jwt;
using RatingService.Shared.Protos.GrpcOrderService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ShopOwner", policy => policy.RequireClaim(ClaimTypes.Role, "ShopOwner"));
    options.AddPolicy("ShopCustomer", policy => policy.RequireClaim(ClaimTypes.Role, "ShopCustomer"));
    options.AddPolicy("SuperAdmin", policy => policy.RequireClaim(ClaimTypes.Role, "SuperAdmin"));
    options.AddPolicy("SuperAdminOrShopOwner", policy => policy.RequireClaim(ClaimTypes.Role, "ShopOwner", "SuperAdmin"));
});


// FOR CLIENTS PROTOS -- in the future -- Shop and Product mb
// builder.Services.AddGrpcClient<ProductService.ProductServiceClient>(options =>
// {
//     options.Address = new Uri(builder.Configuration["gRPC:ProductService"]); 
// });
// builder.Services.AddGrpcClient<ShopService.ShopServiceClient>(options =>
// {
//     options.Address = new Uri(builder.Configuration["gRPC:ShopService"]); 
// });

builder.Services.AddGrpcClient<OrderService.OrderServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["gRPC:OrderService"]); 
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 5140, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
    

    options.Listen(IPAddress.Any, 5006, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2; 
    });
});



//Jwt
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        RoleClaimType = ClaimTypes.Role,
        ValidateActor = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.HttpContext.Request.Cookies["accessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = async context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                var httpContext = context.HttpContext;
                var accessToken =
                    httpContext.Request.Cookies["accessToken"];

                var refreshToken =
                    httpContext.Request.Cookies["refreshToken"];

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshEndpoint =
                        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/v1/Auth/Refresh";
                    var client = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();

                    var response =
                        await client.PostAsJsonAsync(refreshEndpoint, new TokenDto(accessToken, refreshToken));

                    if (response.IsSuccessStatusCode)
                    {
                        var newTokens = await response.Content.ReadFromJsonAsync<RefreshDto>();
                        if (newTokens != null)
                        {
                            httpContext.Response.Cookies.Append("accessToken", newTokens.AccessToken,
                                new CookieOptions { HttpOnly = true });
                            httpContext.Response.Cookies.Append("refreshToken", newTokens.RefreshToken,
                                new CookieOptions { HttpOnly = true });

                            httpContext.Request.Headers["Authorization"] = $"Bearer {newTokens.AccessToken}";

                            var newToken = new JwtSecurityToken(newTokens.AccessToken);
                            var principal = new ClaimsPrincipal(new ClaimsIdentity(newToken.Claims, "jwt"));
                            

                            context.Principal = principal;
                            context.Success();
                        }
                    }
                }
            }
        }
    };
});

//Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3001", "http://localhost:3000", "http://localhost:5040",
                "http://localhost")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// builder.Services.AddGrpc();
builder.Services.AddControllers();

//Cookie
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddHttpClient("MyClient");


builder.Services.AddApiVersioning(options => { options.ReportApiVersions = true; }
).AddApiExplorer(
    options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddAutoMapper(cfg => { }, 
    typeof(RatingService.Core.Profiles.ReviewProfile),
    typeof(RatingService.Core.Profiles.QuestionsAndAnswersProfile));

builder.Services.AddGrpc();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IQuestionsAndAnswersService, QuestionsAndAnswersService>();
builder.Services.AddScoped<IReviewImageService, ReviewImageService>();
builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
builder.Services.AddScoped<IProductQuestionRepository, ProductQuestionRepository>();
builder.Services.AddScoped<IProductAnswerRepository, ProductAnswerRepository>();
builder.Services.AddScoped<IReviewAggregateRepository, ReviewAggregateRepository>();



builder.Services.AddDbContext<RatingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("RatingService.Infrastructure"))
);



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCookiePolicy();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<RatingService.Infrastructure.gRPC.GrpcRatingService>();
app.MapControllers();


app.Run();

