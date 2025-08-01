using System.Windows;
using GameMacroAssistant.Wpf.ViewModels;

namespace GameMacroAssistant.Wpf.Views;

public partial class MainView : Window
{
    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}