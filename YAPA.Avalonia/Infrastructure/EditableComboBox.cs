using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>
    /// The WPF settings use editable combo boxes (IsEditable="True") for times and counts: type
    /// any number, or pick one from the list. Material.Avalonia's ComboBox template has no text
    /// box, so this is its AutoCompleteBox (Material-styled) with a drop-down arrow that opens the
    /// whole list. Typing does not pop the list up, as in WPF.
    /// </summary>
    public class EditableComboBox : AutoCompleteBox
    {
        protected override Type StyleKeyOverride => typeof(AutoCompleteBox);

        private bool _openingFromArrow;

        public EditableComboBox()
        {
            FilterMode = AutoCompleteFilterMode.None;
            MinimumPrefixLength = 0;
            IsTextCompletionEnabled = false;

            var arrow = new Border
            {
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
                Padding = new Thickness(8, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = new Path
                {
                    Data = Geometry.Parse("M0,0 L8,0 L4,4 Z"),
                    Fill = new SolidColorBrush(Color.Parse("#757575")),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            arrow.PointerPressed += (s, e) =>
            {
                e.Handled = true;
                if (IsDropDownOpen)
                {
                    IsDropDownOpen = false;
                    return;
                }
                _openingFromArrow = true;
                IsDropDownOpen = true;
                _openingFromArrow = false;
            };
            InnerRightContent = arrow;

            DropDownOpening += (s, e) =>
            {
                // only the arrow (or Alt+Down) opens the list, not typing
                if (!_openingFromArrow && !_keyboardOpen)
                {
                    e.Cancel = true;
                }
            };
        }

        private bool _keyboardOpen;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.Key == Key.F4)
            {
                _keyboardOpen = true;
                IsDropDownOpen = !IsDropDownOpen;
                _keyboardOpen = false;
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }
    }
}
