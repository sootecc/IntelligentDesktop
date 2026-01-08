using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using IntelligentDesktop.Core.Interop;
using IntelligentDesktop.Core.Models;

namespace IntelligentDesktop.UI.Views;

/// <summary>
/// BoxWindow.xaml의 코드 비하인드
/// </summary>
public partial class BoxWindow : Window
{
    private readonly Box _box;
    private bool _isMinimized = false;
    private double _originalHeight;

    public Box Box => _box;

    public BoxWindow() : this(new Box())
    {
    }

    public BoxWindow(Box box)
    {
        InitializeComponent();
        _box = box;
        
        // 박스 데이터 바인딩
        Left = box.X;
        Top = box.Y;
        Width = box.Width;
        Height = box.Height;
        TitleText.Text = box.Name;
        
        // 스타일 적용
        ApplyStyle(box.Style);
        
        Loaded += BoxWindow_Loaded;
        LocationChanged += BoxWindow_LocationChanged;
        SizeChanged += BoxWindow_SizeChanged;
    }

    private System.Windows.Point _startPoint;
    private System.IO.FileSystemWatcher? _watcher;

    private IntelligentDesktop.UI.Services.WindowSnappingService _snappingService = new();
    private IntelligentDesktop.UI.Services.ShellContextMenuService _shellService = new();
    private bool _isTitleBarDragging;
    private NativeMethods.POINT _dragStartCursorPos;
    private System.Windows.Point _dragStartWindowPos;

