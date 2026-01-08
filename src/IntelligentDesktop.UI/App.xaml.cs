using System.Windows;
using IntelligentDesktop.Core.Models;
using IntelligentDesktop.Core.Services.Input;
using IntelligentDesktop.Core.Services.Shell;
using IntelligentDesktop.Core.Services.Sorting;
using IntelligentDesktop.Core.Services.Storage;
using IntelligentDesktop.UI.Services;
using IntelligentDesktop.UI.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace IntelligentDesktop.UI;

/// <summary>
/// App.xaml의 코드 비하인드
/// </summary>
public partial class App : Application
{
    private TrayIconService? _trayIconService;
    private ConfigurationService? _configService;
    private DesktopIconService? _desktopIconService;
    public DesktopIconService? DesktopIconService => _desktopIconService;
    private DesktopIntegrationService? _desktopIntegrationService;
    private GlobalMouseHook? _globalMouseHook;
    private SortingService? _sortingService;
    private ScreenMonitorService? _screenMonitorService;
    private LayoutBackupService? _layoutBackupService;
    private readonly List<BoxWindow> _boxWindows = new();
    public IReadOnlyList<BoxWindow> BoxWindows => _boxWindows.AsReadOnly();
    private Views.ClockWidget? _clockWidget;
    public Window? ClockWidget => _clockWidget; // 외부 공개
    private AppConfiguration? _config;
    
    private System.Threading.Mutex? _mutex;
    private StartupService? _startupService;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 글로벌 예외 처리
        AppDomain.CurrentDomain.UnhandledException += (s, ev) => LogError("AppDomain", ev.ExceptionObject as Exception);
        DispatcherUnhandledException += (s, ev) => 
        {
            LogError("Dispatcher", ev.Exception);
            ev.Handled = true; // 앱 종료 방지 (선택 사항, 여기선 종료 안함)
        };
        TaskScheduler.UnobservedTaskException += (s, ev) => 
        {
             LogError("TaskScheduler", ev.Exception);
             ev.SetObserved();
        };

