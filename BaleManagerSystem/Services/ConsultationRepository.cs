using BaleManagerSystem.Models;
using Dapper;
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

        public async Task<List<Consultation>> GetConsultationsAsync()
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString(
                    "SaleBotManagerDB"));

            var sql = @"
                        SELECT
                            ChatId,
                            FullName,
                            PhoneNumber,
                            Company,
                            Category,
                            ShortBrief,
                            CreatedAt
                        FROM Consultations
                        ORDER BY CreatedAt DESC";

            var result =
                await conn.QueryAsync<Consultation>(sql);

            return result.ToList();
        }

        public async Task<int> GetConsultationCountAsync()
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString(
                    "SaleBotManagerDB"));

            const string sql = @"
            SELECT COUNT(*)
            FROM Consultations";

            return await conn.ExecuteScalarAsync<int>(sql);
        }
    }
}
