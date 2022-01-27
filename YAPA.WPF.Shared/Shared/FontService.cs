using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.Shared.Common
{
    public class FontService : IFontService
    {
        public Dictionary<string, string> GetAllFonts()
        {
            var userFontLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"YAPA2", "Fonts");
            var systemFontLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Fonts");

            var userFonts = GetFontsFromFolder(userFontLocation);
            var systemFonts = GetFontsFromFolder(systemFontLocation);

            foreach (var userFont in userFonts)
            {
                if (!systemFonts.ContainsKey(userFont.Key))
                {
                    systemFonts.Add(userFont.Key, userFont.Value);
                }
            }

            return systemFonts;
        }

        private Dictionary<string, string> GetFontsFromFolder(string folder)
        {
            var fonts = new Dictionary<string, string>();
            if (!Directory.Exists(folder))
            {
                return new Dictionary<string, string>();
            }

            var allFiles = Directory.GetFiles(folder);
            foreach (var file in allFiles)
            {
                try
                {
                    if (file.Contains('#'))
                    {
                        fonts.Add(Path.GetFileName(file), file);
                    }
                    else
                    {
                        var families = Fonts.GetFontFamilies(file);
                        fonts.Add(Path.GetFileName(file), families.First().Source);
                    }
                }
                catch { }
            }

            return fonts;
        }

        public string GetFontPath(string name)
        {
            var allFonts = GetAllFonts();
            if (allFonts.ContainsKey(name))
            {
                return allFonts[name];
            }
            return "Segoe UI Light";
        }
    }
}