        // 중복 실행 방지
        const string mutexName = "IntelligentDesktop_Mutex_SingleInstance";
        _mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            // 이미 실행 중이면 조용히 종료 (Mutex 소유권 없음)
            Shutdown();
            return;
        }

        base.OnStartup(e);
        
        try
        {
            Initialize();
        }
        catch (Exception ex)
        {
            LogError("Startup", ex);
            MessageBox.Show($"초기화 중 오류가 발생했습니다:\n{ex.Message}", 
                "IntelligentDesktop", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        // 윈도우 종료/로그오프 시에도 정리 작업 수행
        ExitApplication();
        base.OnSessionEnding(e);
    }



    private void LogError(string source, Exception? ex)
    {
        if (ex == null) return;
        try
        {
            // AppData 폴더에 로그 저장
            string appDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "IntelligentDesktop");
            
            if (!System.IO.Directory.Exists(appDataPath))
            {
                System.IO.Directory.CreateDirectory(appDataPath);
            }

            string logPath = System.IO.Path.Combine(appDataPath, "crash.log");
            string message = $"[{DateTime.Now}] [{source}] {ex.Message}\n{ex.StackTrace}\n\n";
            System.IO.File.AppendAllText(logPath, message);
        }
        catch { }
    }

    private void Initialize()
    {
        // 서비스 초기화
        _configService = new ConfigurationService();
        _config = _configService.Load();
        
        // 데스크톱 아이콘 서비스 초기화
        try
        {
            _desktopIconService = new DesktopIconService();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"데스크톱 서비스 초기화 실패: {ex.Message}");
            // 계속 진행 - 일부 기능만 사용 불가
        }
        
        // 데스크톱 통합 서비스 초기화 (SetParent, WinEventHook)
        _desktopIntegrationService = new DesktopIntegrationService();
        _desktopIntegrationService.DesktopIconsChanged += OnDesktopIconsChanged;
        _desktopIntegrationService.Initialize();
        
        // 시스템 트레이 아이콘 초기화
        _trayIconService = new TrayIconService();
        _trayIconService.ExitRequested += (s, e) => ExitApplication();
        _trayIconService.ShowBoxesRequested += (s, e) => ShowAllBoxes();
        _trayIconService.HideBoxesRequested += (s, e) => HideAllBoxes();
        _trayIconService.NewBoxRequested += (s, e) => CreateNewBox();
        _trayIconService.AutoSortRequested += (s, e) => ExecuteAutoSort();
        _trayIconService.BackupRequested += (s, e) => ExecuteBackup();
        _trayIconService.RestoreRequested += (s, e) => ExecuteRestore();
        _trayIconService.Initialize();
        
        _trayIconService.ToggleClockWidgetRequested += (s, e) => ToggleClockWidget();

        // 시작 프로그램 자동 실행 서비스
        _startupService = new StartupService();
        _trayIconService.StartupStateChanged += (s, enabled) => _startupService.SetStartup(enabled);
        _trayIconService.UpdateStartupState(_startupService.IsStartupEnabled);

        // 자동 정렬 서비스 초기화
        _sortingService = new SortingService();
        
        // 저장된 박스 복원
        RestoreBoxes();

        // 시계 위젯 복원
        if (_config != null && _config.IsClockWidgetVisible)
        {
            ShowClockWidget();
        }
        
        // 박스가 없으면 기본 박스 생성
        if (_boxWindows.Count == 0)
        {
            CreateNewBox();
        }
        
        // 해상도 변경 감지 (박스 위치 자동 조정)
        _screenMonitorService = new ScreenMonitorService();
        _screenMonitorService.ScreenChanged += OnScreenChanged;
        
        // 레이아웃 백업 서비스 초기화
        _layoutBackupService = new LayoutBackupService();
    }

    private void RestoreBoxes()
    {
        if (_config == null) return;

        foreach (var box in _config.Boxes)
        {
            // iTop 방식: 파일 수거 불필요 (아이콘 위치만 관리)

            var window = new BoxWindow(box);
            window.Closing += BoxWindow_Closing;
            window.NewBoxRequested += (s, e) => CreateNewBox();
            window.DeleteBoxRequested += (s, e) => DeleteBox(window);
            window.AutoAlignRequested += (s, e) => AlignBoxes();
            window.Show();
            _boxWindows.Add(window);
            
            // 데스크톱의 자식으로 등록 (Win+D에도 사라지지 않음)
            // _desktopIntegrationService?.RegisterAsDesktopChild(window);
        }
    }

    public void CreateNewBox()
    {
        // 화면 중앙 계산
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;
        double boxWidth = 200;
        double boxHeight = 300;
        
        // 약간의 랜덤 오프셋을 주어 겹치지 않게 함
        double offsetX = (_boxWindows.Count % 5) * 30;
        double offsetY = (_boxWindows.Count % 5) * 30;

        var box = new Box
        {
            Name = $"Box {_boxWindows.Count + 1}",
            X = (screenWidth - boxWidth) / 2 + offsetX,
            Y = (screenHeight - boxHeight) / 2 + offsetY,
            Width = boxWidth,
            Height = boxHeight
        };
        
        // Config에 추가
        if (_config != null && !_config.Boxes.Contains(box))
        {
            _config.Boxes.Add(box);
        }

        var window = new BoxWindow(box);
        window.Closing += BoxWindow_Closing;
        window.NewBoxRequested += (s, e) => CreateNewBox();
        window.DeleteBoxRequested += (s, e) => DeleteBox(window);
        window.AutoAlignRequested += (s, e) => AlignBoxes();
        window.Show();
        _boxWindows.Add(window);
        
        // 데스크톱의 자식으로 등록 (Win+D에도 사라지지 않음)
        // _desktopIntegrationService?.RegisterAsDesktopChild(window);
        
        // 생성 후 저장
        SaveConfiguration();
    }


    private void BoxWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is BoxWindow window)
        {
            // iTop 방식: 파일 복구 불필요 (아이콘 위치만 관리)
            
            _boxWindows.Remove(window);
            _config?.Boxes.Remove(window.Box);
            SaveConfiguration();
        }
    }

    public void ShowAllBoxes()
    {
        foreach (var window in _boxWindows)
        {
            window.Show();
            window.Box.IsVisible = true;
        }
    }

    public void HideAllBoxes()
    {
        foreach (var window in _boxWindows)
        {
            window.Hide();
            window.Box.IsVisible = false;
        }
    }

    private void DeleteBox(BoxWindow window)
    {
        if (MessageBox.Show("이 박스를 삭제하시겠습니까?\n(폴더 안의 파일은 바탕화면으로 복구됩니다)", "박스 삭제", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            window.Close();
        }
    }

    private void AlignBoxes()
    {
        double x = 50;
        double y = 50;
        double gap = 20;
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double maxHeightInRow = 0;

        foreach (var window in _boxWindows)
        {
            if (x + window.ActualWidth > screenWidth)
            {
                x = 50;
                y += maxHeightInRow + gap;
                maxHeightInRow = 0;
            }
            
            window.Left = x;
            window.Top = y;
            window.Box.X = x;
            window.Box.Y = y;
            
            x += window.ActualWidth + gap;
            maxHeightInRow = Math.Max(maxHeightInRow, window.ActualHeight);
        }
        SaveConfiguration();
    }

    public void SaveConfiguration()
    {
        if (_config != null && _configService != null)
        {
            _config.LastScreenWidth = SystemParameters.PrimaryScreenWidth;
            _config.LastScreenHeight = SystemParameters.PrimaryScreenHeight;

            // 현재 박스 위치 및 크기 업데이트
            foreach (var win in _boxWindows)
            {
                if (win.Box != null)
                {
                    // ID로 config 내 Box 찾기 (참조가 같으면 자동 반영되지만, 혹시 모르니 확인)
                    var targetBox = _config.Boxes.FirstOrDefault(b => b.Id == win.Box.Id);
                    if (targetBox != null)
                    {
                        targetBox.X = win.Left;
                        targetBox.Y = win.Top;
                        targetBox.Width = win.Width;
                        targetBox.Height = win.Height;
                    }
                }
            }

            _configService.Save(_config);
        }
    }

    private void ExitApplication()
    {
        // iTop 방식: 파일 복구 불필요 (아이콘 위치만 관리)
        
        SaveConfiguration();
        _globalMouseHook?.Dispose();
        _desktopIntegrationService?.Dispose();
        _trayIconService?.Dispose();
        _desktopIconService?.Dispose();
        
        // 시계 위젯 위치 저장
        if (_clockWidget != null)
        {
            if (_config != null)
            {
                _config.ClockWidgetPosition = $"{_clockWidget.Left},{_clockWidget.Top}";
                // 투명도 저장
                if (_clockWidget.FindName("MainBorder") is System.Windows.Controls.Border border && border.Background is System.Windows.Media.SolidColorBrush brush)
                {
                    _config.ClockWidgetOpacity = brush.Color.A / 255.0;
                    _config.ClockWidgetColor = $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
                }
                
                if (!_clockWidget.IsVisible) _config.IsClockWidgetVisible = false;
            }
            _clockWidget.Close();
        }
        
        foreach (var window in _boxWindows.ToList())
        {
            window.Closing -= BoxWindow_Closing;
            window.Close();
        }
        
        Shutdown();
    }

    /// <summary>
    /// 데스크톱 아이콘 변경 이벤트 핸들러 (WinEventHook에서 호출됨)
    /// </summary>
    private void OnDesktopIconsChanged(object? sender, EventArgs e)
    {
        // 성능을 위해 debounce 적용 가능
        // 필요시 아이콘 정보 새로고침
        System.Diagnostics.Debug.WriteLine("Desktop icons changed");
    }

    /// <summary>
    /// 자동 정리 실행 - 데스크톱 아이콘을 확장자별로 분류
    /// </summary>
    private void ExecuteAutoSort()
    {
        if (_sortingService == null || _desktopIconService == null || _config == null)
            return;

        try
        {
            // 현재 데스크톱 아이콘 가져오기
            var icons = _desktopIconService.GetAllIcons();
            var iconPaths = icons.Select(i => i.FullPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
            
            // 분류 실행
            var sortResult = _sortingService.AutoSortAll(iconPaths!, _config.Boxes);
            
            int totalSorted = 0;
            
            // 각 박스에 분류된 아이콘 추가
            foreach (var (boxId, paths) in sortResult)
            {
                var boxWindow = _boxWindows.FirstOrDefault(bw => bw.Box.Id == boxId);
                if (boxWindow != null)
                {
                    foreach (var path in paths)
                    {
                        boxWindow.AddIconToBox(path);
                    }
                    boxWindow.RefreshIconList();
                    totalSorted += paths.Count;
                }
            }
            
            // 설정 저장
            SaveConfiguration();
            
            // 결과 알림
            _trayIconService?.ShowBalloonTip(
                "자동 정리 완료",
                $"{totalSorted}개 항목이 분류되었습니다.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"자동 정리 오류: {ex.Message}");
            _trayIconService?.ShowBalloonTip(
                "자동 정리 실패",
                "정리 중 오류가 발생했습니다.");
        }
    }

    /// <summary>
    /// 해상도 변경 이벤트 핸들러 - 박스 위치 재조정
    /// </summary>
    private void OnScreenChanged(object? sender, ScreenChangedEventArgs e)
    {
        if (_config == null) return;
        
        double prevWidth = _config.LastScreenWidth;
        double prevHeight = _config.LastScreenHeight;
        
        // 이전 해상도와 같으면 무시
        if (Math.Abs(prevWidth - e.NewWidth) < 1 && Math.Abs(prevHeight - e.NewHeight) < 1)
            return;
        
        // 정규화된 좌표를 사용하여 새 해상도에 맞게 위치 조정
        foreach (var boxWindow in _boxWindows)
        {
            var box = boxWindow.Box;
            
            // 정규화된 좌표가 있으면 사용
            if (box.NormalizedX > 0 || box.NormalizedY > 0)
            {
                boxWindow.Left = box.NormalizedX * e.NewWidth;
                boxWindow.Top = box.NormalizedY * e.NewHeight;
            }
            else
            {
                // 비율로 위치 재계산
                double ratioX = e.NewWidth / prevWidth;
                double ratioY = e.NewHeight / prevHeight;
                boxWindow.Left = box.X * ratioX;
                boxWindow.Top = box.Y * ratioY;
            }
            
            // 화면 벗어남 방지
            if (boxWindow.Left + boxWindow.Width > e.NewWidth)
                boxWindow.Left = e.NewWidth - boxWindow.Width;
            if (boxWindow.Top + boxWindow.Height > e.NewHeight)
                boxWindow.Top = e.NewHeight - boxWindow.Height;
            if (boxWindow.Left < 0) boxWindow.Left = 0;
            if (boxWindow.Top < 0) boxWindow.Top = 0;
        }
        
        // 새 해상도 저장
        _config.LastScreenWidth = e.NewWidth;
        _config.LastScreenHeight = e.NewHeight;
        SaveConfiguration();
        
        _trayIconService?.ShowBalloonTip(
            "해상도 변경 감지",
            "박스 위치가 새 해상도에 맞게 조정되었습니다.");
    }

    /// <summary>
    /// 레이아웃 백업 실행
    /// </summary>
    private void ExecuteBackup()
    {
        if (_config == null || _layoutBackupService == null) return;

        try
        {
            // 현재 상태 저장
            foreach (var window in _boxWindows)
            {
                window.UpdateBoxPosition(); // 현재 위치 확실히 업데이트
            }
            
            string path = _layoutBackupService.CreateBackup(_config);
            _trayIconService?.ShowBalloonTip("백업 완료", $"레이아웃이 백업되었습니다.\n{System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"백업 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 레이아웃 복원 실행
    /// </summary>
    private void ExecuteRestore()
    {
        if (_config == null || _layoutBackupService == null) return;

        try
        {
            // 파일 열기 대화상자로 백업 파일 선택
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IntelligentDesktop", "Backups"),
                Title = "레이아웃 복원 파일 선택"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _layoutBackupService.RestoreBackup(openFileDialog.FileName, _config);
                
                // 현재 박스들 닫기
                foreach (var window in _boxWindows)
                {
                    window.Close();
                }
                _boxWindows.Clear();
                
                // 설정 저장
                SaveConfiguration();
                
                // 박스 재생성
                RestoreBoxes();
                
                _trayIconService?.ShowBalloonTip("복원 완료", "레이아웃이 성공적으로 복원되었습니다.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"복원 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // iTop 방식: 파일 복구 불필요
        _screenMonitorService?.Dispose();
        // _globalMouseHook 제거됨
        _desktopIntegrationService?.Dispose();
        _trayIconService?.Dispose();
        _desktopIconService?.Dispose();
        base.OnExit(e);
    }
    
    // ==============================================
    // 이전 파일 이동 방식의 메서드들은 삭제됨 (iTop 방식 전환)
    // - RestoreFilesToDesktop
    // - MoveAllContentToDest
    // - GetLongPath  
    // - CollectFilesFromDesktop
    // - RestoreFilesForBox
    // - RestoreIconPosition
    // ==============================================

    private void ToggleClockWidget()
    {
        if (_clockWidget == null || !_clockWidget.IsVisible)
        {
            ShowClockWidget();
            if (_config != null) _config.IsClockWidgetVisible = true;
        }
        else
        {
            _clockWidget.Hide();
            if (_config != null) _config.IsClockWidgetVisible = false;
        }
        SaveConfiguration();
    }

    private void ShowClockWidget()
    {
        if (_clockWidget == null)
        {
            _clockWidget = new Views.ClockWidget();
            _clockWidget.WidgetHidden += (s, e) => {
                if (_config != null) 
                {
                    _config.IsClockWidgetVisible = false;
                    SaveConfiguration();
                }
            };

            // 위치 복원
            if (_config != null)
            {
                if (!string.IsNullOrEmpty(_config.ClockWidgetPosition))
                {
                    try
                    {
                        var parts = _config.ClockWidgetPosition.Split(',');
                        if (parts.Length == 2)
                        {
                            _clockWidget.Left = double.Parse(parts[0]);
                            _clockWidget.Top = double.Parse(parts[1]);
                        }
                    }
                    catch { }
                }
                
                // 투명도 복원
                // 투명도 및 색상 복원
                if (_clockWidget.FindName("MainBorder") is System.Windows.Controls.Border border)
                {
                    byte alpha = (byte)(_config.ClockWidgetOpacity * 255);
                    System.Windows.Media.Color baseColor = System.Windows.Media.Colors.Black;
                    
                    try 
                    {
                        if (!string.IsNullOrEmpty(_config.ClockWidgetColor))
                        {
                            var colorConv = System.Windows.Media.ColorConverter.ConvertFromString(_config.ClockWidgetColor);
                            if (colorConv is System.Windows.Media.Color c) baseColor = c;
                        }
                    } 
                    catch { }

                    baseColor.A = alpha;
                    border.Background = new System.Windows.Media.SolidColorBrush(baseColor);
                }
            }
        }
        _clockWidget.Show();
    }
}
