using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using userspace_backend.Logging;

namespace userinterface.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private readonly ILoggingService loggingService;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Specific property name for language changes
    public const string LanguageChangedPropertyName = "CurrentLanguage";

    public LocalizationService(ILoggingService loggingService)
    {
        this.loggingService = loggingService;
    }

    public bool TryChangeLanguage(string cultureCode, out CultureInfo? culture)
    {
        culture = null;
        try
        {
            culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Properties.Resources.Strings.Culture = culture;

            OnPropertyChanged(LanguageChangedPropertyName);
            return true;
        }
        catch (CultureNotFoundException ex)
        {
            loggingService.LogError(LogSource.UI, ex, "Failed to change language to culture code: {CultureCode}", cultureCode);
            return false;
        }
    }

    public void ChangeLanguage(string cultureCode)
    {
        TryChangeLanguage(cultureCode, out _);
    }

    public string GetText(string key)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = Properties.Resources.Strings.ResourceManager.GetString(key) ?? key;

        if (stopwatch.ElapsedMilliseconds >= 10)
        {
        }

        return result;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}