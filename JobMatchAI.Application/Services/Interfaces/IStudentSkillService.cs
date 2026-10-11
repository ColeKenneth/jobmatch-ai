using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IStudentSkillService
    {
        Task<StudentSkillDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<StudentSkillDto>> GetByStudentAsync(Guid studentId);
        Task<StudentSkillDto> AddSkillAsync(Guid studentId, StudentSkillDto dto);
        Task UpdateSkillAsync(Guid id, StudentSkillDto dto);
        Task RemoveSkillAsync(Guid id);
        Task RemoveAllForStudentAsync(Guid studentId);
        Task<bool> ExistsAsync(Guid studentId, Guid skillId);
    }
}
