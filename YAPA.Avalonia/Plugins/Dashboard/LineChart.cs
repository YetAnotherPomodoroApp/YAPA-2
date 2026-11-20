using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YAPA.Avalonia.Plugins.Dashboard
{
    /// <summary>
    /// The dashboard's "Last 60 days" chart, drawn to look like the LiveCharts 0.9 CartesianChart
    /// with one LineSeries that the WPF version uses: a smoothed blue line over a light fill,
    /// hollow round points, grey separators, axis titles "Total time" and "Day".
    /// LiveCharts' WPF package has no Avalonia build that matches, so this draws the same thing.
    /// </summary>
    public class LineChart : Control
    {
        // LiveCharts' first default series colour
        private static readonly Color SeriesColor = Color.Parse("#2195F2");
        private static readonly IBrush AxisBrush = new SolidColorBrush(Color.Parse("#A0A0A0"));
        private static readonly IPen SeparatorPen = new Pen(new SolidColorBrush(Color.Parse("#F0F0F0")), 1);
        private static readonly Typeface LabelFace = new Typeface(FontFamily.Default);
        private const double LabelSize = 11;
        private const double PointSize = 8;
        private const double Smoothness = 0.7;

        private IReadOnlyList<double> _values = Array.Empty<double>();
        private IReadOnlyList<string> _labels = Array.Empty<string>();

        public static readonly StyledProperty<string> AxisYTitleProperty = AvaloniaProperty.Register<LineChart, string>(nameof(AxisYTitle));
        public static readonly StyledProperty<string> AxisXTitleProperty = AvaloniaProperty.Register<LineChart, string>(nameof(AxisXTitle));

        public string AxisYTitle
        {
            get => GetValue(AxisYTitleProperty);
            set => SetValue(AxisYTitleProperty, value);
        }

        public string AxisXTitle
        {
            get => GetValue(AxisXTitleProperty);
            set => SetValue(AxisXTitleProperty, value);
        }

        public Func<double, string> LabelFormatter { get; set; } = x => x.ToString(CultureInfo.CurrentCulture);

        public void SetData(IReadOnlyList<double> values, IReadOnlyList<string> labels)
        {
            _values = values ?? Array.Empty<double>();
            _labels = labels ?? Array.Empty<string>();
            InvalidateVisual();
        }

        private FormattedText Text(string text, double size = LabelSize, IBrush brush = null)
        {
            return new FormattedText(text ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, LabelFace, size, brush ?? AxisBrush);
        }

        private static double NiceStep(double range, int wantedSteps)
        {
            if (range <= 0)
            {
                return 1;
            }
            var rough = range / wantedSteps;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
            var residual = rough / magnitude;
            var nice = residual <= 1 ? 1 : residual <= 2 ? 2 : residual <= 5 ? 5 : 10;
            return nice * magnitude;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_values.Count == 0)
            {
                return;
            }

            var bounds = Bounds;
            var max = Math.Max(_values.Max(), 1);
            var step = NiceStep(max, 2);
            var top = Math.Ceiling(max / step) * step;

            // Y axis labels decide the left margin
            var yLabels = new List<(double value, FormattedText text)>();
            for (var v = 0.0; v <= top + step / 1000; v += step)
            {
                yLabels.Add((v, Text(LabelFormatter(v))));
            }
            var yTitle = Text(AxisYTitle, LabelSize);
            var yLabelWidth = yLabels.Max(x => x.text.Width);
            var left = yTitle.Height + 4 + yLabelWidth + 6;

            var xTitle = Text(AxisXTitle, LabelSize);
            var sampleX = Text("0");
            var bottom = sampleX.Height + 2 + xTitle.Height;
            var plotTop = sampleX.Height / 2;
            var right = 10.0;

            var plot = new Rect(left, plotTop, Math.Max(10, bounds.Width - left - right), Math.Max(10, bounds.Height - bottom - plotTop));

            double X(int i) => _values.Count == 1 ? plot.X + plot.Width / 2 : plot.X + plot.Width * i / (_values.Count - 1);
            double Y(double v) => plot.Bottom - plot.Height * v / top;

            // Horizontal separators and Y labels
            foreach (var (value, text) in yLabels)
            {
                var y = Y(value);
                context.DrawLine(SeparatorPen, new Point(plot.X, y), new Point(plot.Right, y));
                context.DrawText(text, new Point(left - 6 - text.Width, y - text.Height / 2));
            }

            // Y axis title, rotated like LiveCharts'
            using (context.PushTransform(Matrix.CreateTranslation(-yTitle.Width / 2, -yTitle.Height / 2)
                                         * Matrix.CreateRotation(-Math.PI / 2)
                                         * Matrix.CreateTranslation(yTitle.Height / 2, plot.Y + plot.Height / 2)))
            {
                context.DrawText(yTitle, new Point(0, 0));
            }

            // X labels: as many as fit without overlapping
            if (_labels.Count > 0)
            {
                var widest = _labels.Max(x => Text(x).Width) + 20;
                var every = Math.Max(1, (int)Math.Ceiling(widest / Math.Max(1, plot.Width / Math.Max(1, _labels.Count - 1))));
                for (var i = 0; i < _labels.Count; i += every)
                {
                    var x = X(i);
                    context.DrawLine(SeparatorPen, new Point(x, plot.Y), new Point(x, plot.Bottom));
                    var text = Text(_labels[i]);
                    context.DrawText(text, new Point(Math.Max(0, x - text.Width / 2), plot.Bottom + 2));
                }
            }
            context.DrawText(xTitle, new Point(plot.X + plot.Width / 2 - xTitle.Width / 2, plot.Bottom + 2 + sampleX.Height));

            // The series: a smoothed line (LiveCharts' LineSmoothness 0.7) and its fill
            var points = _values.Select((v, i) => new Point(X(i), Y(v))).ToList();
            var line = new StreamGeometry();
            var area = new StreamGeometry();
            using (var lineContext = line.Open())
            using (var areaContext = area.Open())
            {
                lineContext.BeginFigure(points[0], false);
                areaContext.BeginFigure(new Point(points[0].X, plot.Bottom), true);
                areaContext.LineTo(points[0]);
                for (var i = 0; i < points.Count - 1; i++)
                {
                    var p0 = points[Math.Max(0, i - 1)];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = points[Math.Min(points.Count - 1, i + 2)];
                    var c1 = new Point(p1.X + (p2.X - p0.X) / 6 * Smoothness, p1.Y + (p2.Y - p0.Y) / 6 * Smoothness);
                    var c2 = new Point(p2.X - (p3.X - p1.X) / 6 * Smoothness, p2.Y - (p3.Y - p1.Y) / 6 * Smoothness);
                    // keep the curve inside the plot, as a time can't go below zero
                    c1 = new Point(c1.X, Math.Min(c1.Y, plot.Bottom));
                    c2 = new Point(c2.X, Math.Min(c2.Y, plot.Bottom));
                    lineContext.CubicBezierTo(c1, c2, p2);
                    areaContext.CubicBezierTo(c1, c2, p2);
                }
                lineContext.EndFigure(false);
                areaContext.LineTo(new Point(points[points.Count - 1].X, plot.Bottom));
                areaContext.EndFigure(true);
            }

            context.DrawGeometry(new SolidColorBrush(SeriesColor, 0.18), null, area);
            var seriesPen = new Pen(new SolidColorBrush(SeriesColor), 2);
            context.DrawGeometry(null, seriesPen, line);

            foreach (var point in points)
            {
                context.DrawEllipse(Brushes.White, seriesPen, point, PointSize / 2, PointSize / 2);
            }
        }
    }
}
