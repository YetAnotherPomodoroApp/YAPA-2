using System.Windows.Input;
using System.Windows.Media;

namespace YAPA
{
    /// <summary>
    /// View model definition for the main clock.
    /// </summary>
    public interface IMainViewModel
    {
        /// <summary>
        /// Command binding used to show settings dialog
        /// </summary>
        ICommand ShowSettings { get; }

        /// <summary>
        /// The desired opacity of the clock;
        /// </summary>
        double ClockOpacity { get; set;  }

        /// <summary>
        /// The desired opacity of the shadow;
        /// </summary>
        double ShadowOpacity { get; set; }

        /// <summary>
        /// The color used to render the clock.
        /// </summary>
        Brush TextBrush { get; set; }

        /// <summary>
        /// The color used to render the shadow.
        /// </summary>
        Color TextShadowColor { get; set; }

        /// <summary>
        /// The color used to render on mouse hover.
        /// </summary>
        Brush MouseOverBackgroundColor { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        int WorkTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        int BreakTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        int BreakLongTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        bool SoundEffects { get; set; }
    }
}
