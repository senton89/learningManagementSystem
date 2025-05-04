using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class UserService
    {
        private readonly LmsDbContext _context;
        private User _currentUser;

        public UserService(LmsDbContext context)
        {
            _context = context;
        }

        public User CurrentUser => _currentUser;

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.CustomPermissions)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> SetCurrentUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                _currentUser = user;
                return true;
            }
            return false;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .ToListAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        
        public void ClearCurrentUser()
        {
            _currentUser = null;
        }
    }
}