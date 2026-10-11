using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto?> GetByEmailAsync(string email);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto> CreateAsync(UserDto dto);
        Task UpdateAsync(Guid id, UserDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
        Task DeactivateAsync(Guid id);
        Task ActivateAsync(Guid id);
    }
}
