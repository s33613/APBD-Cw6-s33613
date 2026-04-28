namespace APBD_Cw6_s33613.DTOs;

public class AppointmentListDto
{
    public int ID { get; set; }
    public string FullName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status {get;set; }
    public string Reason { get; set; }
    public string EMail { get; set; }

}