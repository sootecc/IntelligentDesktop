using System.Windows;

namespace IntelligentDesktop.UI.Views;

/// <summary>
/// IME 입력을 지원하는 제목 편집 전용 창
/// AllowsTransparency 창에서 IME가 작동하지 않는 WPF 문제를 우회
/// </summary>
public partial class TitleEditWindow : Window
{
    public string ResultText { get; private set; } = string.Empty;
    public bool IsConfirmed { get; private set; } = false;

    public TitleEditWindow(string initialText)
    {
        InitializeComponent();
        EditBox.Text = initialText;

        Loaded += (s, e) =>
        {
            EditBox.Focus();
            EditBox.SelectAll();
        };
    }

    private void EditBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            Confirm();
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            Cancel();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // 포커스를 잃으면 확인 처리
        if (IsVisible)
        {
            Confirm();
        }
    }

    private void Confirm()
    {
        ResultText = EditBox.Text;
        IsConfirmed = true;
        Close();
    }

    private void Cancel()
    {
        IsConfirmed = false;
        Close();
    }
}
