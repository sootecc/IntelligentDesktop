namespace IntelligentDesktop.Core.Services.Input;

using IntelligentDesktop.Core.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// 전역 마우스 훅 서비스 - 바탕화면 더블클릭 감지용
/// </summary>
public partial class GlobalMouseHook : IDisposable
{
    private IntPtr _hookHandle = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _hookProc;
    private bool _disposed;
    private DateTime _lastClickTime = DateTime.MinValue;
    private NativeMethods.POINT _lastClickPoint;
    private const int DOUBLE_CLICK_TIME_MS = 500;
    private const int DOUBLE_CLICK_DISTANCE = 4;

    /// <summary>
    /// 바탕화면 더블클릭 이벤트
    /// </summary>
    public event EventHandler? DesktopDoubleClicked;

    /// <summary>
    /// 훅 시작
    /// </summary>
    public void Start()
    {
        if (_hookHandle != IntPtr.Zero) return;

        _hookProc = HookCallback;
        
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        
        _hookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            NativeMethods.GetModuleHandleW(curModule?.ModuleName),
            0);
    }

    /// <summary>
    /// 훅 중지
    /// </summary>
    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            const int WM_LBUTTONDOWN = 0x0201;
            
            if ((int)wParam == WM_LBUTTONDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                
                // 더블클릭 감지 (시간 + 거리)
                var now = DateTime.Now;
                var timeDiff = (now - _lastClickTime).TotalMilliseconds;
                var distance = Math.Sqrt(
                    Math.Pow(hookStruct.pt.X - _lastClickPoint.X, 2) + 
                    Math.Pow(hookStruct.pt.Y - _lastClickPoint.Y, 2));
                
                if (timeDiff < DOUBLE_CLICK_TIME_MS && distance < DOUBLE_CLICK_DISTANCE)
                {
                    // 더블클릭 감지됨 - 클릭 위치가 바탕화면인지 확인
                    if (IsClickOnDesktop(hookStruct.pt))
                    {
                        DesktopDoubleClicked?.Invoke(this, EventArgs.Empty);
                    }
                    
                    _lastClickTime = DateTime.MinValue; // 리셋
                }
                else
                {
                    _lastClickTime = now;
                    _lastClickPoint = hookStruct.pt;
                }
            }
        }
        
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    /// 클릭 위치가 바탕화면(Progman/WorkerW)인지 확인
    /// </summary>
    private bool IsClickOnDesktop(NativeMethods.POINT pt)
    {
        IntPtr hwndAtPoint = WindowFromPoint(pt);
        if (hwndAtPoint == IntPtr.Zero) return false;

        // 클릭된 창의 클래스 이름 확인
        var className = GetWindowClassName(hwndAtPoint);
        
        // 바탕화면 관련 클래스들
        return className == "Progman" || 
               className == "WorkerW" || 
               className == "SysListView32" ||
               className == "SHELLDLL_DefView";
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr WindowFromPoint(NativeMethods.POINT pt);

    private static string GetWindowClassName(IntPtr hwnd)
    {
        const int maxLength = 256;
        var className = new char[maxLength];
        int length = GetClassNameW(hwnd, className, maxLength);
        return length > 0 ? new string(className, 0, length) : string.Empty;
    }

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetClassNameW(IntPtr hWnd, [Out] char[] lpClassName, int nMaxCount);

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _hookProc = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
