using System.Collections.Generic;

namespace YAPA.Plugins.Unclickable
{
    public partial class UnclickableSettingsWindow
    {
        public UnclickableSettings Settings { get; }

        public UnclickableSettingsWindow(UnclickableSettings settings)
        {
            Settings = settings;
            Settings.DeferChanges();

            InitializeComponent();

            var counterValues = new List<BehaviourListItem>
            {
                new BehaviourListItem{ Item = UnclickablityType.ClickThrough, Title = "Click through"},
                new BehaviourListItem{ Item = UnclickablityType.MoveHorizontally, Title = "Move horizontally"},
                new BehaviourListItem{ Item = UnclickablityType.MoveVertically, Title = "Move vertically"},
            };

            BehaviourList.ItemsSource = counterValues;

            DataContext = this;
        }

        public class BehaviourListItem
        {
            public UnclickablityType Item { get; set; }
            public string Title { get; set; }
        }
    }
}
