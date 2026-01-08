namespace IntelligentDesktop.Core.Services.Shell;

using System.IO;
using IntelligentDesktop.Core.Interop;
using IntelligentDesktop.Core.Models;
using System.Runtime.InteropServices;
using System.Windows;

/// <summary>
/// 데스크톱 아이콘 관리 서비스 인터페이스
/// </summary>
public interface IDesktopIconService
{
    /// <summary>모든 데스크톱 아이콘 정보를 가져옵니다</summary>
    IReadOnlyList<DesktopIcon> GetAllIcons();
    
    /// <summary>특정 아이콘의 위치를 설정합니다</summary>
    bool SetIconPosition(int index, Point position);
    
    /// <summary>여 아이콘 위치를 새로고침합니다</summary>
    void RefreshIconPositions();
    
    /// <summary>데스크톱 ListView 핸들을 가져옵니다</summary>
    IntPtr GetDesktopListViewHandle();
    
    /// <summary>모든 데스크톱 아이콘을 숨깁니다</summary>
    void HideAllIcons();
    
    /// <summary>모든 데스크톱 아이콘을 표시합니다</summary>
    void ShowAllIcons();
    
    /// <summary>아이콘 표시 상태를 토글합니다</summary>
    bool ToggleIconVisibility();
}

/// <summary>
/// Windows 데스크톱 아이콘 관리 서비스 구현
/// </summary>
public partial class DesktopIconService : IDesktopIconService, IDisposable
{
    private IntPtr _desktopHandle;
    private IntPtr _explorerProcess;
    private uint _explorerProcessId;
    private readonly List<DesktopIcon> _cachedIcons = new();
    private bool _disposed;

    public DesktopIconService()
    {
        Initialize();
    }

