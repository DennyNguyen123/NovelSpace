using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSharedLibrary
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // Thiết lập đường dẫn đến cơ sở dữ liệu ở đây
            var dbPath = "D:\\Truyen\\SQLite\\data.db"; // Đường dẫn bạn muốn

            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            return new AppDbContext(dbPath, optionsBuilder.Options);
        }
    }
}
