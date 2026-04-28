using System.Data;
using System.Text;
using APBD_Cw6_s33613.DTOs;
using APBD_Cw6_s33613.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_Cw6_s33613.Services;

public class AppointmentService(IConfiguration  configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointments(CancellationToken cancellationToken,string? status,string? patientLastName)
    {
        var result = new List<AppointmentListDto>();
        var sqlBuilder = new StringBuilder("""
                                           SELECT
                                               a.IdAppointment,
                                               a.AppointmentDate,
                                               a.Status,
                                               a.Reason,
                                               p.FirstName + N' ' + p.LastName AS PatientFullName,
                                               p.Email AS PatientEmail
                                           FROM dbo.Appointments a
                                           JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                                           """);
        
        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();

        if (status is not null)
        {
            conditions.Add("a.Status = @Status");
            parameters.Add(new SqlParameter("@Status", status));
        }

        if (patientLastName is not null)
        {
            conditions.Add("p.LastName = @patientLastName");
            parameters.Add(new SqlParameter("@patientLastName", patientLastName));
        }

        if (parameters.Count > 0)
        {
            sqlBuilder.Append(" WHERE ");
            sqlBuilder.Append(string.Join(" AND ", conditions));
        }
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
        command.Parameters.AddRange(parameters.ToArray());
        await connection.OpenAsync(cancellationToken);
    
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AppointmentListDto()
            {
                ID = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                FullName = reader.GetString(4),
                EMail = reader.GetString(5),
            });
        }

        return result;
    }

    public async Task<AppointmentDetailsDto> GetAppointment(int id, CancellationToken cancellationToken = default)
    {
        AppointmentDetailsDto result = null;
        var sqlBuilder = new StringBuilder("""
                                           SELECT a.IdAppointment, 
                                                  p.FirstName, p.LastName
                                                  ,d.FirstName,d.LastName, 
                                                   s.Name, p.DateOfBirth, 
                                                  a.AppointmentDate,a.Reason,a.Status,a.InternalNotes
                                           FROM Appointments a 
                                            LEFT JOIN Patients p ON a.IdPatient = p.IdPatient
                                            LEFT JOIN Doctors d ON a.IdDoctor = d.IdDoctor
                                            Left Join Specializations s ON s.IdSpecialization = d.IdSpecialization
                                            where a.IdAppointment = @id
                                           """);


        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
        command.Parameters.AddWithValue("@id", id);


        await connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result = new AppointmentDetailsDto()
            {
                ID = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DoctorFirstName = reader.GetString(3),
                DoctorLastName = reader.GetString(4),
                Specialization = reader.GetString(5),
                DateOfBirth = reader.GetDateTime(6),
                AppointmentDate = reader.GetDateTime(7),
                Reason = reader.GetString(8),
                Status = reader.GetString(9),
                InternalNotes = reader.IsDBNull(10)? null : reader.GetString(10),
            };
        }

        if (result == null)
        {
            throw new NotFoundException("Appointment by ID" + id + "not found");
        }
        return result;
     }

    public async Task<AppointmentDetailsDto> CreateAppointment(CreateAppointmentRequestDto CARD,CancellationToken cancellationToken = default)
    {
        if (CARD.appointmentDate <= System.DateTime.Now)
            throw new ConflictException("Appointment date must be in the future");
        // usunięcie milisekund dla prawidłowego porównania
        CARD.appointmentDate = new DateTime( CARD.appointmentDate.Year, CARD.appointmentDate.Month, CARD.appointmentDate.Day, CARD.appointmentDate.Hour, CARD.appointmentDate.Minute,CARD.appointmentDate.Second);
        string FirstName, LastName, DoctorFirstName, DoctorLastName,specialization;
        DateTime DateOfBirth;
        // check patient
        var sqlBuilder1 = new StringBuilder("""
                                            SELECT IsActive,FirstName,LastName,DateOfBirth FROM Patients
                                            WHERE IdPatient = @pId
                                            """);


        await using var connection1 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command1 = new SqlCommand(sqlBuilder1.ToString(), connection1);
        command1.Parameters.AddWithValue("@pId", CARD.idPatient);

        await connection1.OpenAsync(cancellationToken);

        using var reader1 = await command1.ExecuteReaderAsync(cancellationToken);
        if(!await reader1.ReadAsync(cancellationToken))
        {
                throw new NotFoundException("Patient with ID " + CARD.idPatient + " not found");
        }
        if(!reader1.GetBoolean(0))
            throw new NotActiveException("Patient with ID " + CARD.idPatient + " is not active");
        FirstName = reader1.GetString(1); 
        LastName = reader1.GetString(2);
        DateOfBirth = reader1.GetDateTime(3);
            
        
        //////////////////////////////////////// check doctor
        var sqlBuilder2 = new StringBuilder("""
                                            SELECT d.IsActive, d.FirstName,d.LastName,s.Name  FROM Doctors d
                                            Left Join Specializations s ON s.IdSpecialization = d.IdSpecialization
                                            WHERE IdDoctor = @dId
                                            """);


        await using var connection2 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command2 = new SqlCommand(sqlBuilder2.ToString(), connection2);
        command2.Parameters.AddWithValue("@dId", CARD.idDoctor);

        await connection2.OpenAsync(cancellationToken);

        using var reader2 = await command2.ExecuteReaderAsync(cancellationToken);
        if(!await reader2.ReadAsync(cancellationToken))
        {
            throw new NotFoundException("Doctor with ID " + CARD.idPatient + " not found");
        }
        if(!reader2.GetBoolean(0))
            throw new NotActiveException("Doctor with ID " + CARD.idPatient + " is not active");
        DoctorFirstName = reader2.GetString(1);
        DoctorLastName = reader2.GetString(2);
        specialization = reader2.GetString(3);
        /////////////////////////////////////////////////////// other appointments
        var sqlBuilder3 = new StringBuilder("""
                                            SELECT AppointmentDate FROM Appointments
                                            WHERE IdDoctor = @dId AND AppointmentDate = @Date
                                            """);


        await using var connection3 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command3 = new SqlCommand(sqlBuilder3.ToString(), connection3);
        command3.Parameters.AddWithValue("@dId", CARD.idDoctor);
        command3.Parameters.AddWithValue("@Date", CARD.appointmentDate);

        await connection3.OpenAsync(cancellationToken);

        using var reader3 = await command3.ExecuteReaderAsync(cancellationToken);
        if (await reader3.ReadAsync(cancellationToken))
        {
            throw new ConflictException("This doctor has an appointment at this time");
        }

        
        /////////////////////////////////////////////////////// 
        var sqlBuilder = new StringBuilder("""
                                           INSERT INTO Appointments
                                           VALUES(@pId,@dId, @Date ,'Scheduled','@Reason',null,SYSUTCDATETIME());
                                           SELECT SCOPE_IDENTITY();
                                           """);


        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
        command.Parameters.AddWithValue("@pId", CARD.idPatient);
        command.Parameters.AddWithValue("@dId", CARD.idDoctor);
        command.Parameters.AddWithValue("@Date", CARD.appointmentDate);
        command.Parameters.AddWithValue("@Reason", CARD.reason);
        
        await connection.OpenAsync(cancellationToken);
        var newID = await command.ExecuteScalarAsync(cancellationToken);
        int insertedID = Convert.ToInt32(newID);

        
        return new AppointmentDetailsDto()
        {
            ID = insertedID,
            FirstName = FirstName,
            LastName = LastName,
            DoctorFirstName = DoctorFirstName,
            DoctorLastName = DoctorLastName,
            Specialization = specialization,
            DateOfBirth = DateOfBirth,
            AppointmentDate = CARD.appointmentDate,
            Reason = CARD.reason,
            Status = "Scheduled"
        };
        
    }

    public async Task UpdateAppointment(int id, UpdateAppointmentRequestDto UARD,
        CancellationToken cancellationToken = default)
    {
        if(UARD.status is not ("Scheduled" or "Completed" or "Cancelled"))
            throw new ConflictException("Appointment status is invalid");
        UARD.appointmentDate = new DateTime( UARD.appointmentDate.Year, UARD.appointmentDate.Month, UARD.appointmentDate.Day, UARD.appointmentDate.Hour, UARD.appointmentDate.Minute,UARD.appointmentDate.Second);
        var sqlBuilder4 = new StringBuilder("""
                                            SELECT Status,AppointmentDate FROM Appointments
                                            WHERE IdAppointment = @Id
                                            """);


        await using var connection4 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command4 = new SqlCommand(sqlBuilder4.ToString(), connection4);
        command4.Parameters.AddWithValue("@Id", id);
        
        

        await connection4.OpenAsync(cancellationToken);

        using var reader4 = await command4.ExecuteReaderAsync(cancellationToken);
        if(!await reader4.ReadAsync(cancellationToken))
        {
            throw new NotFoundException("Appointment not found");
        }

        var statusRead = reader4.GetString(0);
        DateTime appointmentDate = reader4.GetDateTime(1);
        if (statusRead == "Completed" && appointmentDate == UARD.appointmentDate)
        {
            throw new ConflictException("Can't change date of completed appointment");
        }

        var sqlBuilder1 = new StringBuilder("""
                                            SELECT IsActive FROM Patients
                                            WHERE IdPatient = @pId
                                            """);


        await using var connection1 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command1 = new SqlCommand(sqlBuilder1.ToString(), connection1);
        command1.Parameters.AddWithValue("@pId", UARD.idPatient);

        await connection1.OpenAsync(cancellationToken);

        using var reader1 = await command1.ExecuteReaderAsync(cancellationToken);
        if(!await reader1.ReadAsync(cancellationToken))
        {
                throw new NotFoundException("Patient with ID " + UARD.idPatient + " not found");
        }
        if(!reader1.GetBoolean(0))
            throw new NotActiveException("Patient with ID " + UARD.idPatient + " is not active");
            
        
        //////////////////////////////////////// check doctor
        var sqlBuilder2 = new StringBuilder("""
                                            SELECT d.IsActive  FROM Doctors d
                                            Left Join Specializations s ON s.IdSpecialization = d.IdSpecialization
                                            WHERE IdDoctor = @dId
                                            """);


        await using var connection2 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command2 = new SqlCommand(sqlBuilder2.ToString(), connection2);
        command2.Parameters.AddWithValue("@dId", UARD.idDoctor);

        await connection2.OpenAsync(cancellationToken);

        using var reader2 = await command2.ExecuteReaderAsync(cancellationToken);
        if(!await reader2.ReadAsync(cancellationToken))
        {
            throw new NotFoundException("Doctor with ID " + UARD.idPatient + " not found");
        }
        if(!reader2.GetBoolean(0))
            throw new NotActiveException("Doctor with ID " + UARD.idPatient + " is not active");
        /////////////////////////////////////////////////////// other appointments
        var sqlBuilder3 = new StringBuilder("""
                                            SELECT AppointmentDate FROM Appointments
                                            WHERE IdDoctor = @dId AND AppointmentDate = @Date
                                            """);


        await using var connection3 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command3 = new SqlCommand(sqlBuilder3.ToString(), connection3);
        command3.Parameters.AddWithValue("@dId", UARD.idDoctor);
        command3.Parameters.AddWithValue("@Date", UARD.appointmentDate);

        await connection3.OpenAsync(cancellationToken);

        using var reader3 = await command3.ExecuteReaderAsync(cancellationToken);
        if (await reader3.ReadAsync(cancellationToken))
        {
            throw new ConflictException("This doctor has an appointment at this time");
        }
        ////////
        var sqlBuilder = new StringBuilder("""
                                           Update Appointments
                                           Set idPatient = @pId, idDoctor = @dId, appointmentDate = @Date,
                                               status = @Status, reason = @Reason, internalNotes = @InternalNotes;
                                           """);


        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
        command.Parameters.AddWithValue("@pId", UARD.idPatient);
        command.Parameters.AddWithValue("@dId", UARD.idDoctor);
        command.Parameters.AddWithValue("@Date", UARD.appointmentDate);
        command.Parameters.AddWithValue("@Status", UARD.status);
        command.Parameters.AddWithValue("@Reason", UARD.reason);
        command.Parameters.AddWithValue("@InternalNotes", UARD.internalNotes);
        
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        
        
    }

    public async Task DeleteAppointment(int id, CancellationToken cancellationToken = default)
    {
        var sqlBuilder3 = new StringBuilder("""
                                            SELECT Status FROM Appointments
                                            WHERE IdAppointment = @IdAppointment
                                            """);


        await using var connection3 = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command3 = new SqlCommand(sqlBuilder3.ToString(), connection3);
        command3.Parameters.AddWithValue("@IdAppointment", id);

        await connection3.OpenAsync(cancellationToken);

        using var reader3 = await command3.ExecuteReaderAsync(cancellationToken);
        if(!await reader3.ReadAsync(cancellationToken))
        {
            throw new NotFoundException("Appointment with " + id + " not found");
        }
        if(reader3.GetString(0) == "Completed")
            throw new ConflictException("Appointment with " + id + " is completed");
        
        var sqlBuilder = new StringBuilder("""
                                            DELETE FROM Appointments
                                            WHERE IdAppointment = @IdAppointment
                                            """);


        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
        command.Parameters.AddWithValue("@IdAppointment", id);

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
        
    }
}