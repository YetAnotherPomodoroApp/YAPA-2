using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>
    /// Stands in for the Xceed WPF Toolkit ColorPicker the WPF settings use: a box showing the
    /// colour with a drop-down arrow. The drop-down is Avalonia's own ColorView.
    /// </summary>
    public class ColorPickerBox : UserControl
    {
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<ColorPickerBox, Color>(nameof(Color), Colors.White, defaultBindingMode: BindingMode.TwoWay);

        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private readonly Border _swatch;

        public ColorPickerBox()
        {
            Height = 22;
            Cursor = new Cursor(StandardCursorType.Hand);

            _swatch = new Border { Margin = new Thickness(3), Background = new SolidColorBrush(Color) };
            var arrow = new Path
            {
                Data = Geometry.Parse("M0,0 L8,0 L4,4 Z"),
                Fill = new SolidColorBrush(Color.Parse("#444444")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0),
            };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(_swatch);
            Grid.SetColumn(arrow, 1);
            grid.Children.Add(arrow);

            var box = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#ABADB3")),
                BorderThickness = new Thickness(1),
                Child = grid,
            };
            Content = box;

            var view = new ColorView
            {
                Width = 300,
                Height = 340,
            };
            view.Bind(ColorView.ColorProperty, new Binding(nameof(Color)) { Source = this, Mode = BindingMode.TwoWay });
            var flyout = new Flyout { Content = view, Placement = PlacementMode.BottomEdgeAlignedLeft };
            FlyoutBase.SetAttachedFlyout(this, flyout);

            PointerReleased += (s, e) =>
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    FlyoutBase.ShowAttachedFlyout(this);
                }
            };
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ColorProperty && _swatch != null)
            {
                _swatch.Background = new SolidColorBrush(Color);
            }
        }
    }
}
