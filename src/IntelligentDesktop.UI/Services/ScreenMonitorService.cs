namespace IntelligentDesktop.UI.Services;

using System.Windows;
using System.Windows.Interop;

/// <summary>
/// 해상도/디스플레이 변경 감지 서비스
/// </summary>
public class ScreenMonitorService : IDisposable
{
    private HwndSource? _hwndSource;
    private bool _disposed;
    
    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_SETTINGCHANGE = 0x001A;
    
    /// <summary>
    /// 해상도 변경 이벤트
    /// </summary>
    public event EventHandler<ScreenChangedEventArgs>? ScreenChanged;

    /// <summary>
    /// 윈도우를 감시 대상으로 등록
    /// </summary>
    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DISPLAYCHANGE || msg == WM_SETTINGCHANGE)
        {
            // 새 해상도 가져오기
            double newWidth = SystemParameters.PrimaryScreenWidth;
            double newHeight = SystemParameters.PrimaryScreenHeight;
            
            ScreenChanged?.Invoke(this, new ScreenChangedEventArgs(newWidth, newHeight));
        }
        
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 화면 변경 이벤트 인자
/// </summary>
public class ScreenChangedEventArgs : EventArgs
{
    public double NewWidth { get; }
    public double NewHeight { get; }
    
    public ScreenChangedEventArgs(double width, double height)
    {
        NewWidth = width;
        NewHeight = height;
    }
}
