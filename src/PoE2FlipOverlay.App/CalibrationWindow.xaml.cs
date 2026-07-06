using System.Windows;
using System.Windows.Controls;
// WinForms/System.Drawing are in scope (tray icon), so pin the input types to WPF.
using Point = System.Windows.Point;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;

namespace PoE2FlipOverlay.App;

/// <summary>
/// Full-screen overlay for calibration: the user drags a rectangle over the
/// ratio panel. The chosen region is exposed (in physical pixels) via
/// <see cref="Result"/>, and <see cref="Window.DialogResult"/> is true on success.
/// Covers the primary screen only for the MVP.
/// </summary>
public partial class CalibrationWindow : Window
{
    public CaptureRegion? Result { get; private set; }

    private Point _start;
    private bool _dragging;

    public CalibrationWindow(string instruction)
    {
        InitializeComponent();
        InstructionText.Text = instruction + "  ·  Esc cancela";

        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        MouseLeftButtonDown += OnDown;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnUp;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(RootCanvas);
        _dragging = true;

        Canvas.SetLeft(SelRect, _start.X);
        Canvas.SetTop(SelRect, _start.Y);
        SelRect.Width = 0;
        SelRect.Height = 0;
        SelRect.Visibility = Visibility.Visible;
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        var p = e.GetPosition(RootCanvas);
        Canvas.SetLeft(SelRect, Math.Min(p.X, _start.X));
        Canvas.SetTop(SelRect, Math.Min(p.Y, _start.Y));
        SelRect.Width = Math.Abs(p.X - _start.X);
        SelRect.Height = Math.Abs(p.Y - _start.Y);
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;

        var p = e.GetPosition(RootCanvas);
        double xDip = Math.Min(p.X, _start.X);
        double yDip = Math.Min(p.Y, _start.Y);
        double wDip = Math.Abs(p.X - _start.X);
        double hDip = Math.Abs(p.Y - _start.Y);

        if (wDip < 5 || hDip < 5) // treat a tiny drag as a cancel
        {
            DialogResult = false;
            Close();
            return;
        }

        // WPF coordinates are DIPs; screen capture needs physical pixels.
        var dpi = VisualTreeHelper.GetDpi(this);
        Result = new CaptureRegion
        {
            X = (int)Math.Round(xDip * dpi.DpiScaleX),
            Y = (int)Math.Round(yDip * dpi.DpiScaleY),
            Width = (int)Math.Round(wDip * dpi.DpiScaleX),
            Height = (int)Math.Round(hDip * dpi.DpiScaleY)
        };
        DialogResult = true;
        Close();
    }
}
