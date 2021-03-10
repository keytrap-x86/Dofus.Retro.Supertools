using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using AutoIt;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class MouseHelper
    {
		/// <summary>
		/// Returns the mouse cursor location.  This method is necessary during 
		/// a drag-drop operation because the WPF mechanisms for retrieving the
		/// cursor coordinates are unreliable.
		/// </summary>
		/// <param name="relativeTo">The Visual to which the mouse coordinates will be relative.</param>
		public static Point GetMousePosition(Visual relativeTo)
		{
			var mouse = new PInvokeHelper.Point();
			PInvokeHelper.GetCursorPos(ref mouse);
			return relativeTo.PointFromScreen(new Point(mouse.X, mouse.Y));
		}

		/// <summary>
		/// Centers the mouse on screen
		/// </summary>
		public static void CenterMouseOnScreen()
        {
			AutoItX.MouseMove(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2);
		}
	}
}
