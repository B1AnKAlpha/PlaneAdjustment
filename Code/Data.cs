using System.Collections.Generic;

namespace Adjustment
{
    public class KnownData
    {
        public double DirectionError { get; }
        public double SideError { get; }
        public double SidePPMError { get; }
        public List<PointData> PointDatas { get; }

        public KnownData(double directionError, double sideError, double sidePPMError)
        {
            this.DirectionError = directionError;
            this.SideError = sideError;
            this.SidePPMError = sidePPMError;
            this.PointDatas = new List<PointData>();
        }
    }

    public class PointData
    {
        public string Id { get; }
        public double X { get; }
        public double Y { get; }

        public PointData(string id, double x, double y)
        {
            this.Id = id;
            this.X = x;
            this.Y = y;
        }
    }

    public class ObservedData
    {
        public string TestSiteNum { get; }
        public List<OriginalData> OriginalDatas { get; }

        public ObservedData(string testSiteNum)
        {
            this.TestSiteNum = testSiteNum;
            this.OriginalDatas = new List<OriginalData>();
        }
    }

    public class OriginalData
    {
        public string Id { get; }
        public string Type { get; }
        public double Value { get; }

        public OriginalData(string id, string type, double value)
        {
            this.Id = id;
            this.Type = type;
            this.Value = value;
        }
    }

    public class AngleObservation
    {
        public string SightingPoint { get; }
        public string ObservingPoint { get; }
        public string ReferencePoint { get; }
        public double Value { get; }

        public AngleObservation(string referencePoint, string observingPoint, string sightingPoint, double value)
        {
            this.SightingPoint = sightingPoint;
            this.ObservingPoint = observingPoint;
            this.ReferencePoint = referencePoint;
            this.Value = value;
        }
    }
    public class EdgeObservation
    {
        public string SightingPoint { get; }
        public string ObservingPoint { get; }
        public double Value { get; }

        public EdgeObservation(string observingPoint, string sightingPoint, double value)
        {
            this.SightingPoint = sightingPoint;
            this.ObservingPoint = observingPoint;
            this.Value = value;
        }
    }

    public class PrioriErrors
    {
        public double DirectionStdDev { get; set; } // 方向标准差 (秒)
        public double DistanceFixedError { get; set; } // 距离固定误差 (毫米)
        public double DistanceRatioError { get; set; } // 距离比例误差 (ppm)

        public PrioriErrors(double dir, double distFix, double distRatio)
        {
            DirectionStdDev = dir;
            DistanceFixedError = distFix;
            DistanceRatioError = distRatio;
        }
    }

    public class ObservationRecord
    {
        public string TargetId { get; set; }
        public string Type { get; set; } // "L" for Angle, "S" for Side
        public double Value { get; set; }

        public ObservationRecord(string targetId, string type, double value)
        {
            TargetId = targetId;
            Type = type;
            Value = value;
        }
    }
    public class StationObservations
    {
        public string StationId { get; set; }
        public List<ObservationRecord> Records { get; } = new List<ObservationRecord>();

        public StationObservations(string stationId)
        {
            StationId = stationId;
        }
    }
    public class SurveyData
    {
        public PrioriErrors Errors { get; set; }
        public List<PointData> KnownPoints { get; } = new List<PointData>();
        public List<StationObservations> AllStations { get; } = new List<StationObservations>();
        public Dictionary<string, string> OrientationPoints { get; } = new Dictionary<string, string>();
    }
}