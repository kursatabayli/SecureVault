using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace SecureVault.ApiGateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
            builder.Services.AddOcelot(builder.Configuration);
            builder.Services.AddAuthorization();


            var app = builder.Build();

            await app.UseOcelot();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.Run();
        }
    }
}
