using System.Drawing;
using System.Windows.Controls;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class ControlHelper
    {
        public static Point GetCenter(this Border control, System.Windows.Point absolutePoint)
        {
            return new Point((int)absolutePoint.X + (int)control.Width, (int)absolutePoint.Y + (int)control.Height);
        }
    }
}
