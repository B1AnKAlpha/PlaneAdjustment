using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Adjustment
{
    /// <summary>
    /// 封装所有与 SQLite 数据库交互的逻辑。
    /// </summary>
    public class DataAccessHelper
    {
        private readonly string _connectionString;
        private const string DbFileName = "AdjustmentResults.db";

        public DataAccessHelper()
        {
            _connectionString = $"Data Source={DbFileName};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (File.Exists(DbFileName)) return;
            SQLiteConnection.CreateFile(DbFileName);
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string createSummaryTable = @"CREATE TABLE AdjustmentSummary (Id INTEGER PRIMARY KEY AUTOINCREMENT, CalculationTime TEXT NOT NULL, ProjectName TEXT, KnownPointsCount INTEGER, UnknownPointsCount INTEGER, AngleObservationsCount INTEGER, EdgeObservationsCount INTEGER, Redundancy INTEGER, Sigma0 REAL, WeakestPointId TEXT, MaxPointError REAL);";
                string createDetailsTable = @"CREATE TABLE AdjustedPointDetails (Id INTEGER PRIMARY KEY AUTOINCREMENT, SummaryId INTEGER NOT NULL, PointId TEXT NOT NULL, AdjustedX REAL, AdjustedY REAL, Mx REAL, My REAL, Mp REAL, EllipseA REAL, EllipseB REAL, EllipseAngle TEXT, FOREIGN KEY (SummaryId) REFERENCES AdjustmentSummary(Id) ON DELETE CASCADE);";
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = createSummaryTable;
                    command.ExecuteNonQuery();
                    command.CommandText = createDetailsTable;
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveAdjustmentResult(AdjustmentSummary summary, List<AdjustedPointDetail> pointDetails)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    long summaryId = InsertSummary(summary, connection);
                    foreach (var detail in pointDetails)
                    {
                        detail.SummaryId = summaryId;
                        InsertPointDetail(detail, connection);
                    }
                    transaction.Commit();
                }
            }
        }

        private long InsertSummary(AdjustmentSummary summary, SQLiteConnection connection)
        {
            string sql = @"INSERT INTO AdjustmentSummary (CalculationTime, ProjectName, KnownPointsCount, UnknownPointsCount, AngleObservationsCount, EdgeObservationsCount, Redundancy, Sigma0, WeakestPointId, MaxPointError) VALUES (@Time, @Project, @Known, @Unknown, @Angle, @Edge, @Redun, @Sigma, @WeakId, @MaxErr);";
            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Time", summary.CalculationTime.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Project", summary.ProjectName);
                command.Parameters.AddWithValue("@Known", summary.KnownPointsCount);
                command.Parameters.AddWithValue("@Unknown", summary.UnknownPointsCount);
                command.Parameters.AddWithValue("@Angle", summary.AngleObservationsCount);
                command.Parameters.AddWithValue("@Edge", summary.EdgeObservationsCount);
                command.Parameters.AddWithValue("@Redun", summary.Redundancy);
                command.Parameters.AddWithValue("@Sigma", summary.Sigma0);
                command.Parameters.AddWithValue("@WeakId", summary.WeakestPointId);
                command.Parameters.AddWithValue("@MaxErr", summary.MaxPointError);
                command.ExecuteNonQuery();
            }
            return connection.LastInsertRowId;
        }

        private void InsertPointDetail(AdjustedPointDetail detail, SQLiteConnection connection)
        {
            string sql = @"INSERT INTO AdjustedPointDetails (SummaryId, PointId, AdjustedX, AdjustedY, Mx, My, Mp, EllipseA, EllipseB, EllipseAngle) VALUES (@SumId, @PId, @AdjX, @AdjY, @Mx, @My, @Mp, @EA, @EB, @EAngle);";
            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@SumId", detail.SummaryId);
                command.Parameters.AddWithValue("@PId", detail.PointId);
                command.Parameters.AddWithValue("@AdjX", detail.AdjustedX);
                command.Parameters.AddWithValue("@AdjY", detail.AdjustedY);
                command.Parameters.AddWithValue("@Mx", detail.Mx);
                command.Parameters.AddWithValue("@My", detail.My);
                command.Parameters.AddWithValue("@Mp", detail.Mp);
                command.Parameters.AddWithValue("@EA", detail.EllipseA);
                command.Parameters.AddWithValue("@EB", detail.EllipseB);
                command.Parameters.AddWithValue("@EAngle", detail.EllipseAngle);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateSummaryProjectName(long summaryId, string newProjectName)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE AdjustmentSummary SET ProjectName = @ProjectName WHERE Id = @Id";
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProjectName", newProjectName);
                    command.Parameters.AddWithValue("@Id", summaryId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteSummaryResult(long summaryId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM AdjustmentSummary WHERE Id = @Id";
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", summaryId);
                    command.ExecuteNonQuery();
                }
            }
        }
        // 在 public class DataAccessHelper { ... } 的内部

        /// <summary>
        /// 根据给定的详情对象，更新数据库中的一条点位详情记录。
        /// </summary>
        /// <param name="detailToUpdate">包含要更新的数据的详情对象，其 Id 必须有效。</param>
        public void UpdatePointDetail(AdjustedPointDetail detailToUpdate)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
        UPDATE AdjustedPointDetails 
        SET 
            AdjustedX = @AdjX,
            AdjustedY = @AdjY,
            Mx = @Mx,
            My = @My,
            Mp = @Mp,
            EllipseAngle = @EAngle
        WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    // 使用参数化查询，防止SQL注入
                    command.Parameters.AddWithValue("@Id", detailToUpdate.Id);
                    command.Parameters.AddWithValue("@AdjX", detailToUpdate.AdjustedX);
                    command.Parameters.AddWithValue("@AdjY", detailToUpdate.AdjustedY);
                    command.Parameters.AddWithValue("@Mx", detailToUpdate.Mx);
                    command.Parameters.AddWithValue("@My", detailToUpdate.My);
                    command.Parameters.AddWithValue("@Mp", detailToUpdate.Mp);
                    command.Parameters.AddWithValue("@EAngle", detailToUpdate.EllipseAngle);

                    command.ExecuteNonQuery();
                }
            }
        }
    }

    // --- 【【【 关键修复：在这里定义数据传输对象 (DTOs) 】】】 ---

    /// <summary>
    /// 用于传输平差摘要信息的数据对象。
    /// </summary>
    public class AdjustmentSummary
    {
        public DateTime CalculationTime { get; set; } = DateTime.Now;
        public string ProjectName { get; set; }
        public int KnownPointsCount { get; set; }
        public int UnknownPointsCount { get; set; }
        public int AngleObservationsCount { get; set; }
        public int EdgeObservationsCount { get; set; }
        public int Redundancy { get; set; }
        public double Sigma0 { get; set; }
        public string WeakestPointId { get; set; }
        public double MaxPointError { get; set; }
        public long Id { get; set; }
    }

    /// <summary>
    /// 用于传输单个平差点详细信息的数据对象。
    /// </summary>
    public class AdjustedPointDetail
    {

        public long Id { get; set; }
        public long SummaryId { get; set; } // Foreign key
        public string PointId { get; set; }
        public double AdjustedX { get; set; }
        public double AdjustedY { get; set; }
        public double Mx { get; set; }
        public double My { get; set; }
        public double Mp { get; set; }
        public double EllipseA { get; set; }
        public double EllipseB { get; set; }
        public string EllipseAngle { get; set; }
    }
}