    private void BoxWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 디버깅: Z-Order 조작 제거
        SetupWatcher();
        RefreshIconList();
    }



    private void ApplyStyle(BoxStyle style)
    {
        try
        {
            var bgColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(style.BackgroundColor);
            
            MainBorder.Background = new SolidColorBrush(bgColor);
            MainBorder.BorderBrush = System.Windows.Media.Brushes.Transparent; // 테두리 제거
            MainBorder.BorderThickness = new Thickness(0); // 테두리 두께 0
            MainBorder.CornerRadius = new CornerRadius(style.CornerRadius);
        }
        catch
        {
            // 기본 스타일 유지
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMinimize();
        }
        else
        {
            _isTitleBarDragging = true;
            NativeMethods.GetCursorPos(out _dragStartCursorPos);
            _dragStartWindowPos = new System.Windows.Point(Left, Top);
            if (sender is UIElement el) el.CaptureMouse();
        }
    }

    private void TitleBar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isTitleBarDragging)
        {
            NativeMethods.GetCursorPos(out var currentCursor);
            var dpiScale = NativeMethods.GetDpiScale(new WindowInteropHelper(this).Handle);
            
            // 물리 픽셀 차이
            int dx = currentCursor.X - _dragStartCursorPos.X;
            int dy = currentCursor.Y - _dragStartCursorPos.Y;
            
            // 논리 단위로 변환
            double ldx = NativeMethods.PhysicalToLogical(dx, dpiScale);
            double ldy = NativeMethods.PhysicalToLogical(dy, dpiScale);
            
            double newLeft = _dragStartWindowPos.X + ldx;
            double newTop = _dragStartWindowPos.Y + ldy;
            
            // 자석 효과 (Snapping)
            var snapped = _snappingService.Snap(this, newLeft, newTop);
            
            Left = snapped.X;
            Top = snapped.Y;
            _box.X = Left;
            _box.Y = Top;
        }
    }

    private void TitleBar_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isTitleBarDragging)
        {
            _isTitleBarDragging = false;
            if (sender is UIElement el) el.ReleaseMouseCapture();
            
            // 박스 이동량 계산
            double deltaX = Left - _dragStartWindowPos.X;
            double deltaY = Top - _dragStartWindowPos.Y;
            
            // 내부 아이콘들도 함께 이동
            if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1)
            {
                MoveIconsWithBox(deltaX, deltaY);
            }
            
            if (System.Windows.Application.Current is IntelligentDesktop.UI.App app)
            {
                app.SaveConfiguration();
            }
        }
    }
    
    /// <summary>
    /// 박스 이동 시 내부 아이콘들도 함께 이동
    /// </summary>
    private void MoveIconsWithBox(double deltaX, double deltaY)
    {
        if (_box.IconPaths.Count == 0) return;
        
        var app = System.Windows.Application.Current as IntelligentDesktop.UI.App;
        if (app?.DesktopIconService == null) return;
        
        var allIcons = app.DesktopIconService.GetAllIcons();
        
        foreach (var path in _box.IconPaths)
        {
            for (int i = 0; i < allIcons.Count; i++)
            {
                if (string.Equals(allIcons[i].FullPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    // 현재 위치에서 이동량만큼 더함
                    double newX = allIcons[i].Position.X + deltaX;
                    double newY = allIcons[i].Position.Y + deltaY;
                    app.DesktopIconService.SetIconPosition(i, new System.Windows.Point(newX, newY));
                    break;
                }
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new IntelligentDesktop.UI.Views.BoxSettingsWindow(_box);
        dlg.Owner = this;
        if (dlg.ShowDialog() == true)
        {
             _box.Name = dlg.BoxName;
             _box.Style.BackgroundColor = dlg.ResultBackgroundColor;
             
             TitleText.Text = _box.Name;
             ApplyStyle(_box.Style);
             
             if (System.Windows.Application.Current is IntelligentDesktop.UI.App app)
             {
                 app.SaveConfiguration();
             }
        }
    }

    private void QuickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement item && item.Tag is string colorCode)
        {
            _box.Style.BackgroundColor = colorCode;
            _box.Style.BorderColor = colorCode; // 테두리 색도 배경색과 동일하게 (혹은 무시됨)
            ApplyStyle(_box.Style);
            
            if (System.Windows.Application.Current is IntelligentDesktop.UI.App app)
            {
                app.SaveConfiguration();
            }
        }
    }

    private void QuickOpacity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement item && item.Tag is string opacityStr && double.TryParse(opacityStr, out double opacity))
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_box.Style.BackgroundColor);
                color.A = (byte)(255 * opacity);
                _box.Style.BackgroundColor = color.ToString();
                ApplyStyle(_box.Style);
                
                if (System.Windows.Application.Current is IntelligentDesktop.UI.App app)
                {
                    app.SaveConfiguration();
                }
            }
            catch { }
        }
    }

    private void TitleText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            e.Handled = true;
            ShowTitleEditWindow();
        }
    }

    /// <summary>
    /// 제목 편집을 위한 별도 창 표시 (IME 입력 지원)
    /// </summary>
    private void ShowTitleEditWindow()
    {
        // Owner를 설정하지 않음 (투명 창이 Owner면 IME가 안 됨)
        var editWindow = new TitleEditWindow(_box.Name);

        editWindow.ShowDialog();

        if (editWindow.IsConfirmed && !string.IsNullOrWhiteSpace(editWindow.ResultText))
        {
            _box.Name = editWindow.ResultText;
            TitleText.Text = editWindow.ResultText;

            if (System.Windows.Application.Current is IntelligentDesktop.UI.App app)
            {
                app.SaveConfiguration();
            }
        }
    }

    private void Icon_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement element && element.DataContext is DesktopIcon icon)
        {
            // 우클릭 아이템 선택 (없으면 선택)
            if (!IconList.SelectedItems.Contains(icon))
            {
                IconList.SelectedItems.Clear();
                IconList.SelectedItem = icon;
            }
            
            var paths = new System.Collections.Generic.List<string>();
            foreach (DesktopIcon item in IconList.SelectedItems)
            {
                paths.Add(item.FullPath);
            }
            
            NativeMethods.GetCursorPos(out var pt);
            
            _shellService.ShowContextMenu(new WindowInteropHelper(this).Handle, paths.ToArray(), pt.X, pt.Y);
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMinimize();
    }

    private void ToggleMinimize()
    {
        if (_isMinimized)
        {
            // 복원
            Height = _originalHeight;
            _isMinimized = false;
            MinimizeButton.Content = "─";
        }
        else
        {
            // 최소화 (제목바만 보이게)
            _originalHeight = Height;
            Height = 32;
            _isMinimized = true;
            MinimizeButton.Content = "□";
        }
        
        _box.IsMinimized = _isMinimized;
    }

    private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newWidth = Width + e.HorizontalChange;
        double newHeight = Height + e.VerticalChange;
        
        if (newWidth >= 100) Width = newWidth;
        if (newHeight >= 50) Height = newHeight;
    }

    private void BoxWindow_LocationChanged(object? sender, EventArgs e)
    {
        _box.X = Left;
        _box.Y = Top;
        UpdateNormalizedCoordinates();
    }

    private void BoxWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _box.Width = Width;
        _box.Height = Height;
        UpdateNormalizedCoordinates();
    }

    /// <summary>
    /// 박스 위치 및 크기 정보 강제 업데이트
    /// </summary>
    public void UpdateBoxPosition()
    {
        _box.X = Left;
        _box.Y = Top;
        _box.Width = Width;
        _box.Height = Height;
        UpdateNormalizedCoordinates();
    }

    private void UpdateNormalizedCoordinates()
    {
        var screen = SystemParameters.WorkArea;
        _box.NormalizedX = Left / screen.Width;
        _box.NormalizedY = Top / screen.Height;
        _box.NormalizedWidth = Width / screen.Width;
        _box.NormalizedHeight = Height / screen.Height;
    }

    /// <summary>
    /// 아이콘 목록 업데이트
    /// </summary>
    public void SetIcons(IEnumerable<DesktopIcon> icons)
    {
        IconList.ItemsSource = icons;
    }

    /// <summary>
    /// 박스 이름 변경
    /// </summary>
    public void SetTitle(string title)
    {
        _box.Name = title;
        TitleText.Text = title;
    }

    #region Drag & Drop

    private void BoxWindow_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            MainBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.LightBlue);
            MainBorder.BorderThickness = new Thickness(2);
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void BoxWindow_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        // 원래 스타일로 복원
        ApplyStyle(_box.Style);
    }

    private void BoxWindow_Drop(object sender, System.Windows.DragEventArgs e)
    {
        // 스타일 복원
        ApplyStyle(_box.Style);
        
        // 드롭된 파일의 아이콘 위치를 박스 내부로 이동
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[]? files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (files != null)
            {
                var app = System.Windows.Application.Current as IntelligentDesktop.UI.App;
                if (app?.DesktopIconService != null)
                {
                    // 현재 바탕화면 아이콘 가져오기
                    var allIcons = app.DesktopIconService.GetAllIcons();
                    
                    // 박스 내부 시작 위치 계산 (화면 좌표)
                    var boxScreenPos = PointToScreen(new System.Windows.Point(10, 40)); // 타이틀바 아래
                    int iconSpacing = 80;
                    int iconsPerRow = Math.Max(1, (int)(Width - 20) / iconSpacing);
                    int iconIndex = 0;
                    
                    foreach (string file in files)
                    {
                        // 해당 파일의 아이콘 인덱스 찾기
                        for (int i = 0; i < allIcons.Count; i++)
                        {
                            if (string.Equals(allIcons[i].FullPath, file, StringComparison.OrdinalIgnoreCase))
                            {
                                // 박스 내 위치 계산
                                int row = iconIndex / iconsPerRow;
                                int col = iconIndex % iconsPerRow;
                                double newX = boxScreenPos.X + col * iconSpacing;
                                double newY = boxScreenPos.Y + row * iconSpacing;
                                
                                // 아이콘 위치 이동
                                app.DesktopIconService.SetIconPosition(i, new System.Windows.Point(newX, newY));
                                
                                // 박스에 경로 저장 (추적용)
                                if (!_box.IconPaths.Contains(file))
                                {
                                    _box.IconPaths.Add(file);
                                }
                                
                                iconIndex++;
                                break;
                            }
                        }
                    }
                    
                    app.SaveConfiguration();
                    RefreshIconList();
                    IconsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        e.Handled = true;
    }

    /// <summary>
    /// 파일/폴더를 박스에 추가
    /// </summary>
    public void AddIconToBox(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        // 이미 추가되어 있으면 무시
        if (_box.IconPaths.Contains(path)) return;
        
        _box.IconPaths.Add(path);
    }

    /// <summary>
    /// 파일/폴더를 박스에서 제거
    /// </summary>
    public void RemoveIconFromBox(string path)
    {
        _box.IconPaths.Remove(path);
        RefreshIconList();
    }

    /// <summary>
    /// 아이콘 목록 새로고침 (IconPaths 기반)
    /// </summary>
    public void RefreshIconList()
    {
        try
        {
            var icons = new List<DesktopIcon>();
            
            // IconPaths에서 존재하는 파일/폴더만 표시
            var validPaths = new List<string>();
            foreach (var path in _box.IconPaths.ToList())
            {
                if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
                {
                    validPaths.Add(path);
                    bool isDir = System.IO.Directory.Exists(path);
                    icons.Add(new DesktopIcon
                    {
                        Name = System.IO.Path.GetFileName(path),
                        FullPath = path,
                        IsDirectory = isDir,
                        Icon = GetIconForPath(path)
                    });
                }
            }
            
            // 유효하지 않은 경로 제거
            _box.IconPaths.Clear();
            foreach (var p in validPaths) _box.IconPaths.Add(p);

            IconList.ItemsSource = icons;
            EmptyHint.Visibility = icons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch { }
    }

    /// <summary>
    /// 파일/폴더의 아이콘 가져오기
    /// </summary>
    private System.Windows.Media.ImageSource? GetIconForPath(string path)
    {
        try
        {
            var shinfo = new NativeMethods.SHFILEINFO();
            
            // SHGetFileInfo 호출
            NativeMethods.SHGetFileInfo(
                path,
                0,
                ref shinfo,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf(shinfo),
                NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);
                
            if (shinfo.hIcon != IntPtr.Zero)
            {
                var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
                var bitmap = icon.ToBitmap();
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                
                NativeMethods.DestroyIcon(shinfo.hIcon);
                return bitmapSource;
            }
        }
        catch { }
        return null;
    }

    private void Icon_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (sender is FrameworkElement element && element.DataContext is DesktopIcon icon)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = icon.FullPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 아이콘 변경 이벤트
    /// </summary>
    public event EventHandler? IconsChanged;
    
    // 메뉴 이벤트
    public event EventHandler? NewBoxRequested;
    public event EventHandler? DeleteBoxRequested;
    public event EventHandler? AutoAlignRequested;

    private void NewBox_Click(object sender, RoutedEventArgs e) => NewBoxRequested?.Invoke(this, EventArgs.Empty);
    private void DeleteBox_Click(object sender, RoutedEventArgs e) => DeleteBoxRequested?.Invoke(this, EventArgs.Empty);
    private void AutoAlign_Click(object sender, RoutedEventArgs e) => AutoAlignRequested?.Invoke(this, EventArgs.Empty);
    
    // 추가했던 코드 제거됨

    private void Icon_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
    }

    private void Icon_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            System.Windows.Vector diff = _startPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is FrameworkElement element && element.DataContext is DesktopIcon icon)
                {
                    // 드래그 시작 아이템이 선택되지 않았다면 선택
                    if (!IconList.SelectedItems.Contains(icon))
                    {
                        IconList.SelectedItem = icon;
                    }

                    var selectedFiles = new System.Collections.Generic.List<string>();
                    foreach (DesktopIcon item in IconList.SelectedItems)
                    {
                        selectedFiles.Add(item.FullPath);
                    }

                    if (selectedFiles.Count > 0)
                    {
                        try
                        {
                            var data = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, selectedFiles.ToArray());
                            System.Windows.DragDrop.DoDragDrop(this, data, System.Windows.DragDropEffects.Move | System.Windows.DragDropEffects.Copy);
                        }
                        catch { }
                    }
                }
            }
        }
    }

    private void SetupWatcher()
    {
        if (_watcher != null) return;
        
        string path = GetBoxFolderPath();
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        try
        {
            _watcher = new System.IO.FileSystemWatcher(path);
            _watcher.NotifyFilter = System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.DirectoryName | System.IO.NotifyFilters.LastWrite;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;
        }
        catch { }
    }

    private void OnFileChanged(object sender, System.IO.FileSystemEventArgs e)
    {
        Dispatcher.Invoke(() => RefreshIconList());
    }

    private string GetBoxFolderPath()
    {
        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "IntelligentDesktop",
            "Boxes",
            _box.Id.ToString());
    }

    private string GetLongPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.StartsWith(@"\\?\")) return path;
        return @"\\?\" + path;
    }

    #endregion
}
