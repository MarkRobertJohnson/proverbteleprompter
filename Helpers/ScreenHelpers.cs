using System.Drawing;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace ProverbTeleprompter.Helpers
{
    internal class ScreenHelpers
    {
        public static int GetScreenIndexForPoint(Point point)
        {
            for (int screenNum = 0; screenNum < SystemInformation.MonitorCount; ++screenNum)
            {
                Rectangle workingArea = Screen.AllScreens[screenNum].WorkingArea;
                if(IsPointInRectangle(point, workingArea))
                {
                    return screenNum;
                }
            }

            return 0;
        }

        public static bool IsPointInRectangle(Point point, Rectangle workingArea)
        {
            if(point.X >= workingArea.Left &&
                point.X < workingArea.Right &&
                point.Y >= workingArea.Top &&
                point.Y <= workingArea.Bottom)
            {
                return true;
            }

            return false;
        }

        public static Rectangle GetEntireDesktopArea()
        {
            int left = 999999;
            int top = 999999;
            int right = -999999;
            int bottom = -999999;
            var rect = new Rectangle();

            if(SystemInformation.MonitorCount <= 0) return rect;

            for (int screenNum = 0; screenNum < SystemInformation.MonitorCount; ++screenNum)
            {
                Rectangle workingArea = Screen.AllScreens[screenNum].WorkingArea;

                if (workingArea.Left < left)
                {
                    left = workingArea.Left;
                }
                if (workingArea.Top < top)
                {
                    top = workingArea.Top;
                }
                if (workingArea.Right > right)
                {
                    right = workingArea.Right;
                }
                if(workingArea.Bottom > bottom)
                {
                    bottom = workingArea.Bottom;
                }

            }

            rect.Location = new System.Drawing.Point(left,top);
            rect.Width = (right - left);
            rect.Height = bottom - top;
            return rect;
        }
    }
}
