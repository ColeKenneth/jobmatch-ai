using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace JobMatchAI.Application.Services.Implementations
{
    public class StudentService(AppDbContext context, ILogger<StudentService> logger) : IStudentService
    {
        public async Task<StudentDto> CreateAsync(StudentDto dto)
        {
            var user = await context.Users.FindAsync(dto.UserId)
                ?? throw new NotFoundException($"User not found: {dto.UserId}");

            if (user.Role != UserRole.Student)
            {
                throw new BusinessRuleException("User must have the student role.");
            }
            
            var exists = await context.Students
                .AnyAsync(s => s.StudentIdNumber == dto.StudentIdNumber);

            if (exists) throw new AlreadyExistsException($"Student {dto.StudentIdNumber} already exists.");

            
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<StudentDto>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<StudentDto?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<StudentDto?> GetByStudentIdNumberAsync(string studentIdNumber)
        {
            throw new NotImplementedException();
        }

        public Task<StudentDto?> GetByUserIdAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> StudentIdNumberExistsAsync(string studentIdNumber)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Guid id, StudentDto dto)
        {
            throw new NotImplementedException();
        }

        public Task UpdateResumeAsync(Guid id, string filePath, string resumeText)
        {
            throw new NotImplementedException();
        }

        private static StudentDto MapToDto(Student student) => new()
        {
            Id = student.Id,
            UserId = student.UserId,
            StudentIdNumber = student.StudentIdNumber,
            Program = student.Program,
            YearLevel = student.YearLevel,
            Gwa = student.Gwa,
            GraduationDate = student.GraduationDate,
            ResumeFilePath = student.ResumeFilePath,
            ResumeText = student.ResumeText
        };
    }
}
