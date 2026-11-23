using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>
    /// Read by a screen reader as one piece of text instead of one element per child (YAPA.WPF.Shared's
    /// SpokenStackPanel). The timer digits are separate text blocks; without this they are read "0", "0", ":", "0", "0".
    /// </summary>
    public class SpokenStackPanel : StackPanel
    {
        /// <summary>Gives the text to speak for the whole panel. Asked each time a screen reader reads it.</summary>
        public Func<string> SpokenText { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer() => new SpokenAsOnePeer(this, () => SpokenText?.Invoke());
    }

    /// <summary>The same for a grid (the Motivational theme's digits).</summary>
    public class SpokenGrid : Grid
    {
        public Func<string> SpokenText { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer() => new SpokenAsOnePeer(this, () => SpokenText?.Invoke());
    }

    internal sealed class SpokenAsOnePeer : ControlAutomationPeer
    {
        private readonly Func<string> _text;

        public SpokenAsOnePeer(Control owner, Func<string> text) : base(owner)
        {
            _text = text;
        }

        protected override string GetNameCore() => _text() ?? base.GetNameCore();

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => Array.Empty<AutomationPeer>();

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
