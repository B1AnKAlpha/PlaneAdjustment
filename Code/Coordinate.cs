using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Adjustment // 确保命名空间与您的项目一致
{
    public class Coordinate
    {

        public List<PointData> Points { get; private set; }


        public Coordinate(SurveyData surveyData)
        {
            this.Points = new List<PointData>();

            var pointCache = surveyData.KnownPoints.ToDictionary(p => p.Id);

            bool newPointCalculated;
            do
            {
                newPointCalculated = false;
                foreach (var station in surveyData.AllStations)
                {
                    if (!pointCache.TryGetValue(station.StationId, out var stationPoint))
                    {
                        continue; 
                    }
                    double startAzimuth;

                    if (surveyData.OrientationPoints.TryGetValue(station.StationId, out string orientationTargetId))
                    {

                        if (pointCache.TryGetValue(orientationTargetId, out var orientationPoint))
                        {
                            startAzimuth = CaculateTool.CoordinateAzimuth(stationPoint.X, stationPoint.Y, orientationPoint.X, orientationPoint.Y);
                        }
                        else
                        {
                            startAzimuth = 0.0; 
                        }
                    }
                    else
                    {
                        var backSightRecord = station.Records.FirstOrDefault(r => r.Type == "L" && r.Value == 0.0);
                        if (backSightRecord == null) continue; 

                        if (!pointCache.TryGetValue(backSightRecord.TargetId, out var backSightPoint))
                        {
                            continue; 
                        }
                        startAzimuth = CaculateTool.CoordinateAzimuth(stationPoint.X, stationPoint.Y, backSightPoint.X, backSightPoint.Y);
                    }
                    foreach (var record in station.Records)
                    {
                        if (pointCache.ContainsKey(record.TargetId)) continue;
                        if (record.Type != "L") continue;

                        var distanceRecord = station.Records.FirstOrDefault(r => r.TargetId == record.TargetId && r.Type == "S");
                        if (distanceRecord == null) continue;

                        double distance = distanceRecord.Value;
                        double observationAzimuth = startAzimuth + CaculateTool.DmsToRad(record.Value);

                        double deltaX = distance * Math.Cos(observationAzimuth);
                        double deltaY = distance * Math.Sin(observationAzimuth);

                        var newPoint = new PointData(record.TargetId, stationPoint.X + deltaX, stationPoint.Y + deltaY);

                        if (!pointCache.ContainsKey(newPoint.Id))
                        {
                            pointCache.Add(newPoint.Id, newPoint); // 先添加到缓存
                            this.Points.Add(newPoint);             // 再添加到结果列表
                            newPointCalculated = true;             // 标记有新点产生
                        }
                    }
                }
            }
            while (newPointCalculated);

            // 检查是否有未成功计算的点
            var allPointIds = surveyData.AllStations.Select(s => s.StationId).Union(surveyData.AllStations.SelectMany(s => s.Records.Select(r => r.TargetId))).Distinct();
            var uncalculatedPoints = allPointIds.Where(id => !pointCache.ContainsKey(id)).ToList();
            if (uncalculatedPoints.Any())
            {
                MessageBox.Show($"警告: 以下点的近似坐标未能计算出来: {string.Join(", ", uncalculatedPoints)}", "计算不完全", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    public static class CaculateTool
    {
        public static double CoordinateAzimuth(double x1, double y1, double x2, double y2)
        {
            return Math.Atan2(y2 - y1, x2 - x1);
        }

        public static double DmsToRad(double dmsValue)
        {
            int degrees = (int)dmsValue;
            double minutes = (int)((dmsValue - degrees) * 100);
            double seconds = (((dmsValue - degrees) * 100) - minutes) * 100;
            double totalDegrees = degrees + minutes / 60.0 + seconds / 3600.0;
            return totalDegrees * Math.PI / 180.0;
        }
    }
}