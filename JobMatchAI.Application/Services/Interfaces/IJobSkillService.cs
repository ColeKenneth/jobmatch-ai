using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IJobSkillService
    {
        Task<JobSkillDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<JobSkillDto>> GetByJobPostingAsync(Guid jobPostingId);
        Task<JobSkillDto> AddSkillAsync(Guid jobPostingId, JobSkillDto dto);
        Task UpdateSkillAsync(Guid id, JobSkillDto dto);
        Task RemoveSkillAsync(Guid id);
        Task RemoveAllForJobPostingAsync(Guid jobPostingId);
        Task<bool> ExistsAsync(Guid jobPostingId, Guid skillId);
    }
}
