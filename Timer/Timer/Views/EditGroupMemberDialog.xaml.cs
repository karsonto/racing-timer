using System.Windows;
using Timer.ViewModels;

namespace Timer.Views
{
    /// <summary>
    /// 编辑分组成员对话框
    /// </summary>
    public partial class EditGroupMemberDialog : Window
    {
        public EditGroupMemberDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditGroupMemberDialogViewModel viewModel)
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




