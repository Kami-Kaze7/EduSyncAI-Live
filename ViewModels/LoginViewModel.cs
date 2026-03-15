using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EduSyncAI
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        private readonly BiometricAuthenticationService _biometricService;

        private string _username;
        private string _password;
        private string _pinInput;
        private string _errorMessage;
        private bool _isBiometricAvailable;
        private string _biometricMessage;
        private int _selectedTabIndex;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public string PinInput
        {
            get => _pinInput;
            set { _pinInput = value; OnPropertyChanged(nameof(PinInput)); OnPropertyChanged(nameof(PinDisplay)); }
        }

        public string PinDisplay => string.IsNullOrEmpty(_pinInput) ? "" : new string('●', _pinInput.Length);

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public bool IsBiometricAvailable
        {
            get => _isBiometricAvailable;
            set { _isBiometricAvailable = value; OnPropertyChanged(nameof(IsBiometricAvailable)); }
        }

        public string BiometricMessage
        {
            get => _biometricMessage;
            set { _biometricMessage = value; OnPropertyChanged(nameof(BiometricMessage)); }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(nameof(SelectedTabIndex)); ErrorMessage = ""; }
        }

        public ICommand LoginWithPasswordCommand { get; }
        public ICommand LoginWithPINCommand { get; }
        public ICommand LoginWithBiometricCommand { get; }
        public ICommand AddPinDigitCommand { get; }
        public ICommand ClearPinCommand { get; }
        public ICommand BackspacePinCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<Lecturer>? LoginSuccessful;
        public event EventHandler<Student>? StudentLoginSuccessful;

        public LoginViewModel()
        {
            _authService = new AuthenticationService();
            _biometricService = new BiometricAuthenticationService();

            LoginWithPasswordCommand = new RelayCommand(LoginWithPassword);
            LoginWithPINCommand = new RelayCommand(LoginWithPIN);
            LoginWithBiometricCommand = new RelayCommand(async () => await LoginWithBiometricAsync());
            AddPinDigitCommand = new RelayCommand(param => 
            {
                try
                {
                    string digit = param?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(digit))
                    {
                        AddPinDigit(digit);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error: {ex.Message}";
                }
            });
            ClearPinCommand = new RelayCommand(ClearPin);
            BackspacePinCommand = new RelayCommand(BackspacePin);

            CheckBiometricAvailability();
        }

        private async void CheckBiometricAvailability()
        {
            IsBiometricAvailable = await _biometricService.IsBiometricAvailableAsync();
            BiometricMessage = await _biometricService.GetAvailabilityMessageAsync();
        }

        private void LoginWithPassword()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username/matric number and password";
                return;
            }

            // Try lecturer first
            var lecturer = _authService.AuthenticateWithPassword(Username, Password);
            if (lecturer != null)
            {
                LoginSuccessful?.Invoke(this, lecturer);
                return;
            }

            // Try student (using matric number as username)
            var student = _authService.AuthenticateStudentWithPassword(Username, Password);
            if (student != null)
            {
                StudentLoginSuccessful?.Invoke(this, student);
                return;
            }

            ErrorMessage = "Invalid credentials";
        }

        private void LoginWithPIN()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length < 4)
            {
                ErrorMessage = "Please enter a 4-6 digit PIN";
                return;
            }

            // Try lecturer first
            var lecturer = _authService.AuthenticateWithPIN(PinInput);
            if (lecturer != null)
            {
                LoginSuccessful?.Invoke(this, lecturer);
                return;
            }

            // Try student
            var student = _authService.AuthenticateStudentWithPIN(PinInput);
            if (student != null)
            {
                StudentLoginSuccessful?.Invoke(this, student);
                return;
            }

            ErrorMessage = "Invalid PIN";
            PinInput = "";
        }

        private async Task LoginWithBiometricAsync()
        {
            ErrorMessage = "";

            if (!IsBiometricAvailable)
            {
                ErrorMessage = "Biometric authentication not available";
                return;
            }

            bool authenticated = await _biometricService.AuthenticateWithBiometricAsync("Verify your identity to access EduSync AI");
            
            if (authenticated)
            {
                // For now, use a default lecturer (in production, link biometric to lecturer account)
                // This is a simplified implementation
                ErrorMessage = "Biometric verified! Please also enter your username for account linking.";
                SelectedTabIndex = 1; // Switch to password tab
            }
            else
            {
                ErrorMessage = "Biometric verification failed";
            }
        }


        private void AddPinDigit(string digit)
        {
            if (string.IsNullOrEmpty(digit))
            {
                return;
            }

            if (PinInput == null)
            {
                PinInput = "";
            }

            if (PinInput.Length < 6)
            {
                PinInput += digit;
                
                // Auto-login when 4-6 digits entered
                if (PinInput.Length >= 4)
                {
                    LoginWithPIN();
                }
            }
        }

        private void ClearPin()
        {
            PinInput = "";
            ErrorMessage = "";
        }

        private void BackspacePin()
        {
            if (!string.IsNullOrEmpty(PinInput))
            {
                PinInput = PinInput.Substring(0, PinInput.Length - 1);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
