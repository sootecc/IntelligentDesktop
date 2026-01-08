using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using IntelligentDesktop.Core.Interop;

namespace IntelligentDesktop.UI.Services;

public class ShellContextMenuService
{
    public void ShowContextMenu(IntPtr ownerHwnd, string[] filePaths, int x, int y)
    {
        if (filePaths == null || filePaths.Length == 0) return;

        // 1. Desktop Folder 얻기
        ShellApis.SHGetDesktopFolder(out IShellFolder desktop);
        if (desktop == null) return;

        IntPtr pidlParent = IntPtr.Zero;
        
        try 
        {
            string parentDir = Path.GetDirectoryName(filePaths[0])!;
            if (string.IsNullOrEmpty(parentDir)) return;

            uint pchEaten = 0;
            uint pdwAttributes = 0;
            
            // 2. 부모 폴더 PIDL 구하기
            desktop.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, parentDir, out pchEaten, out pidlParent, ref pdwAttributes);
            
            if (pidlParent == IntPtr.Zero) return;
            
            IntPtr pFolder = IntPtr.Zero;
            try
            {
                // 3. 부모 폴더 바인딩
                desktop.BindToObject(pidlParent, IntPtr.Zero, ShellApis.IID_IShellFolder, out pFolder);
                
                if (pFolder != IntPtr.Zero)
                {
                    IShellFolder parentFolder = (IShellFolder)Marshal.GetObjectForIUnknown(pFolder);
                    
                    List<IntPtr> childPidls = new List<IntPtr>();
                    try
                    {
                        // 4. 자식 PIDL 구하기
                        foreach (var path in filePaths)
                        {
                            string fileName = Path.GetFileName(path);
                            // Parse relative PIDL
                            parentFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, fileName, out pchEaten, out IntPtr pidlChild, ref pdwAttributes);
                            if (pidlChild != IntPtr.Zero)
                                childPidls.Add(pidlChild);
                        }
                        
                        if (childPidls.Count > 0)
                        {
                             // 5. IContextMenu 얻기
                             IntPtr[] apidl = childPidls.ToArray();
                             parentFolder.GetUIObjectOf(ownerHwnd, (uint)apidl.Length, apidl, ShellApis.IID_IContextMenu, IntPtr.Zero, out IntPtr pContextMenu);
                             
                             if (pContextMenu != IntPtr.Zero)
                             {
                                 IContextMenu contextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(pContextMenu);
                                 
                                 if (contextMenu != null)
                                 {
                                     IntPtr hMenu = ShellApis.CreatePopupMenu();
                                     if (hMenu != IntPtr.Zero)
                                     {
                                         // 메뉴 쿼리
                                         contextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, ShellApis.CMF_NORMAL);
                                         
                                         // 메뉴 표시 및 선택 대기
                                         int cmd = ShellApis.TrackPopupMenu(hMenu, ShellApis.TPM_RETURNCMD | ShellApis.TPM_RIGHTBUTTON, x, y, 0, ownerHwnd, IntPtr.Zero);
                                         
                                         // 커맨드 실행
                                         if (cmd > 0)
                                         {
                                             CMINVOKECOMMANDINFO info = new CMINVOKECOMMANDINFO();
                                             info.cbSize = Marshal.SizeOf(info);
                                             info.fMask = 0;
                                             info.hwnd = ownerHwnd;
                                             info.lpVerb = (IntPtr)(cmd - 1); // idCmd - idCmdFirst
                                             info.nShow = 1; // SW_SHOWNORMAL
                                             
                                             contextMenu.InvokeCommand(ref info);
                                         }
                                         ShellApis.DestroyMenu(hMenu);
                                     }
                                     Marshal.ReleaseComObject(contextMenu);
                                 }
                                 Marshal.Release(pContextMenu);
                             }
                        }
                    }
                    finally
                    {
                        foreach(var p in childPidls) ShellApis.ILFree(p);
                        Marshal.ReleaseComObject(parentFolder);
                    }
                }
            }
            finally
            {
               if (pFolder != IntPtr.Zero) Marshal.Release(pFolder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Shell Context Menu Error: {ex}");
        }
        finally
        {
            if (pidlParent != IntPtr.Zero) ShellApis.ILFree(pidlParent);
            Marshal.ReleaseComObject(desktop);
        }
    }
}
