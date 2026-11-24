using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Specifics
{
    /// <summary>
    /// Fonts for the timer digits, from the same two folders as on Windows: the Resources/Fonts
    /// folder next to the app and Documents/YAPA2/Fonts. The key is the file name, which is what
    /// the "FontFamily" setting stores ("Segoe UI Light.ttf" by default).
    /// </summary>
    public class FontService : IFontService
    {
        public const string DefaultFontFamily = "avares://YAPA/Assets/Fonts#Segoe UI Light";

        private static readonly object Lock = new object();
        private static FileFontCollection _collection;
        private static FontManager _fontManager;
        private static readonly Dictionary<string, string> Loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> GetAllFonts()
        {
            var userFontLocation = Path.Combine(AvaloniaEnvironment.BaseDir, "Fonts");
            var systemFontLocation = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts");

            var fonts = GetFontsFromFolder(systemFontLocation);
            foreach (var userFont in GetFontsFromFolder(userFontLocation))
            {
                if (!fonts.ContainsKey(userFont.Key))
                {
                    fonts.Add(userFont.Key, userFont.Value);
                }
            }
            return fonts;
        }

        public string GetFontPath(string name)
        {
            var allFonts = GetAllFonts();
            return name != null && allFonts.TryGetValue(name, out var font) ? font : DefaultFontFamily;
        }

        private static Dictionary<string, string> GetFontsFromFolder(string folder)
        {
            var fonts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(folder))
            {
                return fonts;
            }
            foreach (var file in Directory.GetFiles(folder).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".ttf" && ext != ".otf")
                {
                    continue;
                }
                var family = Load(file);
                if (family != null)
                {
                    fonts[Path.GetFileName(file)] = family;
                }
            }
            return fonts;
        }

        private static string Load(string file)
        {
            lock (Lock)
            {
                // A new FontManager (a restarted Avalonia, as in the UI tests) knows none of them
                if (!ReferenceEquals(_fontManager, FontManager.Current))
                {
                    _fontManager = FontManager.Current;
                    _collection = new FileFontCollection();
                    _fontManager.AddFontCollection(_collection);
                    Loaded.Clear();
                }
                if (Loaded.TryGetValue(file, out var known))
                {
                    return known;
                }
                var familyName = _collection.AddFile(file);
                var source = familyName == null ? null : $"{FileFontCollection.CollectionKey}#{familyName}";
                Loaded[file] = source;
                return source;
            }
        }
    }

    /// <summary>Font files loaded from disk at run time.</summary>
    public sealed class FileFontCollection : FontCollectionBase
    {
        public const string CollectionKey = "fonts:YapaFiles";

        public override Uri Key { get; } = new Uri(CollectionKey, UriKind.Absolute);

        public string AddFile(string file)
        {
            try
            {
                var memory = new MemoryStream(File.ReadAllBytes(file));
                if (TryAddGlyphTypeface(memory, out var glyphTypeface))
                {
                    return glyphTypeface.FamilyName;
                }
            }
            catch
            {
                // not a font Avalonia can read
            }
            return null;
        }
    }
}
