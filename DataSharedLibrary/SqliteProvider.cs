using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DataSharedLibrary
{
    public class SqliteProvider
    {
        private SQLiteConnection _connection;

        /// <summary>
        /// Khởi tạo SqliteProvider với đường dẫn đến file cơ sở dữ liệu.
        /// </summary>
        /// <param name="databaseFilePath">Đường dẫn đến file SQLite (.db)</param>
        public SqliteProvider(string databaseFilePath)
        {
            // Khởi tạo chuỗi kết nối
            string connectionString = $"Data Source={databaseFilePath};Version=3;";
            _connection = new SQLiteConnection(connectionString);
            _connection.Open();
        }


        /// <summary>
        /// Thực thi câu lệnh SELECT và trả về danh sách các đối tượng T.
        /// </summary>
        /// <typeparam name="T">Kiểu đối tượng muốn trả về</typeparam>
        /// <param name="query">Câu lệnh SELECT cần thực thi</param>
        /// <returns>Danh sách các đối tượng kiểu T</returns>
        public List<T> ExecuteQuery<T>(string query)
        {
            try
            {
                var jsonStr = ExecuteQueryToJson(query);

                // Deserialize JSON thành danh sách các đối tượng kiểu T
                var result = JsonSerializer.Deserialize<List<T>>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Tùy chọn: không phân biệt chữ hoa chữ thường trong tên thuộc tính
                });

                return result ?? new List<T>(); // Trả về danh sách rỗng nếu kết quả là null
            }
            catch (JsonException jsonEx)
            {
                // Xử lý lỗi JSON
                Console.WriteLine($"Lỗi chuyển đổi JSON: {jsonEx.Message}");
                return new List<T>(); // Trả về danh sách rỗng nếu xảy ra lỗi
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                Console.WriteLine($"Lỗi khi thực thi truy vấn hoặc chuyển đổi: {ex.Message}");
                return new List<T>(); // Trả về danh sách rỗng nếu xảy ra lỗi
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SELECT và trả về kết quả dưới dạng JSON.
        /// </summary>
        /// <param name="query">Câu lệnh SELECT cần thực thi</param>
        /// <returns>Chuỗi JSON chứa dữ liệu kết quả của truy vấn</returns>
        public string ExecuteQueryToJson(string query)
        {
            using (var command = new SQLiteCommand(query, _connection))
            {
                using (var adapter = new SQLiteDataAdapter(command))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Chuyển đổi DataTable thành danh sách các Dictionary để dễ dàng serialize
                    var rows = ConvertDataTableToDictionaryList(dataTable);

                    // Chuyển đổi danh sách Dictionary thành chuỗi JSON
                    var jsonResult = JsonSerializer.Serialize(rows, new JsonSerializerOptions
                    {
                        WriteIndented = true // Để dễ đọc (Indented JSON)
                    });

                    return jsonResult;
                }
            }
        }


        /// <summary>
        /// Chuyển đổi DataTable thành danh sách các Dictionary&lt;string, object&gt;.
        /// </summary>
        /// <param name="dataTable">DataTable chứa dữ liệu cần chuyển đổi</param>
        /// <returns>Danh sách các Dictionary đại diện cho từng hàng trong DataTable</returns>
        private List<Dictionary<string, object>> ConvertDataTableToDictionaryList(DataTable dataTable)
        {
            return dataTable.AsEnumerable().Select(row =>
            {
                var dictionary = new Dictionary<string, object>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    dictionary[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                }
                return dictionary;
            }).ToList();
        }


        /// <summary>
        /// Thực thi câu lệnh SELECT và trả về DataTable.
        /// </summary>
        /// <param name="query">Câu lệnh SELECT cần thực thi</param>
        /// <returns>DataTable chứa kết quả truy vấn</returns>
        public DataTable ExecuteQuery(string query)
        {
            using (var command = new SQLiteCommand(query, _connection))
            {
                using (var adapter = new SQLiteDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh không trả về kết quả như INSERT, UPDATE, DELETE.
        /// </summary>
        /// <param name="query">Câu lệnh cần thực thi</param>
        /// <returns>Số dòng bị ảnh hưởng bởi lệnh</returns>
        public int ExecuteNonQuery(string query)
        {
            using (var command = new SQLiteCommand(query, _connection))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Thực thi lệnh và trả về giá trị đơn lẻ, chẳng hạn SELECT COUNT(*).
        /// </summary>
        /// <param name="query">Câu lệnh cần thực thi</param>
        /// <returns>Giá trị đơn lẻ (object)</returns>
        public object ExecuteScalar(string query)
        {
            using (var command = new SQLiteCommand(query, _connection))
            {
                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Đóng kết nối và giải phóng tài nguyên.
        /// </summary>
        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
