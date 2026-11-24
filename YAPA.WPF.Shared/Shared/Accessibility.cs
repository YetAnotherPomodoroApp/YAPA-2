using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace YAPA.Shared
{
    /// <summary>
    /// Screen reader support that the themes and settings pages share.
    /// </summary>
    public static class Accessible
    {
        //The parts that take keyboard focus in place of the control they belong to:
        //the text box of an editable combo box and the button of a color picker
        private static readonly string[] FocusedParts = { "PART_EditableTextBox", "PART_ColorPickerToggleButton" };

        /// <summary>
        /// The name a screen reader speaks for a control, for controls whose label is a separate piece of text.
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.RegisterAttached(
            "Label", typeof(string), typeof(Accessible), new PropertyMetadata(null, OnLabelChanged));

        public static string GetLabel(DependencyObject element)
        {
            return (string)element.GetValue(LabelProperty);
        }

        public static void SetLabel(DependencyObject element, string value)
        {
            element.SetValue(LabelProperty, value);
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element))
            {
                return;
            }

            AutomationProperties.SetName(element, (string)e.NewValue ?? string.Empty);

            element.Loaded -= Element_Loaded;
            element.Loaded += Element_Loaded;

            if (element.IsLoaded)
            {
                NameFocusedParts(element);
            }
        }

        private static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            NameFocusedParts((FrameworkElement)sender);
        }

        private static void NameFocusedParts(FrameworkElement element)
        {
            if (!(element is Control control) || control.Template == null)
            {
                return;
            }

            var label = GetLabel(element) ?? string.Empty;
            foreach (var partName in FocusedParts)
            {
                if (control.Template.FindName(partName, control) is DependencyObject part)
                {
                    AutomationProperties.SetName(part, label);
                }
            }

            //The two halves of a slider's track are buttons of their own
            if (control is Slider && control.Template.FindName("PART_Track", control) is Track track)
            {
                if (track.DecreaseRepeatButton != null)
                {
                    AutomationProperties.SetName(track.DecreaseRepeatButton, $"Decrease {label}");
                }

                if (track.IncreaseRepeatButton != null)
                {
                    AutomationProperties.SetName(track.IncreaseRepeatButton, $"Increase {label}");
                }
            }
        }

        /// <summary>
        /// Names the small arrow that opens and closes a tree entry; the entry itself already says whether it is open.
        /// </summary>
        public static void NameExpander(TreeViewItem item)
        {
            void Apply()
            {
                if (item.Template?.FindName("Expander", item) is DependencyObject expander)
                {
                    AutomationProperties.SetName(expander, $"Expand or collapse {item.Header}");
                }
            }

            item.Loaded += (sender, args) => Apply();
            if (item.IsLoaded)
            {
                Apply();
            }
        }

        //Notifications came with .NET Framework 4.8.1; the application also runs on 4.8, which has only live regions
        private static readonly MethodInfo RaiseNotificationEvent = typeof(AutomationPeer).GetMethod("RaiseNotificationEvent");

        public static bool NotificationsSupported => RaiseNotificationEvent != null;

        /// <summary>
        /// Has a screen reader speak <paramref name="text"/> now, whatever has keyboard focus.
        /// Returns false where notifications are not available (.NET Framework 4.8 without the 4.8.1 update).
        /// </summary>
        public static bool Notify(UIElement element, string text)
        {
            if (element == null || string.IsNullOrEmpty(text) || RaiseNotificationEvent == null)
            {
                return false;
            }

            try
            {
                var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
                if (peer == null)
                {
                    return false;
                }

                var parameters = RaiseNotificationEvent.GetParameters();
                var kind = Enum.Parse(parameters[0].ParameterType, "Other");
                var processing = Enum.Parse(parameters[1].ParameterType, "ImportantMostRecent");
                RaiseNotificationEvent.Invoke(peer, new[] { kind, processing, text, "YapaTimer" });
                return true;
            }
            catch (Exception)
            {
                //Announcements are an extra; never let them get in the way of the timer
                return false;
            }
        }

        /// <summary>
        /// A piece of text nobody sees that a screen reader speaks whenever it changes.
        /// Used for announcements where <see cref="Notify"/> is not available.
        /// </summary>
        public static TextBlock CreateLiveRegion()
        {
            var region = new TextBlock
            {
                Width = 1,
                Height = 1,
                Opacity = 0,
                IsHitTestVisible = false,
                Focusable = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            AutomationProperties.SetLiveSetting(region, AutomationLiveSetting.Assertive);
            return region;
        }

        public static void SpeakThroughLiveRegion(TextBlock region, string text)
        {
            if (region == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                region.Text = text;
                var peer = UIElementAutomationPeer.FromElement(region) ?? UIElementAutomationPeer.CreatePeerForElement(region);
                peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
            catch (Exception)
            {
                //As above
            }
        }
    }

    /// <summary>
    /// Read by a screen reader as one piece of text instead of one element per child.
    /// The timer digits are separate text blocks; without this they are read "0", "0", ":", "0", "0".
    /// </summary>
    internal class SpokenAsOnePeer : FrameworkElementAutomationPeer
    {
        private readonly Func<string> _text;

        public SpokenAsOnePeer(FrameworkElement owner, Func<string> text) : base(owner)
        {
            _text = text;
        }

        protected override string GetNameCore()
        {
            return _text() ?? string.Empty;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string GetClassNameCore()
        {
            return "TextBlock";
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            return null;
        }

        protected override bool IsContentElementCore()
        {
            return true;
        }

        protected override bool IsControlElementCore()
        {
            return true;
        }
    }

    /// <summary>
    /// A status bar nobody sees, for the screen reader key that reads the status bar.
    /// NVDA finds a status bar by asking what is at the bottom left corner of the window,
    /// so this sits in that corner, spans the whole grid and can be hit there.
    /// Its text is worked out each time it is read, so it never needs updating.
    /// </summary>
    public class TimerStatusBar : FrameworkElement
    {
        private readonly Func<string> _text;

        public TimerStatusBar(Func<string> text)
        {
            _text = text;
            Width = 3;
            Height = 3;
            Focusable = false;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRowSpan(this, 1000);
            Grid.SetColumnSpan(this, 1000);
        }

        //Windows looks through fully transparent pixels of a see-through window, so the corner is
        //almost transparent instead: nobody sees it, but it is found there. A click there still reaches the window.
        private static readonly System.Windows.Media.Brush AlmostTransparent = CreateAlmostTransparent();

        private static System.Windows.Media.Brush CreateAlmostTransparent()
        {
            var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 0, 0, 0));
            brush.Freeze();
            return brush;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(AlmostTransparent, null, new Rect(RenderSize));
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new StatusBarPeer(this, _text);
        }

        private class StatusBarPeer : FrameworkElementAutomationPeer
        {
            private readonly Func<string> _text;

            public StatusBarPeer(FrameworkElement owner, Func<string> text) : base(owner)
            {
                _text = text;
            }

            protected override string GetNameCore()
            {
                return _text() ?? string.Empty;
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.StatusBar;
            }

            protected override string GetClassNameCore()
            {
                return "StatusBar";
            }

            protected override List<AutomationPeer> GetChildrenCore()
            {
                return null;
            }

            protected override bool IsContentElementCore()
            {
                return true;
            }

            protected override bool IsControlElementCore()
            {
                return true;
            }
        }
    }

    public class SpokenStackPanel : StackPanel
    {
        /// <summary>
        /// Gives the text to speak for the whole panel. Asked each time a screen reader reads it.
        /// </summary>
        public Func<string> SpokenText { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SpokenAsOnePeer(this, () => SpokenText?.Invoke());
        }
    }

    public class SpokenGrid : Grid
    {
        /// <summary>
        /// Gives the text to speak for the whole grid. Asked each time a screen reader reads it.
        /// </summary>
        public Func<string> SpokenText { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SpokenAsOnePeer(this, () => SpokenText?.Invoke());
        }
    }
}
