using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class UserProfileViewModel : NotifyPropertyChangedBase, IDataErrorInfo
    {
        private User _user;
        private readonly UserService _userService;

        public User User 
        { 
            get => _user; 
            set => SetProperty(ref _user, value); 
        }

        public UserProfileViewModel(User user, UserService userService)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task SaveChangesAsync()
        {
            await _userService.UpdateUserAsync(User);
        }

        // IDataErrorInfo implementation
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(User.FirstName):
                        return string.IsNullOrWhiteSpace(User.FirstName) ? "First Name is required" : null;
                    case nameof(User.LastName):
                        return string.IsNullOrWhiteSpace(User.LastName) ? "Last Name is required" : null;
                    case nameof(User.Email):
                        return string.IsNullOrWhiteSpace(User.Email) ? "Email is required" : null;
                    default:
                        return null;
                }
            }
        }

        public string Error => null;
    }
}
