using MagdyPOS.Data;
using MagdyPOS.Infrastructure;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MagdyPOS;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=magdypos.db";

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IClaimsTransformation, UserTypeClaimsTransformation>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Login/Login";
            options.AccessDeniedPath = "/Home/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        var jwtSection = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var jwtIssuer = jwtSection["Issuer"] ?? "MagdyPOS";
        var jwtAudience = jwtSection["Audience"] ?? "MagdyPOS.Mobile";

        builder.Services
            .AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MobileApp", policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });

        builder.Services.AddAuthorization();
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseCors("MobileApp");

        app.UseAuthentication();
        app.UseAuthorization();

        // Ensure schema is up-to-date (SQLite file can be created/migrated automatically).
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync().ConfigureAwait(false);
        }

        await IdentityDataSeeder.SeedAsync(app.Services, app.Configuration, app.Logger).ConfigureAwait(false);

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await app.RunAsync().ConfigureAwait(false);
    }
}
