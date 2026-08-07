using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow
{
    private bool _insertMoveInProgress;
    private bool _loadedHooksRegistered;
    private MacroStep? _editTargetStep;

    private async void RunVisible_Click(object sender, RoutedEventArgs e)
    {
        await RunMacroAsync();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loadedHooksRegistered)
        {
            return;
        }

        _loadedHooksRegistered = true;
        _steps.CollectionChanged += Steps_CollectionChanged;

        var editButton = ActionPanel.Children
            .OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "편집", StringComparison.Ordinal));

        if (editButton is not null)
        {
            editButton.PreviewMouseLeftButtonDown += EditButton_PreviewMouseLeftButtonDown;
            editButton.Click += EditButton_ClickCompleted;
        }
    }

    private void EditButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _editTargetStep = StepList.SelectedItem as MacroStep;
    }

    private void EditButton_ClickCompleted(object sender, RoutedEventArgs e)
    {
        _editTargetStep = null;
    }

    private void Steps_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_insertMoveInProgress)
        {
            return;
        }

        if (_editTargetStep is not null)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                _editTargetStep = null;
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add &&
                e.NewItems?.Count == 1 &&
                e.NewItems[0] is MacroStep addedStep)
            {
                var originalIndex = _steps.IndexOf(_editTargetStep);
                var editAddedIndex = _steps.IndexOf(addedStep);

                if (originalIndex >= 0 && editAddedIndex >= 0 && !ReferenceEquals(_editTargetStep, addedStep))
                {
                    try
                    {
                        _insertMoveInProgress = true;

                        _steps.RemoveAt(originalIndex);

                        var currentAddedIndex = _steps.IndexOf(addedStep);
                        var targetIndex = Math.Min(originalIndex, _steps.Count - 1);
                        if (currentAddedIndex >= 0 && currentAddedIndex != targetIndex)
                        {
                            _steps.Move(currentAddedIndex, targetIndex);
                        }

                        StepList.SelectedItem = addedStep;
                        StepList.ScrollIntoView(addedStep);
                        SetStatus("선택한 동작을 수정했습니다.");
                    }
                    finally
                    {
                        _insertMoveInProgress = false;
                        _editTargetStep = null;
                    }

                    return;
                }
            }
        }

        if (InsertBelowCheck.IsChecked != true ||
            e.Action != NotifyCollectionChangedAction.Add || e.NewItems?.Count != 1 ||
            e.NewItems[0] is not MacroStep newStep || StepList.SelectedItem is not MacroStep selectedStep)
        {
            return;
        }

        var selectedIndex = _steps.IndexOf(selectedStep);
        var newAddedIndex = _steps.IndexOf(newStep);
        if (selectedIndex < 0 || newAddedIndex < 0 || selectedStep == newStep)
        {
            return;
        }

        var insertIndex = Math.Min(selectedIndex + 1, _steps.Count - 1);
        if (newAddedIndex == insertIndex)
        {
            return;
        }

        try
        {
            _insertMoveInProgress = true;
            _steps.Move(newAddedIndex, insertIndex);
        }
        finally
        {
            _insertMoveInProgress = false;
        }
    }

    private void CopyInline_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (_running || sender is not Button { Tag: MacroStep sourceStep })
        {
            return;
        }

        var sourceIndex = _steps.IndexOf(sourceStep);
        if (sourceIndex < 0)
        {
            return;
        }

        var copy = CloneStep(sourceStep);
        copy.NormalizeLegacyDefaults();
        _steps.Insert(sourceIndex + 1, copy);
        StepList.SelectedItem = copy;
        StepList.ScrollIntoView(copy);
        RememberPosition(copy);
        SetStatus($"{sourceStep.Summary} 단계를 바로 아래에 복사했습니다.");
    }

    private void DeleteInline_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (_running || sender is not Button { Tag: MacroStep step })
        {
            return;
        }

        var index = _steps.IndexOf(step);
        if (index < 0)
        {
            return;
        }

        _steps.RemoveAt(index);

        if (_steps.Count == 0)
        {
            ClearEditor();
        }
        else
        {
            StepList.SelectedIndex = Math.Min(index, _steps.Count - 1);
        }

        SetStatus("선택한 동작을 삭제했습니다.");
    }
}
