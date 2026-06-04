using AppointmentService.Common;
using AppointmentService.Dtos.Appointments;
using AppointmentService.Dtos.DoctorSchedules;
using AppointmentService.Dtos.Doctors;
using AppointmentService.Dtos.Integration;
using AppointmentService.Dtos.Specialties;
using AppointmentService.Dtos.WaitingQueue;

namespace AppointmentService.Services;

public interface IAppointmentService
{
    IReadOnlyList<AppointmentDto> GetAppointments();

    ServiceResult<AppointmentDto> GetAppointmentById(int id);

    IReadOnlyList<AppointmentDto> GetAppointmentsByPatient(int patientId);

    IReadOnlyList<AppointmentDto> GetAppointmentsByDoctor(int doctorId);

    IReadOnlyList<AppointmentDto> GetConfirmedAppointments();

    ServiceResult<AppointmentDto> CreateAppointment(CreateAppointmentRequest request);

    ServiceResult<AppointmentDto> ConfirmAppointment(int id);

    ServiceResult<AppointmentDto> StartAppointment(int id);

    ServiceResult<AppointmentDto> CancelAppointment(int id);

    ServiceResult<AppointmentDto> CompleteAppointment(int id);

    ServiceResult<AppointmentDto> CheckInAppointment(int id);

    ServiceResult<AppointmentForMedicalDto> GetMedicalInfo(int appointmentId);

    ServiceResult<BillingInfoDto> GetBillingInfo(int appointmentId);

    IReadOnlyList<DoctorDto> GetDoctors();

    IReadOnlyList<DoctorDto> GetDoctorsBySpecialty(int specialtyId);

    ServiceResult<IReadOnlyList<TimeOnly>> GetAvailableSlots(int doctorId, DateOnly date);

    ServiceResult<DoctorDto> GetDoctorById(int id);

    ServiceResult<DoctorDto> CreateDoctor(CreateDoctorRequest request);

    ServiceResult<DoctorDto> UpdateDoctor(int id, UpdateDoctorRequest request);

    ServiceResult<bool> DeleteDoctor(int id);

    IReadOnlyList<SpecialtyDto> GetSpecialties();

    ServiceResult<SpecialtyDto> GetSpecialtyById(int id);

    ServiceResult<SpecialtyDto> CreateSpecialty(CreateSpecialtyRequest request);

    ServiceResult<SpecialtyDto> UpdateSpecialty(int id, UpdateSpecialtyRequest request);

    ServiceResult<bool> DeleteSpecialty(int id);

    IReadOnlyList<DoctorScheduleDto> GetDoctorSchedules();

    IReadOnlyList<DoctorScheduleDto> GetDoctorSchedulesByDoctor(int doctorId);

    ServiceResult<DoctorScheduleDto> GetDoctorScheduleById(int id);

    ServiceResult<DoctorScheduleDto> CreateDoctorSchedule(CreateDoctorScheduleRequest request);

    ServiceResult<DoctorScheduleDto> UpdateDoctorSchedule(int id, UpdateDoctorScheduleRequest request);

    ServiceResult<bool> DeleteDoctorSchedule(int id);

    IReadOnlyList<QueueEntryDto> GetWaitingQueue(DateOnly? date);

    ServiceResult<QueueEntryDto> GetQueueEntryById(int id);

    ServiceResult<QueueEntryDto> StartQueueEntry(int id);

    ServiceResult<QueueEntryDto> CompleteQueueEntry(int id);

    ServiceResult<QueueEntryDto> CancelQueueEntry(int id);

    IReadOnlyList<AppointmentEventDto> GetIntegrationEvents();
}
