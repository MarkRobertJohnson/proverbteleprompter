using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace ProverbTeleprompter
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
    }
}
