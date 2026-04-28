using System.ComponentModel.DataAnnotations;

namespace APBD_Cw6_s33613.DTOs;

public class CreateAppointmentRequestDto
{
    [Required] 
    public int idPatient { get; set; }
    [Required]
    public int idDoctor { get; set; }
    [Required]
    public DateTime appointmentDate { get; set; }
    [Required, MaxLength(250)]
    public string reason { get; set; }
}