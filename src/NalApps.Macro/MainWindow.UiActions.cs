using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow
{
    private bool _insertMoveInProgress;
    private bool _loadedHooksRegistered;

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
    }

    private void Steps_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_insertMoveInProgress || InsertBelowCheck.IsChecked != true ||
            e.Action != NotifyCollectionChangedAction.Add || e.NewItems?.Count != 1 ||
            e.NewItems[0] is not MacroStep addedStep || StepList.SelectedItem is not MacroStep selectedStep)
        {
            return;
        }

        var selectedIndex = _steps.IndexOf(selectedStep);
        var addedIndex = _steps.IndexOf(addedStep);
        if (selectedIndex < 0 || addedIndex < 0 || selectedStep == addedStep)
        {
            return;
        }

        var targetIndex = Math.Min(selectedIndex + 1, _steps.Count - 1);
        if (addedIndex == targetIndex)
        {
            return;
        }

        try
        {
            _insertMoveInProgress = true;
            _steps.Move(addedIndex, targetIndex);
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
