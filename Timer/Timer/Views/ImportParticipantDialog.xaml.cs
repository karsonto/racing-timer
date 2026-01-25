using System.Windows;
using Timer.ViewModels;

namespace Timer.Views
{
    public partial class ImportParticipantDialog : Window
    {
        public ImportParticipantDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ImportParticipantDialogViewModel viewModel)
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
