using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualBasic; // 确保已添加对 Microsoft.VisualBasic.dll 的引用

namespace Adjustment
{
    public partial class DatabaseViewerForm : Form
    {
        #region 成员变量
        private readonly DataAccessHelper _dataAccess = new DataAccessHelper();
        private readonly string _connectionString;
        private const string DbFileName = "AdjustmentResults.db";
        #endregion

        public DatabaseViewerForm()
        {
            InitializeComponent();
            _connectionString = $"Data Source={DbFileName};Version=3;";
        }

        #region 窗体加载与数据加载逻辑

        /// <summary>
        /// 窗体加载事件处理器 (确保在设计器中连接到这个方法)。
        /// </summary>
        private void DatabaseViewerForm_Load_1(object sender, EventArgs e)
        {
            if (!File.Exists(DbFileName))
            {
                MessageBox.Show($"数据库文件 '{DbFileName}' 未找到。\n请先运行平差并保存结果。", "数据库未找到", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }
            LoadSummaryData();
        }

        /// <summary>
        /// 加载摘要数据到 dgvSummary。
        /// </summary>
        // 在 DatabaseViewerForm.cs 文件中

        /// <summary>
        /// 加载平差摘要信息到 dgvSummary，支持按项目名进行模糊搜索。
        /// </summary>
        /// <param name="projectNameFilter">用于项目名模糊搜索的关键词，如果为空则显示所有。</param>
        private void LoadSummaryData(string projectNameFilter = null)
        {
            try
            {
                string filterClause = "ORDER BY CalculationTime DESC"; // 默认排序

                // 如果提供了搜索词，则构建 WHERE 子句
                if (!string.IsNullOrWhiteSpace(projectNameFilter))
                {
                    // 使用 LIKE 和 % 实现模糊搜索
                    // 注意：参数化查询是防止SQL注入的最佳方式，但对于 LIKE，拼接有时更直接
                    // 为了安全，我们对输入进行简单的清理
                    string sanitizedFilter = projectNameFilter.Replace("'", "''"); // 防止单引号注入
                    filterClause = $"WHERE ProjectName LIKE '%{sanitizedFilter}%' ORDER BY CalculationTime DESC";
                }

                DataTable summaryData = LoadTableData("AdjustmentSummary", filterClause);
                dgvSummary.DataSource = summaryData;

                // 只有在没有搜索的情况下才设置表头，避免重复设置
                if (string.IsNullOrWhiteSpace(projectNameFilter))
                {
                    SetSummaryGridColumnHeaders();
                }

                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                if (summaryData.Rows.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(projectNameFilter))
                    {
                        MessageBox.Show("没有找到匹配的记录。", "查找结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // 这个消息只在首次加载时显示
                        // MessageBox.Show("数据库中的摘要表为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载摘要数据时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 根据摘要 ID 加载详情数据到 dgvDetails。
        /// </summary>
        private void LoadDetailsData(long summaryId)
        {
            try
            {
                string whereClause = $"WHERE SummaryId = {summaryId}";
                DataTable detailsData = LoadTableData("AdjustedPointDetails", whereClause);
                dgvDetails.DataSource = detailsData;
                SetDetailsGridColumnHeaders();
                dgvDetails.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载详情数据时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 通用的数据加载方法。
        /// </summary>
        private DataTable LoadTableData(string tableName, string filterClause = "")
        {
            var dataTable = new DataTable();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string query = $"SELECT * FROM {tableName} {filterClause}";
                using (var adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }

        #endregion

        #region 设置中文表头

        /// <summary>
        /// 设置摘要表 (dgvSummary) 的列标题为中文。
        /// </summary>
        private void SetSummaryGridColumnHeaders()
        {
            // 使用一个辅助方法来安全地设置表头
            SetColumnHeader(dgvSummary, "Id", "ID");
            SetColumnHeader(dgvSummary, "CalculationTime", "计算时间");
            SetColumnHeader(dgvSummary, "ProjectName", "项目名");
            SetColumnHeader(dgvSummary, "KnownPointsCount", "已知点");
            SetColumnHeader(dgvSummary, "UnknownPointsCount", "待求点");
            SetColumnHeader(dgvSummary, "AngleObservationsCount", "角度数");
            SetColumnHeader(dgvSummary, "EdgeObservationsCount", "边数");
            SetColumnHeader(dgvSummary, "Redundancy", "多余观测");
            SetColumnHeader(dgvSummary, "Sigma0", "σ₀");
            SetColumnHeader(dgvSummary, "WeakestPointId", "最弱点");
            SetColumnHeader(dgvSummary, "MaxPointError", "最大误差(mm)");
        }

        /// <summary>
        /// 设置详情表 (dgvDetails) 的列标题为中文。
        /// </summary>
        private void SetDetailsGridColumnHeaders()
        {
            SetColumnHeader(dgvDetails, "Id", "ID");
            SetColumnHeader(dgvDetails, "SummaryId", "摘要ID");
            SetColumnHeader(dgvDetails, "PointId", "点号");
            SetColumnHeader(dgvDetails, "AdjustedX", "X坐标(m)");
            SetColumnHeader(dgvDetails, "AdjustedY", "Y坐标(m)");
            SetColumnHeader(dgvDetails, "Mx", "Mx(mm)");
            SetColumnHeader(dgvDetails, "My", "My(mm)");
            SetColumnHeader(dgvDetails, "Mp", "点位误差(mm)");
            SetColumnHeader(dgvDetails, "EllipseA", "长半轴(mm)");
            SetColumnHeader(dgvDetails, "EllipseB", "短半轴(mm)");
            SetColumnHeader(dgvDetails, "EllipseAngle", "方向角");
        }

        /// <summary>
        /// 一个安全的辅助方法，用于设置列标题。
        /// </summary>
        private void SetColumnHeader(DataGridView dgv, string columnName, string headerText)
        {
            if (dgv.Columns[columnName] != null)
            {
                dgv.Columns[columnName].HeaderText = headerText;
            }
        }

        #endregion

        #region UI 事件处理

        /// <summary>
        /// 摘要表选中行改变事件处理器。
        /// </summary>
        private void dgvSummary_SelectionChanged_1(object sender, EventArgs e)
        {
            if (dgvSummary.CurrentRow == null || dgvSummary.CurrentRow.Cells["Id"].Value is DBNull)
            {
                if (dgvDetails.DataSource is DataTable dt) dt.Clear();
                return;
            }
            long selectedSummaryId = Convert.ToInt64(dgvSummary.CurrentRow.Cells["Id"].Value);
            LoadDetailsData(selectedSummaryId);
        }

        /// <summary>
        /// "修改项目名" 按钮点击事件。
        /// </summary>
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvSummary.CurrentRow == null) { MessageBox.Show("请先选择一条要修改的记录。"); return; }
            long selectedId = Convert.ToInt64(dgvSummary.CurrentRow.Cells["Id"].Value);
            string currentProjectName = dgvSummary.CurrentRow.Cells["ProjectName"].Value.ToString();
            string newProjectName = Interaction.InputBox("请输入新的项目名称:", "修改项目名", currentProjectName);

            if (string.IsNullOrWhiteSpace(newProjectName)) return;

            try
            {
                _dataAccess.UpdateSummaryProjectName(selectedId, newProjectName);
                MessageBox.Show("项目名修改成功！");
                LoadSummaryData();
            }
            catch (Exception ex) { MessageBox.Show($"修改失败: {ex.Message}"); }
        }

        /// <summary>
        /// "删除记录" 按钮点击事件。
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (dgvSummary.CurrentRow == null) { MessageBox.Show("请先选择一条要删除的记录。"); return; }
            long selectedId = Convert.ToInt64(dgvSummary.CurrentRow.Cells["Id"].Value);
            var confirmResult = MessageBox.Show($"您确定要删除 ID 为 {selectedId} 的记录吗？\n这将同时删除其所有关联的点位详情！", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    _dataAccess.DeleteSummaryResult(selectedId);
                    MessageBox.Show("记录删除成功！");
                    LoadSummaryData();
                }
                catch (Exception ex) { MessageBox.Show($"删除失败: {ex.Message}"); }
            }
        }

        #endregion

        private void search_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearchProject.Text.Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("请输入要查找的项目名关键词。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 调用一个带搜索条件的数据加载方法
            LoadSummaryData(searchTerm);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearchProject.Text.Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("请输入要查找的项目名关键词。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 调用一个带搜索条件的数据加载方法
            LoadSummaryData(searchTerm);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txtSearchProject.Clear();
            // 调用不带搜索条件的数据加载方法
            LoadSummaryData();
        }

        private void dgvDetails_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDetails.CurrentRow != null && dgvDetails.CurrentRow.DataBoundItem != null)
            {
                var selectedRow = dgvDetails.CurrentRow;

                // 从选中的行安全地获取数据并填充 TextBox
                textBox1.Text = selectedRow.Cells["AdjustedX"].Value.ToString();
                textBox2.Text = selectedRow.Cells["AdjustedY"].Value.ToString();
                textBox3.Text = selectedRow.Cells["Mx"].Value.ToString();
                textBox4.Text = selectedRow.Cells["My"].Value.ToString();
                textBox5.Text = selectedRow.Cells["Mp"].Value.ToString();
                textBox6.Text = selectedRow.Cells["EllipseAngle"].Value.ToString();
            }
            else
            {
            }
        }
        // 在 DataAccessHelper.cs 类中

        /// <summary>
        /// 根据给定的详情对象，更新数据库中的一条点位详情记录。
        /// </summary>
        /// <param name="detailToUpdate">包含要更新的数据的详情对象，其 Id 必须有效。</param>

        private void button4_Click(object sender, EventArgs e)
        {
            if (dgvDetails.CurrentRow == null)
            {
                MessageBox.Show("请先在下方的详情表中选择一条要修改的记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. 从 TextBox 读取新值，并进行数据验证和转换
            try
            {
                // 创建一个 AdjustedPointDetail 对象来承载更新后的数据
                var detailToUpdate = new AdjustedPointDetail
                {
                    // 获取要更新记录的唯一 ID
                    Id = Convert.ToInt64(dgvDetails.CurrentRow.Cells["Id"].Value),

                    // 从文本框读取并转换数据
                    AdjustedX = Convert.ToDouble(textBox1.Text),
                    AdjustedY = Convert.ToDouble(textBox2.Text),
                    Mx = Convert.ToDouble(textBox3.Text),
                    My = Convert.ToDouble(textBox4.Text),
                    Mp = Convert.ToDouble(textBox5.Text),
                    EllipseAngle = textBox6.Text
                };

                // 3. 弹出确认对话框
                var confirmResult = MessageBox.Show("您确定要将修改保存到数据库吗？", "确认修改", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    // 4. 调用数据访问层的方法来更新数据库
                    _dataAccess.UpdatePointDetail(detailToUpdate);

                    MessageBox.Show("记录修改成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新详情表以显示更新后的数据
                    // 我们需要知道当前的 summaryId 是多少
                    long currentSummaryId = Convert.ToInt64(dgvSummary.CurrentRow.Cells["Id"].Value);
                    LoadDetailsData(currentSummaryId);
                }
            }
            catch (FormatException)
            {
                // 如果用户输入了非数字内容，会触发这个异常
                MessageBox.Show("输入的数据格式不正确，请确保所有坐标和误差值都是有效的数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}