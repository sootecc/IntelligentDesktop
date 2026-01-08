using System;
using System.Windows;
using System.Windows.Threading;

namespace IntelligentDesktop.UI.Views;

public partial class ClockWidget : Window
{
    private DispatcherTimer _timer;

    public ClockWidget()
    {
        InitializeComponent();
        SetupTimer();
        UpdateClock();
    }

    private void SetupTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateClock();
        _timer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm");
        DateText.Text = now.ToString("yyyy년 M월 d일 dddd");
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            DragMove();
            SnapToNearbyBoxes();
        }
        catch { }
    }

    
    // 자석 기능: 근처 박스 경계에 달라붙기
    private void SnapToNearbyBoxes()
    {
        try
        {
            double threshold = 20.0;
            if (System.Windows.Application.Current is not IntelligentDesktop.UI.App app) return;
            
            System.Collections.IEnumerable list = app.BoxWindows;
            
            // 현재 위젯 좌표
            double myLeft = this.Left;
            double myRight = this.Left + this.Width;
            double myTop = this.Top;
            double myBottom = this.Top + this.Height;
            
            foreach (object item in list)
            {
                if (item is System.Windows.Window box)
                {
                    if (!box.IsVisible) continue;
    
                    // Box 좌표
                    double targetLeft = box.Left;
                    double targetRight = box.Left + box.Width;
                    double targetTop = box.Top;
                    double targetBottom = box.Top + box.Height;
                    
                    // 수평 스냅 (좌우)
                    if (Math.Abs(myLeft - targetRight) < threshold) this.Left = targetRight + 10;
                    else if (Math.Abs(myRight - targetLeft) < threshold) this.Left = targetLeft - this.Width - 10;
                    else if (Math.Abs(myLeft - targetLeft) < threshold) this.Left = targetLeft; 
                    else if (Math.Abs(myRight - targetRight) < threshold) this.Left = targetRight - this.Width;
                    
                    // 수직 스냅 (상하)
                    if (myRight > targetLeft && myLeft < targetRight) 
                    {
                        if (Math.Abs(myTop - targetBottom) < threshold) this.Top = targetBottom + 10;
                        else if (Math.Abs(myBottom - targetTop) < threshold) this.Top = targetTop - this.Height - 10;
                        else if (Math.Abs(myTop - targetTop) < threshold) this.Top = targetTop;
                        else if (Math.Abs(myBottom - targetBottom) < threshold) this.Top = targetBottom - this.Height;
                    }
                }
            }
        }
        catch { }
    }

    private void QuickOpacity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string tag && double.TryParse(tag, out double opacity))
        {
            if (this.FindName("MainBorder") is System.Windows.Controls.Border border && border.Background is System.Windows.Media.SolidColorBrush brush)
            {
                var color = brush.Color;
                color.A = (byte)(opacity * 255);
                border.Background = new System.Windows.Media.SolidColorBrush(color);
            }
        }
    }
    
    private void QuickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string colorCode)
        {
            try
            {
                var newColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorCode);
                
                // 현재 투명도 유지
                if (this.FindName("MainBorder") is System.Windows.Controls.Border border && border.Background is System.Windows.Media.SolidColorBrush brush)
                {
                    newColor.A = brush.Color.A;
                    border.Background = new System.Windows.Media.SolidColorBrush(newColor);
                }
            }
            catch { }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // 닫기 = 숨기기 (App에서 관리)
        this.Hide();
        // App.xaml.cs에서 상태 업데이트 필요하면 이벤트 발생 가능
        WidgetHidden?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? WidgetHidden;
}
