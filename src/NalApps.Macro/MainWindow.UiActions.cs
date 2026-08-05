using System.Windows;
using System.Windows.Controls;
using NalApps.Macro.Models;

namespace NalApps.Macro;

public partial class MainWindow
{
    private async void RunVisible_Click(object sender, RoutedEventArgs e)
    {
        await RunMacroAsync();
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
