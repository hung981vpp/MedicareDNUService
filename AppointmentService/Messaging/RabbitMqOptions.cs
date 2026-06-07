namespace AppointmentService.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "clinic.events";
    public string AppointmentQueue { get; init; } = "n2.appointment-events";
}
