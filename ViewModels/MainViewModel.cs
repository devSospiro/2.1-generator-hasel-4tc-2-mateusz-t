using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PasswordGenerator.Helpers;

namespace PasswordGenerator.ViewModels
{
    /// <summary>
    /// Główny ViewModel dla aplikacji generatora haseł.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _generatedPassword = string.Empty;
        private int _passwordLength = 16;
        private bool _includeUppercase = true;
        private bool _includeLowercase = true;
        private bool _includeDigits = true;
        private bool _includeSpecial = true;
        private bool _excludeAmbiguous = false;
        private bool _isPasswordHidden = false;
        private bool _isDarkMode = true;
        private int _passwordStrength = 0;
        private string _strengthLabel = "Brak hasła";
        private string _strengthColor = "#6366F1";
        private double _entropyBits = 0;
        private string _crackTimeEstimate = "-";
        private bool _isCopiedToastVisible = false;
        private string _themeIcon = "☀️";
        private string _themeTooltip = "Przełącz na tryb jasny";
        private readonly DispatcherTimer _toastTimer;

        public MainViewModel()
        {
            History = new ObservableCollection<string>();

            GenerateCommand = new RelayCommand(_ => GeneratePassword());
            CopyCommand = new RelayCommand(param => CopyPassword(param as string), _ => HasPassword);
            ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
            TogglePasswordVisibilityCommand = new RelayCommand(_ => TogglePasswordVisibility());
            SetLengthPresetCommand = new RelayCommand(param => SetLengthPreset(param));
            ClearHistoryCommand = new RelayCommand(_ => History.Clear());

            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _toastTimer.Tick += (s, e) =>
            {
                IsCopiedToastVisible = false;
                _toastTimer.Stop();
            };

            // Wygeneruj hasło na start
            GeneratePassword();
        }

        #region Properties

        public ObservableCollection<string> History { get; }

