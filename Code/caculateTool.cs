using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjustment
{
    class caculateTool
    {
        public static double dms2rad(double dms)
        {
            double d = Math.Floor(dms);
            double m = Math.Floor((dms - d) * 100.0);
            double s = (dms - d - m / 100.0) * 10000.0;
            return (d + m / 60.0 + s / 3600.0) * Math.PI / 180.0;
        }
        public static double rad2dms(double rad)
        {
            double temp = rad * 180 / Math.PI;
            double d = Math.Floor(temp);
            double m = Math.Floor((temp - d) * 60);
            double s = Math.Floor((temp - d - m / 60.0) * 3600);
            return d + m / 100.0 + s / 10000.0;
        }

        public static double coordinateAzimuth(double xA, double yA, double xB, double yB)
        {
            if (yB - yA < 0)
            {
                return Math.Atan2(yB - yA, xB - xA) + 2.0 * Math.PI;
            }
            if (xB - xA == 0 && yB - yA >= 0)
            {
                return Math.PI / 2.0;
            }
            if (xB - xA == 0 && yB - yA < 0)
            {
                return Math.PI * 3.0 / 2.0;
            }
            return Math.Atan2(yB - yA, xB - xA);
        }
    }
}
