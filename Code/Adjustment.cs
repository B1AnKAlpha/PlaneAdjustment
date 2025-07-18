using System;
using System.Collections.Generic;
using System.Linq;

namespace Adjustment
{
    public class IndirectAdj
    {
        public double[,] B, P, l, N, Q, V, X;
        public double sigma0;
        public List<AngleObservation> AngleObservations;
        public List<EdgeObservation> EdgeObservations;
        private readonly Coordinate _approximateCoordinate;
        private readonly List<ObservedData> _observedDataList;
        private readonly KnownData _knownData;
        private Dictionary<string, int> _pointIndexLookup;

        public IndirectAdj(Coordinate approximateCoordinate, List<ObservedData> observedDatas, KnownData knownData)
        {
            _approximateCoordinate = approximateCoordinate;
            _observedDataList = observedDatas;
            _knownData = knownData;
        }

        public void PerformAdjustment()
        {
            InitializeAndProcessObservations();
            BuildBMatrix();
            BuildPMatrix();
            BuildLMatrix();
            SolveNormalEquationsAndAnalyze();
        }

        private void InitializeAndProcessObservations()
        {
            AngleObservations = new List<AngleObservation>();
            EdgeObservations = new List<EdgeObservation>();
            foreach (var observedData in _observedDataList)
            {
                var refPoint = observedData.OriginalDatas.FirstOrDefault();
                if (refPoint == null) continue;
                foreach (var originalData in observedData.OriginalDatas)
                {
                    if (originalData.Type == "L" && originalData.Value != 0)
                        AngleObservations.Add(new AngleObservation(refPoint.Id, observedData.TestSiteNum, originalData.Id, originalData.Value));
                    else if (originalData.Type == "S")
                        EdgeObservations.Add(new EdgeObservation(observedData.TestSiteNum, originalData.Id, originalData.Value));
                }
            }
            _pointIndexLookup = _approximateCoordinate.Points.Select((p, i) => new { p.Id, i }).ToDictionary(x => x.Id, x => x.i);
        }

        // --- 以下所有方法都精确复现原始逻辑 ---

        private void BuildBMatrix()
        {
            B = new double[AngleObservations.Count + EdgeObservations.Count, 2 * _approximateCoordinate.Points.Count];
            for (int i = 0; i < AngleObservations.Count; i++)
            {
                var obs = AngleObservations[i];
                var A = FindPoint(obs.ObservingPoint);
                var P = FindPoint(obs.SightingPoint);
                if (A == null || P == null) continue;
                bool A_IsKnown = !_approximateCoordinate.Points.Any(p => p.Id == A.Id);
                bool P_IsKnown = !_approximateCoordinate.Points.Any(p => p.Id == P.Id);
                double alpha = caculateTool.coordinateAzimuth(A.X, A.Y, P.X, P.Y);
                double S = Math.Sqrt(Math.Pow(A.X - P.X, 2) + Math.Pow(A.Y - P.Y, 2));
                if (S < 1e-6) continue;
                double a = 206265 * Math.Sin(alpha) / S / 100.0;
                double b = -206265 * Math.Cos(alpha) / S / 100.0;
                if (!A_IsKnown) SetCoefficientInB(i, A, a, b);
                if (!P_IsKnown) SetCoefficientInB(i, P, -a, -b);
            }
            for (int i = 0; i < EdgeObservations.Count; i++)
            {
                var obs = EdgeObservations[i];
                var A = FindPoint(obs.ObservingPoint);
                var P = FindPoint(obs.SightingPoint);
                if (A == null || P == null) continue;
                bool A_IsKnown = !_approximateCoordinate.Points.Any(p => p.Id == A.Id);
                bool P_IsKnown = !_approximateCoordinate.Points.Any(p => p.Id == P.Id);
                double alpha = caculateTool.coordinateAzimuth(A.X, A.Y, P.X, P.Y);
                double a_cos = Math.Cos(alpha);
                double b_sin = Math.Sin(alpha);
                if (!A_IsKnown) SetCoefficientInB(i + AngleObservations.Count, A, -a_cos, -b_sin);
                if (!P_IsKnown) SetCoefficientInB(i + AngleObservations.Count, P, -a_cos, -b_sin);
            }
        }

        private void BuildPMatrix()
        {
            P = new double[AngleObservations.Count + EdgeObservations.Count, AngleObservations.Count + EdgeObservations.Count];
            for (int i = 0; i < AngleObservations.Count; i++) P[i, i] = 1.0;
            for (int i = 0; i < EdgeObservations.Count; i++)
            {
                double xigmaB = _knownData.DirectionError;
                double xigmaS = _knownData.SideError + Math.Pow(10, -6) * EdgeObservations[i].Value;
                P[i + AngleObservations.Count, i + AngleObservations.Count] = Math.Pow(xigmaB / xigmaS, 2);
            }
        }

        private void BuildLMatrix()
        {
            l = new double[AngleObservations.Count + EdgeObservations.Count, 1];
            for (int i = 0; i < AngleObservations.Count; i++)
            {
                var obs = AngleObservations[i];
                var A = FindPoint(obs.ReferencePoint);
                var B = FindPoint(obs.ObservingPoint);
                var C = FindPoint(obs.SightingPoint);
                if (A == null || B == null || C == null) continue;
                double alphaAB = caculateTool.coordinateAzimuth(B.X, B.Y, A.X, A.Y);
                double alphaCB = caculateTool.coordinateAzimuth(B.X, B.Y, C.X, C.Y);
                double deltaAlpha = alphaCB - alphaAB;
                if (deltaAlpha < 0) deltaAlpha += 2 * Math.PI;
                double delta = caculateTool.dms2rad(obs.Value) - deltaAlpha;
                l[i, 0] = delta * 180 / Math.PI * 3600;
            }
            for (int i = 0; i < EdgeObservations.Count; i++)
            {
                var obs = EdgeObservations[i];
                var A = FindPoint(obs.ObservingPoint);
                var B = FindPoint(obs.SightingPoint);
                if (A == null || B == null) continue;
                double S = Math.Sqrt(Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2));
                l[i + AngleObservations.Count, 0] = (obs.Value - S) * 1000.0;
            }
        }

        private void SolveNormalEquationsAndAnalyze()
        {
            N = Matrix.Multiply(Matrix.Transpose(B), Matrix.Multiply(P, B));
            var W = Matrix.Multiply(Matrix.Transpose(B), Matrix.Multiply(P, l));
            Q = Matrix.Inverse(N);
            X = Matrix.Multiply(Q, W);
            V = Matrix.Sub(Matrix.Multiply(B, X), l);
            var temp = Matrix.Multiply(Matrix.Transpose(V), V);
            double n = AngleObservations.Count + EdgeObservations.Count;
            double t = 2 * _approximateCoordinate.Points.Count;
            if (n > t) sigma0 = Math.Sqrt(temp[0, 0] / (n - t));
        }

        private void SetCoefficientInB(int rowIndex, PointData point, double coeffDx, double coeffDy)
        {
            if (_pointIndexLookup.TryGetValue(point.Id, out int colIndex))
            {
                B[rowIndex, 2 * colIndex] = coeffDx;
                B[rowIndex, 2 * colIndex + 1] = coeffDy;
            }
        }

        private PointData FindPoint(string id)
        {
            return _approximateCoordinate.Points.FirstOrDefault(p => p.Id == id) ?? _knownData.PointDatas.FirstOrDefault(p => p.Id == id);
        }
    }
}