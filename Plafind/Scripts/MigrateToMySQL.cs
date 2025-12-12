using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plafind.Data;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace Plafind.Scripts
{
    /// <summary>
    /// SQL Server'dan MySQL'e veri taşıma scripti
    /// Kullanım: dotnet run --project Plafind -- migrate-to-mysql
    /// </summary>
    public class MigrateToMySQL
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MigrateToMySQL> _logger;

        public MigrateToMySQL(IConfiguration configuration, ILogger<MigrateToMySQL> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task MigrateAsync()
        {
            var sqlServerConnection = _configuration.GetConnectionString("SqlServerConnection");
            var mySqlConnection = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(sqlServerConnection) || string.IsNullOrEmpty(mySqlConnection))
            {
                _logger.LogError("Connection string'ler eksik!");
                return;
            }

            _logger.LogInformation("Veri taşıma işlemi başlatılıyor...");

            try
            {
                // Önce MySQL'de tabloların oluşturulduğundan emin ol
                await EnsureMySQLSchemaAsync(mySqlConnection);

                // Verileri taşı
                await MigrateDataAsync(sqlServerConnection, mySqlConnection);

                _logger.LogInformation("Veri taşıma işlemi tamamlandı!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veri taşıma sırasında hata oluştu!");
                throw;
            }
        }

        private async Task EnsureMySQLSchemaAsync(string mySqlConnection)
        {
            _logger.LogInformation("MySQL şeması kontrol ediliyor...");

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var serverVersion = new Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlServerVersion(new Version(8, 0, 21));
                options.UseMySql(mySqlConnection, serverVersion);
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Migration'ları uygula
            await context.Database.MigrateAsync();
            _logger.LogInformation("MySQL şeması hazır!");
        }

        private async Task MigrateDataAsync(string sqlServerConnection, string mySqlConnection)
        {
            using var sqlServerConn = new SqlConnection(sqlServerConnection);
            using var mySqlConn = new MySqlConnection(mySqlConnection);

            await sqlServerConn.OpenAsync();
            await mySqlConn.OpenAsync();

            _logger.LogInformation("Bağlantılar açıldı.");

            // Tabloları sırayla taşı (foreign key sırasına göre)
            var tables = new[]
            {
                "AspNetRoles",
                "AspNetUsers",
                "Categories",
                "Businesses",
                "AspNetUserRoles",
                "AspNetUserClaims",
                "AspNetUserLogins",
                "AspNetUserTokens",
                "AspNetRoleClaims",
                "UserProfiles",
                "UserPhotos",
                "BusinessImages",
                "Reviews",
                "ReviewReplies",
                "ReviewLikes",
                "UserFavorites",
                "News",
                "Reservations",
                "Branches",
                "Employees",
                "Events",
                "EventAttendees",
                "Campaigns",
                "CampaignUsages",
                "Payments",
                "Subscriptions",
                "Notifications",
                "NotificationPreferences",
                "Messages",
                "Conversations",
                "CustomerInteractions",
                "ContactMessages",
                "AdminLogs"
            };

            foreach (var tableName in tables)
            {
                try
                {
                    await MigrateTableAsync(sqlServerConn, mySqlConn, tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Table} tablosu taşınırken hata oluştu, devam ediliyor...", tableName);
                }
            }
        }

        private async Task MigrateTableAsync(SqlConnection sqlServerConn, MySqlConnection mySqlConn, string tableName)
        {
            _logger.LogInformation("{Table} tablosu taşınıyor...", tableName);

            // SQL Server'dan verileri oku
            var selectQuery = $"SELECT * FROM [{tableName}]";
            using var sqlCmd = new SqlCommand(selectQuery, sqlServerConn);
            using var reader = await sqlCmd.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                _logger.LogInformation("{Table} tablosu boş, atlanıyor.", tableName);
                return;
            }

            // MySQL'de tablo yapısını kontrol et
            var columns = new List<string>();
            var columnTypes = new Dictionary<string, string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var columnType = reader.GetFieldType(i);
                columns.Add(columnName);
                columnTypes[columnName] = GetMySqlType(columnType);
            }

            // MySQL'de tabloyu temizle (isteğe bağlı - dikkatli kullanın!)
            using var truncateCmd = new MySqlCommand($"SET FOREIGN_KEY_CHECKS = 0; TRUNCATE TABLE `{tableName}`; SET FOREIGN_KEY_CHECKS = 1;", mySqlConn);
            try
            {
                await truncateCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Tablo yoksa veya truncate edilemezse devam et
            }

            // Verileri MySQL'e yaz
            var batchSize = 100;
            var batch = new List<object[]>();
            var rowCount = 0;

            while (await reader.ReadAsync())
            {
                var row = new object[reader.FieldCount];
                reader.GetValues(row);
                batch.Add(row);

                if (batch.Count >= batchSize)
                {
                    await InsertBatchAsync(mySqlConn, tableName, columns, batch);
                    rowCount += batch.Count;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await InsertBatchAsync(mySqlConn, tableName, columns, batch);
                rowCount += batch.Count;
            }

            _logger.LogInformation("{Table} tablosundan {Count} satır taşındı.", tableName, rowCount);
        }

        private async Task InsertBatchAsync(MySqlConnection connection, string tableName, List<string> columns, List<object[]> rows)
        {
            if (rows.Count == 0) return;

            var columnNames = string.Join(", ", columns.Select(c => $"`{c}`"));
            var valuesPlaceholders = string.Join(", ", Enumerable.Range(0, columns.Count).Select(_ => "?"));

            var insertQuery = $"INSERT INTO `{tableName}` ({columnNames}) VALUES ({valuesPlaceholders})";

            using var cmd = new MySqlCommand(insertQuery, connection);
            cmd.Parameters.Clear();

            foreach (var row in rows)
            {
                cmd.Parameters.Clear();
                for (int i = 0; i < row.Length; i++)
                {
                    var param = new MySqlParameter($"@p{i}", row[i] ?? DBNull.Value);
                    cmd.Parameters.Add(param);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private string GetMySqlType(Type type)
        {
            if (type == typeof(string)) return "TEXT";
            if (type == typeof(int) || type == typeof(int?)) return "INT";
            if (type == typeof(long) || type == typeof(long?)) return "BIGINT";
            if (type == typeof(decimal) || type == typeof(decimal?)) return "DECIMAL(18,2)";
            if (type == typeof(DateTime) || type == typeof(DateTime?)) return "DATETIME";
            if (type == typeof(bool) || type == typeof(bool?)) return "BOOLEAN";
            if (type == typeof(double) || type == typeof(double?)) return "DOUBLE";
            if (type == typeof(float) || type == typeof(float?)) return "FLOAT";
            if (type == typeof(Guid) || type == typeof(Guid?)) return "CHAR(36)";
            if (type == typeof(byte[])) return "BLOB";
            return "TEXT";
        }
    }
}
