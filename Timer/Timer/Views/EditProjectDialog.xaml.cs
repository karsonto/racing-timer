using System.Windows;
using Timer.ViewModels;

namespace Timer.Views
{
    public partial class EditProjectDialog : Window
    {
        public EditProjectDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditProjectDialogViewModel viewModel)
            {
                viewModel.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose(bool dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }
    }
}
