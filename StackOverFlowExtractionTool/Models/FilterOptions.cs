using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StackOverFlowExtractionTool.Models;

public class FilterOptions : INotifyPropertyChanged
{
    private QuickFilterOption? _selectedFilter;
    
    public ObservableCollection<QuickFilterOption> AvailableFilters { get; } = new();
    public QuickFilterOption? SelectedFilter 
    { 
        get => _selectedFilter;
        set
        {
            _selectedFilter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFilterActive));
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public bool IsFilterActive => SelectedFilter != null && SelectedFilter != AvailableFilters.First();
    
    public event EventHandler? FilterChanged;
    
    public FilterOptions()
    {
        InitializeFilters();
    }
    
    private void InitializeFilters()
    {
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "All Questions", 
            Filter = q => true,
            Description = "Show all questions"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "Answered", 
            Filter = q => q.IsAnswered,
            Description = "Questions with accepted answers"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "Unanswered", 
            Filter = q => !q.IsAnswered,
            Description = "Questions without answers"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "Has Accepted Answer", 
            Filter = q => q.AcceptedAnswerId.HasValue,
            Description = "Questions with accepted answers"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "High Score (10+)", 
            Filter = q => q.Score >= 10,
            Description = "Questions with score of 10 or more"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "Popular (100+ views)", 
            Filter = q => q.ViewCount >= 100,
            Description = "Questions with 100+ views"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "Many Answers (5+)", 
            Filter = q => q.AnswerCount >= 5,
            Description = "Questions with 5 or more answers"
        });
        
        AvailableFilters.Add(new QuickFilterOption 
        { 
            DisplayName = "New (Last 7 days)", 
            Filter = q => q.CreationDateTime >= DateTime.Now.AddDays(-7),
            Description = "Questions from the last 7 days"
        });
        
        // Set default filter
        SelectedFilter = AvailableFilters.First();
    }
    
    public void ClearFilter()
    {
        SelectedFilter = AvailableFilters.First(); // "All Questions"
    }
    
    public IEnumerable<StackOverflowQuestion> ApplyFilter(IEnumerable<StackOverflowQuestion> questions)
    {
        return SelectedFilter?.Filter != null 
            ? questions.Where(SelectedFilter.Filter) 
            : questions;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}