    private void Initialize()
    {
        _desktopHandle = GetDesktopListViewHandle();
        if (_desktopHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("데스크톱 ListView 핸들을 찾을 수 없습니다.");
        }

        NativeMethods.GetWindowThreadProcessId(_desktopHandle, out _explorerProcessId);
        _explorerProcess = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE,
            false,
            _explorerProcessId);
    }

    public IntPtr GetDesktopListViewHandle()
    {
        // 방법 1: Progman -> SHELLDLL_DefView -> SysListView32
        IntPtr progman = NativeMethods.FindWindowW("Progman", null);
        IntPtr defView = NativeMethods.FindWindowExW(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        
        if (defView != IntPtr.Zero)
        {
            IntPtr listView = NativeMethods.FindWindowExW(defView, IntPtr.Zero, "SysListView32", "FolderView");
            if (listView != IntPtr.Zero) return listView;
        }

        // 방법 2: WorkerW 창에서 찾기 (Windows 10/11 슬라이드쇼 배경화면 등)
        IntPtr workerW = IntPtr.Zero;
        IntPtr desktopWindow = NativeMethods.GetDesktopWindow();
        
        do
        {
            workerW = NativeMethods.FindWindowExW(desktopWindow, workerW, "WorkerW", null);
            if (workerW != IntPtr.Zero)
            {
                defView = NativeMethods.FindWindowExW(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    IntPtr listView = NativeMethods.FindWindowExW(defView, IntPtr.Zero, "SysListView32", "FolderView");
                    if (listView != IntPtr.Zero) return listView;
                }
            }
        } while (workerW != IntPtr.Zero);

        return IntPtr.Zero;
    }

    public IReadOnlyList<DesktopIcon> GetAllIcons()
    {
        _cachedIcons.Clear();

        if (_desktopHandle == IntPtr.Zero) return _cachedIcons;

        int itemCount = (int)NativeMethods.SendMessageW(_desktopHandle, NativeMethods.LVM_GETITEMCOUNT, 0, 0);
        
        // Explorer 프로세스 메모리에 할당
        int pointSize = Marshal.SizeOf<NativeMethods.POINT>();
        int lvItemSize = Marshal.SizeOf<NativeMethods.LVITEMW>();
        int textBufferSize = 520; // MAX_PATH * 2 + 8

        IntPtr remotePoint = NativeMethods.VirtualAllocEx(_explorerProcess, IntPtr.Zero, (uint)pointSize,
            NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE);
        IntPtr remoteLvItem = NativeMethods.VirtualAllocEx(_explorerProcess, IntPtr.Zero, (uint)lvItemSize,
            NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE);
        IntPtr remoteText = NativeMethods.VirtualAllocEx(_explorerProcess, IntPtr.Zero, (uint)textBufferSize,
            NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE);

        try
        {
            IntPtr localPoint = Marshal.AllocHGlobal(pointSize);
            IntPtr localLvItem = Marshal.AllocHGlobal(lvItemSize);
            IntPtr localText = Marshal.AllocHGlobal(textBufferSize);

            try
            {
                for (int i = 0; i < itemCount; i++)
                {
                    var icon = new DesktopIcon();

                    // 위치 가져오기
                    NativeMethods.SendMessageW(_desktopHandle, NativeMethods.LVM_GETITEMPOSITION, i, remotePoint);
                    NativeMethods.ReadProcessMemory(_explorerProcess, remotePoint, localPoint, (uint)pointSize, out _);
                    var point = Marshal.PtrToStructure<NativeMethods.POINT>(localPoint);
                    icon.Position = new Point(point.X, point.Y);

                    // 이름 가져오기
                    var lvItem = new NativeMethods.LVITEMW
                    {
                        mask = NativeMethods.LVIF_TEXT,
                        iItem = i,
                        iSubItem = 0,
                        pszText = remoteText,
                        cchTextMax = 260
                    };
                    
                    Marshal.StructureToPtr(lvItem, localLvItem, false);
                    NativeMethods.WriteProcessMemory(_explorerProcess, remoteLvItem, localLvItem, (uint)lvItemSize, out _);
                    NativeMethods.SendMessageW(_desktopHandle, NativeMethods.LVM_GETITEMTEXTW, i, remoteLvItem);
                    NativeMethods.ReadProcessMemory(_explorerProcess, remoteText, localText, (uint)textBufferSize, out _);
                    
                    icon.Name = Marshal.PtrToStringUni(localText) ?? string.Empty;

                    // 데스크톱 경로 구성
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string publicDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                    
                    string userPath = Path.Combine(desktopPath, icon.Name);
                    string publicPath = Path.Combine(publicDesktopPath, icon.Name);
                    
                    if (File.Exists(userPath) || Directory.Exists(userPath))
                        icon.FullPath = userPath;
                    else if (File.Exists(publicPath) || Directory.Exists(publicPath))
                        icon.FullPath = publicPath;
                    else
                        icon.FullPath = userPath; // 기본값

                    icon.IsDirectory = Directory.Exists(icon.FullPath);

                    _cachedIcons.Add(icon);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(localPoint);
                Marshal.FreeHGlobal(localLvItem);
                Marshal.FreeHGlobal(localText);
            }
        }
        finally
        {
            NativeMethods.VirtualFreeEx(_explorerProcess, remotePoint, 0, NativeMethods.MEM_RELEASE);
            NativeMethods.VirtualFreeEx(_explorerProcess, remoteLvItem, 0, NativeMethods.MEM_RELEASE);
            NativeMethods.VirtualFreeEx(_explorerProcess, remoteText, 0, NativeMethods.MEM_RELEASE);
        }

        return _cachedIcons;
    }

    public bool SetIconPosition(int index, Point position)
    {
        if (_desktopHandle == IntPtr.Zero) return false;

        // MAKELPARAM(x, y)
        nint lParam = ((int)position.Y << 16) | ((int)position.X & 0xFFFF);
        NativeMethods.SendMessageW(_desktopHandle, NativeMethods.LVM_SETITEMPOSITION, index, lParam);
        return true;
    }

    public void RefreshIconPositions()
    {
        GetAllIcons();
    }

    /// <summary>
    /// 모든 데스크톱 아이콘 숨기기
    /// </summary>
    public void HideAllIcons()
    {
        if (_desktopHandle != IntPtr.Zero)
        {
            ShowWindowAsync(_desktopHandle, SW_HIDE);
            _iconsHidden = true;
        }
    }

    /// <summary>
    /// 모든 데스크톱 아이콘 표시
    /// </summary>
    public void ShowAllIcons()
    {
        if (_desktopHandle != IntPtr.Zero)
        {
            ShowWindowAsync(_desktopHandle, SW_SHOW);
            _iconsHidden = false;
        }
    }

    /// <summary>
    /// 아이콘 표시 상태 토글
    /// </summary>
    public bool ToggleIconVisibility()
    {
        if (_iconsHidden)
            ShowAllIcons();
        else
            HideAllIcons();
        
        return _iconsHidden;
    }

    private bool _iconsHidden = false;
    
    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_explorerProcess != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_explorerProcess);
                _explorerProcess = IntPtr.Zero;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
