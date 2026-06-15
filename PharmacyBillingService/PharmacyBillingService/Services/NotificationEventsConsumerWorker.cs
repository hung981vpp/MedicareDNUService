using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Hubs;
using PharmacyBillingService.Messaging;
using PharmacyBillingService.Models;
using PharmacyBillingService.Security;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PharmacyBillingService.Services
{
    public sealed class NotificationEventsConsumerWorker : BackgroundService
    {
        private static readonly string[] RoutingKeys =
        {
            "appointment.created",
            "appointment.confirmed",
            "appointment.cancelled",
            "appointment.checked_in",
            "patient.checked_in",
            "appointment.started",
            "appointment.completed",
            "invoice.created",
            "invoice.paid",
            "prescription.created",
            "prescription.submitted",
            "prescription.approved",
            "prescription.dispensed",
            "medicine.dispensed",
            "medical_record.created",
            "medical_record.updated"
        };

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationEventsConsumerWorker> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public NotificationEventsConsumerWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<NotificationEventsConsumerWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    StartConsumer();
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ notification consumer failed. Retrying in 5 seconds.");
                    DisposeRabbitMq();
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private void StartConsumer()
        {
            var options = RabbitMqConnectionFactory.GetOptions(_configuration);
            _connection = RabbitMqConnectionFactory.CreateConnection(_configuration);
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.QueueDeclare(options.NotificationQueue, durable: true, exclusive: false, autoDelete: false);
            foreach (var routingKey in RoutingKeys)
            {
                _channel.QueueBind(options.NotificationQueue, options.Exchange, routingKey);
            }

            _channel.BasicQos(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += HandleMessageAsync;
            _channel.BasicConsume(options.NotificationQueue, autoAck: false, consumer);

            _logger.LogInformation("RabbitMQ notification consumer started on queue {Queue}.", options.NotificationQueue);
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            if (_channel is null) return;

            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                var resolver = scope.ServiceProvider.GetRequiredService<INotificationTargetResolver>();

                await ProcessEventAsync(context, hubContext, resolver, ea.RoutingKey, payload);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                _logger.LogError(ex, "Failed to consume notification event {RoutingKey}.", ea.RoutingKey);
            }

            await Task.CompletedTask;
        }

        private async Task ProcessEventAsync(
            PharmacyDbContext context,
            IHubContext<NotificationHub> hubContext,
            INotificationTargetResolver resolver,
            string routingKey,
            string payload)
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var eventType = ReadString(root, "eventType", "EventType", "eventName", "EventName") ?? routingKey;
            if (eventType == "medicine.dispensed") eventType = "prescription.dispensed";

            var eventCode = ReadString(root, "eventCode", "EventCode");
            var source = ReadString(root, "source", "Source") ?? "unknown";
            var data = TryGetProperty(root, "data", out var dataElement) || TryGetProperty(root, "Data", out dataElement)
                ? dataElement
                : root;

            var appointmentId = ReadInt(data, "appointmentId", "AppointmentId");
            var patientId = ReadInt(data, "patientId", "PatientId");
            var doctorId = ReadInt(data, "doctorId", "DoctorId");
            var prescriptionId = ReadInt(data, "prescriptionId", "PrescriptionId");
            var invoiceId = ReadInt(data, "invoiceId", "InvoiceId");

            _logger.LogInformation(
                "Notification event received. EventType={EventType}, RoutingKey={RoutingKey}, AppointmentId={AppointmentId}, PatientId={PatientId}, DoctorId={DoctorId}.",
                eventType,
                routingKey,
                appointmentId,
                patientId,
                doctorId);

            var eventKey = BuildEventKey(eventType, eventCode, appointmentId, prescriptionId, invoiceId, patientId, doctorId);
            if (await context.ProcessedEvents.AsNoTracking().AnyAsync(e => e.EventKey == eventKey && e.Status == "Success"))
            {
                _logger.LogInformation("Notification event {EventKey} already processed. Skipping duplicate delivery.", eventKey);
                return;
            }

            var candidates = await BuildNotificationsAsync(resolver, eventType, data, appointmentId, patientId, doctorId, prescriptionId, invoiceId);
            var notifications = candidates
                .GroupBy(n => new { n.UserId, n.Type, n.ReferenceId, n.Title })
                .Select(group => group.First())
                .ToList();

            if (!notifications.Any())
            {
                _logger.LogWarning("Notification event {EventType} resolved no recipients.", eventType);
                await MarkEventProcessedAsync(context, eventKey, eventType, source, "No recipients");
                return;
            }

            foreach (var notification in notifications)
            {
                notification.CreatedAt = DateTime.UtcNow;
                context.Notifications.Add(notification);
            }

            await MarkEventProcessedAsync(context, eventKey, eventType, source, null, saveChanges: false);
            await context.SaveChangesAsync();

            _logger.LogInformation("Notification event {EventType} created {Count} notifications.", eventType, notifications.Count);

            foreach (var notification in notifications)
            {
                try
                {
                    await hubContext.Clients.User(notification.UserId.ToString())
                        .SendAsync("ReceiveNotification", NotificationDto.FromEntity(notification));
                    _logger.LogInformation("SignalR notification sent to UserId {UserId} for event {EventType}.", notification.UserId, eventType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SignalR notification failed for UserId {UserId} and event {EventType}.", notification.UserId, eventType);
                }
            }
        }

        private async Task<List<Notification>> BuildNotificationsAsync(
            INotificationTargetResolver resolver,
            string eventType,
            JsonElement data,
            int? appointmentId,
            int? patientId,
            int? doctorId,
            int? prescriptionId,
            int? invoiceId)
        {
            return eventType switch
            {
                "appointment.created" => await AppointmentCreatedAsync(resolver, data, appointmentId, patientId, doctorId),
                "appointment.confirmed" => await AppointmentStatusAsync(resolver, data, eventType, "Lịch hẹn đã được xác nhận", appointmentId, patientId, doctorId),
                "appointment.cancelled" => await AppointmentStatusAsync(resolver, data, eventType, "Lịch hẹn đã bị hủy", appointmentId, patientId, doctorId),
                "appointment.checked_in" or "patient.checked_in" => await AppointmentCheckedInAsync(resolver, data, appointmentId, patientId, doctorId),
                "appointment.started" => await AppointmentStartedAsync(resolver, data, appointmentId, patientId, doctorId),
                "appointment.completed" => await AppointmentCompletedAsync(resolver, data, appointmentId, patientId, doctorId),
                "invoice.created" => await InvoiceCreatedAsync(resolver, data, invoiceId, patientId),
                "invoice.paid" => await InvoicePaidAsync(resolver, data, invoiceId, patientId),
                "prescription.created" or "prescription.submitted" => await PrescriptionCreatedAsync(resolver, data, prescriptionId, patientId),
                "prescription.approved" => await PrescriptionApprovedAsync(resolver, data, prescriptionId, patientId),
                "prescription.dispensed" => await PrescriptionDispensedAsync(resolver, data, prescriptionId, patientId, doctorId),
                "medical_record.created" or "medical_record.updated" => await MedicalRecordUpdatedAsync(resolver, data, patientId, doctorId),
                _ => new List<Notification>()
            };
        }

        private async Task<List<Notification>> AppointmentCreatedAsync(
            INotificationTargetResolver resolver,
            JsonElement data,
            int? appointmentId,
            int? patientId,
            int? doctorId)
        {
            var result = new List<Notification>();
            var content = AppointmentContent("appointment.created", ReadDateTime(data, "scheduledAt", "ScheduledAt"));
            await AddPatientAsync(result, resolver, patientId, "Đặt lịch thành công", content, "Appointment", appointmentId?.ToString(), PatientAppointmentUrl(appointmentId));
            await AddDoctorAsync(result, resolver, doctorId, "Có lịch hẹn mới đang chờ xác nhận", content, "Appointment", appointmentId?.ToString(), DoctorAppointmentUrl(appointmentId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Có lịch hẹn mới cần xử lý", content, "Appointment", appointmentId?.ToString(), NurseAppointmentUrl(appointmentId), AdminAppointmentUrl(appointmentId));
            return result;
        }

        private async Task<List<Notification>> AppointmentStatusAsync(
            INotificationTargetResolver resolver,
            JsonElement data,
            string eventType,
            string title,
            int? appointmentId,
            int? patientId,
            int? doctorId)
        {
            var result = new List<Notification>();
            var content = AppointmentContent(eventType, ReadDateTime(data, "scheduledAt", "ScheduledAt"));
            await AddPatientAsync(result, resolver, patientId, title, content, "Appointment", appointmentId?.ToString(), PatientAppointmentUrl(appointmentId));
            await AddDoctorAsync(result, resolver, doctorId, title, content, "Appointment", appointmentId?.ToString(), DoctorAppointmentUrl(appointmentId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, title, content, "Appointment", appointmentId?.ToString(), NurseAppointmentUrl(appointmentId), AdminAppointmentUrl(appointmentId));
            return result;
        }

        private async Task<List<Notification>> AppointmentCheckedInAsync(
            INotificationTargetResolver resolver,
            JsonElement data,
            int? appointmentId,
            int? patientId,
            int? doctorId)
        {
            var result = new List<Notification>();
            var content = "Bệnh nhân đã check-in và sẵn sàng vào hàng đợi khám.";
            await AddPatientAsync(result, resolver, patientId, "Bạn đã check-in thành công", "Bạn đã check-in thành công. Vui lòng theo dõi hàng chờ khám.", "Appointment", appointmentId?.ToString(), PatientAppointmentUrl(appointmentId));
            await AddDoctorAsync(result, resolver, doctorId, "Bệnh nhân đã check-in", content, "Appointment", appointmentId?.ToString(), DoctorQueueUrl(appointmentId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Bệnh nhân đã check-in", content, "Appointment", appointmentId?.ToString(), NurseQueueUrl(appointmentId), AdminAppointmentUrl(appointmentId));
            return result;
        }

        private async Task<List<Notification>> AppointmentStartedAsync(
            INotificationTargetResolver resolver,
            JsonElement data,
            int? appointmentId,
            int? patientId,
            int? doctorId)
        {
            var result = new List<Notification>();
            var content = "Lượt khám của bạn đã bắt đầu.";
            await AddPatientAsync(result, resolver, patientId, "Lượt khám đã bắt đầu", content, "Appointment", appointmentId?.ToString(), PatientAppointmentUrl(appointmentId));
            await AddDoctorAsync(result, resolver, doctorId, "Lượt khám đã bắt đầu", "Bạn đã bắt đầu lượt khám cho bệnh nhân.", "Appointment", appointmentId?.ToString(), DoctorQueueUrl(appointmentId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Lượt khám đã bắt đầu", "Bác sĩ đã bắt đầu lượt khám.", "Appointment", appointmentId?.ToString(), NurseQueueUrl(appointmentId), AdminAppointmentUrl(appointmentId));
            return result;
        }

        private async Task<List<Notification>> AppointmentCompletedAsync(
            INotificationTargetResolver resolver,
            JsonElement data,
            int? appointmentId,
            int? patientId,
            int? doctorId)
        {
            var result = new List<Notification>();
            await AddPatientAsync(result, resolver, patientId, "Lượt khám đã hoàn tất", "Lượt khám đã hoàn tất. Vui lòng kiểm tra hồ sơ, đơn thuốc hoặc viện phí nếu có.", "Appointment", appointmentId?.ToString(), PatientAppointmentUrl(appointmentId));
            await AddDoctorAsync(result, resolver, doctorId, "Lượt khám đã hoàn tất", "Lượt khám của bệnh nhân đã được hoàn tất.", "Appointment", appointmentId?.ToString(), DoctorAppointmentUrl(appointmentId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Lượt khám đã hoàn tất", "Một lượt khám vừa được hoàn tất.", "Appointment", appointmentId?.ToString(), NurseAppointmentUrl(appointmentId), AdminAppointmentUrl(appointmentId));
            return result;
        }

        private async Task<List<Notification>> InvoiceCreatedAsync(INotificationTargetResolver resolver, JsonElement data, int? invoiceId, int? patientId)
        {
            var result = new List<Notification>();
            var totalAmount = ReadDecimal(data, "totalAmount", "TotalAmount");
            var content = totalAmount is null
                ? "Bạn có hóa đơn viện phí mới cần kiểm tra."
                : $"Bạn có hóa đơn viện phí mới với tổng tiền {totalAmount.Value:n0} VND.";
            await AddPatientAsync(result, resolver, patientId, "Bạn có hóa đơn viện phí mới", content, "Billing", invoiceId?.ToString(), PatientBillUrl(invoiceId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Có hóa đơn mới cần theo dõi", "Có hóa đơn viện phí mới trong hệ thống.", "Billing", invoiceId?.ToString(), NurseBillUrl(invoiceId), AdminBillUrl(invoiceId));
            return result;
        }

        private async Task<List<Notification>> InvoicePaidAsync(INotificationTargetResolver resolver, JsonElement data, int? invoiceId, int? patientId)
        {
            var result = new List<Notification>();
            await AddPatientAsync(result, resolver, patientId, "Thanh toán thành công", "Hệ thống đã ghi nhận thanh toán hóa đơn viện phí.", "Billing", invoiceId?.ToString(), PatientBillUrl(invoiceId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Hóa đơn đã thanh toán", "Một hóa đơn viện phí vừa được thanh toán.", "Billing", invoiceId?.ToString(), NurseBillUrl(invoiceId), AdminBillUrl(invoiceId));
            return result;
        }

        private async Task<List<Notification>> PrescriptionCreatedAsync(INotificationTargetResolver resolver, JsonElement data, int? prescriptionId, int? patientId)
        {
            var result = new List<Notification>();
            await AddPatientAsync(result, resolver, patientId, "Bạn có đơn thuốc mới", "Bác sĩ đã tạo đơn thuốc mới cho bạn.", "Prescription", prescriptionId?.ToString(), PatientPrescriptionUrl(prescriptionId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Pharmacist }, "Có đơn thuốc mới cần chuẩn bị", "Có đơn thuốc mới cần kiểm tra và chuẩn bị thuốc.", "Prescription", prescriptionId?.ToString(), NursePrescriptionUrl(prescriptionId), NursePrescriptionUrl(prescriptionId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Có đơn thuốc mới cần xử lý", "Có đơn thuốc mới cần theo dõi.", "Prescription", prescriptionId?.ToString(), NursePrescriptionUrl(prescriptionId), AdminPrescriptionUrl(prescriptionId));
            return result;
        }

        private async Task<List<Notification>> PrescriptionApprovedAsync(INotificationTargetResolver resolver, JsonElement data, int? prescriptionId, int? patientId)
        {
            var result = new List<Notification>();
            await AddPatientAsync(result, resolver, patientId, "Đơn thuốc đã được duyệt", "Đơn thuốc của bạn đã được duyệt và sẵn sàng xử lý.", "Prescription", prescriptionId?.ToString(), PatientPrescriptionUrl(prescriptionId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Đơn thuốc đã được duyệt", "Một đơn thuốc đã được duyệt.", "Prescription", prescriptionId?.ToString(), NursePrescriptionUrl(prescriptionId), AdminPrescriptionUrl(prescriptionId));
            return result;
        }

        private async Task<List<Notification>> PrescriptionDispensedAsync(INotificationTargetResolver resolver, JsonElement data, int? prescriptionId, int? patientId, int? doctorId)
        {
            var result = new List<Notification>();
            await AddPatientAsync(result, resolver, patientId, "Đơn thuốc đã được phát", "Nhà thuốc đã phát thuốc theo đơn của bạn.", "Prescription", prescriptionId?.ToString(), PatientPrescriptionUrl(prescriptionId));
            await AddDoctorAsync(result, resolver, doctorId, "Đơn thuốc đã được phát", "Đơn thuốc của bệnh nhân đã được phát.", "Prescription", prescriptionId?.ToString(), DoctorAppointmentUrl(ReadInt(data, "appointmentId", "AppointmentId")));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Đơn thuốc đã được phát", "Một đơn thuốc đã được phát cho bệnh nhân.", "Prescription", prescriptionId?.ToString(), NursePrescriptionUrl(prescriptionId), AdminPrescriptionUrl(prescriptionId));
            return result;
        }

        private async Task<List<Notification>> MedicalRecordUpdatedAsync(INotificationTargetResolver resolver, JsonElement data, int? patientId, int? doctorId)
        {
            var recordId = ReadInt(data, "medicalRecordId", "MedicalRecordId", "recordId", "RecordId");
            var result = new List<Notification>();
            await AddPatientAsync(result, resolver, patientId, "Hồ sơ bệnh án đã được cập nhật", "Hồ sơ bệnh án của bạn vừa được cập nhật.", "MedicalRecord", recordId?.ToString(), PatientRecordUrl(recordId));
            await AddDoctorAsync(result, resolver, doctorId, "Hồ sơ bệnh án đã được cập nhật", "Một hồ sơ bệnh án vừa được cập nhật.", "MedicalRecord", recordId?.ToString(), DoctorRecordUrl(recordId));
            await AddRolesAsync(result, resolver, new[] { RoleConstants.Nurse, RoleConstants.Admin }, "Hồ sơ bệnh án đã được cập nhật", "Một hồ sơ bệnh án vừa được cập nhật.", "MedicalRecord", recordId?.ToString(), NurseAppointmentUrl(null), "/admin/reports");
            return result;
        }

        private async Task AddPatientAsync(List<Notification> result, INotificationTargetResolver resolver, int? patientId, string title, string content, string type, string? referenceId, string navigateUrl)
        {
            if (patientId is null)
            {
                _logger.LogWarning("Cannot create notification '{Title}' because PatientId is missing.", title);
                return;
            }
            var target = await resolver.ResolvePatientAsync(patientId.Value);
            if (target is null) return;
            result.Add(Build(target, title, content, type, referenceId, navigateUrl));
        }

        private async Task AddDoctorAsync(List<Notification> result, INotificationTargetResolver resolver, int? doctorId, string title, string content, string type, string? referenceId, string navigateUrl)
        {
            if (doctorId is null)
            {
                _logger.LogWarning("Cannot create notification '{Title}' because DoctorId is missing.", title);
                return;
            }
            var target = await resolver.ResolveDoctorAsync(doctorId.Value);
            if (target is null) return;
            result.Add(Build(target, title, content, type, referenceId, navigateUrl));
        }

        private static async Task AddRolesAsync(List<Notification> result, INotificationTargetResolver resolver, string[] roles, string title, string content, string type, string? referenceId, string nurseUrl, string adminUrl)
        {
            var targets = await resolver.ResolveRolesAsync(roles);
            foreach (var target in targets)
            {
                var navigateUrl = target.Role == RoleConstants.Admin ? adminUrl : nurseUrl;
                result.Add(Build(target, title, content, type, referenceId, navigateUrl));
            }
        }

        private static Notification Build(NotificationTarget target, string title, string content, string type, string? referenceId, string navigateUrl)
            => new()
            {
                UserId = target.UserId,
                Role = target.Role,
                Title = title,
                Content = content,
                Type = type,
                ReferenceId = referenceId,
                NavigateUrl = navigateUrl,
                IsRead = false
            };

        private async Task MarkEventProcessedAsync(PharmacyDbContext context, string eventKey, string eventType, string source, string? message, bool saveChanges = true)
        {
            if (await context.ProcessedEvents.AnyAsync(e => e.EventKey == eventKey)) return;

            context.ProcessedEvents.Add(new ProcessedEvent
            {
                EventKey = eventKey,
                EventType = eventType,
                Source = source,
                Status = "Success",
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                FailureReason = message
            });

            if (saveChanges) await context.SaveChangesAsync();
        }

        private static string BuildEventKey(string eventType, string? eventCode, int? appointmentId, int? prescriptionId, int? invoiceId, int? patientId, int? doctorId)
        {
            if (!string.IsNullOrWhiteSpace(eventCode)) return $"notification:{eventCode.Trim()}";
            return $"notification:{eventType}:{appointmentId?.ToString() ?? "-"}:{prescriptionId?.ToString() ?? "-"}:{invoiceId?.ToString() ?? "-"}:{patientId?.ToString() ?? "-"}:{doctorId?.ToString() ?? "-"}";
        }

        private static string AppointmentContent(string eventType, DateTime? scheduledAt)
        {
            var timeText = scheduledAt is null ? string.Empty : $" vào {scheduledAt.Value:dd/MM/yyyy HH:mm}";
            return eventType switch
            {
                "appointment.created" => $"Thông tin lịch hẹn{timeText} đã được ghi nhận.",
                "appointment.confirmed" => $"Lịch hẹn{timeText} đã được xác nhận.",
                "appointment.cancelled" => $"Lịch hẹn{timeText} đã bị hủy.",
                _ => $"Lịch hẹn{timeText} vừa được cập nhật."
            };
        }

        private static string PatientAppointmentUrl(int? id) => id is null ? "/patient/appointments" : $"/patient/appointments?appointmentId={id}";
        private static string DoctorAppointmentUrl(int? id) => id is null ? "/doctor/appointments" : $"/doctor/appointments?appointmentId={id}";
        private static string DoctorQueueUrl(int? id) => id is null ? "/doctor/queue" : $"/doctor/queue?appointmentId={id}";
        private static string DoctorRecordUrl(int? id) => id is null ? "/doctor/records" : $"/doctor/records?recordId={id}";
        private static string NurseAppointmentUrl(int? id) => id is null ? "/nurse/appointments" : $"/nurse/appointments?appointmentId={id}";
        private static string NurseQueueUrl(int? id) => id is null ? "/nurse/queue" : $"/nurse/queue?appointmentId={id}";
        private static string AdminAppointmentUrl(int? id) => id is null ? "/admin/appointments" : $"/admin/appointments?appointmentId={id}";
        private static string PatientBillUrl(int? id) => id is null ? "/patient/bills" : $"/patient/bills?invoiceId={id}";
        private static string NurseBillUrl(int? id) => id is null ? "/nurse/bills" : $"/nurse/bills?invoiceId={id}";
        private static string AdminBillUrl(int? id) => id is null ? "/admin/bills" : $"/admin/bills?invoiceId={id}";
        private static string PatientPrescriptionUrl(int? id) => id is null ? "/patient/prescriptions" : $"/patient/prescriptions?prescriptionId={id}";
        private static string NursePrescriptionUrl(int? id) => id is null ? "/nurse/prescriptions" : $"/nurse/prescriptions?prescriptionId={id}";
        private static string AdminPrescriptionUrl(int? id) => id is null ? "/admin/prescriptions" : $"/admin/prescriptions?prescriptionId={id}";
        private static string PatientRecordUrl(int? id) => id is null ? "/patient/records" : $"/patient/records?recordId={id}";

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value)) return true;
            value = default;
            return false;
        }

        private static string? ReadString(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }

        private static int? ReadInt(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetProperty(element, name, out var value)) continue;
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number)) return number;
            }

            return null;
        }

        private static decimal? ReadDecimal(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetProperty(element, name, out var value)) continue;
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out number)) return number;
            }

            return null;
        }

        private static DateTime? ReadDateTime(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetProperty(element, name, out var value)) continue;
                if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var dateTime)) return dateTime;
            }

            return null;
        }

        public override void Dispose()
        {
            DisposeRabbitMq();
            base.Dispose();
        }

        private void DisposeRabbitMq()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }
    }
}
