using System.Windows.Input;

namespace YAPA
{
    /// <summary>
    /// View model used for settings UI.
    /// </summary>
    public interface ISettingsViewModel
    {
        /// <summary>
        /// Closes this view.
        /// </summary>
        void CloseSettings();

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        double ClockOpacity { get; set; }

        /// <summary>
        /// True if we are to use white text to render;
        /// otherwise, false.
        /// </summary>
        bool UseWhiteText { get; set; }

        /// <summary>
        /// The font size used to render the clock;
        /// </summary>
        int WorkTime { get; set; }

        /// <summary>
        /// The font size used to render the clock;
        /// </summary>
        int BreakTime { get; set; }

        /// <summary>
        /// The font size used to render the clock;
        /// </summary>
        int BreakLongTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        bool SoundEfects { get; set; }

        /// <summary>
        /// Command invoked when user clicks 'Done'
        /// </summary>
        ICommand SaveSettings { get; }
    }
}
