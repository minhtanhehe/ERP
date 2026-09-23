using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using Npgsql;

namespace HR_Management.DAL
{
    public static class DatabaseHelper
    {
        // =========================================================================
        // CÔNG TẮC CHỌN DATABASE:
        // true  => Dùng Cloud Neon PostgreSQL (Đang ngắt kết nối với MINHTANSQL)
        // false => Dùng Microsoft SQL Server Local (.\MINHTANSQL)
        // =========================================================================
        public static bool UseCloud { get; set; } = true;

        // Chuỗi kết nối SQL Server máy local
        public static string SqlServerConnectionString { get; set; } = 
            @"Server=.\MINHTANSQL;Database=ERP_BanHang;Integrated Security=True;TrustServerCertificate=True;";

        // Chuỗi kết nối Neon Cloud PostgreSQL
        public static string CloudConnectionString { get; set; } = 
            "Host=ep-bitter-heart-b3yu3xlc-pooler.c-4.ap-southeast-1.aws.neon.tech;Port=5432;Database=erp_banhang;Username=neondb_owner;Password=npg_fVzi2bH5uYaj;SSL Mode=Require;Trust Server Certificate=true;";

        // Thuộc tính tương thích ngược
        public static string ConnectionString
        {
            get => UseCloud ? CloudConnectionString : SqlServerConnectionString;
            set
            {
                if (UseCloud) CloudConnectionString = value;
                else SqlServerConnectionString = value;
            }
        }

        public static DbConnection GetConnection()
        {
            if (UseCloud)
            {
                return new NpgsqlConnection(CloudConnectionString);
            }
            return new SqlConnection(SqlServerConnectionString);
        }

        public static DbCommand CreateCommand(string query, DbConnection conn, DbTransaction? trans = null)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = UseCloud ? PrepareQueryForPostgres(query) : query;
            if (trans != null)
            {
                cmd.Transaction = trans;
            }
            return cmd;
        }

        public static void AddParameter(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        public static DbParameter CreateParameter(string name, object? value)
        {
            if (UseCloud)
            {
                return new NpgsqlParameter(name, value ?? DBNull.Value);
            }
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static bool TestConnection(out string message)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                message = UseCloud 
                    ? "Kết nối Cloud PostgreSQL (Neon) thành công!" 
                    : "Kết nối Microsoft SQL Server (MINHTANSQL) thành công!";
                return true;
            }
            catch (Exception ex)
            {
                message = "Lỗi kết nối CSDL: " + ex.Message;
                return false;
            }
        }

        public static DateTime? ToNullableDateTime(object? value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            try { return Convert.ToDateTime(value); } catch { return null; }
        }

        public static DateTime ToDateTime(object? value)
        {
            var dt = ToNullableDateTime(value);
            return dt ?? DateTime.MinValue;
        }

        public static DataTable ExecuteQuery(string query, DbParameter[]? parameters = null)
        {
            using var conn = GetConnection();
            conn.Open();
            string finalQuery = UseCloud ? PrepareQueryForPostgres(query) : query;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = finalQuery;
            AttachParameters(cmd, parameters);

            using var reader = cmd.ExecuteReader();
            return LoadDataTableSafe(reader);
        }

        public static DataTable LoadDataTableSafe(DbDataReader reader)
        {
            var dt = new DataTable();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                Type colType = reader.GetFieldType(i);
                if (colType.Name == "DateOnly")
                    colType = typeof(DateTime);
                else if (colType.Name == "TimeOnly")
                    colType = typeof(TimeSpan);

                string uniqueName = colName;
                int suffix = 1;
                while (dt.Columns.Contains(uniqueName))
                {
                    uniqueName = $"{colName}_{suffix++}";
                }

                dt.Columns.Add(uniqueName, Nullable.GetUnderlyingType(colType) ?? colType);
            }

            while (reader.Read())
            {
                var row = dt.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i);
                    if (val == null || val == DBNull.Value)
                    {
                        row[i] = DBNull.Value;
                    }
                    else if (val.GetType().Name == "DateOnly")
                    {
                        row[i] = Convert.ToDateTime(val);
                    }
                    else if (val.GetType().Name == "TimeOnly")
                    {
                        row[i] = TimeSpan.Parse(val.ToString());
                    }
                    else
                    {
                        row[i] = val;
                    }
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters)
        {
            return ExecuteQuery(query, (DbParameter[]?)parameters);
        }

        public static int ExecuteNonQuery(string query, DbParameter[]? parameters = null)
        {
            using var conn = GetConnection();
            conn.Open();
            string finalQuery = UseCloud ? PrepareQueryForPostgres(query) : query;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = finalQuery;
            AttachParameters(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }

        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters)
        {
            return ExecuteNonQuery(query, (DbParameter[]?)parameters);
        }

        public static object? ExecuteScalar(string query, DbParameter[]? parameters = null)
        {
            using var conn = GetConnection();
            conn.Open();
            string finalQuery = UseCloud ? PrepareQueryForPostgres(query) : query;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = finalQuery;
            AttachParameters(cmd, parameters);
            return cmd.ExecuteScalar();
        }

        public static object? ExecuteScalar(string query, SqlParameter[]? parameters)
        {
            return ExecuteScalar(query, (DbParameter[]?)parameters);
        }

        private static void AttachParameters(DbCommand cmd, DbParameter[]? parameters)
        {
            if (parameters == null || parameters.Length == 0) return;

            foreach (var p in parameters)
            {
                var newParam = cmd.CreateParameter();
                newParam.ParameterName = p.ParameterName;
                newParam.Value = p.Value ?? DBNull.Value;
                cmd.Parameters.Add(newParam);
            }
        }

        private static string PrepareQueryForPostgres(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return query;

            // 1. Chuyển TOP X thành LIMIT X (nếu có)
            string res = Regex.Replace(query, @"(?i)\bSELECT\s+TOP\s+(\d+)\s+(.+)", "SELECT $2 LIMIT $1");

            // 2. Chuyển DATEADD(day, ...) thành DATEADD('day', ...)
            res = Regex.Replace(res, @"(?i)\bDATEADD\s*\(\s*([a-zA-Z]+)\s*,", "DATEADD('$1',");

            // 3. Chuyển [Tên Cột] thành "Tên Cột"
            res = res.Replace('[', '"').Replace(']', '"');

            // 4. Chuyển ISNULL thành COALESCE
            res = Regex.Replace(res, @"(?i)\bISNULL\b", "COALESCE");

            return res;
        }
    }
}
