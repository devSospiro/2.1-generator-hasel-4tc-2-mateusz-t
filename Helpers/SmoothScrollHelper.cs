using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PasswordGenerator.Helpers
{
    /// <summary>
    /// Klasa pomocnicza zapewniająca płynną animację przewijania (Smooth Scrolling)
    /// dla ScrollViewera z dynamicznym wygładzaniem i obsługą kółka myszy.
    /// </summary>
    public static class SmoothScrollHelper
    {
        public static readonly DependencyProperty IsSmoothScrollEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsSmoothScrollEnabled",
                typeof(bool),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(false, OnIsSmoothScrollEnabledChanged));

        public static bool GetIsSmoothScrollEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsSmoothScrollEnabledProperty);

        public static void SetIsSmoothScrollEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsSmoothScrollEnabledProperty, value);

        public static readonly DependencyProperty AnimatedVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset",
                typeof(double),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(0.0, OnAnimatedVerticalOffsetChanged));

        public static double GetAnimatedVerticalOffset(DependencyObject obj) =>
            (double)obj.GetValue(AnimatedVerticalOffsetProperty);

        public static void SetAnimatedVerticalOffset(DependencyObject obj, double value) =>
            obj.SetValue(AnimatedVerticalOffsetProperty, value);

        private static void OnAnimatedVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                sv.ScrollToVerticalOffset((double)e.NewValue);
            }
        }

        private static readonly Dictionary<ScrollViewer, double> TargetOffsets = new();

        private static void OnIsSmoothScrollEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                if ((bool)e.NewValue)
                {
                    sv.PreviewMouseWheel += OnPreviewMouseWheel;
                    sv.ScrollChanged += OnScrollChanged;
                    sv.PreviewMouseDown += OnPreviewMouseDown;
                    sv.Unloaded += OnScrollViewerUnloaded;
                }
                else
                {
                    sv.PreviewMouseWheel -= OnPreviewMouseWheel;
                    sv.ScrollChanged -= OnScrollChanged;
                    sv.PreviewMouseDown -= OnPreviewMouseDown;
                    sv.Unloaded -= OnScrollViewerUnloaded;
                    TargetOffsets.Remove(sv);
                }
            }
        }

        private static void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                TargetOffsets.Remove(sv);
            }
        }

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                // W przypadku ręcznego kliknięcia przerywamy bieżącą animację kółka
                sv.BeginAnimation(AnimatedVerticalOffsetProperty, null);
                TargetOffsets[sv] = sv.VerticalOffset;
            }
        }

        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                // Jeśli zmiana pozycji była wynikiem przeciągania suwaka lub zmiany rozmiaru
                if (!TargetOffsets.ContainsKey(sv) || Math.Abs(TargetOffsets[sv] - sv.VerticalOffset) > 120)
                {
                    TargetOffsets[sv] = sv.VerticalOffset;
                }
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;

            double scrollable = sv.ScrollableHeight;
            if (scrollable <= 0) return;

            e.Handled = true;

            if (!TargetOffsets.TryGetValue(sv, out double currentTarget))
            {
                currentTarget = sv.VerticalOffset;
            }

            // Przelicznik kółka myszy na piksele (1 notch = 120 delta -> 90 px przewinięcia)
            double deltaPixels = (e.Delta / 120.0) * 90.0;

            // Jeśli zmieniono kierunek przewijania w trakcie ruchu, zaczynamy od aktualnej pozycji
            if ((deltaPixels > 0 && currentTarget > sv.VerticalOffset) ||
                (deltaPixels < 0 && currentTarget < sv.VerticalOffset))
            {
                currentTarget = sv.VerticalOffset;
            }

            double newTarget = Math.Clamp(currentTarget - deltaPixels, 0, scrollable);
            TargetOffsets[sv] = newTarget;

            // Płynna animacja przejścia do nowego docelowego offsetu (120 FPS)
            var animation = new DoubleAnimation
            {
                To = newTarget,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Timeline.SetDesiredFrameRate(animation, 120);

            // Uruchomienie animacji na właściwości AnimatedVerticalOffset
            sv.BeginAnimation(AnimatedVerticalOffsetProperty, animation);
        }
    }
}
