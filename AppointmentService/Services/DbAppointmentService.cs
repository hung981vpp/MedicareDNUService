using AppointmentService.Common;
using AppointmentService.Constants;
using AppointmentService.Data;
using AppointmentService.Dtos.Appointments;
using AppointmentService.Dtos.DoctorSchedules;
using AppointmentService.Dtos.Doctors;
using AppointmentService.Dtos.Integration;
using AppointmentService.Dtos.Specialties;
using AppointmentService.Dtos.WaitingQueue;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Services;

public sealed class DbAppointmentService : IAppointmentService
{
    private static readonly List<AppointmentEventDto> IntegrationEvents = [];
    private static readonly object IntegrationEventsLock = new();

    private readonly AppointmentDbContext _dbContext;

    public DbAppointmentService(AppointmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<AppointmentDto> GetAppointments()
    {
        return _dbContext.Appointments
            .AsNoTracking()
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotTime)
            .ToArray()
            .Select(ToAppointmentDto)
            .ToArray();
    }

    public ServiceResult<AppointmentDto> GetAppointmentById(int id)
    {
        var appointment = _dbContext.Appointments.AsNoTracking().FirstOrDefault(x => x.Id == id);
        return appointment is null
            ? ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound)
            : ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Appointment retrieved successfully");
    }

    public IReadOnlyList<AppointmentDto> GetAppointmentsByPatient(int patientId)
    {
        return _dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotTime)
            .ToArray()
            .Select(ToAppointmentDto)
            .ToArray();
    }

    public IReadOnlyList<AppointmentDto> GetAppointmentsByDoctor(int doctorId)
    {
        return _dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotTime)
            .ToArray()
            .Select(ToAppointmentDto)
            .ToArray();
    }

    public IReadOnlyList<AppointmentDto> GetConfirmedAppointments()
    {
        return _dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.Status == AppointmentStatus.Confirmed ||
                        x.Status == AppointmentStatus.InProgress ||
                        x.Status == AppointmentStatus.Completed)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotTime)
            .ToArray()
            .Select(ToAppointmentDto)
            .ToArray();
    }

    public ServiceResult<AppointmentDto> CreateAppointment(CreateAppointmentRequest request)
    {
        var validation = ValidateCreateAppointmentRequest(request);
        if (validation is not null)
        {
            return ServiceResult<AppointmentDto>.Fail(validation, ServiceErrorType.BadRequest);
        }

        var doctor = _dbContext.Doctors.AsNoTracking().FirstOrDefault(x => x.Id == request.DoctorId);
        if (doctor is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Doctor not found", ServiceErrorType.BadRequest);
        }

        if (!doctor.IsActive)
        {
            return ServiceResult<AppointmentDto>.Fail("Doctor is not active", ServiceErrorType.BadRequest);
        }

        var schedule = GetScheduleContainingSlot(request.DoctorId, request.AppointmentDate, request.SlotTime);
        if (schedule is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Doctor is not available at this time", ServiceErrorType.BadRequest);
        }

        if (!IsSlotAligned(schedule, request.SlotTime))
        {
            return ServiceResult<AppointmentDto>.Fail("Slot time does not match the schedule duration", ServiceErrorType.BadRequest);
        }

        if (_dbContext.Appointments.Any(x =>
            x.DoctorId == request.DoctorId &&
            x.AppointmentDate == request.AppointmentDate &&
            x.SlotTime == request.SlotTime &&
            x.Status != AppointmentStatus.Cancelled))
        {
            return ServiceResult<AppointmentDto>.Fail("Slot is already booked", ServiceErrorType.BadRequest);
        }

        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            PatientNameSnapshot = request.PatientNameSnapshot.Trim(),
            PatientPhoneSnapshot = request.PatientPhoneSnapshot.Trim(),
            DoctorId = request.DoctorId,
            AppointmentDate = request.AppointmentDate,
            SlotTime = request.SlotTime,
            Reason = request.Reason?.Trim() ?? string.Empty,
            Status = AppointmentStatus.Pending,
            QueueNumber = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _dbContext.Appointments.Add(appointment);

        try
        {
            _dbContext.SaveChanges();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<AppointmentDto>.Fail("Slot is already booked", ServiceErrorType.BadRequest);
        }

        AddIntegrationEvent("AppointmentCreated", appointment);
        return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Appointment created successfully");
    }

    public ServiceResult<AppointmentDto> ConfirmAppointment(int id)
    {
        const int maxRetries = 10;
        int retries = 0;

        while (retries < maxRetries)
        {
            using var transaction = _dbContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == id);
                if (appointment is null)
                {
                    return ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
                }

                if (appointment.Status != AppointmentStatus.Pending)
                {
                    return ServiceResult<AppointmentDto>.Fail("Only pending appointments can be confirmed", ServiceErrorType.BadRequest);
                }

                var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.AppointmentId == id);
                int assignedQueueNumber = 0;

                if (queueEntry is null)
                {
                    var nextQueueNumber = _dbContext.WaitingQueues.Any(x => x.QueueDate == appointment.AppointmentDate)
                        ? _dbContext.WaitingQueues
                            .Where(x => x.QueueDate == appointment.AppointmentDate)
                            .Max(x => x.QueueNumber) + 1
                        : 1;

                    queueEntry = new QueueEntry
                    {
                        AppointmentId = appointment.Id,
                        PatientId = appointment.PatientId,
                        DoctorId = appointment.DoctorId,
                        QueueDate = appointment.AppointmentDate,
                        QueueNumber = nextQueueNumber,
                        Status = QueueStatus.Waiting,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.WaitingQueues.Add(queueEntry);
                    appointment.QueueNumber = nextQueueNumber;
                    assignedQueueNumber = nextQueueNumber;
                }
                else
                {
                    assignedQueueNumber = queueEntry.QueueNumber;
                }

                appointment.Status = AppointmentStatus.Confirmed;
                appointment.UpdatedAt = DateTime.UtcNow;

                var doctor = _dbContext.Doctors.AsNoTracking().First(d => d.Id == appointment.DoctorId);
                var specialty = _dbContext.Specialties.AsNoTracking().First(s => s.Id == doctor.SpecialtyId);

                var outboxEvent = new OutboxEvent
                {
                    EventType = "appointment.confirmed",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        eventCode = "", // Will be updated
                        eventType = "appointment.confirmed",
                        source = "AppointmentService",
                        occurredAt = DateTime.UtcNow,
                        data = new
                        {
                            appointmentId = appointment.Id,
                            patientId = appointment.PatientId,
                            patientName = appointment.PatientNameSnapshot,
                            phoneNumber = appointment.PatientPhoneSnapshot,
                            doctorId = appointment.DoctorId,
                            doctorName = doctor.FullName,
                            specialtyId = specialty.Id,
                            specialtyName = specialty.Name,
                            scheduledAt = new DateTime(appointment.AppointmentDate.Year, appointment.AppointmentDate.Month, appointment.AppointmentDate.Day, appointment.SlotTime.Hour, appointment.SlotTime.Minute, appointment.SlotTime.Second, DateTimeKind.Utc),
                            queueNumber = assignedQueueNumber,
                            status = "Confirmed"
                        }
                    }),
                    Status = "Pending"
                };

                _dbContext.OutboxEvents.Add(outboxEvent);
                _dbContext.SaveChanges();

                var eventCode = $"N1EV{outboxEvent.Id:D3}";
                outboxEvent.EventCode = eventCode;
                
                // Update the eventCode in the payload
                outboxEvent.Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    eventCode = eventCode,
                    eventType = "appointment.confirmed",
                    source = "AppointmentService",
                    occurredAt = DateTime.UtcNow,
                    data = new
                    {
                        appointmentId = appointment.Id,
                        patientId = appointment.PatientId,
                        patientName = appointment.PatientNameSnapshot,
                        phoneNumber = appointment.PatientPhoneSnapshot,
                        doctorId = appointment.DoctorId,
                        doctorName = doctor.FullName,
                        specialtyId = specialty.Id,
                        specialtyName = specialty.Name,
                        scheduledAt = new DateTime(appointment.AppointmentDate.Year, appointment.AppointmentDate.Month, appointment.AppointmentDate.Day, appointment.SlotTime.Hour, appointment.SlotTime.Minute, appointment.SlotTime.Second, DateTimeKind.Utc),
                        queueNumber = assignedQueueNumber,
                        status = "Confirmed"
                    }
                });

                _dbContext.SaveChanges();
                transaction.Commit();

                AddIntegrationEvent("AppointmentConfirmed", appointment);
                return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Appointment confirmed successfully");
            }
            catch (DbUpdateException)
            {
                transaction.Rollback();
                _dbContext.ChangeTracker.Clear();
                retries++;
                if (retries >= maxRetries)
                {
                    return ServiceResult<AppointmentDto>.Fail("Failed to confirm appointment due to heavy traffic on waiting queue queueNumber.", ServiceErrorType.BadRequest);
                }
                System.Threading.Thread.Sleep(50);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        return ServiceResult<AppointmentDto>.Fail("Queue number conflict", ServiceErrorType.BadRequest);
    }

    public ServiceResult<AppointmentDto> CheckInAppointment(int id)
    {
        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == id);
        if (appointment is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return ServiceResult<AppointmentDto>.Fail("Only confirmed appointments can be checked in", ServiceErrorType.BadRequest);
        }

        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.AppointmentId == id);
        if (queueEntry is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Waiting queue entry not found", ServiceErrorType.BadRequest);
        }

        AddPatientCheckedInEvent(appointment, queueEntry, "CheckedIn");

        return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Patient checked in successfully");
    }

    public ServiceResult<AppointmentDto> StartAppointment(int id)
    {
        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == id);
        if (appointment is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return ServiceResult<AppointmentDto>.Fail("Only confirmed appointments can be started", ServiceErrorType.BadRequest);
        }

        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.AppointmentId == id);
        if (queueEntry is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Waiting queue entry not found", ServiceErrorType.BadRequest);
        }

        appointment.Status = AppointmentStatus.InProgress;
        appointment.UpdatedAt = DateTime.UtcNow;
        queueEntry.Status = QueueStatus.InProgress;
        _dbContext.SaveChanges();

        AddPatientCheckedInEvent(appointment, queueEntry, "InProgress");
        return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Appointment started successfully");
    }

    public ServiceResult<AppointmentDto> CancelAppointment(int id)
    {
        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == id);
        if (appointment is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status is AppointmentStatus.Completed)
        {
            return ServiceResult<AppointmentDto>.Fail("Completed appointment cannot be cancelled", ServiceErrorType.BadRequest);
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTime.UtcNow;

        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.AppointmentId == id);
        if (queueEntry is not null)
        {
            queueEntry.Status = QueueStatus.Cancelled;
        }

        _dbContext.SaveChanges();

        AddIntegrationEvent("AppointmentCancelled", appointment);
        return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Appointment cancelled successfully");
    }

    public ServiceResult<AppointmentDto> CompleteAppointment(int id)
    {
        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == id);
        if (appointment is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status != AppointmentStatus.InProgress)
        {
            return ServiceResult<AppointmentDto>.Fail("Only in-progress appointments can be completed", ServiceErrorType.BadRequest);
        }

        appointment.Status = AppointmentStatus.Completed;
        appointment.UpdatedAt = DateTime.UtcNow;

        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.AppointmentId == id);
        if (queueEntry is not null)
        {
            queueEntry.Status = QueueStatus.Done;
        }

        _dbContext.SaveChanges();

        AddIntegrationEvent("AppointmentCompleted", appointment);
        return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), "Appointment completed successfully");
    }

    public ServiceResult<AppointmentForMedicalDto> GetMedicalInfo(int appointmentId)
    {
        var appointment = _dbContext.Appointments.AsNoTracking().FirstOrDefault(x => x.Id == appointmentId);
        if (appointment is null)
        {
            return ServiceResult<AppointmentForMedicalDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return ServiceResult<AppointmentForMedicalDto>.Fail("Cancelled appointment cannot be used for medical records", ServiceErrorType.BadRequest);
        }

        if (appointment.Status is not (AppointmentStatus.Confirmed or AppointmentStatus.InProgress or AppointmentStatus.Completed))
        {
            return ServiceResult<AppointmentForMedicalDto>.Fail("Appointment must be confirmed before creating a medical record", ServiceErrorType.BadRequest);
        }

        return ServiceResult<AppointmentForMedicalDto>.Ok(ToMedicalDto(appointment), "Medical appointment information retrieved successfully");
    }

    public ServiceResult<BillingInfoDto> GetBillingInfo(int appointmentId)
    {
        var appointment = _dbContext.Appointments.AsNoTracking().FirstOrDefault(x => x.Id == appointmentId);
        if (appointment is null)
        {
            return ServiceResult<BillingInfoDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status is not (AppointmentStatus.Confirmed or AppointmentStatus.InProgress or AppointmentStatus.Completed))
        {
            return ServiceResult<BillingInfoDto>.Fail("Billing info is available only for confirmed or completed appointments", ServiceErrorType.BadRequest);
        }

        return ServiceResult<BillingInfoDto>.Ok(ToBillingDto(appointment), "Billing appointment information retrieved successfully");
    }

    public IReadOnlyList<DoctorDto> GetDoctors()
    {
        return _dbContext.Doctors
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToArray()
            .Select(ToDoctorDto)
            .ToArray();
    }

    public IReadOnlyList<DoctorDto> GetDoctorsBySpecialty(int specialtyId)
    {
        return _dbContext.Doctors
            .AsNoTracking()
            .Where(x => x.SpecialtyId == specialtyId)
            .OrderBy(x => x.FullName)
            .ToArray()
            .Select(ToDoctorDto)
            .ToArray();
    }

    public ServiceResult<IReadOnlyList<TimeOnly>> GetAvailableSlots(int doctorId, DateOnly date)
    {
        var doctor = _dbContext.Doctors.AsNoTracking().FirstOrDefault(x => x.Id == doctorId);
        if (doctor is null)
        {
            return ServiceResult<IReadOnlyList<TimeOnly>>.Fail("Doctor not found", ServiceErrorType.NotFound);
        }

        if (!doctor.IsActive)
        {
            return ServiceResult<IReadOnlyList<TimeOnly>>.Fail("Doctor is not active", ServiceErrorType.BadRequest);
        }

        var schedules = _dbContext.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.WorkDate == date && x.IsAvailable)
            .OrderBy(x => x.StartTime)
            .ToArray();

        var bookedSlots = _dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId &&
                        x.AppointmentDate == date &&
                        x.Status != AppointmentStatus.Cancelled)
            .Select(x => x.SlotTime)
            .ToHashSet();

        var availableSlots = new List<TimeOnly>();
        foreach (var schedule in schedules)
        {
            var slot = schedule.StartTime;
            while (slot < schedule.EndTime)
            {
                if (!bookedSlots.Contains(slot))
                {
                    availableSlots.Add(slot);
                }

                slot = slot.AddMinutes(schedule.SlotDurationMinutes);
            }
        }

        return ServiceResult<IReadOnlyList<TimeOnly>>.Ok(availableSlots, "Available slots retrieved successfully");
    }

    public ServiceResult<DoctorDto> GetDoctorById(int id)
    {
        var doctor = _dbContext.Doctors.AsNoTracking().FirstOrDefault(x => x.Id == id);
        return doctor is null
            ? ServiceResult<DoctorDto>.Fail("Doctor not found", ServiceErrorType.NotFound)
            : ServiceResult<DoctorDto>.Ok(ToDoctorDto(doctor), "Doctor retrieved successfully");
    }

    public ServiceResult<DoctorDto> CreateDoctor(CreateDoctorRequest request)
    {
        if (!_dbContext.Specialties.Any(x => x.Id == request.SpecialtyId))
        {
            return ServiceResult<DoctorDto>.Fail("Specialty not found", ServiceErrorType.BadRequest);
        }

        var fullName = GetDoctorFullName(request.FullName, request.DoctorName);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ServiceResult<DoctorDto>.Fail("Doctor full name is required", ServiceErrorType.BadRequest);
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            _dbContext.Doctors.Any(x => x.Email == request.Email.Trim()))
        {
            return ServiceResult<DoctorDto>.Fail("Doctor email already exists", ServiceErrorType.BadRequest);
        }

        var doctor = new Doctor
        {
            UserId = request.UserId,
            FullName = fullName,
            SpecialtyId = request.SpecialtyId,
            Degree = request.Degree.Trim(),
            ExperienceYears = request.ExperienceYears,
            ExamFee = request.ExamFee,
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            Gender = request.Gender.Trim(),
            DateOfBirth = request.DateOfBirth,
            Description = request.Description.Trim(),
            AvatarUrl = request.AvatarUrl.Trim(),
            RoomNumber = request.RoomNumber.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _dbContext.Doctors.Add(doctor);
        _dbContext.SaveChanges();

        return ServiceResult<DoctorDto>.Ok(ToDoctorDto(doctor), "Doctor created successfully");
    }

    public ServiceResult<DoctorDto> UpdateDoctor(int id, UpdateDoctorRequest request)
    {
        var doctor = _dbContext.Doctors.FirstOrDefault(x => x.Id == id);
        if (doctor is null)
        {
            return ServiceResult<DoctorDto>.Fail("Doctor not found", ServiceErrorType.NotFound);
        }

        if (!_dbContext.Specialties.Any(x => x.Id == request.SpecialtyId))
        {
            return ServiceResult<DoctorDto>.Fail("Specialty not found", ServiceErrorType.BadRequest);
        }

        var fullName = GetDoctorFullName(request.FullName, request.DoctorName);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ServiceResult<DoctorDto>.Fail("Doctor full name is required", ServiceErrorType.BadRequest);
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            _dbContext.Doctors.Any(x => x.Id != id && x.Email == request.Email.Trim()))
        {
            return ServiceResult<DoctorDto>.Fail("Doctor email already exists", ServiceErrorType.BadRequest);
        }

        doctor.UserId = request.UserId;
        doctor.FullName = fullName;
        doctor.SpecialtyId = request.SpecialtyId;
        doctor.Degree = request.Degree.Trim();
        doctor.ExperienceYears = request.ExperienceYears;
        doctor.ExamFee = request.ExamFee;
        doctor.Phone = request.Phone.Trim();
        doctor.Email = request.Email.Trim();
        doctor.Gender = request.Gender.Trim();
        doctor.DateOfBirth = request.DateOfBirth;
        doctor.Description = request.Description.Trim();
        doctor.AvatarUrl = request.AvatarUrl.Trim();
        doctor.RoomNumber = request.RoomNumber.Trim();
        doctor.IsActive = request.IsActive;
        doctor.UpdatedAt = DateTime.UtcNow;
        _dbContext.SaveChanges();

        return ServiceResult<DoctorDto>.Ok(ToDoctorDto(doctor), "Doctor updated successfully");
    }

    public ServiceResult<bool> DeleteDoctor(int id)
    {
        var doctor = _dbContext.Doctors.FirstOrDefault(x => x.Id == id);
        if (doctor is null)
        {
            return ServiceResult<bool>.Fail("Doctor not found", ServiceErrorType.NotFound);
        }

        if (_dbContext.Appointments.Any(x => x.DoctorId == id))
        {
            return ServiceResult<bool>.Fail("Doctor has appointments and cannot be deleted", ServiceErrorType.BadRequest);
        }

        if (_dbContext.DoctorSchedules.Any(x => x.DoctorId == id))
        {
            return ServiceResult<bool>.Fail("Doctor has schedules and cannot be deleted", ServiceErrorType.BadRequest);
        }

        _dbContext.Doctors.Remove(doctor);
        _dbContext.SaveChanges();

        return ServiceResult<bool>.Ok(true, "Doctor deleted successfully");
    }

    public IReadOnlyList<SpecialtyDto> GetSpecialties()
    {
        return _dbContext.Specialties
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new SpecialtyDto { SpecialtyId = x.Id, SpecialtyName = x.Name })
            .ToArray();
    }

    public ServiceResult<SpecialtyDto> GetSpecialtyById(int id)
    {
        var specialty = _dbContext.Specialties.AsNoTracking().FirstOrDefault(x => x.Id == id);
        return specialty is null
            ? ServiceResult<SpecialtyDto>.Fail("Specialty not found", ServiceErrorType.NotFound)
            : ServiceResult<SpecialtyDto>.Ok(ToSpecialtyDto(specialty), "Specialty retrieved successfully");
    }

    public ServiceResult<SpecialtyDto> CreateSpecialty(CreateSpecialtyRequest request)
    {
        var name = request.SpecialtyName.Trim();
        if (_dbContext.Specialties.Any(x => x.Name == name))
        {
            return ServiceResult<SpecialtyDto>.Fail("Specialty name already exists", ServiceErrorType.BadRequest);
        }

        var specialty = new Specialty { Name = name };
        _dbContext.Specialties.Add(specialty);
        _dbContext.SaveChanges();

        return ServiceResult<SpecialtyDto>.Ok(ToSpecialtyDto(specialty), "Specialty created successfully");
    }

    public ServiceResult<SpecialtyDto> UpdateSpecialty(int id, UpdateSpecialtyRequest request)
    {
        var specialty = _dbContext.Specialties.FirstOrDefault(x => x.Id == id);
        if (specialty is null)
        {
            return ServiceResult<SpecialtyDto>.Fail("Specialty not found", ServiceErrorType.NotFound);
        }

        var name = request.SpecialtyName.Trim();
        if (_dbContext.Specialties.Any(x => x.Id != id && x.Name == name))
        {
            return ServiceResult<SpecialtyDto>.Fail("Specialty name already exists", ServiceErrorType.BadRequest);
        }

        specialty.Name = name;
        _dbContext.SaveChanges();

        return ServiceResult<SpecialtyDto>.Ok(ToSpecialtyDto(specialty), "Specialty updated successfully");
    }

    public ServiceResult<bool> DeleteSpecialty(int id)
    {
        var specialty = _dbContext.Specialties.FirstOrDefault(x => x.Id == id);
        if (specialty is null)
        {
            return ServiceResult<bool>.Fail("Specialty not found", ServiceErrorType.NotFound);
        }

        if (_dbContext.Doctors.Any(x => x.SpecialtyId == id))
        {
            return ServiceResult<bool>.Fail("Specialty has doctors and cannot be deleted", ServiceErrorType.BadRequest);
        }

        _dbContext.Specialties.Remove(specialty);
        _dbContext.SaveChanges();

        return ServiceResult<bool>.Ok(true, "Specialty deleted successfully");
    }

    public IReadOnlyList<DoctorScheduleDto> GetDoctorSchedules()
    {
        return _dbContext.DoctorSchedules
            .AsNoTracking()
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.StartTime)
            .ToArray()
            .Select(schedule =>
            {
                var doctor = GetDoctor(schedule.DoctorId);
                return new DoctorScheduleDto
                {
                    ScheduleId = schedule.Id,
                    DoctorId = schedule.DoctorId,
                    DoctorName = doctor.FullName,
                    WorkDate = schedule.WorkDate,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    SlotDurationMinutes = schedule.SlotDurationMinutes,
                    IsAvailable = schedule.IsAvailable
                };
            })
            .ToArray();
    }

    public IReadOnlyList<DoctorScheduleDto> GetDoctorSchedulesByDoctor(int doctorId)
    {
        return _dbContext.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.StartTime)
            .ToArray()
            .Select(ToDoctorScheduleDto)
            .ToArray();
    }

    public ServiceResult<DoctorScheduleDto> GetDoctorScheduleById(int id)
    {
        var schedule = _dbContext.DoctorSchedules.AsNoTracking().FirstOrDefault(x => x.Id == id);
        return schedule is null
            ? ServiceResult<DoctorScheduleDto>.Fail("Doctor schedule not found", ServiceErrorType.NotFound)
            : ServiceResult<DoctorScheduleDto>.Ok(ToDoctorScheduleDto(schedule), "Doctor schedule retrieved successfully");
    }

    public ServiceResult<DoctorScheduleDto> CreateDoctorSchedule(CreateDoctorScheduleRequest request)
    {
        var validation = ValidateScheduleRequest(
            request.DoctorId,
            request.WorkDate,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);
        if (validation is not null)
        {
            return ServiceResult<DoctorScheduleDto>.Fail(validation, ServiceErrorType.BadRequest);
        }

        if (HasOverlappingSchedule(null, request.DoctorId, request.WorkDate, request.StartTime, request.EndTime))
        {
            return ServiceResult<DoctorScheduleDto>.Fail("Doctor schedule overlaps an existing schedule", ServiceErrorType.BadRequest);
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = request.DoctorId,
            WorkDate = request.WorkDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotDurationMinutes = request.SlotDurationMinutes,
            IsAvailable = request.IsAvailable
        };

        _dbContext.DoctorSchedules.Add(schedule);
        _dbContext.SaveChanges();

        return ServiceResult<DoctorScheduleDto>.Ok(ToDoctorScheduleDto(schedule), "Doctor schedule created successfully");
    }

    public ServiceResult<DoctorScheduleDto> UpdateDoctorSchedule(int id, UpdateDoctorScheduleRequest request)
    {
        var schedule = _dbContext.DoctorSchedules.FirstOrDefault(x => x.Id == id);
        if (schedule is null)
        {
            return ServiceResult<DoctorScheduleDto>.Fail("Doctor schedule not found", ServiceErrorType.NotFound);
        }

        var validation = ValidateScheduleRequest(
            request.DoctorId,
            request.WorkDate,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);
        if (validation is not null)
        {
            return ServiceResult<DoctorScheduleDto>.Fail(validation, ServiceErrorType.BadRequest);
        }

        if (HasOverlappingSchedule(id, request.DoctorId, request.WorkDate, request.StartTime, request.EndTime))
        {
            return ServiceResult<DoctorScheduleDto>.Fail("Doctor schedule overlaps an existing schedule", ServiceErrorType.BadRequest);
        }

        if (HasAppointmentOutsideSchedule(id, request.DoctorId, request.WorkDate, request.StartTime, request.EndTime))
        {
            return ServiceResult<DoctorScheduleDto>.Fail("Existing appointments would fall outside the updated schedule", ServiceErrorType.BadRequest);
        }

        schedule.DoctorId = request.DoctorId;
        schedule.WorkDate = request.WorkDate;
        schedule.StartTime = request.StartTime;
        schedule.EndTime = request.EndTime;
        schedule.SlotDurationMinutes = request.SlotDurationMinutes;
        schedule.IsAvailable = request.IsAvailable;
        _dbContext.SaveChanges();

        return ServiceResult<DoctorScheduleDto>.Ok(ToDoctorScheduleDto(schedule), "Doctor schedule updated successfully");
    }

    public ServiceResult<bool> DeleteDoctorSchedule(int id)
    {
        var schedule = _dbContext.DoctorSchedules.FirstOrDefault(x => x.Id == id);
        if (schedule is null)
        {
            return ServiceResult<bool>.Fail("Doctor schedule not found", ServiceErrorType.NotFound);
        }

        var hasAppointments = _dbContext.Appointments.Any(x =>
            x.DoctorId == schedule.DoctorId &&
            x.AppointmentDate == schedule.WorkDate &&
            x.SlotTime >= schedule.StartTime &&
            x.SlotTime < schedule.EndTime &&
            x.Status != AppointmentStatus.Cancelled);

        if (hasAppointments)
        {
            return ServiceResult<bool>.Fail("Doctor schedule has active appointments and cannot be deleted", ServiceErrorType.BadRequest);
        }

        _dbContext.DoctorSchedules.Remove(schedule);
        _dbContext.SaveChanges();

        return ServiceResult<bool>.Ok(true, "Doctor schedule deleted successfully");
    }

    public IReadOnlyList<QueueEntryDto> GetWaitingQueue(DateOnly? date)
    {
        var query = _dbContext.WaitingQueues.AsNoTracking();
        if (date.HasValue)
        {
            query = query.Where(x => x.QueueDate == date.Value);
        }

        return query
            .OrderBy(x => x.QueueDate)
            .ThenBy(x => x.QueueNumber)
            .ToArray()
            .Select(ToQueueEntryDto)
            .ToArray();
    }

    public ServiceResult<QueueEntryDto> GetQueueEntryById(int id)
    {
        var queueEntry = _dbContext.WaitingQueues.AsNoTracking().FirstOrDefault(x => x.Id == id);
        return queueEntry is null
            ? ServiceResult<QueueEntryDto>.Fail("Waiting queue entry not found", ServiceErrorType.NotFound)
            : ServiceResult<QueueEntryDto>.Ok(ToQueueEntryDto(queueEntry), "Waiting queue entry retrieved successfully");
    }

    public ServiceResult<QueueEntryDto> StartQueueEntry(int id)
    {
        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.Id == id);
        if (queueEntry is null)
        {
            return ServiceResult<QueueEntryDto>.Fail("Waiting queue entry not found", ServiceErrorType.NotFound);
        }

        if (queueEntry.Status != QueueStatus.Waiting)
        {
            return ServiceResult<QueueEntryDto>.Fail("Only waiting queue entries can be started", ServiceErrorType.BadRequest);
        }

        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == queueEntry.AppointmentId);
        if (appointment is null || appointment.Status != AppointmentStatus.Confirmed)
        {
            return ServiceResult<QueueEntryDto>.Fail("Appointment must be confirmed before starting queue entry", ServiceErrorType.BadRequest);
        }

        queueEntry.Status = QueueStatus.InProgress;
        appointment.Status = AppointmentStatus.InProgress;
        appointment.UpdatedAt = DateTime.UtcNow;
        _dbContext.SaveChanges();

        AddPatientCheckedInEvent(appointment, queueEntry, "InProgress");
        return ServiceResult<QueueEntryDto>.Ok(ToQueueEntryDto(queueEntry), "Waiting queue entry started successfully");
    }

    public ServiceResult<QueueEntryDto> CompleteQueueEntry(int id)
    {
        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.Id == id);
        if (queueEntry is null)
        {
            return ServiceResult<QueueEntryDto>.Fail("Waiting queue entry not found", ServiceErrorType.NotFound);
        }

        if (queueEntry.Status != QueueStatus.InProgress)
        {
            return ServiceResult<QueueEntryDto>.Fail("Only in-progress queue entries can be completed", ServiceErrorType.BadRequest);
        }

        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == queueEntry.AppointmentId);
        if (appointment is null)
        {
            return ServiceResult<QueueEntryDto>.Fail("Appointment not found", ServiceErrorType.BadRequest);
        }

        queueEntry.Status = QueueStatus.Done;
        appointment.Status = AppointmentStatus.Completed;
        appointment.UpdatedAt = DateTime.UtcNow;
        _dbContext.SaveChanges();

        AddIntegrationEvent("AppointmentCompleted", appointment);
        return ServiceResult<QueueEntryDto>.Ok(ToQueueEntryDto(queueEntry), "Waiting queue entry completed successfully");
    }

    public ServiceResult<QueueEntryDto> CancelQueueEntry(int id)
    {
        var queueEntry = _dbContext.WaitingQueues.FirstOrDefault(x => x.Id == id);
        if (queueEntry is null)
        {
            return ServiceResult<QueueEntryDto>.Fail("Waiting queue entry not found", ServiceErrorType.NotFound);
        }

        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == queueEntry.AppointmentId);
        queueEntry.Status = QueueStatus.Cancelled;
        if (appointment is not null && appointment.Status != AppointmentStatus.Completed)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            AddIntegrationEvent("AppointmentCancelled", appointment);
        }

        _dbContext.SaveChanges();

        return ServiceResult<QueueEntryDto>.Ok(ToQueueEntryDto(queueEntry), "Waiting queue entry cancelled successfully");
    }

    public IReadOnlyList<AppointmentEventDto> GetIntegrationEvents()
    {
        lock (IntegrationEventsLock)
        {
            return IntegrationEvents.ToArray();
        }
    }

    private ServiceResult<AppointmentDto> ChangeStatus(int id, AppointmentStatus status, string successMessage, string eventType)
    {
        var appointment = _dbContext.Appointments.FirstOrDefault(x => x.Id == id);
        if (appointment is null)
        {
            return ServiceResult<AppointmentDto>.Fail("Appointment not found", ServiceErrorType.NotFound);
        }

        if (appointment.Status == AppointmentStatus.Cancelled && status != AppointmentStatus.Cancelled)
        {
            return ServiceResult<AppointmentDto>.Fail("Cancelled appointment cannot change status", ServiceErrorType.BadRequest);
        }

        appointment.Status = status;
        _dbContext.SaveChanges();

        AddIntegrationEvent(eventType, appointment);
        return ServiceResult<AppointmentDto>.Ok(ToAppointmentDto(appointment), successMessage);
    }

    private AppointmentDto ToAppointmentDto(Appointment appointment)
    {
        var doctor = GetDoctor(appointment.DoctorId);
        var specialty = GetSpecialty(doctor.SpecialtyId);

        return new AppointmentDto
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = appointment.PatientNameSnapshot,
            PatientPhone = appointment.PatientPhoneSnapshot,
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            SpecialtyId = specialty.Id,
            SpecialtyName = specialty.Name,
            ExamFee = doctor.ExamFee,
            AppointmentDate = appointment.AppointmentDate,
            SlotTime = appointment.SlotTime,
            Reason = appointment.Reason,
            Status = appointment.Status.ToString(),
            QueueNumber = appointment.QueueNumber,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }

    private AppointmentForMedicalDto ToMedicalDto(Appointment appointment)
    {
        var appointmentDto = ToAppointmentDto(appointment);
        return new AppointmentForMedicalDto
        {
            AppointmentId = appointmentDto.AppointmentId,
            PatientId = appointmentDto.PatientId,
            PatientName = appointmentDto.PatientName,
            PatientPhone = appointmentDto.PatientPhone,
            DoctorId = appointmentDto.DoctorId,
            DoctorName = appointmentDto.DoctorName,
            SpecialtyId = appointmentDto.SpecialtyId,
            SpecialtyName = appointmentDto.SpecialtyName,
            AppointmentDate = appointmentDto.AppointmentDate,
            SlotTime = appointmentDto.SlotTime,
            Status = appointmentDto.Status,
            QueueNumber = appointmentDto.QueueNumber
        };
    }

    private BillingInfoDto ToBillingDto(Appointment appointment)
    {
        var appointmentDto = ToAppointmentDto(appointment);
        return new BillingInfoDto
        {
            AppointmentId = appointmentDto.AppointmentId,
            PatientId = appointmentDto.PatientId,
            PatientName = appointmentDto.PatientName,
            DoctorId = appointmentDto.DoctorId,
            DoctorName = appointmentDto.DoctorName,
            SpecialtyName = appointmentDto.SpecialtyName,
            ExamFee = appointmentDto.ExamFee,
            AppointmentDate = appointmentDto.AppointmentDate,
            SlotTime = appointmentDto.SlotTime,
            Status = appointmentDto.Status
        };
    }

    private DoctorDto ToDoctorDto(Doctor doctor)
    {
        var specialty = GetSpecialty(doctor.SpecialtyId);
        return new DoctorDto
        {
            DoctorId = doctor.Id,
            FullName = doctor.FullName,
            SpecialtyId = specialty.Id,
            SpecialtyName = specialty.Name,
            Degree = doctor.Degree,
            ExperienceYears = doctor.ExperienceYears,
            ExamFee = doctor.ExamFee,
            Phone = doctor.Phone,
            Email = doctor.Email,
            Gender = doctor.Gender,
            DateOfBirth = doctor.DateOfBirth,
            Description = doctor.Description,
            AvatarUrl = doctor.AvatarUrl,
            RoomNumber = doctor.RoomNumber,
            IsActive = doctor.IsActive,
            CreatedAt = doctor.CreatedAt,
            UpdatedAt = doctor.UpdatedAt,
            UserId = doctor.UserId
        };
    }

    private SpecialtyDto ToSpecialtyDto(Specialty specialty)
    {
        return new SpecialtyDto
        {
            SpecialtyId = specialty.Id,
            SpecialtyName = specialty.Name
        };
    }

    private DoctorScheduleDto ToDoctorScheduleDto(DoctorSchedule schedule)
    {
        var doctor = GetDoctor(schedule.DoctorId);
        return new DoctorScheduleDto
        {
            ScheduleId = schedule.Id,
            DoctorId = schedule.DoctorId,
            DoctorName = doctor.FullName,
            WorkDate = schedule.WorkDate,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            SlotDurationMinutes = schedule.SlotDurationMinutes,
            IsAvailable = schedule.IsAvailable
        };
    }

    private static QueueEntryDto ToQueueEntryDto(QueueEntry queueEntry)
    {
        return new QueueEntryDto
        {
            QueueId = queueEntry.Id,
            AppointmentId = queueEntry.AppointmentId,
            PatientId = queueEntry.PatientId,
            DoctorId = queueEntry.DoctorId,
            QueueDate = queueEntry.QueueDate,
            QueueNumber = queueEntry.QueueNumber,
            Status = queueEntry.Status.ToString(),
            CreatedAt = queueEntry.CreatedAt
        };
    }

    private Doctor GetDoctor(int doctorId)
    {
        return _dbContext.Doctors.AsNoTracking().First(x => x.Id == doctorId);
    }

    private Specialty GetSpecialty(int specialtyId)
    {
        return _dbContext.Specialties.AsNoTracking().First(x => x.Id == specialtyId);
    }

    private static string GetDoctorFullName(string? fullName, string? doctorName)
    {
        return (string.IsNullOrWhiteSpace(fullName) ? doctorName : fullName)?.Trim() ?? string.Empty;
    }

    private string? ValidateScheduleRequest(
        int doctorId,
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        if (!_dbContext.Doctors.Any(x => x.Id == doctorId))
        {
            return "Doctor not found";
        }

        if (workDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return "Doctor schedule cannot be created in the past";
        }

        if (startTime >= endTime)
        {
            return "Start time must be earlier than end time";
        }

        if ((endTime - startTime).TotalMinutes < slotDurationMinutes)
        {
            return "Doctor schedule is shorter than one slot";
        }

        return null;
    }

    private bool HasOverlappingSchedule(int? scheduleId, int doctorId, DateOnly workDate, TimeOnly startTime, TimeOnly endTime)
    {
        return _dbContext.DoctorSchedules.Any(x =>
            x.Id != scheduleId &&
            x.DoctorId == doctorId &&
            x.WorkDate == workDate &&
            startTime < x.EndTime &&
            endTime > x.StartTime);
    }

    private bool HasAppointmentOutsideSchedule(int scheduleId, int doctorId, DateOnly workDate, TimeOnly startTime, TimeOnly endTime)
    {
        var oldSchedule = _dbContext.DoctorSchedules.AsNoTracking().First(x => x.Id == scheduleId);
        return _dbContext.Appointments.Any(x =>
            x.DoctorId == oldSchedule.DoctorId &&
            x.AppointmentDate == oldSchedule.WorkDate &&
            x.SlotTime >= oldSchedule.StartTime &&
            x.SlotTime < oldSchedule.EndTime &&
            x.Status != AppointmentStatus.Cancelled &&
            (x.DoctorId != doctorId ||
             x.AppointmentDate != workDate ||
             x.SlotTime < startTime ||
             x.SlotTime >= endTime));
    }

    private string? ValidateCreateAppointmentRequest(CreateAppointmentRequest request)
    {
        if (request.PatientId <= 0)
        {
            return "Patient id is required";
        }

        if (string.IsNullOrWhiteSpace(request.PatientNameSnapshot))
        {
            return "Patient name is required";
        }

        if (string.IsNullOrWhiteSpace(request.PatientPhoneSnapshot))
        {
            return "Patient phone is required";
        }

        if (request.AppointmentDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return "Appointment date cannot be in the past";
        }

        return null;
    }

    private DoctorSchedule? GetScheduleContainingSlot(int doctorId, DateOnly date, TimeOnly slotTime)
    {
        return _dbContext.DoctorSchedules.AsNoTracking().FirstOrDefault(x =>
            x.DoctorId == doctorId &&
            x.WorkDate == date &&
            x.IsAvailable &&
            slotTime >= x.StartTime &&
            slotTime < x.EndTime);
    }

    private static bool IsSlotAligned(DoctorSchedule schedule, TimeOnly slotTime)
    {
        var minutesFromStart = (slotTime - schedule.StartTime).TotalMinutes;
        return minutesFromStart >= 0 &&
               minutesFromStart % schedule.SlotDurationMinutes == 0 &&
               slotTime.AddMinutes(schedule.SlotDurationMinutes) <= schedule.EndTime;
    }

    private static void AddIntegrationEvent(string eventType, Appointment appointment)
    {
        lock (IntegrationEventsLock)
        {
            IntegrationEvents.Add(new AppointmentEventDto
            {
                EventId = Guid.NewGuid(),
                EventType = eventType,
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                SlotTime = appointment.SlotTime,
                OccurredAt = DateTime.UtcNow
            });
        }
    }

    private void AddPatientCheckedInEvent(Appointment appointment, QueueEntry queueEntry, string status)
    {
        var outboxEvent = new OutboxEvent
        {
            EventType = "patient.checked_in",
            Payload = string.Empty,
            Status = "Pending"
        };

        _dbContext.OutboxEvents.Add(outboxEvent);
        _dbContext.SaveChanges();

        var eventCode = $"N1EV{outboxEvent.Id:D3}";
        outboxEvent.EventCode = eventCode;
        outboxEvent.Payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            eventCode,
            eventType = "patient.checked_in",
            source = "AppointmentService",
            occurredAt = DateTime.UtcNow,
            data = new
            {
                appointmentId = appointment.Id,
                doctorId = appointment.DoctorId,
                queueNumber = appointment.QueueNumber ?? queueEntry.QueueNumber,
                checkedInAt = DateTime.UtcNow,
                status
            }
        });

        _dbContext.SaveChanges();
    }

    private static QueueStatus ToQueueStatus(AppointmentStatus status)
    {
        return status switch
        {
            AppointmentStatus.Confirmed => QueueStatus.Waiting,
            AppointmentStatus.InProgress => QueueStatus.InProgress,
            AppointmentStatus.Completed => QueueStatus.Done,
            AppointmentStatus.Cancelled => QueueStatus.Cancelled,
            _ => QueueStatus.Waiting
        };
    }
}
