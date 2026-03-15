using System;
using System.Windows;

namespace EduSyncAI
{
    public partial class LoginWindow : Window
    {
        private LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            _viewModel.LoginSuccessful += OnLecturerLoginSuccessful;
            _viewModel.StudentLoginSuccessful += OnStudentLoginSuccessful;
            
            // Handle password box separately (WPF limitation)
            PasswordBox.PasswordChanged += (s, e) => _viewModel.Password = PasswordBox.Password;
        }

        private void OnLecturerLoginSuccessful(object sender, Lecturer lecturer)
        {
            // Open lecturer dashboard
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void OnStudentLoginSuccessful(object sender, Student student)
        {
            // Open student dashboard
            var studentWindow = new StudentMainWindow();
            studentWindow.Show();
            this.Close();
        }
    }
}
