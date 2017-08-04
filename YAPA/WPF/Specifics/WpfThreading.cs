using System;
using System.Windows;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.Specifics
{
    public class WpfThreading : IThreading
    {
        public void RunOnUiThread(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
    }
}
