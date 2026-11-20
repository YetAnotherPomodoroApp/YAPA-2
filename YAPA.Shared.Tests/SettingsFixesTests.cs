using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Yapa.Shared.Common;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class SettingsFixesTests
    {
        [TestMethod]
        public void AreEqual_ComparesAListWithOneReadFromSettingsJson()
        {
            //settings.json gives back a JArray for a list; this used to throw an InvalidCastException
            var json = new NewftonsJson();
            var saved = JArray.Parse("[\"SystemTray\"]");

            Assert.IsTrue(json.AreEqual(new List<string> { "SystemTray" }, saved));
            Assert.IsFalse(json.AreEqual(new List<string> { "SystemTray", "Unclickable" }, saved));
            Assert.IsFalse(json.AreEqual(new List<string>(), saved));
        }

        [TestMethod]
        public void ProfileSetters_DoNotChangeTheStoredProfile()
        {
            //The setters used to edit the stored profile object, so a change was live before Save
            var stored = new PomodoroProfile { WorkTime = 1500, BreakTime = 300, LongBreakTime = 900, PomodorosBeforeLongBreak = 4 };
            var profiles = new Dictionary<string, PomodoroProfile> { ["main"] = stored };
            var component = Substitute.For<ISettingsForComponent>();
            var settings = Substitute.For<ISettings>();
            settings.GetSettingsForComponent(Arg.Any<string>()).Returns(component);
            var engineSettings = new PomodoroEngineSettings(settings);
            engineSettings.Profiles.Returns(profiles);
            engineSettings.ActiveProfile.Returns("main");

            engineSettings.WorkTime = 1800;
            engineSettings.BreakTime = 600;
            engineSettings.AutoStartBreak = true;

            Assert.AreEqual(1500, stored.WorkTime);
            Assert.AreEqual(300, stored.BreakTime);
            Assert.IsFalse(stored.AutoStartBreak);
            component.Received().Update("Profiles", Arg.Is<Dictionary<string, PomodoroProfile>>(x => x["main"].WorkTime == 1800), Arg.Any<bool>());
        }
    }
}
