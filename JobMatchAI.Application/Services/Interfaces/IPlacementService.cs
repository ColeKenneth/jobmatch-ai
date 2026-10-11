using JobMatchAI.Application.DTOs;
using JobMatchAI.Domain.Enums;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IPlacementService
    {
        Task<PlacementRecordDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<PlacementRecordDto>> GetByStudentAsync(Guid studentId);
        Task<IEnumerable<PlacementRecordDto>> GetByJobPostingAsync(Guid jobPostingId);
        Task<IEnumerable<PlacementRecordDto>> GetByStatusAsync(PlacementStatus status);
        Task<PlacementRecordDto> CreateAsync(PlacementRecordDto dto);
        Task UpdateAsync(Guid id, PlacementRecordDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task UpdateStatusAsync(Guid id, PlacementStatus status);
    }
}