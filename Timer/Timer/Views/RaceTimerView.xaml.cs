using System.Windows.Controls;
using Timer.ViewModels;

namespace Timer.Views
{
    public partial class RaceTimerView : UserControl
    {
        public RaceTimerView()
        {
            InitializeComponent();
        }

        public RaceTimerView(MultiRaceTimerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        // 兼容旧的 ViewModel
        public RaceTimerView(RaceTimerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
