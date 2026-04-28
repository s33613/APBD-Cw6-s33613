namespace APBD_Cw6_s33613.DTOs;

public class AppointmentDetailsDto
{
    public int ID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DoctorFirstName { get; set; }
    public string DoctorLastName { get; set; }
    public string Specialization { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; }
    public string Reason { get; set; }
    public string? InternalNotes { get; set; }
}