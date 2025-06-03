using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace NETPhotoGallery.Services
{
    public interface ITableStorageService
    {
        TableClient GetTableClient(string tableName);
        Task InitializeTableAsync(string tableName);
    }

    public class TableStorageService : ITableStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly IConfiguration _configuration;

        public TableStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration.GetValue<string>("StorageAccount:ConnectionString");
            _tableServiceClient = new TableServiceClient(connectionString);
        }

        public TableClient GetTableClient(string tableName)
        {
            return _tableServiceClient.GetTableClient(tableName);
        }

        public async Task InitializeTableAsync(string tableName)
        {
            var tableClient = GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
        }
    }
}
