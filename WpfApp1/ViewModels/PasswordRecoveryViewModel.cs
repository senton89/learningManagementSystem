using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.ViewModels;

public partial class PasswordRecoveryViewModel : ViewModelBase
{
    private readonly LmsDbContext _dbContext;

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email = string.Empty;

    public PasswordRecoveryViewModel(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [RelayCommand]
    private async Task RecoverPassword()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            StatusMessage = string.Join("\n", GetErrors().Select(e => e.ErrorMessage));
            return;
        }

        IsBusy = true;
        StatusMessage = "Processing request...";

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                StatusMessage = "Email not found";
                return;
            }

            // TODO: Implement actual password recovery logic
            StatusMessage = "Password recovery instructions sent to your email";
        }
        catch (Exception ex)
        {
            StatusMessage = "Password recovery failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
