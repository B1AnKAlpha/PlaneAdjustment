using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D; 
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Adjustment;

namespace Adjustment
{

    public partial class main : Form
    {
        #region 成员变量 (Member Variables)

        private KnownData _knownData;
        private List<ObservedData> _observedDataList;
        private Coordinate _approximateCoords;
        private IndirectAdj _adjustmentResults;
        private readonly DataAccessHelper _dataAccess = new DataAccessHelper();

        private double zoom = 1.0;
        private Point _canvasDragStartPosition;
        private bool isMove = false;
        private bool isEllip = false;
        private bool allowDraw = false;

        #endregion

        public main()
        {
            InitializeComponent();
            this.pictureBox1.MouseWheel += new MouseEventHandler(this.pictureBox1_MouseWheel);
        }

        #region 文件IO与数据显示


        private void ProcessObservationBlocks(string currentLine, StreamReader reader)
        {
            if (string.IsNullOrWhiteSpace(currentLine)) return;
            var observedData = new ObservedData(currentLine);
            _observedDataList.Add(observedData);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length != 3)
                {
                    ProcessObservationBlocks(line, reader);
                    return;
                }
                observedData.OriginalDatas.Add(new OriginalData(parts[0], parts[1], Convert.ToDouble(parts[2])));
            }
        }


        private void PrepareDataForCalculationAndDisplay()
        {
            if (_surveyData == null)
            {
                MessageBox.Show("内部错误：勘测数据对象为空，无法准备数据。", "错误");
                return;
            }

            // --- 步骤一：准备并填充旧的数据结构 ---

            // 1.1 初始化并填充 _knownData
            // 假设你的旧 knownData 类定义仍然存在
            _knownData = new KnownData(
                _surveyData.Errors.DirectionStdDev,
                _surveyData.Errors.DistanceFixedError,
                _surveyData.Errors.DistanceRatioError
            );
            // 清空可能存在的旧数据，然后填充
            _knownData.PointDatas.Clear();
            _surveyData.KnownPoints.ForEach(p =>
                _knownData.PointDatas.Add(new PointData(p.Id, p.X, p.Y))
            );

            // 1.2 初始化并填充 _observedDataList
            _observedDataList = new List<ObservedData>();
            foreach (var station in _surveyData.AllStations)
            {
                var obsData = new ObservedData(station.StationId);
                foreach (var record in station.Records)
                {
                    // 假设你的旧 originalData 类定义仍然存在
                    obsData.OriginalDatas.Add(new OriginalData(record.TargetId, record.Type, record.Value));
                }
                _observedDataList.Add(obsData);
            }


            // --- 步骤二：使用已填充好的旧数据结构来更新界面 ---
            // 这样做可以确保界面显示的数据与即将用于计算的数据完全一致。

            // 2.1 填充 dataGridView1: 已知点数据
            var knownPointsTable = new DataTable();
            knownPointsTable.Columns.Add("已知点点号");
            knownPointsTable.Columns.Add("X坐标 (m)");
            knownPointsTable.Columns.Add("Y坐标 (m)");

            // 从刚刚填充好的 _knownData 中读取数据
            _knownData.PointDatas.ForEach(p =>
                knownPointsTable.Rows.Add(p.Id, p.X.ToString("F3"), p.Y.ToString("F3"))
            );
            dataGridView1.DataSource = knownPointsTable;

            // 2.2 填充 dataGridView2: 观测数据
            var observationsTable = new DataTable();
            observationsTable.Columns.Add("测站号");
            observationsTable.Columns.Add("照准点号");
            observationsTable.Columns.Add("观测类型");
            observationsTable.Columns.Add("观测值");

            // 从刚刚填充好的 _observedDataList 中读取数据
            _observedDataList.ForEach(obsData =>
                obsData.OriginalDatas.ForEach(p =>
                    observationsTable.Rows.Add(obsData.TestSiteNum, p.Id, p.Type, p.Value)
                )
            );
            dataGridView2.DataSource = observationsTable;

            // 调整列宽
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dataGridView2.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        #endregion

        #region 计算与报告

        private void showText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("             --- 近似坐标值 ---");
            sb.AppendLine($"{"点名",-14} {"X坐标",-18} {"Y坐标",-18}");
            sb.AppendLine("--------------------------------------------------");
            _knownData.PointDatas.ForEach(p => sb.AppendLine($"{p.Id,-16} {p.X,-18:F3} {p.Y,-18:F3}"));
            _approximateCoords.Points.ForEach(p => sb.AppendLine($"{p.Id,-16} {p.X,-18:F3} {p.Y,-18:F3}"));
            richTextBox1.AppendText(sb.ToString());
        }

        private void DisplayFullAdjustmentReport()
        {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("\n========== 平面网间接平差计算报告 ==========");
            AppendStatisticsSummary(reportBuilder);
            AppendAccuracyAssessment(reportBuilder);
            AppendMatrixDetails(reportBuilder);
            reportBuilder.AppendLine("\n========== 报告结束 ==========");
            richTextBox1.AppendText(reportBuilder.ToString());
        }

        private void AppendStatisticsSummary(StringBuilder sb)
        {
            sb.AppendLine("\n            一、概况统计");
            sb.AppendLine("------------------------------------");
            int unknownPointsCount = _approximateCoords.Points.Count;
            int knownPointsCount = _knownData.PointDatas.Count;
            int angleObsCount = _adjustmentResults.AngleObservations.Count;
            int edgeObsCount = _adjustmentResults.EdgeObservations.Count;
            int redundancy = (angleObsCount + edgeObsCount) - 2 * unknownPointsCount;

            sb.AppendLine($"  总计点数      : {knownPointsCount + unknownPointsCount}");
            sb.AppendLine($"  已知点数      : {knownPointsCount}");
            sb.AppendLine($"  待求点数      : {unknownPointsCount}");
            sb.AppendLine($"  方向观测数    : {angleObsCount}");
            sb.AppendLine($"  边长观测数    : {edgeObsCount}");
            sb.AppendLine($"  多余观测数 (r): {redundancy}");
            sb.AppendLine($"  验后单位权中误差 (σ₀): {_adjustmentResults.sigma0:F4}");
        }

        private void AppendAccuracyAssessment(StringBuilder sb)
        {
            if (_adjustmentResults.Q == null)
            {
                sb.AppendLine("\n警告: 协因数矩阵(Q)未计算，无法生成精度评定报告。");
                return;
            }

            sb.AppendLine("\n             二、平差坐标与精度评定");
            sb.AppendLine("-------------------------------------------------------------------------------------------------------------------");
            sb.AppendLine($"{"点号",-8} {"X平差坐标(m)",-18} {"Y平差坐标(m)",-18} {"Mx(mm)",-10} {"My(mm)",-10} {"Mp(mm)",-10} {"长半轴A(mm)",-15} {"短半轴B(mm)",-15} {"方向角(° ' \")",-18}");
            sb.AppendLine("-------------------------------------------------------------------------------------------------------------------");

            double maxPointError = 0;
            string weakestPointId = "N/A";

            for (int i = 0; i < _approximateCoords.Points.Count; i++)
            {
                var approxPoint = _approximateCoords.Points[i];
                double adjustedX = approxPoint.X + _adjustmentResults.X[2 * i, 0] / 1000.0;
                double adjustedY = approxPoint.Y + _adjustmentResults.X[2 * i + 1, 0] / 1000.0;
                double Qxx = _adjustmentResults.Q[2 * i, 2 * i];
                double Qyy = _adjustmentResults.Q[2 * i + 1, 2 * i + 1];
                double Qxy = _adjustmentResults.Q[2 * i, 2 * i + 1];
                double mx = _adjustmentResults.sigma0 * Math.Sqrt(Qxx);
                double my = _adjustmentResults.sigma0 * Math.Sqrt(Qyy);
                double mp = Math.Sqrt(mx * mx + my * my);
                double K = Math.Sqrt(Math.Pow(Qxx - Qyy, 2) + 4 * Qxy * Qxy);
                double ellipseA = _adjustmentResults.sigma0 * Math.Sqrt((Qxx + Qyy + K) / 2.0);
                double ellipseB = _adjustmentResults.sigma0 * Math.Sqrt((Qxx + Qyy - K) / 2.0);
                string angleDMS = ConvertRadToDMS(0.5 * Math.Atan2(2 * Qxy, Qxx - Qyy));
                sb.AppendFormat("{0,-8} {1,-18:F4} {2,-18:F4} {3,-10:F2} {4,-10:F2} {5,-10:F2} {6,-15:F2} {7,-15:F2} {8,-18}\n",
                                approxPoint.Id, adjustedX, adjustedY, mx, my, mp, ellipseA, ellipseB, angleDMS);
                if (mp > maxPointError)
                {
                    maxPointError = mp;
                    weakestPointId = approxPoint.Id;
                }
            }
            sb.AppendLine("-------------------------------------------------------------------------------------------------------------------");
            sb.AppendLine($"\n  结论: 最弱点为 [ {weakestPointId} ]，其点位中误差 Mp = {maxPointError:F2} mm");
        }

        private void AppendMatrixDetails(StringBuilder sb)
        {
            sb.AppendLine("\n             三、计算矩阵详情");
            sb.Append(FormatMatrixToString("        B - 设计矩阵", _adjustmentResults.B, "F2"));
            sb.Append(FormatMatrixToString("        P - 权矩阵", _adjustmentResults.P, "F2"));
            sb.Append(FormatMatrixToString("        l - 常数向量 (″/mm)", _adjustmentResults.l, "F4"));
            sb.Append(FormatMatrixToString("        N - 法方程矩阵", _adjustmentResults.N, "F4"));
            if (_adjustmentResults.Q != null)
                sb.Append(FormatMatrixToString("        Q - 协因数矩阵 (N的逆)", _adjustmentResults.Q, "F6"));
            sb.Append(FormatMatrixToString("        V - 残差向量 (″/mm)", _adjustmentResults.V, "F4"));
            sb.Append(FormatMatrixToString("        X - 改正数向量 (mm)", _adjustmentResults.X, "F4"));
        }

        #endregion

        #region 图形绘制与交互

        private void DrawPicture(double size,int time=10)
        {
            if (!allowDraw || _knownData == null || _approximateCoords == null)
            {
                return;
            }

            int canvasSize = (int)size;
            var bitmap = new Bitmap(canvasSize + 100, canvasSize + 100);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                g.TranslateTransform(50, 50);

                var allPoints = _knownData.PointDatas.Concat(_approximateCoords.Points);
                double xMax = allPoints.Max(p => p.X);
                double yMax = allPoints.Max(p => p.Y);
                double xMin = allPoints.Min(p => p.X);
                double yMin = allPoints.Min(p => p.Y);

                if (xMax - xMin < 1e-6 || yMax - yMin < 1e-6) return;

                double scale = Math.Min(canvasSize / (xMax - xMin), canvasSize / (yMax - yMin));

                // 绘制线
                using (var pen = new Pen(Color.Black))
                {
                    // 假设 _adjustmentResults.AngleObservations 或 EdgeObservations 可以代表连线
                    var allObservations = _adjustmentResults.AngleObservations.Select(a => new { P1 = a.ObservingPoint, P2 = a.SightingPoint })
                        .Concat(_adjustmentResults.EdgeObservations.Select(e => new { P1 = e.ObservingPoint, P2 = e.SightingPoint }))
                        .Distinct();

                    foreach (var obs in allObservations)
                    {
                        var p1 = allPoints.FirstOrDefault(p => p.Id == obs.P1);
                        var p2 = allPoints.FirstOrDefault(p => p.Id == obs.P2);
                        if (p1 != null && p2 != null)
                        {
                            g.DrawLine(pen,
                                (float)((p1.X - xMin) * scale), (float)(canvasSize - (p1.Y - yMin) * scale),
                                (float)((p2.X - xMin) * scale), (float)(canvasSize - (p2.Y - yMin) * scale));
                        }
                    }
                }

                // 绘制点和点号
                using (var redPen = new Pen(Color.Red))
                using (var redBrush = new SolidBrush(Color.Red))
                using (var blueBrush = new SolidBrush(Color.Blue))
                using (var font = new Font("Arial", 9))
                {
                    foreach (var p in _knownData.PointDatas)
                    {
                        float sx = (float)((p.X - xMin) * scale);
                        float sy = (float)(canvasSize - (p.Y - yMin) * scale);
                        g.DrawRectangle(redPen, sx - 3, sy - 3, 6, 6);
                        g.DrawString(p.Id, font, redBrush, sx + 5, sy - 5);
                    }
                    foreach (var p in _approximateCoords.Points)
                    {
                        float sx = (float)((p.X - xMin) * scale);
                        float sy = (float)(canvasSize - (p.Y - yMin) * scale);
                        g.FillEllipse(blueBrush, sx - 3, sy - 3, 6, 6);
                        g.DrawString(p.Id, font, blueBrush, sx + 5, sy - 5);
                    }
                }

                if (isEllip && _adjustmentResults?.Q != null)
                {
                    for (int i = 0; i < _approximateCoords.Points.Count; i++)
                    {
                        var p = _approximateCoords.Points[i];
                        float sx = (float)((p.X - xMin) * scale);
                        float sy = (float)(canvasSize - (p.Y - yMin) * scale);

                        double Qxx = _adjustmentResults.Q[2 * i, 2 * i];
                        double Qyy = _adjustmentResults.Q[2 * i + 1, 2 * i + 1];
                        double Qxy = _adjustmentResults.Q[2 * i, 2 * i + 1];

                        double K = Math.Sqrt(Math.Pow(Qxx - Qyy, 2) + 4 * Qxy * Qxy);
                        double ellipseA = _adjustmentResults.sigma0 * Math.Sqrt((Qxx + Qyy + K) / 2.0);
                        double ellipseB = _adjustmentResults.sigma0 * Math.Sqrt((Qxx + Qyy - K) / 2.0);
                        double angleRad = 0.5 * Math.Atan2(2 * Qxy, Qxx - Qyy);
                        float angleDeg = (float)(angleRad * 180.0 / Math.PI);

                        float ellipseWidth = (float)(ellipseA * scale * zoom*time);
                        float ellipseHeight = (float)(ellipseB * scale * zoom*time);

                        if (ellipseWidth < 1 || ellipseHeight < 1) continue;

                        using (var greenPen = new Pen(Color.Green, 1))
                        {
                            var state = g.Save();
                            g.TranslateTransform(sx, sy);
                            g.RotateTransform(-angleDeg); 
                            g.DrawEllipse(greenPen, -ellipseWidth / 2, -ellipseHeight / 2, ellipseWidth, ellipseHeight);
                            g.Restore(state);
                        }
                    }
                }
            }
            pictureBox1.Image = bitmap;
        }

        #endregion

        #region UI 事件处理

        private SurveyData _surveyData;

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "IN2 文件 (*.in2)|*.in2|所有文件 (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var reader = new In2FileReader();
                        _surveyData = reader.Parse(openFileDialog.FileName); // 解析文件

                        MessageBox.Show("数据文件读取并解析成功！");

                        // 在这里调用新的数据显示方法
                        PrepareDataForCalculationAndDisplay();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"文件处理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }




        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (_knownData == null || _observedDataList == null)
            {
                MessageBox.Show("请先加载数据文件！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            richTextBox1.Clear();
            try
            {
                _approximateCoords = new Coordinate(_surveyData);
                showText();

                _adjustmentResults = new IndirectAdj(_approximateCoords, _observedDataList, _knownData);
                _adjustmentResults.PerformAdjustment();
                DisplayFullAdjustmentReport();
                allowDraw = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算过程中发生错误: {ex.Message}", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            // 1. 创建数据库查看窗体的一个新实例
            DatabaseViewerForm viewerForm = new DatabaseViewerForm();

            // 2. 使用 Show() 方法来显示这个新窗体
            // Show() 会以非模态方式打开，不阻塞主窗体
            viewerForm.Show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            _knownData = null;
            _observedDataList = null;
            _approximateCoords = null;
            _adjustmentResults = null;
            dataGridView1.DataSource = null;
            dataGridView2.DataSource = null;
            richTextBox1.Clear();
            pictureBox1.Image = null;
            allowDraw = false;
            isEllip = false;
            if (checkBox1 != null) checkBox1.Checked = false;
        }



        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (tabControl1 != null && tabPage3 != null) tabControl1.SelectedTab = tabPage3;
            zoom = 1.0;
            DrawPicture(500);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMove = true;
                _canvasDragStartPosition = e.Location;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMove)
            {
                int deltaX = e.X - _canvasDragStartPosition.X;
                int deltaY = e.Y - _canvasDragStartPosition.Y;
                pictureBox1.Left += deltaX;
                pictureBox1.Top += deltaY;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isMove = false;
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            double scaleFactor = e.Delta > 0 ? 1.2 : 1 / 1.2;
            zoom *= scaleFactor;
            DrawPicture(500 * zoom);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            isEllip = checkBox1.Checked;
            if (allowDraw) DrawPicture(500 * zoom);
        }

        private void main_Load(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e) { }
        private void tabPage1_Click(object sender, EventArgs e) { }
        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }

        #endregion

        #region 辅助方法

        private string FormatMatrixToString(string title, double[,] matrix, string format, int columnWidth = 16)
        {
            if (matrix == null) return $"\n  {title}:\n  (矩阵数据为空)\n";
            var sb = new StringBuilder();
            sb.AppendLine($"\n  {title}:");
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                sb.Append("  ");
                for (int j = 0; j < matrix.GetLength(1); j++)
                    sb.Append(string.Format($"{{0,{columnWidth}:{format}}}", matrix[i, j]));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string ConvertRadToDMS(double rad)
        {
            double totalDegrees = rad * 180.0 / Math.PI;
            if (totalDegrees < 0) totalDegrees += 180;
            int degrees = (int)totalDegrees;
            double remainingMinutes = (totalDegrees - degrees) * 60;
            int minutes = (int)remainingMinutes;
            double seconds = (remainingMinutes - minutes) * 60;
            return $"{degrees}° {minutes:00}' {seconds:00.0}\"";
        }

        #endregion

        private void toolStripButton6_Click_1(object sender, EventArgs e)
        {
            // 步骤 1: 检查数据有效性
            if (_adjustmentResults == null || _adjustmentResults.Q == null)
            {
                MessageBox.Show("没有可供保存的平差结果，或结果不完整。\n请先执行平差计算。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 步骤 2: 创建并填充摘要信息对象
                var summary = new AdjustmentSummary
                {
                    // 你可以从一个文件名或一个文本框获取项目名，这里使用一个默认值
                    ProjectName = "导线网平差项目 " + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    KnownPointsCount = _knownData.PointDatas.Count,
                    UnknownPointsCount = _approximateCoords.Points.Count,
                    AngleObservationsCount = _adjustmentResults.AngleObservations.Count,
                    EdgeObservationsCount = _adjustmentResults.EdgeObservations.Count,
                    Redundancy = _adjustmentResults.AngleObservations.Count + _adjustmentResults.EdgeObservations.Count - 2 * _approximateCoords.Points.Count,
                    Sigma0 = _adjustmentResults.sigma0
                };

                // 步骤 3: 创建详情列表，并循环填充每个点的数据
                var pointDetails = new List<AdjustedPointDetail>();
                double maxError = -1;
                string weakestPointId = "N/A";

                for (int i = 0; i < _approximateCoords.Points.Count; i++)
                {
                    var approxPoint = _approximateCoords.Points[i];

                    // 从协因数矩阵 Q 中提取数据
                    double Qxx = _adjustmentResults.Q[2 * i, 2 * i];
                    double Qyy = _adjustmentResults.Q[2 * i + 1, 2 * i + 1];
                    double Qxy = _adjustmentResults.Q[2 * i, 2 * i + 1];

                    // 计算各项精度指标
                    double mx = _adjustmentResults.sigma0 * Math.Sqrt(Qxx);
                    double my = _adjustmentResults.sigma0 * Math.Sqrt(Qyy);
                    double mp = Math.Sqrt(mx * mx + my * my);

                    double K = Math.Sqrt(Math.Pow(Qxx - Qyy, 2) + 4 * Qxy * Qxy);
                    double ellipseA = _adjustmentResults.sigma0 * Math.Sqrt((Qxx + Qyy + K) / 2.0);
                    double ellipseB = _adjustmentResults.sigma0 * Math.Sqrt((Qxx + Qyy - K) / 2.0);
                    string ellipseAngle = ConvertRadToDMS(0.5 * Math.Atan2(2 * Qxy, Qxx - Qyy));

                    // 创建并填充单个点的详情对象
                    var detail = new AdjustedPointDetail
                    {
                        PointId = approxPoint.Id,
                        // 注意：改正数X的单位是米，所以不需要除以1000
                        AdjustedX = approxPoint.X + _adjustmentResults.X[2 * i, 0],
                        AdjustedY = approxPoint.Y + _adjustmentResults.X[2 * i + 1, 0],
                        Mx = mx * 1000, // 将中误差从中转换为毫米
                        My = my * 1000,
                        Mp = mp * 1000,
                        EllipseA = ellipseA * 1000,
                        EllipseB = ellipseB * 1000,
                        EllipseAngle = ellipseAngle
                    };

                    pointDetails.Add(detail);

                    // 同时查找最弱点
                    if (detail.Mp > maxError)
                    {
                        maxError = detail.Mp;
                        weakestPointId = detail.PointId;
                    }
                }

                // 将最弱点信息更新到摘要对象中
                summary.WeakestPointId = weakestPointId;
                summary.MaxPointError = maxError;

                // 步骤 4: 调用数据访问层的方法来保存数据
                _dataAccess.SaveAdjustmentResult(summary, pointDetails);

                // 步骤 5: 给用户成功的反馈
                MessageBox.Show("平差结果已成功保存到数据库！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // 统一的错误处理
                MessageBox.Show($"保存到数据库时发生错误: {ex.Message}", "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            zoom *= 1.3;
            DrawPicture(500 * zoom);
        }

        private void label1_Click(object sender, EventArgs e)
        {
            zoom *= 1.3;
            DrawPicture(500 * zoom);
        }


        private void label3_Click(object sender, EventArgs e)
        {
            zoom *= 0.7;
            DrawPicture(500 * zoom);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            zoom *= 0.7;
            DrawPicture(500 * zoom);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("当前没有可供保存的示意图。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. 创建并配置 SaveFileDialog
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Title = "保存示意图";
                // 设置文件类型过滤器，提供多种常用图片格式选项
                saveDialog.Filter = "PNG 图像 (*.png)|*.png|JPEG 图像 (*.jpg)|*.jpg|BMP 位图 (*.bmp)|*.bmp|所有文件 (*.*)|*.*";
                // 设置默认的文件名
                saveDialog.FileName = "平差示意图_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // 3. 显示对话框，并检查用户是否点击了“保存”
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 4. 根据用户选择的文件扩展名，确定保存格式
                        string extension = Path.GetExtension(saveDialog.FileName).ToLower();
                        System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png; // 默认PNG

                        switch (extension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                format = System.Drawing.Imaging.ImageFormat.Jpeg;
                                break;
                            case ".bmp":
                                format = System.Drawing.Imaging.ImageFormat.Bmp;
                                break;
                        }

                        // 5. 保存图像到文件
                        pictureBox1.Image.Save(saveDialog.FileName, format);

                        MessageBox.Show("示意图已成功保存！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存图像时发生错误: {ex.Message}", "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                MessageBox.Show("报告内容为空，无法保存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. 创建并配置 SaveFileDialog
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Title = "保存平差报告";
                // 设置文件类型过滤器，限制为文本文档
                saveDialog.Filter = "文本文档 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                // 设置默认的文件名
                saveDialog.FileName = "平差报告_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

                // 3. 显示对话框，并检查用户是否点击了“保存”
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 4. 使用 File.WriteAllText 方法，简单高效地将文本写入文件
                        // 它会自动处理文件的创建、写入和关闭
                        File.WriteAllText(saveDialog.FileName, richTextBox1.Text);

                        MessageBox.Show("报告已成功保存！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存文件时发生错误: {ex.Message}", "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            isEllip = checkBox2.Checked;
            if (allowDraw) DrawPicture(500 * zoom,1);
        }
    }
}