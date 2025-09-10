namespace SecureVault.Shared.RabbitMQ.Options
{
    public class RabbitMqConsumerOptions
    {
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string RoutingKey { get; set; }
    }
}
