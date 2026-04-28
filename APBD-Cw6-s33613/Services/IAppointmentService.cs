using APBD_Cw6_s33613.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APBD_Cw6_s33613.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointments(CancellationToken cancellationToken,string? status,string? patientLastName);
    Task<AppointmentDetailsDto?> GetAppointment(int ID, CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto> CreateAppointment(CreateAppointmentRequestDto CARD,CancellationToken cancellationToken = default);
    Task UpdateAppointment(int id,UpdateAppointmentRequestDto UARD,CancellationToken cancellationToken = default);
    Task DeleteAppointment(int id,CancellationToken cancellationToken = default);
}