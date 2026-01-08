using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace IntelligentDesktop.UI.Services;

/// <summary>
/// 시스템 트레이 아이콘 관리 서비스
/// </summary>
public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private bool _disposed;

    public event EventHandler? ExitRequested;
    public event EventHandler? ShowBoxesRequested;
    public event EventHandler? HideBoxesRequested;
    public event EventHandler? NewBoxRequested;
    public event EventHandler? AutoSortRequested;
    public event EventHandler? BackupRequested;
    public event EventHandler? RestoreRequested;
    public event EventHandler? ToggleClockWidgetRequested;
    public event EventHandler<bool>? StartupStateChanged;
    
    private ToolStripMenuItem? _startupMenuItem;

    public void UpdateStartupState(bool isEnabled)
    {
        if (_startupMenuItem != null)
        {
            _startupMenuItem.Checked = isEnabled;
        }
    }

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "Intelligent Desktop",
            Visible = true
        };

        _notifyIcon.ContextMenuStrip = CreateContextMenu();
        _notifyIcon.DoubleClick += (s, e) => ShowBoxesRequested?.Invoke(this, EventArgs.Empty);
    }

    private Icon CreateDefaultIcon()
    {
        // 앱 아이콘 사용 시도
        try 
        {
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch { }

        // 실패 시 기본 사각형
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.FromArgb(100, 149, 237)); // CornflowerBlue
            g.DrawRectangle(Pens.White, 2, 2, 11, 11);
            g.FillRectangle(new SolidBrush(Color.FromArgb(180, 255, 255, 255)), 4, 4, 8, 8);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("박스 표시");
        showItem.Click += (s, e) => ShowBoxesRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(showItem);

        var hideItem = new ToolStripMenuItem("박스 숨김");
        hideItem.Click += (s, e) => HideBoxesRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(hideItem);

        menu.Items.Add(new ToolStripSeparator());

        var newBoxItem = new ToolStripMenuItem("새 박스 만들기");
        newBoxItem.Click += (s, e) => NewBoxRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(newBoxItem);

        menu.Items.Add(new ToolStripSeparator());

        var autoSortItem = new ToolStripMenuItem("📦 자동 정리");
        autoSortItem.Click += (s, e) => AutoSortRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(autoSortItem);

        menu.Items.Add(new ToolStripSeparator());

        var backupItem = new ToolStripMenuItem("💾 레이아웃 백업");
        backupItem.Click += (s, e) => BackupRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(backupItem);

        var restoreItem = new ToolStripMenuItem("📂 레이아웃 복원");
        restoreItem.Click += (s, e) => RestoreRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(restoreItem);

        menu.Items.Add(new ToolStripSeparator());

        var clockItem = new ToolStripMenuItem("🕒 시계 위젯 켜기/끄기");
        clockItem.Click += (s, e) => ToggleClockWidgetRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(clockItem);

        menu.Items.Add(new ToolStripSeparator());

        _startupMenuItem = new ToolStripMenuItem("🚀 윈도우 시작 시 자동 실행"); // CheckOnClick은 수동 제어를 위해 false 유지
        _startupMenuItem.Click += (s, e) =>
        {
            _startupMenuItem.Checked = !_startupMenuItem.Checked;
            StartupStateChanged?.Invoke(this, _startupMenuItem.Checked);
        };
        menu.Items.Add(_startupMenuItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        return menu;
    }

    public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
