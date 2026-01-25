using System.Windows;
using Timer.ViewModels;

namespace Timer.Views
{
    /// <summary>
    /// 编辑比赛分组对话框
    /// </summary>
    public partial class EditRaceGroupDialog : Window
    {
        public EditRaceGroupDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditRaceGroupDialogViewModel viewModel)
            {
                viewModel.RequestClose += (s, args) =>
                {
                    DialogResult = viewModel.DialogResult;
                    Close();
                };
            }
        }
    }
}




