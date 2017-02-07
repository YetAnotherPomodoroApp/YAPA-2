using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Primitives;
using YAPA.Contracts;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class PomodoroEngineSettingWindow : UserControl
    {

        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();

            //HACK!
            Enumerable.Range(1, 60).Select(x =>
             {
                 WorkTimeSelect.Items.Add(x);
                 BreakTimeSelect.Items.Add(x);
                 LongBreakTimeSelect.Items.Add(x);

                 return true;
             }).ToList();

            DataContext = settings;
        }

    }
}
