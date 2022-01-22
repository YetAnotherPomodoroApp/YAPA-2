using System;

namespace YAPA.WPF
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BuiltInPluginAttribute : Attribute
    {
        public int Order { get; set; }
        public bool Hide { get; set; }
        public bool Disabled { get; set; }

        public BuiltInPluginAttribute()
        {
            Order = 999;
            Hide = false;
        }

    }
}
