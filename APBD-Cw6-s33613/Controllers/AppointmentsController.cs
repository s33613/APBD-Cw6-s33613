using APBD_Cw6_s33613.DTOs;
using APBD_Cw6_s33613.Exceptions;
using APBD_Cw6_s33613.Services;
using Microsoft.AspNetCore.Mvc;
namespace APBD_Cw6_s33613.Controllers;

[ApiController]
[Route("api/Appointments")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetAllReservations(string? status,string? lastName,
        CancellationToken cancellationToken = default)
    {
        var res = await appointmentService.GetAllAppointments(cancellationToken,status,lastName);
        if (res.Any())
            return Ok(res);
        return NotFound("Appointments not found");
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReservation([FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await appointmentService.GetAppointment(id, cancellationToken));
        }
        catch (NotFoundException e)
        {
            return NotFound("Appointment by ID " + id + " not found");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateReservation([FromBody] CreateAppointmentRequestDto createAppointmentRequestDto, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var createdAppointment = await appointmentService.CreateAppointment(createAppointmentRequestDto, cancellationToken);
            return Created("api/Appointments/" + createdAppointment.ID, createdAppointment);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException ce)
        {
            return Conflict(ce.Message);
        }
        catch (NotActiveException e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateReservation([FromRoute] int id,
        [FromBody] UpdateAppointmentRequestDto updateAppointmentRequestDto, CancellationToken cancellationToken = default)
    {
        try
        {
            await appointmentService.UpdateAppointment(id,updateAppointmentRequestDto, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (NotActiveException e)
        {
            return Conflict(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReservation([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await appointmentService.DeleteAppointment(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }

}