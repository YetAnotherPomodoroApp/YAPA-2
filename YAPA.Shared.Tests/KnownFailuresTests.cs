using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAPA.Shared.Common;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class KnownFailuresTests
    {
        //The stack trace from the crash report in issue 124
        private const string ReportedStackTrace = @"   at MS.Internal.AppModel.ITaskbarList.HrInit()
   at System.Windows.Window.ApplyTaskbarItemInfo()
   at System.Windows.Window.WindowFilterMessage(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam, Boolean& handled)
   at System.Windows.Interop.HwndSource.PublicHooksFilterMessage(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam, Boolean& handled)
   at MS.Win32.HwndWrapper.WndProc(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam, Boolean& handled)
   at MS.Win32.HwndSubclass.DispatcherCallbackOperation(Object o)
   at System.Windows.Threading.ExceptionWrapper.InternalRealCall(Delegate callback, Object args, Int32 numArgs)
   at System.Windows.Threading.ExceptionWrapper.TryCatchWhen(Object source, Delegate callback, Object args, Int32 numArgs, Delegate catchHandler)";

        private class ExceptionWithStackTrace : NotImplementedException
        {
            private readonly string _stackTrace;

            public ExceptionWithStackTrace(string stackTrace)
            {
                _stackTrace = stackTrace;
            }

            public override string StackTrace => _stackTrace;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception Thrown(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }

        [TestMethod]
        public void ReportedCrash_IsRecognised()
        {
            Assert.IsTrue(KnownFailures.IsTaskbarIntegrationFailure(new ExceptionWithStackTrace(ReportedStackTrace)));
        }

        [TestMethod]
        public void AnyExceptionFromSettingUpTheTaskbarButton_IsRecognised()
        {
            var exception = Thrown(System.Windows.Window.ApplyTaskbarItemInfo);

            StringAssert.Contains(exception.StackTrace, "System.Windows.Window.ApplyTaskbarItemInfo");
            Assert.IsTrue(KnownFailures.IsTaskbarIntegrationFailure(exception));
        }

        [TestMethod]
        public void OtherExceptions_AreNotRecognised()
        {
            Assert.IsFalse(KnownFailures.IsTaskbarIntegrationFailure(Thrown(() => throw new NotImplementedException())));
            Assert.IsFalse(KnownFailures.IsTaskbarIntegrationFailure(Thrown(() => throw new InvalidOperationException("ApplyTaskbarItemInfo"))));
            Assert.IsFalse(KnownFailures.IsTaskbarIntegrationFailure(new ExceptionWithStackTrace("   at System.Windows.Window.Show()\r\n   at YAPA.App.Main()")));
        }

        [TestMethod]
        public void MissingExceptionOrStackTrace_IsNotRecognised()
        {
            Assert.IsFalse(KnownFailures.IsTaskbarIntegrationFailure(null));
            Assert.IsFalse(KnownFailures.IsTaskbarIntegrationFailure(new NotImplementedException()));
            Assert.IsFalse(KnownFailures.IsTaskbarIntegrationFailure(new ExceptionWithStackTrace(null)));
        }
    }
}

namespace System.Windows
{
    /// <summary>
    /// Stands in for the WPF window, so that a real stack trace names the same method.
    /// </summary>
    internal static class Window
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ApplyTaskbarItemInfo()
        {
            throw new InvalidOperationException("The taskbar is not available");
        }
    }
}
