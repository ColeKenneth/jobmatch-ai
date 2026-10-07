using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IJobPostingService
    {
        Task<JobPostingDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<JobPostingDto>> GetAllAsync();
        Task<IEnumerable<JobPostingDto>> GetByEmployerAsync(Guid employerId);
        Task<IEnumerable<JobPostingDto>> GetActiveJobsAsync();
        Task<IEnumerable<JobPostingDto>> GetByLocationAsync(string location);
        Task<JobPostingDto> CreateAsync(JobPostingDto dto);
        Task UpdateAsync(Guid id, JobPostingDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task CloseAsync(Guid id);
        Task ReopenAsync(Guid id);
        Task IncrementViewsAsync(Guid id);
        Task IncrementApplicationsAsync(Guid id);
    }
}