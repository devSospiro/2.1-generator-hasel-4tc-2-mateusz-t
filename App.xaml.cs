using System.Windows;
using System.Windows.Media.Animation;

namespace PasswordGenerator;

/// <summary>
/// Główna klasa aplikacji App.xaml
/// </summary>
public partial class App : Application
{
    static App()
    {
        // Globalne ustawienie silnika animacji WPF na 120 FPS
        Timeline.DesiredFrameRateProperty.OverrideMetadata(
            typeof(Timeline),
            new FrameworkPropertyMetadata((int?)120));
    }
}
