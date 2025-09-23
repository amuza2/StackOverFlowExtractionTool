using CommunityToolkit.Mvvm.ComponentModel;
using StackOverFlowExtractionTool.ViewModels;

namespace StackOverFlowExtractionTool.Models;

public partial class FilterTag : ViewModelBase
{
    [ObservableProperty]
    private string _tag = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}