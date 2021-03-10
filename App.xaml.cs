using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Dofus.Retro.Supertools.Controls;

//using Keytrap.Theme.Controls;

namespace Dofus.Retro.Supertools
{
    public partial class App
    {
        private void GrdDragForm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(sender as Grid) is Window window)
                if (e.ChangedButton == MouseButton.Left)
                    window.DragMove();

        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(sender as WindowControlButton) is Window window)
                window.Close();
        }

        private void BtnMinimize_OnClick(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(sender as WindowControlButton) is Window window)
                window.WindowState = WindowState.Minimized;
        }
    }
}