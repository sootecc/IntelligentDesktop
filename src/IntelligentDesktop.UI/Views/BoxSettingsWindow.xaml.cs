using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IntelligentDesktop.Core.Models;

namespace IntelligentDesktop.UI.Views;

public partial class BoxSettingsWindow : Window
{
    public string BoxName => NameInput.Text;
    public string ResultBackgroundColor { get; private set; } = "#80000000";

    private string _selectedHexColor = "#000000";

    public BoxSettingsWindow(Box box)
    {
        InitializeComponent();
        
        NameInput.Text = box.Name;
        
        // 초기 색상 값 파싱 (#AARRGGBB)
        try
        {
            string currentBg = box.Style.BackgroundColor; 
            if (currentBg.Length == 9 && currentBg.StartsWith("#"))
            {
                string alphaHex = currentBg.Substring(1, 2);
                string rgbHex = "#" + currentBg.Substring(3);
                
                _selectedHexColor = rgbHex;
                int alpha = int.Parse(alphaHex, System.Globalization.NumberStyles.HexNumber);
                OpacitySlider.Value = alpha / 255.0;
            }
        }
        catch { }
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string hex)
        {
            _selectedHexColor = hex;
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        int alpha = (int)(OpacitySlider.Value * 255);
        string alphaHex = alpha.ToString("X2");
        string rgb = _selectedHexColor.StartsWith("#") ? _selectedHexColor.Substring(1) : _selectedHexColor;
        
        ResultBackgroundColor = $"#{alphaHex}{rgb}";
        
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
