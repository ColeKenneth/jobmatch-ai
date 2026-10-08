using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace JobMatchAI.Application.Services.Implementations
{
    public class StudentSkillService(AppDbContext context, ILogger<StudentSkillService> logger) : IStudentSkillService
    {
        public async Task<StudentSkillDto> AddSkillAsync(Guid studentId, StudentSkillDto dto)
        {
            var studentExists = await context.Students.AnyAsync(s => s.Id == studentId);

            if (!studentExists) throw new NotFoundException($"Student {studentId} not found.");

            var skillExists = await context.Skills.AnyAsync(s => s.Id == dto.SkillId);

            if (!skillExists) throw new NotFoundException($"Skill {dto.SkillId} not found.");

            var alreadyAttached = await context.StudentSkills
                .AnyAsync(ss => ss.StudentId == studentId && ss.SkillId == dto.SkillId);

            if (alreadyAttached) throw new AlreadyExistsException("Skill is already attached to this student.");

            var studentSkill = new StudentSkill
            {
                StudentId = studentId,
                SkillId = dto.SkillId,
                ProficiencyLevel = dto.ProficiencyLevel,
                YearsOfExperience = dto.YearsOfExperience
            };

            context.StudentSkills.Add(studentSkill);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating student skill for student {StudentId}", studentId);
                throw new BusinessRuleException("Failed to create student skill.");
            }

            logger.LogInformation("Attached skill {SkillId} to student {StudentId}", dto.SkillId, studentId);
            return MapToDto(studentSkill);
        }

        public async Task<bool> ExistsAsync(Guid studentId, Guid skillId)
        {
            return await context.StudentSkills
                .AnyAsync(ss => ss.StudentId == studentId && ss.SkillId == skillId);
        }

        public async Task<StudentSkillDto?> GetByIdAsync(Guid id)
        {
            return await context.StudentSkills
                .AsNoTracking()
                .Where(ss => ss.Id == id)
                .Select(AsStudentSkillDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<StudentSkillDto>> GetByStudentAsync(Guid studentId)
        {
            return await context.StudentSkills
                .AsNoTracking()
                .Where(ss => ss.StudentId == studentId)
                .Select(AsStudentSkillDto)
                .ToListAsync();
        }

        public async Task RemoveAllForStudentAsync(Guid studentId)
        {
            var deletedCount = await context.StudentSkills
                .Where(ss => ss.StudentId == studentId)
                .ExecuteDeleteAsync();

            if (deletedCount == 0)
            {
                var studentExists = await context.Students.AnyAsync(s => s.Id == studentId);
                if (!studentExists) throw new NotFoundException($"Student {studentId} not found.");

                return;
            }

            logger.LogInformation("Removed all skills from student {StudentId}", studentId);
        }

        public async Task RemoveSkillAsync(Guid id)
        {
            var deleted = await context.StudentSkills
                .Where(ss => ss.Id == id)
                .ExecuteDeleteAsync();

            if (deleted == 0) throw new NotFoundException($"Student skill {id} not found.");

            logger.LogInformation("Removed student skill {Id}", id);
        }

        public async Task UpdateSkillAsync(Guid id, StudentSkillDto dto)
        {
            var studentSkill = await context.StudentSkills.FindAsync(id)
                ?? throw new NotFoundException($"Student skill {id} not found.");

            if (dto.StudentId != Guid.Empty && dto.StudentId != studentSkill.StudentId)
            {
                throw new BusinessRuleException("Cannot reassign student skill to a different student.");
            }

            if (dto.SkillId != Guid.Empty && dto.SkillId != studentSkill.SkillId)
            {
                throw new BusinessRuleException("Cannot reassign student skill to a different skill.");
            }

            studentSkill.ProficiencyLevel = dto.ProficiencyLevel;
            studentSkill.YearsOfExperience = dto.YearsOfExperience;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating student skill {Id}", id);
                throw new BusinessRuleException("Failed to update student skill.");
            }

            logger.LogInformation("Updated student skill {Id}", id);
        }

        private static readonly Expression<Func<StudentSkill, StudentSkillDto>> AsStudentSkillDto = ss => new StudentSkillDto
        {
            Id = ss.Id,
            StudentId = ss.StudentId,
            SkillId = ss.SkillId,
            ProficiencyLevel = ss.ProficiencyLevel,
            YearsOfExperience = ss.YearsOfExperience
        };

        private static readonly Func<StudentSkill, StudentSkillDto> MapToDto = AsStudentSkillDto.Compile();
    }
}
