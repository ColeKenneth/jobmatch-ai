using JobMatchAI.Application.DTOs;
using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface ISkillService
    {
        Task<SkillDto?> GetByIdAsync(Guid Id);
        Task<IEnumerable<SkillDto>> GetAllAsync();
        Task<SkillDto?> GetByNameAsync(string name);
        Task<IEnumerable<SkillDto>> GetByCategoryAsync(Category category);
        Task<SkillDto> CreateAsync(SkillDto dto);
        Task UpdateAsync(Guid id, SkillDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
