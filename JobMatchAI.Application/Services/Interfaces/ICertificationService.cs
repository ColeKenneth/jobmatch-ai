using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface ICertificationService
    {
        Task<CertificationDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<CertificationDto>> GetByStudentAsync(Guid studentId);
        Task<CertificationDto> CreateAsync(Guid studentId, CertificationDto dto);
        Task UpdateAsync(Guid id, CertificationDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
