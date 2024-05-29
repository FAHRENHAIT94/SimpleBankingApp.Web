using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using SýmpleBankingApp.Web.Data;
using SýmpleBankingApp.Web.Services;
using System.Text;
using Polly;

// yapilandirici
var builder = WebApplication.CreateBuilder(args); 

// mvc hizemetlerini ekle
builder.Services.AddControllersWithViews();


// Entity Framework Core için veritabaný baðlamýný ekler.
builder.Services.AddDbContext<BankContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//JWT doðrulama hizmetini yapýlandýrýr.
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

////Add RabbitMQ ConnectionFactory
//builder.Services.AddSingleton<IConnectionFactory>(sp =>
//{
//    var factory = new ConnectionFactory()
//                  {
//                      HostName = builder.Configuration["RabbitMQ:HostName"],
//                      UserName = builder.Configuration["RabbitMQ:UserName"],
//                      Password = builder.Configuration["RabbitMQ:Password"]
//                  };
//return factory;
//});

//// RabbitMQService ve Worker'ý ekle
//builder.Services.AddSingleton<RabbitMQService>();
//builder.Services.AddHostedService<RabbitMQWorker>();

// Swagger belgelendirme
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference{
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
});

var app = builder.Build();



// Uygulama ortamýný yapýlandýrýn
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//Middleware'lerin eklenmesi ve sýralanmasý. 
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank API V1");
});

//MVC rotalarýný yapýlandýrýr. 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//Uygulama çalýþtýrýlýr.
app.Run();
