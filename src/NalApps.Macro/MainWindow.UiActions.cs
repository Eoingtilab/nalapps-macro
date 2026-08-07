using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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

        // ObservableCollection은 CollectionChanged 이벤트 처리 중 같은 컬렉션을 다시
        // Remove/Move 하면 reentrancy 예외를 던질 수 있다. 따라서 편집 복구 및
        // "선택 단계 아래 추가" 재배치는 이벤트가 완전히 끝난 뒤 Dispatcher에서 수행한다.
        if (_editTargetStep is not null)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                _editTargetStep = null;
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add &&
                e.NewItems?.Count == 1 &&
                e.NewItems[0] is MacroStep editAddedStep)
            {
                var editTarget = _editTargetStep;
                _ = Dispatcher.BeginInvoke(
                    DispatcherPriority.DataBind,
                    new Action(() => RepairUnexpectedEditAdd(editTarget, editAddedStep)));
                return;
            }
        }

        if (InsertBelowCheck.IsChecked != true ||
            e.Action != NotifyCollectionChangedAction.Add || e.NewItems?.Count != 1 ||
            e.NewItems[0] is not MacroStep newStep || StepList.SelectedItem is not MacroStep selectedStep ||
            ReferenceEquals(selectedStep, newStep))
        {
            return;
        }

        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.DataBind,
            new Action(() => MoveNewStepBelowSelection(selectedStep, newStep)));
    }

    private void RepairUnexpectedEditAdd(MacroStep? editTarget, MacroStep addedStep)
    {
        if (editTarget is null || _insertMoveInProgress)
        {
            return;
        }

        var originalIndex = _steps.IndexOf(editTarget);
        var addedIndex = _steps.IndexOf(addedStep);
        if (originalIndex < 0 || addedIndex < 0 || ReferenceEquals(editTarget, addedStep))
        {
            _editTargetStep = null;
            return;
        }

        try
        {
            _insertMoveInProgress = true;
            _steps.RemoveAt(originalIndex);

            var currentAddedIndex = _steps.IndexOf(addedStep);
            var targetIndex = Math.Min(originalIndex, Math.Max(0, _steps.Count - 1));
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
    }

    private void MoveNewStepBelowSelection(MacroStep selectedStep, MacroStep newStep)
    {
        if (_insertMoveInProgress || InsertBelowCheck.IsChecked != true)
        {
            return;
        }

        var selectedIndex = _steps.IndexOf(selectedStep);
        var newAddedIndex = _steps.IndexOf(newStep);
        if (selectedIndex < 0 || newAddedIndex < 0 || ReferenceEquals(selectedStep, newStep))
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
            StepList.SelectedItem = newStep;
            StepList.ScrollIntoView(newStep);
            SetStatus($"{newStep.Summary} 동작을 선택 단계 아래에 추가했습니다.");
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
