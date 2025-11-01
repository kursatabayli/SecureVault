var builder = DistributedApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["JwtSettings:Key"];
var jwtRefreshKey = builder.Configuration["JwtSettings:RefreshTokenKey"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtRefreshKey))
{
  throw new InvalidOperationException("JwtSettings 'secrets.json' dosyasında bulunamadı veya eksik.");
}


var seqPassword = builder.AddParameter("seqpassword", secret: true);
var seq = builder.AddContainer("seq", "datalust/seq", "latest")
                  .WithEnvironment("ACCEPT_EULA", "Y")
                  .WithEnvironment("SEQ_FIRSTRUN_ADMINPASSWORD", seqPassword)
                  .WithHttpEndpoint(port: 5341, targetPort: 80, name: "http");

var postgresUsernameParameter = builder.AddParameter("postgresusername", secret: false);
var postgresPasswordParameter = builder.AddParameter("postgrespassword", secret: true);
var identityDb = builder.AddPostgres("identity-db").WithPassword(postgresPasswordParameter).WithUserName(postgresUsernameParameter);
var redisCache = builder.AddRedis("redis-cache");
var vaultDb = builder.AddMongoDB("vault-db");

var identityApi = builder.AddProject<Projects.SecureVault_Identity_Api>("identityapi");
var vaultApi = builder.AddProject<Projects.SecureVault_Vault_Api>("vaultapi");
var interactionApi = builder.AddProject<Projects.SecureVault_Interaction_Api>("interactionapi");
var apigateway = builder.AddProject<Projects.SecureVault_ApiGateway>("apigateway").WithExternalHttpEndpoints();


var jwtIssuer = identityApi.GetEndpoint("https");
var jwtAudience = apigateway.GetEndpoint("https");

identityApi.WithReference(identityDb)
           .WithReference(redisCache)
           .WithEnvironment("JwtSettings__Key", jwtKey)
           .WithEnvironment("JwtSettings__RefreshTokenKey", jwtRefreshKey)
           .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
           .WithEnvironment("JwtSettings__Audience", jwtAudience)
           .WithEnvironment("Serilog__WriteTo__0__Args__serverUrl", seq.GetEndpoint("http"));

vaultApi.WithReference(vaultDb)
        .WithEnvironment("JwtSettings__Key", jwtKey)
        .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
        .WithEnvironment("JwtSettings__Audience", jwtAudience)
        .WithEnvironment("Serilog__WriteTo__0__Args__serverUrl", seq.GetEndpoint("http"))
        .WithEnvironment("MongoDbSettings__DatabaseName", "securevault_db");

interactionApi.WithEnvironment("JwtSettings__Key", jwtKey)
              .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
              .WithEnvironment("JwtSettings__Audience", jwtAudience)
              .WithEnvironment("Serilog__WriteTo__0__Args__serverUrl", seq.GetEndpoint("http"))
              .WithEnvironment("CorsSettings__AllowedOrigin", apigateway.GetEndpoint("https"));


apigateway.WithReference(redisCache)
          .WithReference(identityApi)
          .WithReference(vaultApi)
          .WithReference(interactionApi)
          .WithEnvironment("JwtSettings__Key", jwtKey)
          .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
          .WithEnvironment("JwtSettings__Audience", jwtAudience)
          .WithEnvironment("Serilog__WriteTo__0__Args__serverUrl", seq.GetEndpoint("http"));

builder.Build().Run();
