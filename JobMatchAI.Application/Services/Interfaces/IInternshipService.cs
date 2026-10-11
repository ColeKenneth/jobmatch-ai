using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IInternshipService
    {
        Task<InternshipDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<InternshipDto>> GetByStudentAsync(Guid studentId);
        Task<InternshipDto> CreateAsync(Guid studentId, InternshipDto dto);
        Task UpdateAsync(Guid id, InternshipDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
