using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IStackOverflowService _stackOverflowService;

    [ObservableProperty]
    private string _tag = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Enter a tag and click Search";

    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _questions = new();

    [ObservableProperty]
    private StackOverflowQuestion? _selectedQuestion;
    
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty] private bool _hasMore;
    
    public List<PageSizeOption> Pagesizes {get;} = new()
    {
        new PageSizeOption(10),
        new PageSizeOption(20),
        new PageSizeOption(30),
        new PageSizeOption(50),
    };
    
    [ObservableProperty]
    private PageSizeOption _selectedPageSize;
    
    [ObservableProperty]
    private bool _canGoToPreviousPage;

    [ObservableProperty] private bool _canGoToNextPage;
    
    public string PaginationInfo => HasMore || CurrentPage > 1 
        ? $"Page {CurrentPage} of {(HasMore ? "many" : CurrentPage)} • {Questions.Count} questions"
        : $"{Questions.Count} questions found";
    

    public MainWindowViewModel(IStackOverflowService stackOverflowService)
    {
        _stackOverflowService = stackOverflowService ?? throw new ArgumentNullException(nameof(stackOverflowService));
        SelectedPageSize = Pagesizes.First(x => x.Value == 10);
    }

    public MainWindowViewModel() : this(null!) { }

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(PaginationInfo));
    }

    partial void OnSelectedPageSizeChanged(PageSizeOption obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        
        var option = Pagesizes.FirstOrDefault(x => x.Value == obj?.Value);
        if (option != null && option != SelectedPageSize)
            SelectedPageSize = option;

        if (CurrentPage != 1)
            CurrentPage = 1;

        if (!string.IsNullOrWhiteSpace(Tag) && !IsLoading)
        {
            _ = SearchQuestions();
        }
        
        StatusMessage = $"Page size changed to {obj.Value}. {StatusMessage}";
    }
    
    [RelayCommand]
    private async Task SearchQuestions()
    {
        if (string.IsNullOrWhiteSpace(Tag))
        {
            StatusMessage = "Please enter a tag";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Loading questions for tag: {Tag}...";

        try
        {
            var questions = await _stackOverflowService.GetRecentQuestionsByTagAsync(Tag.Trim(), CurrentPage, SelectedPageSize.Value + 1);
            Questions.Clear();
            
            HasMore = questions.Count > SelectedPageSize.Value;
            var questionsToShow = questions.Take(SelectedPageSize.Value).ToList();
            
            foreach (var question in questionsToShow)
            {
                Questions.Add(question);
            }
            
            StatusMessage = questions.Count > 0 
                ? PaginationInfo
                : $"No questions found for tag: {Tag}";
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"Invalid input: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadFirstPage()
    {
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            await SearchQuestions();
        }
    }

    [RelayCommand]
    private void OpenQuestionInBrowser(StackOverflowQuestion question)
    {
            try
            {
                var url = question.Link;
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
                StatusMessage = $"Opened in browser: {question.Title}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening browser: {ex.Message}";
            }
    }

    [RelayCommand]
    private void ClearResults()
    {
        Questions.Clear();
        Tag = string.Empty;
        SelectedQuestion = null;
        StatusMessage = "Enter a tag and click Search";
    }
    
    [RelayCommand] 
    private async Task RefreshQuestions()
    {
        if (!string.IsNullOrWhiteSpace(Tag))
        {
            await SearchQuestions();
        }
    }
}