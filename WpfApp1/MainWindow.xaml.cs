using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Map
        private Point? lastCenterPositionOnTarget;
        private Point? lastDragPoint;
        private Point? lastMousePositionOnTarget;
        private ScaleTransform scaleTransform;

        public MainWindow()
        {
            //Imposto la qualità massima di rendering immagini ed elementi grafici
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

            InitializeComponent();

            //MAPPA 
            ScrollMap.ScrollChanged += MapScrollChanged;
            ScrollMap.PreviewMouseWheel += MapMouseWheel;
            ScrollMap.PreviewMouseRightButtonDown += MapRightButtonDown;
            ScrollMap.PreviewMouseRightButtonUp += MapRightButtonUp;
            ScrollMap.MouseMove += OnMouseMove;
            sliderMap.ValueChanged += MapSliderValueChanged; // Aggiunto per gestire il cambiamento dal codice.

            scaleTransform = new ScaleTransform(1, 1);
            CanvasMap.RenderTransform = scaleTransform;
            CanvasMap.RenderTransformOrigin = new Point(0.5, 0.5); // Imposta il centro di scala al centro del Canvas
        }

        private void MapRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(ScrollMap);
            if (mousePos.X <= ScrollMap.ViewportWidth && mousePos.Y < ScrollMap.ViewportHeight) //make sure we still can use the scrollbars
            {
                lastDragPoint = mousePos;
                Mouse.Capture(ScrollMap);
            }
        }

        private void MapRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ScrollMap.Cursor = Cursors.Arrow;
            ScrollMap.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        private void MapScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(ScrollMap.ViewportWidth / 2, ScrollMap.ViewportHeight / 2);
                        Point centerOfTargetNow = ScrollMap.TranslatePoint(centerOfViewport, CanvasMap);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(CanvasMap);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / CanvasMap.ActualWidth;
                    double multiplicatorY = e.ExtentHeight / CanvasMap.ActualHeight;

                    double newOffsetX = ScrollMap.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    double newOffsetY = ScrollMap.VerticalOffset - dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    //ScrollAnimationBehavior.AnimateHorizontalScroll(ScrollMap, newOffsetY);
                    //ScrollAnimationBehavior.AnimateScroll(ScrollMap, newOffsetX);
                    ScrollMap.ScrollToHorizontalOffset(newOffsetX);
                    ScrollMap.ScrollToVerticalOffset(newOffsetY);
                    Debug.WriteLine($"ScrollToHorizontalOffset: {newOffsetX}");
                    Debug.WriteLine($"ScrollToVerticalOffset: {newOffsetY}");
                }
            }
        }

        private void MapMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = 1.2; // Fattore di zoom
            double newValue;

            if (e.Delta > 0)
            {
                newValue = sliderMap.Value * zoomFactor;
            }
            else
            {
                newValue = sliderMap.Value / zoomFactor;
            }

            newValue = Math.Max(sliderMap.Minimum, Math.Min(sliderMap.Maximum, newValue));

            sliderMap.Value = newValue;

            e.Handled = true;
        }

        private void MapSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AnimateScale(e.NewValue);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {

            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(ScrollMap);

                if (lastDragPoint != posNow)
                    ScrollMap.Cursor = Cursors.SizeAll;

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                ScrollMap.ScrollToHorizontalOffset(ScrollMap.HorizontalOffset - dX);
                ScrollMap.ScrollToVerticalOffset(ScrollMap.VerticalOffset - dY);
            }
            else
            {
                Point posNow = e.GetPosition(ScrollMap);
                Debug.WriteLine($"posNow: {posNow}");
                lastMousePositionOnTarget = posNow;
            }
        }

        private void AnimateScale(double newValue)
        {
            var animation = new DoubleAnimation
            {
                To = newValue,
                Duration = TimeSpan.FromMilliseconds(500), // Durata dell'animazione
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } // Effetto di easing per una transizione più fluida
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
    }
}
