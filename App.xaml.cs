using System.Windows;

namespace PasswordGenerator;

/// <summary>
/// Główna klasa aplikacji App.xaml
/// </summary>
public partial class App : Application
{
    // Brak nadpisania DesiredFrameRate — WPF używa natywnego VSync monitora
    // (60 Hz, 120 Hz, 144 Hz itp. — automatycznie dopasowuje się do ekranu).
    // Wymuszanie wyższego FPS powodowało dodatkowe obciążenie CPU bez poprawy płynności,
    // ponieważ CompositionTarget.Rendering i tak synchronizuje się z częstotliwością monitora.
}
