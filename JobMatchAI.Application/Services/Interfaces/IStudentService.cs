using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IStudentService
    {
        Task<StudentDto?> GetByIdAsync(Guid id);
        Task<StudentDto?> GetByUserIdAsync(Guid userId);
        Task<StudentDto?> GetByStudentIdNumberAsync(string studentIdNumber);
        Task<IEnumerable<StudentDto>> GetAllAsync();
        Task<StudentDto> CreateAsync(StudentDto dto);
        Task UpdateAsync(Guid id, StudentDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> StudentIdNumberExistsAsync(string studentIdNumber);
        Task UpdateResumeAsync(Guid id, string filePath, string resumeText);
    }
}
