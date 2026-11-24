using System;
using Avalonia.Media;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>Font families the WPF themes name, with fallbacks for systems that lack them.</summary>
    public static class Fonts
    {
        /// <summary>"Segoe UI" (Windows); elsewhere the closest common sans-serif.</summary>
        public static FontFamily SegoeUi { get; } = new FontFamily(
            OperatingSystem.IsWindows() ? "Segoe UI" :
            OperatingSystem.IsMacOS() ? "Segoe UI, Helvetica Neue" :
            "Segoe UI, Noto Sans, DejaVu Sans, sans-serif");

        /// <summary>The bundled Segoe UI Light, the timer's default digit font.</summary>
        public static FontFamily SegoeUiLight { get; } = new FontFamily("avares://YAPA/Assets/Fonts#Segoe UI Light");

        public static FontFamily Roboto { get; } = new FontFamily("avares://Material.Styles/Fonts/Roboto#Roboto");
    }
}
