using System.Windows;
using GameMacroAssistant.Wpf.ViewModels;

namespace GameMacroAssistant.Wpf.Views;

public partial class EditorView : Window
{
    public EditorView(EditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}