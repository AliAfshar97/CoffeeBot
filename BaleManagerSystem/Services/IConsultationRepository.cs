using BaleManagerSystem.Models;

namespace BaleManagerSystem.Services
{
    public interface IConsultationRepository
    {
        Task SaveConsultation(Consultation consultation);
        Task<List<Consultation>> GetConsultationsAsync();
        Task<int> GetConsultationCountAsync();
    }
}
