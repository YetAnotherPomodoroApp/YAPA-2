using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAPA.Shared.Common;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class InstalledVersionsTests
    {
        [TestMethod]
        public void Previous_IsTheNewestOlderVersion()
        {
            var folders = new[] { "app-2.0.179", "app-2.0.190", "app-2.0.192", "packages" };

            Assert.AreEqual("app-2.0.190", InstalledVersions.Previous(folders, "app-2.0.192"));
            Assert.AreEqual("app-2.0.179", InstalledVersions.Previous(folders, "app-2.0.190"));
        }

        [TestMethod]
        public void Previous_ComparesVersionsAsNumbers()
        {
            //As text "2.0.9" sorts after "2.0.190"
            var folders = new[] { "app-2.0.9", "app-2.0.10", "app-2.0.190", "app-2.1.0" };

            Assert.AreEqual("app-2.0.190", InstalledVersions.Previous(folders, "app-2.1.0"));
            Assert.AreEqual("app-2.0.10", InstalledVersions.Previous(folders, "app-2.0.190"));
            Assert.AreEqual("app-2.0.9", InstalledVersions.Previous(folders, "app-2.0.10"));
        }

        [TestMethod]
        public void Previous_NeverPicksTheSameOrANewerVersion()
        {
            Assert.IsNull(InstalledVersions.Previous(new[] { "app-2.0.192" }, "app-2.0.192"));
            Assert.IsNull(InstalledVersions.Previous(new[] { "app-2.0.192", "app-2.0.193", "app-3.0.0" }, "app-2.0.192"));
            Assert.IsNull(InstalledVersions.Previous(new string[0], "app-2.0.192"));
            Assert.IsNull(InstalledVersions.Previous(null, "app-2.0.192"));
        }

        [TestMethod]
        public void Previous_IgnoresFoldersThatAreNotVersions()
        {
            var folders = new[] { "packages", "Logs", "app-", "app-old", "app-2.0.190-beta", "backup-2.0.100", null, "", "APP-2.0.150" };

            Assert.AreEqual("APP-2.0.150", InstalledVersions.Previous(folders, "app-2.0.192"));
        }

        [TestMethod]
        public void Previous_NeedsTheCurrentFolderToBeAVersion()
        {
            var folders = new[] { "app-2.0.179", "app-2.0.190" };

            //A copy that was not installed runs from a folder such as "Debug"
            Assert.IsNull(InstalledVersions.Previous(folders, "Debug"));
            Assert.IsNull(InstalledVersions.Previous(folders, null));
            Assert.IsNull(InstalledVersions.Previous(folders, "app-latest"));
        }

        [TestMethod]
        public void TryParse_ReadsTheVersionFromAFolderName()
        {
            Assert.IsTrue(InstalledVersions.TryParse("app-2.0.192", out var version));
            Assert.AreEqual(new Version(2, 0, 192), version);

            Assert.IsFalse(InstalledVersions.TryParse("2.0.192", out _));
            Assert.IsFalse(InstalledVersions.TryParse("app-two", out _));
            Assert.IsFalse(InstalledVersions.TryParse(null, out _));
        }
    }
}
