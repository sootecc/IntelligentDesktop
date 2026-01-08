namespace IntelligentDesktop.Core.Interop;

using System.Runtime.InteropServices;

/// <summary>
/// Windows Win32 API P/Invoke 정의
/// </summary>
public static partial class NativeMethods
{
    #region Window Finding
    
    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowW(string? lpClassName, string? lpWindowName);
    
    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowExW(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr GetDesktopWindow();
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr GetShellWindow();
    
    #endregion

    #region Window Messages
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SendMessageW(IntPtr hWnd, uint Msg, nint wParam, nint lParam);
    
    #endregion

    #region Process Memory Access
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr hObject);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, uint nSize, out uint lpNumberOfBytesRead);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);
    
    #endregion

    #region Window Z-Order
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    public static readonly IntPtr HWND_BOTTOM = new(1);
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public static readonly IntPtr HWND_TOP = new(0);
    
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
    
    #endregion

    #region ListView Messages
    
    public const uint LVM_FIRST = 0x1000;
    public const uint LVM_GETITEMCOUNT = LVM_FIRST + 4;
    public const uint LVM_GETITEMPOSITION = LVM_FIRST + 16;
    public const uint LVM_SETITEMPOSITION = LVM_FIRST + 15;
    public const uint LVM_GETITEMTEXTW = LVM_FIRST + 115;
    public const uint LVM_GETITEMW = LVM_FIRST + 75;
    
    #endregion

    #region Process Access Rights
    
    public const uint PROCESS_VM_OPERATION = 0x0008;
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_VM_WRITE = 0x0020;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RELEASE = 0x8000;
    public const uint PAGE_READWRITE = 0x04;
    
    #endregion

    #region Structures
    
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LVITEMW
    {
        public uint mask;
        public int iItem;
        public int iSubItem;
        public uint state;
        public uint stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
        public int iIndent;
        public int iGroupId;
        public uint cColumns;
        public IntPtr puColumns;
        public IntPtr piColFmt;
        public int iGroup;
    }
    
    public const uint LVIF_TEXT = 0x0001;
    
    #endregion

    #region Mouse Hooks
    
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr SetWindowsHookExW(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWindowsHookEx(IntPtr hhk);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr GetModuleHandleW(string? lpModuleName);
    
    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    
    #endregion

    #region Icon Extraction
    
    [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr ExtractIconW(IntPtr hInst, string lpszExeFileName, int nIconIndex);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(IntPtr hIcon);
    
    #endregion

    #region SetParent - Desktop Child Window
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr GetParent(IntPtr hWnd);
    
    // WorkerW 창을 생성하기 위한 메시지 (배경화면 뒤에 창 배치용)
    public const uint WM_SPAWN_WORKER = 0x052C;
    
    #endregion

    #region UIPI - ChangeWindowMessageFilterEx
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ChangeWindowMessageFilterEx(
        IntPtr hWnd, 
        uint message, 
        uint action, 
        IntPtr pChangeFilterStruct);
    
    public const uint MSGFLT_ALLOW = 1;
    public const uint MSGFLT_DISALLOW = 2;
    public const uint MSGFLT_RESET = 0;
    
    // 허용할 메시지들
    public const uint WM_DROPFILES = 0x0233;
    public const uint WM_COPYDATA = 0x004A;
    public const uint WM_COPYGLOBALDATA = 0x0049;
    
    #endregion

    #region WinEventHook - Event-based Icon Monitoring
    
    public delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWinEvent(IntPtr hWinEventHook);
    
    // WinEvent 상수
    public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
    public const uint EVENT_OBJECT_CREATE = 0x8000;
    public const uint EVENT_OBJECT_DESTROY = 0x8001;
    public const uint EVENT_OBJECT_REORDER = 0x8004;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    
    #endregion

    #region DPI Awareness
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetDpiForWindow(IntPtr hwnd);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetDpiForSystem();
    
    [LibraryImport("shcore.dll", SetLastError = true)]
    public static partial int GetDpiForMonitor(
        IntPtr hmonitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int MDT_EFFECTIVE_DPI = 0;
    
    // DPI 변환 헬퍼
    public static double GetDpiScale(IntPtr hwnd)
    {
        try
        {
            uint dpi = GetDpiForWindow(hwnd);
            return dpi > 0 ? dpi / 96.0 : 1.0;
        }
        catch
        {
            return 1.0;
        }
    }
    
    /// <summary>
    /// WPF Logical Unit을 물리적 픽셀로 변환
    /// </summary>
    public static int LogicalToPhysical(double logicalValue, double dpiScale)
    {
        return (int)Math.Round(logicalValue * dpiScale);
    }
    
    /// <summary>
    /// 물리적 픽셀을 WPF Logical Unit으로 변환
    /// </summary>
    public static double PhysicalToLogical(int physicalValue, double dpiScale)
    {
        return dpiScale > 0 ? physicalValue / dpiScale : physicalValue;
    }
    
    #endregion

    #region Shell Icons (SHGetFileInfo)

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public const uint SHGFI_ICON = 0x100;
    public const uint SHGFI_LARGEICON = 0x0;
    public const uint SHGFI_SMALLICON = 0x1;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    public const uint SHGFI_TYPENAME = 0x400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SHGetFileInfo(
        string pszPath, 
        uint dwFileAttributes, 
        ref SHFILEINFO psfi, 
        uint cbFileInfo, 
        uint uFlags);

    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    
    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const int SHCNE_UPDATEDIR = 0x00001000;
    public const uint SHCNF_IDLIST = 0x0000;
    public const uint SHCNF_FLUSH = 0x1000;

    [LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT lpPoint);

    #endregion
}
