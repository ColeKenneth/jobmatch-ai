using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IMatchResultService
    {
        Task<MatchResultDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<MatchResultDto>> GetByStudentAsync(Guid studentId);
        Task<IEnumerable<MatchResultDto>> GetByJobPostingAsync(Guid jobPostingId);
        Task<MatchResultDto?> GetByStudentAndJobAsync(Guid studentId, Guid jobPostingId);
        Task<MatchResultDto> CreateAsync(MatchResultDto dto);
        Task UpdateAsync(Guid id, MatchResultDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task MarkAsViewedAsync(Guid id);
        Task MarkAsAppliedAsync(Guid id);
        Task MarkAsHiredAsync(Guid id, DateTime hireDate);
    }
}