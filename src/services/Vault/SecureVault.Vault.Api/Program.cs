using SecureVault.Vault.Api.Extensions;
using SecureVault.Vault.Application.Services;
namespace SecureVault.Vault.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddApplicationServices();
            builder.Services.AddDbContextConfiguration(builder.Configuration);
            builder.Services.RegisterServices();
            builder.Services.AddOpenApi();
            builder.Services.AddLocalization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
