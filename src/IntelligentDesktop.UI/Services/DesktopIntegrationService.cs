namespace IntelligentDesktop.UI.Services;

using System.Windows;
using System.Windows.Interop;
using IntelligentDesktop.Core.Interop;

/// <summary>
/// 데스크톱 통합 서비스 - 박스 창을 데스크톱의 일부로 만들어 Win+D에도 사라지지 않게 함
/// </summary>
public class DesktopIntegrationService : IDisposable
{
    private IntPtr _workerW = IntPtr.Zero;
    private IntPtr _progman = IntPtr.Zero;
    private IntPtr _winEventHook = IntPtr.Zero;
    private NativeMethods.WinEventDelegate? _winEventDelegate;
    private bool _disposed;
    private readonly List<IntPtr> _registeredWindows = new();

    public event EventHandler? DesktopIconsChanged;

    /// <summary>
    /// 데스크톱 통합 초기화 - WorkerW 창 찾기/생성
    /// </summary>
    public void Initialize()
    {
        // Progman 윈도우 찾기
        _progman = NativeMethods.FindWindowW("Progman", null);
        
        if (_progman != IntPtr.Zero)
        {
            // WorkerW 생성을 요청 (동적 배경화면과 같은 방식)
            NativeMethods.SendMessageW(_progman, NativeMethods.WM_SPAWN_WORKER, 0, 0);
            
            // WorkerW 찾기
            _workerW = FindWorkerW();
        }

        // WinEventHook 설정 (이벤트 기반 아이콘 모니터링)
        SetupWinEventHook();
    }

    private IntPtr FindWorkerW()
    {
        IntPtr workerW = IntPtr.Zero;
        IntPtr desktopWindow = NativeMethods.GetDesktopWindow();
        IntPtr current = IntPtr.Zero;

        do
        {
            current = NativeMethods.FindWindowExW(desktopWindow, current, "WorkerW", null);
            if (current != IntPtr.Zero)
            {
                IntPtr defView = NativeMethods.FindWindowExW(current, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    // SHELLDLL_DefView 다음에 있는 WorkerW를 찾음
                    workerW = NativeMethods.FindWindowExW(desktopWindow, current, "WorkerW", null);
                    break;
                }
            }
        } while (current != IntPtr.Zero);

        return workerW;
    }

    /// <summary>
    /// 박스 창을 데스크톱의 자식으로 등록
    /// </summary>
    public void RegisterAsDesktopChild(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            // 창이 로드되지 않았으면 Loaded 이벤트에서 다시 시도
            window.Loaded += (s, e) => RegisterAsDesktopChildInternal(window);
            return;
        }

        RegisterAsDesktopChildInternal(window);
    }

    private void RegisterAsDesktopChildInternal(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        // UIPI 메시지 필터 허용 (드래그 앤 드롭용)
        AllowUIPI(hwnd);

        // WorkerW가 있으면 그 자식으로 등록, 없으면 Progman의 자식으로
        IntPtr parentHwnd = _workerW != IntPtr.Zero ? _workerW : _progman;
        
        if (parentHwnd != IntPtr.Zero)
        {
            NativeMethods.SetParent(hwnd, parentHwnd);
            _registeredWindows.Add(hwnd);
        }
        else
        {
            // 부모 등록 실패 시 HWND_BOTTOM으로 대체
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_BOTTOM,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    /// <summary>
    /// UIPI 필터 허용 - 관리자 권한과 사용자 권한 간 메시지 통신 허용
    /// </summary>
    private void AllowUIPI(IntPtr hwnd)
    {
        // 드래그 앤 드롭 관련 메시지 허용
        NativeMethods.ChangeWindowMessageFilterEx(hwnd, NativeMethods.WM_DROPFILES, NativeMethods.MSGFLT_ALLOW, IntPtr.Zero);
        NativeMethods.ChangeWindowMessageFilterEx(hwnd, NativeMethods.WM_COPYDATA, NativeMethods.MSGFLT_ALLOW, IntPtr.Zero);
        NativeMethods.ChangeWindowMessageFilterEx(hwnd, NativeMethods.WM_COPYGLOBALDATA, NativeMethods.MSGFLT_ALLOW, IntPtr.Zero);
    }

    /// <summary>
    /// WinEventHook 설정 - 이벤트 기반 아이콘 변경 감시
    /// </summary>
    private void SetupWinEventHook()
    {
        _winEventDelegate = WinEventCallback;
        
        // 아이콘 위치 변경 및 생성/삭제 이벤트 감시
        _winEventHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            NativeMethods.EVENT_OBJECT_REORDER,
            IntPtr.Zero,
            _winEventDelegate,
            0, // 모든 프로세스
            0, // 모든 스레드
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
    }

    private void WinEventCallback(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        // SysListView32 (데스크톱 아이콘 리스트)에서 발생한 이벤트인지 확인
        // 성능을 위해 debounce 로직 적용 가능
        if (eventType == NativeMethods.EVENT_OBJECT_LOCATIONCHANGE ||
            eventType == NativeMethods.EVENT_OBJECT_CREATE ||
            eventType == NativeMethods.EVENT_OBJECT_DESTROY ||
            eventType == NativeMethods.EVENT_OBJECT_REORDER)
        {
            // UI 스레드에서 이벤트 발생
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                DesktopIconsChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    /// <summary>
    /// DPI 스케일 가져오기
    /// </summary>
    public double GetDpiScale(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        return NativeMethods.GetDpiScale(hwnd);
    }

    /// <summary>
    /// WPF 좌표를 물리적 픽셀로 변환
    /// </summary>
    public int ToPhysicalPixels(double logicalValue, Window window)
    {
        return NativeMethods.LogicalToPhysical(logicalValue, GetDpiScale(window));
    }

    /// <summary>
    /// 물리적 픽셀을 WPF 좌표로 변환
    /// </summary>
    public double ToLogicalUnits(int physicalValue, Window window)
    {
        return NativeMethods.PhysicalToLogical(physicalValue, GetDpiScale(window));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // 등록된 창들의 부모 관계 해제
            foreach (var hwnd in _registeredWindows)
            {
                try
                {
                    NativeMethods.SetParent(hwnd, IntPtr.Zero);
                }
                catch { }
            }
            _registeredWindows.Clear();

            // WinEventHook 해제
            if (_winEventHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }
            
            _winEventDelegate = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
