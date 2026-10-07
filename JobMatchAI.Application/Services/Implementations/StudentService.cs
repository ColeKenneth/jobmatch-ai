using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;


namespace JobMatchAI.Application.Services.Implementations
{
    public class StudentService(AppDbContext context, ILogger<StudentService> logger) : IStudentService
    {
        public async Task<StudentDto> CreateAsync(StudentDto dto)
        {
            var normalizedStudentIdNumber = dto.StudentIdNumber.Trim().ToUpperInvariant();

            var user = await context.Users.FindAsync(dto.UserId)
                ?? throw new NotFoundException($"User not found: {dto.UserId}");

            if (user.Role != UserRole.Student)
            {
                throw new BusinessRuleException("User must have the student role.");
            }

            var userAlreadyHasStudentProfile = await context.Students
                .AnyAsync(s => s.UserId == dto.UserId);

            if (userAlreadyHasStudentProfile) throw new AlreadyExistsException($"Student already exists for User {dto.UserId}");
            
            var exists = await context.Students
                .AnyAsync(s => s.StudentIdNumber == normalizedStudentIdNumber);

            if (exists) throw new AlreadyExistsException($"Student {dto.StudentIdNumber} already exists.");

            var student = new Student
            {
                UserId = dto.UserId,
                StudentIdNumber = normalizedStudentIdNumber,
                Program = dto.Program,
                YearLevel = dto.YearLevel,
                Gwa = dto.Gwa,
                GraduationDate = dto.GraduationDate,
                ResumeFilePath = dto.ResumeFilePath,
                ResumeText = dto.ResumeText
            };

            context.Students.Add(student);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating student {StudentIdNumber}", dto.StudentIdNumber);
                throw new AlreadyExistsException($"Student {dto.StudentIdNumber} already exists.");
            }

            logger.LogInformation("Created student {StudentIdNumber}", dto.StudentIdNumber);
            return MapToDto(student);
            
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Students
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Student {id} not found.");

            logger.LogInformation("Deleted student with an ID of {Id}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Students.AnyAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<StudentDto>> GetAllAsync()
        {
            return await context.Students
                .AsNoTracking()
                .Select(AsStudentDto)
                .ToListAsync();
        }

        public async Task<StudentDto?> GetByIdAsync(Guid id)
        {
            return await context.Students
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(AsStudentDto)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentDto?> GetByStudentIdNumberAsync(string studentIdNumber)
        {
            var normalizedStudentIdNumber = studentIdNumber.Trim().ToUpperInvariant();

            return await context.Students
                .AsNoTracking()
                .Where(s => s.StudentIdNumber == normalizedStudentIdNumber)
                .Select(AsStudentDto)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentDto?> GetByUserIdAsync(Guid userId)
        {
            return await context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(AsStudentDto)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> StudentIdNumberExistsAsync(string studentIdNumber)
        {
            var normalizedStudentIdNumber = studentIdNumber.Trim().ToUpperInvariant();
            return await context.Students.AnyAsync(s => s.StudentIdNumber == normalizedStudentIdNumber);
        }

        public async Task UpdateAsync(Guid id, StudentDto dto)
        {
            var normalizedStudentIdNumber = dto.StudentIdNumber.Trim().ToUpperInvariant();

            var exists = await context.Students
                .AnyAsync(s => s.Id != id && s.StudentIdNumber == normalizedStudentIdNumber);

            if (exists) throw new AlreadyExistsException($"Student {dto.StudentIdNumber} already exists.");

            var student = await context.Students.FindAsync(id)
                ?? throw new NotFoundException($"Student {id} not found.");

            student.StudentIdNumber = normalizedStudentIdNumber;
            student.Program = dto.Program;
            student.YearLevel = dto.YearLevel;
            student.Gwa = dto.Gwa;
            student.GraduationDate = dto.GraduationDate;
            student.ResumeFilePath = dto.ResumeFilePath;
            student.ResumeText = dto.ResumeText;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating student {StudentIdNumber}", dto.StudentIdNumber);
                throw new AlreadyExistsException($"Student {student.StudentIdNumber} already exists.");
            }

            logger.LogInformation("Updated student {StudentIdNumber}", student.StudentIdNumber);
        }

        public async Task UpdateResumeAsync(Guid id, string filePath, string resumeText)
        {
            var updatedCount = await context.Students
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(st => st.ResumeFilePath, filePath)
                .SetProperty(st => st.ResumeText, resumeText));

            if (updatedCount == 0) throw new NotFoundException($"Student {id} not found.");

            logger.LogInformation("Updated resume for student {Id}", id);
        }

        private static readonly Expression<Func<Student, StudentDto>> AsStudentDto = s => new StudentDto
        {
            Id = s.Id,
            UserId = s.UserId,
            StudentIdNumber = s.StudentIdNumber,
            Program = s.Program,
            YearLevel = s.YearLevel,
            Gwa = s.Gwa,
            GraduationDate = s.GraduationDate,
            ResumeFilePath = s.ResumeFilePath,
            ResumeText = s.ResumeText
        };

        private static readonly Func<Student, StudentDto> MapToDto = AsStudentDto.Compile();
    }
}
