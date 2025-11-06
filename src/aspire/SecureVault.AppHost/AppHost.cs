var builder = DistributedApplication.CreateBuilder(args);

var jwtKeyParameter = builder.AddParameter("jwtkey", secret: true);
var jwtRefreshKeyParameter = builder.AddParameter("jwtrefreshkey", secret: true);


var seq = builder.AddSeq("seq");

var postgresUsernameParameter = builder.AddParameter("postgresusername", secret: false);
var postgresPasswordParameter = builder.AddParameter("postgrespassword", secret: true);
var identityDb = builder.AddPostgres("identity-db")
                        .WithPassword(postgresPasswordParameter)
                        .WithUserName(postgresUsernameParameter)
                        .WithArgs("-c", "max_connections=500")
                        .WithDataVolume();

var vaultDb = builder.AddMongoDB("vault-db").WithDataVolume();


var redisPasswordParameter = builder.AddParameter("redispassword", secret: true);
var redisCache = builder.AddRedis("redis-cache")
                        .WithPassword(redisPasswordParameter);

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.2.1")
                        .WithBindMount("../../../prometheus", "/etc/prometheus", isReadOnly: true)
                        .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
                        .WithHttpEndpoint(targetPort: 9090, name: "http");

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithBindMount("../../../grafana/config", "/etc/grafana", isReadOnly: true)
                     .WithBindMount("../../../grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
                     .WithEnvironment("PROMETHEUS_ENDPOINT", prometheus.GetEndpoint("http"))
                     .WithHttpEndpoint(targetPort: 3000, name: "http");

builder.AddOpenTelemetryCollector("otelcollector")
       .WithConfig("../../../otelcollector/config.yaml")
       .WithAppForwarding()
       .WithEnvironment("PROMETHEUS_ENDPOINT", $"{prometheus.GetEndpoint("http")}/api/v1/otlp");

var identityApi = builder.AddProject<Projects.SecureVault_Identity_Api>("identity-api");
var vaultApi = builder.AddProject<Projects.SecureVault_Vault_Api>("vault-api");
var interactionApi = builder.AddProject<Projects.SecureVault_Interaction_Api>("interaction-api");
var apigateway = builder.AddProject<Projects.SecureVault_ApiGateway>("api-gateway");

var gatewayTunnel = builder.AddDevTunnel("public-gateway")
                           .WithReference(apigateway)
                           .WithAnonymousAccess();

var jwtIssuer = "https://identity.securevault.local";
var jwtAudience = apigateway.Resource.Name;


identityApi.WithReference(identityDb)
           .WithReference(redisCache)
           .WithReference(seq)
           .WithEnvironment("JwtSettings__Key", jwtKeyParameter)
           .WithEnvironment("JwtSettings__RefreshTokenKey", jwtRefreshKeyParameter)
           .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
           .WithEnvironment("JwtSettings__Audience", jwtAudience)
           .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"))
           .WithReplicas(3);

vaultApi.WithReference(vaultDb)
        .WithReference(seq)
        .WithEnvironment("JwtSettings__Key", jwtKeyParameter)
        .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
        .WithEnvironment("JwtSettings__Audience", jwtAudience)
        .WithEnvironment("MongoDbSettings__DatabaseName", "securevault_db")
        .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"));

interactionApi.WithReference(redisCache)
              .WithReference(seq)
              .WithEnvironment("JwtSettings__Key", jwtKeyParameter)
              .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
              .WithEnvironment("JwtSettings__Audience", jwtAudience)
              .WithEnvironment("CorsSettings__AllowedOrigin", jwtAudience)
              .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"));

apigateway.WithReference(redisCache)
          .WithReference(seq)
          .WithReference(identityApi)
          .WithReference(vaultApi)
          .WithReference(interactionApi)
          .WithEnvironment("JwtSettings__Key", jwtKeyParameter)
          .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
          .WithEnvironment("JwtSettings__Audience", jwtAudience)
          .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"));

builder.AddKubernetesEnvironment("k8s");

builder.Build().Run();
