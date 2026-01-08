using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using IntelligentDesktop.UI.Views;

namespace IntelligentDesktop.UI.Services;

public class WindowSnappingService
{
    private const double SnapDistance = 15.0;

    /// <summary>
    /// 이동 중인 윈도우의 위치를 보정하여 스냅 좌표를 반환합니다.
    /// </summary>
    /// <param name="currentWindow">이동 중인 윈도우</param>
    /// <param name="newLeft">이동하려는 Left 좌표</param>
    /// <param name="newTop">이동하려는 Top 좌표</param>
    /// <returns>보정된 좌표</returns>
    public System.Windows.Point Snap(Window currentWindow, double newLeft, double newTop)
    {
        double snappedLeft = newLeft;
        double snappedTop = newTop;
        
        double currentWidth = currentWindow.Width; // ActualWidth는 렌더링 타이밍 문제 가능성, Width 속성 사용 권장
        if (double.IsNaN(currentWidth)) currentWidth = currentWindow.ActualWidth;
        
        double currentHeight = currentWindow.Height;
        if (double.IsNaN(currentHeight)) currentHeight = currentWindow.ActualHeight;

        // 화면 가장자리 스냅
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        if (Math.Abs(newLeft) < SnapDistance) snappedLeft = 0;
        if (Math.Abs(newTop) < SnapDistance) snappedTop = 0;
        if (Math.Abs(newLeft + currentWidth - screenWidth) < SnapDistance) snappedLeft = screenWidth - currentWidth;
        if (Math.Abs(newTop + currentHeight - screenHeight) < SnapDistance) snappedTop = screenHeight - currentHeight;

        // 다른 박스 윈도우들 및 시계 위젯과 스냅
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            if (window == currentWindow || !window.IsVisible) continue;
            
            // BoxWindow 또는 ClockWidget만 스냅 대상
            if (window is not BoxWindow && window is not ClockWidget) continue;
            
            // 다른 윈도우의 bounds
            double otherLeft = window.Left;
            double otherTop = window.Top;
            double otherWidth = window.ActualWidth;
            double otherHeight = window.ActualHeight;
            
            // 수평 스냅 (좌-우, 우-좌, 좌-좌, 우-우)
            if (Math.Abs(snappedTop + currentHeight - otherTop) < currentHeight + otherHeight + 100) // Y축이 어느정도 겹치거나 근처일때만
            {
                 // 내 왼쪽 - 남 오른쪽
                if (Math.Abs(snappedLeft - (otherLeft + otherWidth)) < SnapDistance) 
                    snappedLeft = otherLeft + otherWidth;
                
                // 내 오른쪽 - 남 왼쪽
                if (Math.Abs((snappedLeft + currentWidth) - otherLeft) < SnapDistance)
                    snappedLeft = otherLeft - currentWidth;
                
                // 내 왼쪽 - 남 왼쪽
                if (Math.Abs(snappedLeft - otherLeft) < SnapDistance)
                    snappedLeft = otherLeft;
                
                // 내 오른쪽 - 남 오른쪽
                if (Math.Abs((snappedLeft + currentWidth) - (otherLeft + otherWidth)) < SnapDistance)
                    snappedLeft = otherLeft + otherWidth - currentWidth;
            }

            // 수직 스냅
            if (Math.Abs(snappedLeft + currentWidth - otherLeft) < currentWidth + otherWidth + 100)
            {
                // 내 위 - 남 아래
                if (Math.Abs(snappedTop - (otherTop + otherHeight)) < SnapDistance)
                    snappedTop = otherTop + otherHeight;

                // 내 아래 - 남 위
                if (Math.Abs((snappedTop + currentHeight) - otherTop) < SnapDistance)
                    snappedTop = otherTop - currentHeight;
                
                // 내 위 - 남 위
                if (Math.Abs(snappedTop - otherTop) < SnapDistance)
                    snappedTop = otherTop;

                // 내 아래 - 남 아래
                if (Math.Abs((snappedTop + currentHeight) - (otherTop + otherHeight)) < SnapDistance)
                    snappedTop = otherTop + otherHeight - currentHeight;
            }
        }

        return new System.Windows.Point(snappedLeft, snappedTop);
    }
}
