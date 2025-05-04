using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WpfApp1.ViewModels;

public abstract class ViewModelBase : ObservableValidator
{
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    // Метод для очистки всех ошибок
    public void ClearErrors()
    {
        StatusMessage = string.Empty;
    }

    // Получение всех ошибок валидации
    public IEnumerable<ValidationResult> GetErrors()
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), validationResults, true);
        return validationResults;
    }
}
