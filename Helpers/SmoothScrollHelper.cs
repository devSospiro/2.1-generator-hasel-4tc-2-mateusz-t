using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PasswordGenerator.Helpers
{
    /// <summary>
    /// Klasa pomocnicza zapewniająca płynną animację przewijania (Smooth Scrolling)
    /// zsynchronizowaną z VSync monitora (CompositionTarget.Rendering).
    /// Używa wygładzania wykładniczego niezależnego od częstotliwości odświeżania.
    /// </summary>
    public static class SmoothScrollHelper
    {
        public static readonly DependencyProperty IsSmoothScrollEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsSmoothScrollEnabled",
                typeof(bool),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(false, OnPropertyChanged));

        public static bool GetIsSmoothScrollEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsSmoothScrollEnabledProperty);

        public static void SetIsSmoothScrollEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsSmoothScrollEnabledProperty, value);

        private static readonly Dictionary<ScrollViewer, ScrollState> States = new();

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv) return;

            if ((bool)e.NewValue)
            {
                var state = new ScrollState(sv);
                States[sv] = state;
            }
            else if (States.TryGetValue(sv, out var state))
            {
                state.Detach();
                States.Remove(sv);
            }
        }

        private sealed class ScrollState
        {
            private readonly ScrollViewer _sv;
            private double _targetOffset;
            private double _currentOffset;
            private bool _isAnimating;
            private TimeSpan _lastRenderTime;

            // Piksele przewinięcia na jeden notch kółka myszy (delta=120)
            // 120 * 0.75 = 90 px — komfortowa prędkość
            private const double ScrollMultiplier = 0.75;

            // Prędkość zbieżności wykładniczej — im wyższe, tym szybciej dogania cel.
            // 20 daje ~300ms do osiągnięcia celu, wystarczająco szybko żeby czuć responsywność,
            // a wystarczająco wolno żeby widzieć płynną animację.
            private const double SmoothingSpeed = 20.0;

            // Próg zakończenia animacji (piksele)
            private const double SnapThreshold = 0.3;

            public ScrollState(ScrollViewer sv)
            {
                _sv = sv;
                _sv.PreviewMouseWheel += OnMouseWheel;
                _sv.Unloaded += OnUnloaded;
            }

            public void Detach()
            {
                StopRendering();
                _sv.PreviewMouseWheel -= OnMouseWheel;
                _sv.Unloaded -= OnUnloaded;
            }

            private void OnUnloaded(object sender, RoutedEventArgs e)
            {
                Detach();
                States.Remove(_sv);
            }

            private void OnMouseWheel(object sender, MouseWheelEventArgs e)
            {
                double scrollable = _sv.ScrollableHeight;
                if (scrollable <= 0) return;

                e.Handled = true;

                double deltaPixels = e.Delta * ScrollMultiplier;

                if (!_isAnimating)
                {
                    // Synchronizujemy z aktualną pozycją scrolla
                    _currentOffset = _sv.VerticalOffset;
                    _targetOffset = _sv.VerticalOffset;
                }

                // Jeśli użytkownik zmienił kierunek, resetujemy cel z bieżącej pozycji
                double pending = _targetOffset - _currentOffset;
                if ((deltaPixels > 0 && pending > 0) || (deltaPixels < 0 && pending < 0))
                {
                    _targetOffset = _currentOffset;
                }

                _targetOffset = Math.Clamp(_targetOffset - deltaPixels, 0, scrollable);

                if (!_isAnimating)
                {
                    StartRendering();
                }
            }

            private void StartRendering()
            {
                _isAnimating = true;
                _lastRenderTime = TimeSpan.Zero;
                CompositionTarget.Rendering += OnRendering;
            }

            private void StopRendering()
            {
                if (_isAnimating)
                {
                    _isAnimating = false;
                    CompositionTarget.Rendering -= OnRendering;
                }
            }

            private void OnRendering(object? sender, EventArgs e)
            {
                // Pobieramy czas renderowania z RenderingEventArgs (zsynchronizowany z VSync)
                var renderArgs = (RenderingEventArgs)e;
                TimeSpan renderTime = renderArgs.RenderingTime;

                // Pomijamy duplikaty (WPF może wywołać Rendering wielokrotnie w jednej klatce)
                if (renderTime == _lastRenderTime) return;

                double dt;
                if (_lastRenderTime == TimeSpan.Zero)
                {
                    dt = 1.0 / 60.0; // Pierwsza klatka — zakładamy 60 FPS
                }
                else
                {
                    dt = (renderTime - _lastRenderTime).TotalSeconds;
                }
                _lastRenderTime = renderTime;

                // Zabezpieczenie przed skokami czasu (np. debugger, sleep, laptop z hibernacji)
                if (dt <= 0 || dt > 0.1) dt = 1.0 / 60.0;

                // Clamp target do aktualnego zakresu (na wypadek zmiany rozmiaru zawartości)
                double scrollable = _sv.ScrollableHeight;
                _targetOffset = Math.Clamp(_targetOffset, 0, scrollable);

                double diff = _targetOffset - _currentOffset;

                if (Math.Abs(diff) < SnapThreshold)
                {
                    // Wystarczająco blisko — przeskakujemy do celu i kończymy
                    _currentOffset = _targetOffset;
                    _sv.ScrollToVerticalOffset(_currentOffset);
                    StopRendering();
                    return;
                }

                // Wygładzanie wykładnicze niezależne od FPS:
                // factor = 1 - e^(-speed * dt)
                // Przy 60 Hz (dt=0.0167): factor ≈ 0.28 → szybka, płynna konwergencja
                // Przy 144 Hz (dt=0.007): factor ≈ 0.13 → proporcjonalnie mniejszy krok, ten sam efekt wizualny
                double factor = 1.0 - Math.Exp(-SmoothingSpeed * dt);
                _currentOffset += diff * factor;

                _sv.ScrollToVerticalOffset(_currentOffset);
            }
        }
    }
}
