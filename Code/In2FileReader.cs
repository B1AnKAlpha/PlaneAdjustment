using Adjustment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adjustment
{
    public class In2FileReader
    {
        public SurveyData Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("指定的数据文件不存在。", filePath);
            }

            var surveyData = new SurveyData();
            var lines = File.ReadAllLines(filePath);

            // 使用一个索引来跟踪处理到哪一行
            int currentIndex = 0;

            // 1. 解析第一行：验前误差
            if (currentIndex < lines.Length)
            {
                ParsePrioriErrors(lines[currentIndex], surveyData);
                currentIndex++;
            }

            // 2. 解析已知点和特殊定向点
            currentIndex = ParseKnownPointsAndOrientation(lines, currentIndex, surveyData);

            // 3. 解析所有测站的观测数据
            ParseAllStations(lines, currentIndex, surveyData);

            return surveyData;
        }

        private void ParsePrioriErrors(string line, SurveyData data)
        {
            var parts = line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) throw new FormatException("文件第一行（验前误差）格式错误，需要至少3个数字。");

            data.Errors = new PrioriErrors(
                double.Parse(parts[0]),
                double.Parse(parts[1]),
                double.Parse(parts[2])
            );
        }

        private int ParseKnownPointsAndOrientation(string[] lines, int startIndex, SurveyData data)
        {
            int i = startIndex;
            for (; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // 检查是否是已知点行 (通常3个字段)
                if (parts.Length == 3 && double.TryParse(parts[1], out double x) && double.TryParse(parts[2], out double y))
                {
                    data.KnownPoints.Add(new PointData(parts[0], x, y));
                }
                // 检查是否是特殊定向行 (通常4个字段，第3个为'A')
                else if (parts.Length == 4 && parts[2].Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    // 格式：测站, 后视点, A, 0
                    data.OrientationPoints[parts[0]] = parts[1];
                }
                // 如果都不是，说明已知点部分结束，进入观测数据部分
                else
                {
                    // 返回当前行索引，让下一个解析函数处理
                    return i;
                }
            }
            return i;
        }

        private void ParseAllStations(string[] lines, int startIndex, SurveyData data)
        {
            StationObservations currentStation = null;

            for (int i = startIndex; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // 检查是否是新的测站名 (通常只有1个字段)
                if (parts.Length == 1)
                {
                    currentStation = new StationObservations(parts[0]);
                    data.AllStations.Add(currentStation);
                }
                // 否则，认为是当前测站的观测记录 (通常3个字段)
                else if (parts.Length == 3 && currentStation != null)
                {
                    currentStation.Records.Add(new ObservationRecord(
                        parts[0],
                        parts[1].ToUpper(), // L 或 S
                        double.Parse(parts[2])
                    ));
                }
            }
        }
    }
}