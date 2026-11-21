using System;
using System.Collections.Generic;

namespace YAPA.Shared.Common
{
    /// <summary>
    /// The installer keeps each version in its own folder ("app-2.0.192") next to the others.
    /// </summary>
    public static class InstalledVersions
    {
        private const string FolderPrefix = "app-";

        public static bool TryParse(string folderName, out Version version)
        {
            version = null;

            if (string.IsNullOrEmpty(folderName) || !folderName.StartsWith(FolderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Version.TryParse(folderName.Substring(FolderPrefix.Length), out version);
        }

        /// <summary>
        /// The folder of the newest version that is older than the current one, or null when there is none.
        /// </summary>
        public static string Previous(IEnumerable<string> folderNames, string currentFolderName)
        {
            if (folderNames == null || !TryParse(currentFolderName, out var current))
            {
                return null;
            }

            string previousFolder = null;
            Version previous = null;

            foreach (var folderName in folderNames)
            {
                if (!TryParse(folderName, out var version) || version >= current)
                {
                    continue;
                }

                if (previous == null || version > previous)
                {
                    previous = version;
                    previousFolder = folderName;
                }
            }

            return previousFolder;
        }
    }
}
