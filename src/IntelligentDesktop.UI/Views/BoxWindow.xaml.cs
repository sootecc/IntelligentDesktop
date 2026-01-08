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
            
            if (System.Windows.Application.Current is IntelligentDesktop.UI.App app)
            {
                app.SaveConfiguration();
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
        
        // [1] 원래 아이콘 위치 저장
        try
        {
            var app = System.Windows.Application.Current as IntelligentDesktop.UI.App;
            if (app?.DesktopIconService != null && e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    // 현재 바탕화면 아이콘 상태 가져오기
                    var icons = app.DesktopIconService.GetAllIcons();
                    
                    foreach (string file in files)
                    {
                        var icon = icons.FirstOrDefault(i => string.Equals(i.FullPath, file, StringComparison.OrdinalIgnoreCase));
                        if (icon != null)
                        {
                            string fileName = System.IO.Path.GetFileName(file);
                            // 좌표 저장 (X,Y)
                            _box.OriginalPositions[fileName] = $"{icon.Position.X},{icon.Position.Y}";
                        }
                    }
                    app.SaveConfiguration();
                }
            }
        }
        catch { }
        
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[]? files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (files != null)
            {
                // 박스 저장소 폴더 준비
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string boxFolder = System.IO.Path.Combine(documentsPath, "IntelligentDesktop", "Boxes", _box.Id.ToString());
                
                if (!System.IO.Directory.Exists(boxFolder))
                {
                    System.IO.Directory.CreateDirectory(boxFolder);
                }

                foreach (string file in files)
                {
                    // 이미 박스 폴더 안에 있는 파일이면 경로만 추가
                    if (file.StartsWith(boxFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        AddIconToBox(file);
                        continue;
                    }

                    // 파일/폴더 이동
                    string fileName = System.IO.Path.GetFileName(file);
                    string destPath = System.IO.Path.Combine(boxFolder, fileName);
                    
                    try 
                    {
                        if (System.IO.File.Exists(file))
                        {
                            if (!System.IO.File.Exists(destPath))
                            {
                                System.IO.File.Move(file, destPath);
                                AddIconToBox(destPath);
                            }
                        }
                        else if (System.IO.Directory.Exists(file))
                        {
                            if (!System.IO.Directory.Exists(destPath))
                            {
                                System.IO.Directory.Move(file, destPath);
                                AddIconToBox(destPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Move failed: {ex.Message}");
                    }
                }
                RefreshIconList();
                
                // 아이콘 추가 이벤트 발생
                IconsChanged?.Invoke(this, EventArgs.Empty);
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
    /// 아이콘 목록 새로고침 (폴더 스캔)
    /// </summary>
    public void RefreshIconList()
    {
        string path = GetBoxFolderPath();
        if (!System.IO.Directory.Exists(path))
        {
            IconList.ItemsSource = null;
            EmptyHint.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var files = System.IO.Directory.GetFiles(path);
            var directories = System.IO.Directory.GetDirectories(path);
            
            _box.IconPaths.Clear();
            foreach(var f in files) _box.IconPaths.Add(f);
            foreach(var d in directories) _box.IconPaths.Add(d);

            var icons = new List<DesktopIcon>();
            
            // 폴더 먼저
            foreach (var dir in directories)
            {
                icons.Add(new DesktopIcon
                {
                    Name = System.IO.Path.GetFileName(dir),
                    FullPath = dir,
                    IsDirectory = true,
                    Icon = GetIconForPath(dir)
                });
            }
            
            // 파일
            foreach (var file in files)
            {
                icons.Add(new DesktopIcon
                {
                    Name = System.IO.Path.GetFileName(file),
                    FullPath = file,
                    IsDirectory = false,
                    Icon = GetIconForPath(file)
                });
            }

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
