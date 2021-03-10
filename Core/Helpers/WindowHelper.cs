using System;
using System.Diagnostics;
using System.Windows.Forms;
using AutoIt;
using Application = System.Windows.Application;
// ReSharper disable PossibleLossOfFraction

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class WindowHelper
    {
        public static void Maximize(IntPtr intPtr)
        {
            PInvokeHelper.ShowWindow(intPtr, PInvokeHelper.SW_MAXIMIZE);
        }

        public static void MaximizeWindow(Character character)
        {
            if (character != null && character.Process != null)
                PInvokeHelper.ShowWindow(character.Process.MainWindowHandle, PInvokeHelper.SW_MAXIMIZE);
        }

        public static void MoveMainWindow()
        {
            var screenWidth = Screen.PrimaryScreen.Bounds.Width;
            var screenHeight = Screen.PrimaryScreen.Bounds.Height;

            if (Application.Current.MainWindow != null)
                AutoItX.WinMove(Process.GetCurrentProcess().MainWindowHandle,
                    (int) (screenWidth - Application.Current.MainWindow.Width),
                    (int) (screenHeight / 2 - Application.Current.MainWindow.Height / 2));
        }

        public static void SetMainWindowOpacity(double op)
        {
            if (Static.Datacenter == null) 
                return;

            if (Static.Datacenter.MainWindow != null)
                Static.Datacenter.MainWindow.Opacity = op;
        }
    }
}
