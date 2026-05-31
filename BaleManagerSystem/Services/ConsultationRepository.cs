using BaleManagerSystem.Models;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class ConsultationRepository : IConsultationRepository
    {
        private readonly IConfiguration _configuration;

        public ConsultationRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SaveConsultation(Consultation consultation)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO Consultations
            (ChatId, FullName, PhoneNumber, Company, Category, ShortBrief, CreatedAt)
            VALUES
            (@ChatId, @FullName, @PhoneNumber, @Company, @Category, @ShortBrief, GETDATE())
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", consultation.ChatId);
            cmd.Parameters.AddWithValue("@FullName", consultation.FullName);
            cmd.Parameters.AddWithValue("@PhoneNumber", consultation.PhoneNumber);
            cmd.Parameters.AddWithValue("@Company", consultation.Company);
            cmd.Parameters.AddWithValue("@ShortBrief", consultation.ShortBrief);
            cmd.Parameters.AddWithValue("@Category", consultation.Category);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
