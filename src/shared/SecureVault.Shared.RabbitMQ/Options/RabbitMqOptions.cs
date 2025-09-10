namespace SecureVault.Shared.RabbitMQ.Options
{
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";

        public string Uri { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
    }

}
