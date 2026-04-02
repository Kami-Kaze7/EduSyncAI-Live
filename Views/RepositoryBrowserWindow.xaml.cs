using System.Windows;
using System.Windows.Controls;
using EduSyncAI.Services;

namespace EduSyncAI.Views
{
    public partial class RepositoryBrowserWindow : Window
    {
        private readonly RepositoryService _repoService;
        public Model3DAssetDto SelectedAsset { get; private set; }

        public RepositoryBrowserWindow()
        {
            InitializeComponent();
            _repoService = new RepositoryService();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void DisciplineCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string discipline)
            {
                // Switch view
                DisciplinesGrid.Visibility = Visibility.Collapsed;
                ModelsGrid.Visibility = Visibility.Visible;
                CurrentDisciplineText.Text = $"{discipline} Models";
                
                LoadingText.Visibility = Visibility.Visible;
                EmptyText.Visibility = Visibility.Collapsed;
                ModelsItemsControl.ItemsSource = null;

                // Fetch models
                var models = await _repoService.GetModelsByDisciplineAsync(discipline);
                
                LoadingText.Visibility = Visibility.Collapsed;
                if (models.Count == 0)
                {
                    EmptyText.Visibility = Visibility.Visible;
                }
                else
                {
                    ModelsItemsControl.ItemsSource = models;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ModelsGrid.Visibility = Visibility.Collapsed;
            DisciplinesGrid.Visibility = Visibility.Visible;
        }

        private void ModelItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Model3DAssetDto asset)
            {
                SelectedAsset = asset;
                DialogResult = true; // Close the window and return selection to Whiteboard
                Close();
            }
        }
    }
}
