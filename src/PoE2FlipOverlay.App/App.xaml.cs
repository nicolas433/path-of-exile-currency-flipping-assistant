using System.Threading;

namespace PoE2FlipOverlay.App;

// Fully-qualified WPF types: enabling WinForms (for the tray icon) pulls
// System.Windows.Forms into scope, which makes bare "Application"/"MessageBox"
// ambiguous.
public partial class App : System.Windows.Application
{
    // Held for the lifetime of the process to enforce a single instance.
    private Mutex? _singleInstance;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        _singleInstance = new Mutex(initiallyOwned: true, "PoE2FlipOverlay.SingleInstance", out var isFirst);
        if (!isFirst)
        {
            System.Windows.MessageBox.Show(
                "O PoE2 Flip Overlay já está aberto.\n\n" +
                "Feche a instância atual (bandeja → Sair, ou Ctrl+Shift+X) antes de abrir de novo.",
                "PoE2 Flip Overlay",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
        new OverlayWindow().Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
