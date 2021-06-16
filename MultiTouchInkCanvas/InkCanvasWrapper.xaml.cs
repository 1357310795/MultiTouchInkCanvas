using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace MultiTouchInkCanvas
{
    /// <summary>
    /// Interaction logic for InkCanvasWrapper.xaml
    /// </summary>
    public partial class InkCanvasWrapper : UserControl
    {
        private const int ThreasholdNearbyDistance = 1;

        /// <summary>
        /// The collection of strokes - a new one will be created for every TouchDown event
        /// </summary>
        private readonly Dictionary<int, Stroke> _currentCanvasStrokes;

        /// <summary>
        /// This is used for testing whether an erasing line as intersected with one of the strokes
        /// </summary>
        private IncrementalStrokeHitTester _strokeHitTester;

        private Stroke _addingStroke;

        public InkCanvasWrapper()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            _currentCanvasStrokes = new Dictionary<int, Stroke>();

            TouchDown += OnTouchDown;
            TouchUp += OnTouchUp;
            TouchMove += OnTouchMove;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            //ControlInkCanvas.IsEnabled = false;
        }

        private void OnTouchDown(object sender, TouchEventArgs touchEventArgs)
        {
            var touchPoint = touchEventArgs.GetTouchPoint(this);
            var point = touchPoint.Position;

            if (ControlInkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                if (_strokeHitTester == null)
                {
                    _strokeHitTester =
                        ControlInkCanvas.Strokes.GetIncrementalStrokeHitTester(new EllipseStylusShape(10, 10));

                    _strokeHitTester.StrokeHit += (o, argsHitTester) =>
                    {
                        var eraseResults = argsHitTester.GetPointEraseResults();

                        ControlInkCanvas.Strokes.Remove(argsHitTester.HitStroke);
                        ControlInkCanvas.Strokes.Add(eraseResults);
                    };
                }

                _strokeHitTester.AddPoint(point);
                return;
            }

            _addingStroke = new Stroke(new StylusPointCollection(new List<Point> { point }),
                       ControlInkCanvas.DefaultDrawingAttributes);

            if (!_currentCanvasStrokes.ContainsKey(touchPoint.TouchDevice.Id))
            {
                _currentCanvasStrokes.Add(touchPoint.TouchDevice.Id, _addingStroke);

                ControlInkCanvas.Strokes.Add(_addingStroke);
            }
        }

        private void OnTouchUp(object sender, TouchEventArgs touchEventArgs)
        {
            var touchPoint = touchEventArgs.GetTouchPoint(this);

            _currentCanvasStrokes.Remove(touchPoint.TouchDevice.Id);
            _strokeHitTester = null;
        }

        private void OnTouchMove(object sender, TouchEventArgs touchEventArgs)
        {
            var touchPoint = touchEventArgs.GetTouchPoint(this);
            var point = touchPoint.Position;

            if (ControlInkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                if (_strokeHitTester != null)
                {
                    _strokeHitTester.AddPoint(point);
                }

                return;
            }

            if (_currentCanvasStrokes.ContainsKey(touchPoint.TouchDevice.Id))
            {
                var stroke = _currentCanvasStrokes[touchPoint.TouchDevice.Id];

                //Before adding the point check if its near the last one
                var nearbyPoint = IsNearbyPoint(stroke, point);

                if (!nearbyPoint)
                {
                    //Add to the existing stroke
                    stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
                }
            }
        }

        private static bool IsNearbyPoint(Stroke stroke, Point point)
        {
            return stroke.StylusPoints.Any(p => (Math.Abs(p.X - point.X) <= ThreasholdNearbyDistance) &&
                (Math.Abs(p.Y - point.Y) <= ThreasholdNearbyDistance));
        }

        public void SetIsErasingMode(bool isErasing)
        {
            if (isErasing)
            {
                ControlInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                ControlInkCanvas.EraserShape = new EllipseStylusShape(20, 20);

                return;
            }
            
            ControlInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        }
    }
}