        public string GeneratedPassword
        {
            get => _generatedPassword;
            set
            {
                if (_generatedPassword != value)
                {
                    _generatedPassword = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayedPassword));
                    OnPropertyChanged(nameof(HasPassword));
                    UpdateStrengthAndMetrics();
                }
            }
        }

        public string DisplayedPassword
        {
            get
            {
                if (string.IsNullOrEmpty(_generatedPassword)) return string.Empty;
                return _isPasswordHidden ? new string('●', _generatedPassword.Length) : _generatedPassword;
            }
        }

        public bool HasPassword => !string.IsNullOrEmpty(_generatedPassword);

        public int PasswordLength
        {
            get => _passwordLength;
            set
            {
                if (_passwordLength != value)
                {
                    _passwordLength = Math.Clamp(value, 4, 64);
                    OnPropertyChanged();
                    GeneratePassword();
                }
            }
        }

        public bool IncludeUppercase
        {
            get => _includeUppercase;
            set
            {
                if (_includeUppercase != value)
                {
                    _includeUppercase = value;
                    OnPropertyChanged();
                    EnsureAtLeastOneSelected(nameof(IncludeUppercase));
                    GeneratePassword();
                }
            }
        }

        public bool IncludeLowercase
        {
            get => _includeLowercase;
            set
            {
                if (_includeLowercase != value)
                {
                    _includeLowercase = value;
                    OnPropertyChanged();
                    EnsureAtLeastOneSelected(nameof(IncludeLowercase));
                    GeneratePassword();
                }
            }
        }

        public bool IncludeDigits
        {
            get => _includeDigits;
            set
            {
                if (_includeDigits != value)
                {
                    _includeDigits = value;
                    OnPropertyChanged();
                    EnsureAtLeastOneSelected(nameof(IncludeDigits));
                    GeneratePassword();
                }
            }
        }

        public bool IncludeSpecial
        {
            get => _includeSpecial;
            set
            {
                if (_includeSpecial != value)
                {
                    _includeSpecial = value;
                    OnPropertyChanged();
                    EnsureAtLeastOneSelected(nameof(IncludeSpecial));
                    GeneratePassword();
                }
            }
        }

        public bool ExcludeAmbiguous
        {
            get => _excludeAmbiguous;
            set
            {
                if (_excludeAmbiguous != value)
                {
                    _excludeAmbiguous = value;
                    OnPropertyChanged();
                    GeneratePassword();
                }
            }
        }

        public bool IsPasswordHidden
        {
            get => _isPasswordHidden;
            set
            {
                if (_isPasswordHidden != value)
                {
                    _isPasswordHidden = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayedPassword));
                    OnPropertyChanged(nameof(VisibilityIcon));
                }
            }
        }

        public string VisibilityIcon => _isPasswordHidden ? "👁️‍🗨️" : "👁️";

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    ThemeIcon = _isDarkMode ? "☀️" : "🌙";
                    ThemeTooltip = _isDarkMode ? "Przełącz na tryb jasny" : "Przełącz na tryb ciemny";
                }
            }
        }

        public string ThemeIcon
        {
            get => _themeIcon;
            set { _themeIcon = value; OnPropertyChanged(); }
        }

        public string ThemeTooltip
        {
            get => _themeTooltip;
            set { _themeTooltip = value; OnPropertyChanged(); }
        }

        public int PasswordStrength
        {
            get => _passwordStrength;
            set { _passwordStrength = value; OnPropertyChanged(); }
        }

        public string StrengthLabel
        {
            get => _strengthLabel;
            set { _strengthLabel = value; OnPropertyChanged(); }
        }

        public string StrengthColor
        {
            get => _strengthColor;
            set { _strengthColor = value; OnPropertyChanged(); }
        }

        public double EntropyBits
        {
            get => _entropyBits;
            set { _entropyBits = value; OnPropertyChanged(); }
        }

        public string CrackTimeEstimate
        {
            get => _crackTimeEstimate;
            set { _crackTimeEstimate = value; OnPropertyChanged(); }
        }

        public bool IsCopiedToastVisible
        {
            get => _isCopiedToastVisible;
            set { _isCopiedToastVisible = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand GenerateCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand SetLengthPresetCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        #endregion

        #region Methods

        private void EnsureAtLeastOneSelected(string changedProperty)
        {
            if (!_includeUppercase && !_includeLowercase && !_includeDigits && !_includeSpecial)
            {
                // Przywróć zmienioną właściwość lub włącz małe litery
                switch (changedProperty)
                {
                    case nameof(IncludeUppercase): _includeUppercase = true; OnPropertyChanged(nameof(IncludeUppercase)); break;
                    case nameof(IncludeLowercase): _includeLowercase = true; OnPropertyChanged(nameof(IncludeLowercase)); break;
                    case nameof(IncludeDigits): _includeDigits = true; OnPropertyChanged(nameof(IncludeDigits)); break;
                    case nameof(IncludeSpecial): _includeSpecial = true; OnPropertyChanged(nameof(IncludeSpecial)); break;
                    default: _includeLowercase = true; OnPropertyChanged(nameof(IncludeLowercase)); break;
                }
            }
        }

        public void GeneratePassword()
        {
            if (!_includeUppercase && !_includeLowercase && !_includeDigits && !_includeSpecial)
            {
                GeneratedPassword = string.Empty;
                StrengthLabel = "Zaznacz opcje!";
                PasswordStrength = 0;
                return;
            }

            try
            {
                string newPassword = PasswordGeneratorEngine.Generate(
                    PasswordLength,
                    IncludeUppercase,
                    IncludeLowercase,
                    IncludeDigits,
                    IncludeSpecial,
                    ExcludeAmbiguous
                );

                GeneratedPassword = newPassword;

                // Dodaj do historii (unikając duplikatów z rzędu)
                if (History.Count == 0 || History[0] != newPassword)
                {
                    History.Insert(0, newPassword);
                    while (History.Count > 10)
                    {
                        History.RemoveAt(History.Count - 1);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                GeneratedPassword = string.Empty;
                StrengthLabel = ex.Message;
            }
        }

        public void CopyPassword(string? text = null)
        {
            string passwordToCopy = string.IsNullOrEmpty(text) ? GeneratedPassword : text;

            if (!string.IsNullOrEmpty(passwordToCopy))
            {
                try
                {
                    Clipboard.SetDataObject(passwordToCopy, true);
                    IsCopiedToastVisible = true;
                    _toastTimer.Stop();
                    _toastTimer.Start();
                }
                catch
                {
                    // Obsługa potencjalnej blokady schowka w systemie Windows
                }
            }
        }

        private void TogglePasswordVisibility()
        {
            IsPasswordHidden = !IsPasswordHidden;
        }

        private void SetLengthPreset(object? param)
        {
            if (param is string strVal && int.TryParse(strVal, out int length))
            {
                PasswordLength = length;
            }
            else if (param is int intVal)
            {
                PasswordLength = intVal;
            }
        }

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;

            var app = Application.Current;
            if (app == null) return;

            var mergedDicts = app.Resources.MergedDictionaries;

            // Znajdź indeks aktualnego słownika motywu
            int themeIndex = -1;
            for (int i = 0; i < mergedDicts.Count; i++)
            {
                var dict = mergedDicts[i];
                if (dict.Source != null &&
                    (dict.Source.OriginalString.Contains("DarkTheme.xaml") ||
                     dict.Source.OriginalString.Contains("LightTheme.xaml")))
                {
                    themeIndex = i;
                    break;
                }
            }

            var themeUri = IsDarkMode
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            var newDict = new ResourceDictionary { Source = themeUri };

            if (themeIndex >= 0)
            {
                mergedDicts[themeIndex] = newDict;
            }
            else
            {
                mergedDicts.Insert(0, newDict);
            }
        }

        private void UpdateStrengthAndMetrics()
        {
            if (string.IsNullOrEmpty(GeneratedPassword))
            {
                PasswordStrength = 0;
                StrengthLabel = "Brak hasła";
                StrengthColor = "#64748B";
                EntropyBits = 0;
                CrackTimeEstimate = "-";
                return;
            }

            PasswordStrength = PasswordGeneratorEngine.CalculateStrength(GeneratedPassword);
            StrengthLabel = PasswordGeneratorEngine.GetStrengthLabel(PasswordStrength);
            EntropyBits = PasswordGeneratorEngine.CalculateEntropy(GeneratedPassword);
            CrackTimeEstimate = PasswordGeneratorEngine.EstimateCrackTime(EntropyBits);

            StrengthColor = PasswordStrength switch
            {
                < 30 => "#EF4444", // Czerwony
                < 55 => "#F59E0B", // Pomarańczowy
                < 75 => "#EAB308", // Żółty
                < 90 => "#10B981", // Zielony szmaragdowy
                _ => "#06B6D4"     // Turkusowy / Cyjan dla bardzo silnego
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
