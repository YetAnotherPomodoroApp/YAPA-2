using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Specifics
{
    /// <summary>
    /// Completed pomodoros in Documents/YAPA2/Yapa.db, the same SQLite file and table the WPF app
    /// creates with Entity Framework, so a database copied from Windows opens here as it is.
    /// </summary>
    public class ItemRepository : IPomodoroRepository
    {
        private readonly string _connectionString;
        private readonly object _lock = new object();

        public ItemRepository()
        {
            var dbPath = Path.Combine(AvaloniaEnvironment.BaseDir, "Yapa.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
            EnsureSchema();
        }

        private void EnsureSchema()
        {
            using (var connection = Open())
            {
                Execute(connection, @"CREATE TABLE IF NOT EXISTS ""Pomodoros"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pomodoros"" PRIMARY KEY AUTOINCREMENT,
                    ""Count"" INTEGER NOT NULL,
                    ""DateTime"" TEXT NOT NULL,
                    ""DurationMin"" INTEGER NOT NULL DEFAULT 0,
                    ""ProfileName"" TEXT NULL)");

                // Databases from the first WPF versions lack the later columns
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"PRAGMA table_info(""Pomodoros"")";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString(1));
                        }
                    }
                }
                if (!columns.Contains("DurationMin"))
                {
                    Execute(connection, @"ALTER TABLE ""Pomodoros"" ADD ""DurationMin"" INTEGER NOT NULL DEFAULT 0");
                }
                if (!columns.Contains("ProfileName"))
                {
                    Execute(connection, @"ALTER TABLE ""Pomodoros"" ADD ""ProfileName"" TEXT NULL");
                }
            }
        }

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        // EF Core 1.1 stored DateTime as "yyyy-MM-dd HH:mm:ss.FFFFFFF"
        private static string Format(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

        private static DateTime Parse(string value)
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public void Delete(int id)
        {
            lock (_lock)
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"DELETE FROM ""Pomodoros"" WHERE ""Id"" = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }

        public void Add(PomodoroEntity pomo)
        {
            lock (_lock)
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO ""Pomodoros"" (""Count"", ""DateTime"", ""DurationMin"", ""ProfileName"") VALUES ($count, $date, $duration, $profile)";
                command.Parameters.AddWithValue("$count", pomo.Count);
                command.Parameters.AddWithValue("$date", Format(pomo.DateTime));
                command.Parameters.AddWithValue("$duration", pomo.DurationMin);
                command.Parameters.AddWithValue("$profile", (object)pomo.ProfileName ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        public int CompletedToday()
        {
            // Pomodoros are stored in UTC, so "today" is the UTC range of the local day
            var today = LocalDay.ToLocal(DateTime.UtcNow, TimeZoneInfo.Local).Date;
            var startDate = LocalDay.StartUtc(today, TimeZoneInfo.Local);
            var endDate = LocalDay.StartUtc(today.AddDays(1), TimeZoneInfo.Local);
            var total = 0;
            foreach (var pomodoro in After(startDate))
            {
                if (pomodoro.DateTime < endDate)
                {
                    total += pomodoro.Count;
                }
            }
            return total;
        }

        public IEnumerable<PomodoroEntity> After(DateTime date)
        {
            var result = new List<PomodoroEntity>();
            lock (_lock)
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT ""Id"", ""Count"", ""DateTime"", ""DurationMin"", ""ProfileName"" FROM ""Pomodoros""";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new PomodoroEntity
                        {
                            Id = reader.GetInt32(0),
                            Count = reader.GetInt32(1),
                            DateTime = Parse(reader.GetString(2)),
                            DurationMin = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            ProfileName = reader.IsDBNull(4) ? null : reader.GetString(4),
                        };
                        if (entity.DateTime >= date)
                        {
                            result.Add(entity);
                        }
                    }
                }
            }
            return result;
        }
    }
}
