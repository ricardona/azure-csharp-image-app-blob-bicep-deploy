using Azure.Data.Tables;
using NETPhotoGallery.Models;

namespace NETPhotoGallery.Services
{
    public interface IImageLikeService
    {
        Task<int> GetLikesAsync(string imageId);
        Task<Dictionary<string, int>> GetAllLikesAsync();
        Task AddLikeAsync(string imageId);
    }

    public class ImageLikeService : IImageLikeService
    {
        private readonly TableClient _tableClient;
        private const string TableName = "imagelikes";

        public ImageLikeService(IConfiguration configuration)
        {
        //Incorrect configuration key used:
        // ----------------
        //  var connectionString = configuration.GetValue<string>("StorageConnectionStringo");
        // ----------------
        // Exception has occurred: CLR/System.ArgumentNullException
        // An exception of type 'System.ArgumentNullException' occurred in Azure.Data.Tables.dll but was not handled in user code: 'Value cannot be null.'
        //    at Azure.Core.Argument.AssertNotNull[T](T value, String name)
        //    at Azure.Data.Tables.TableServiceClient..ctor(String connectionString, TableClientOptions options)
        //    at Azure.Data.Tables.TableServiceClient..ctor(String connectionString)
        //    at NETPhotoGallery.Services.ImageLikeService..ctor(IConfiguration configuration) in c:\02_Repos\azure-csharp-image-app-blob-bicep-deploy\src
        // et-photo-gallery\Services\ImageLikeService.cs:line 21
            var connectionString = configuration.GetValue<string>("StorageConnectionString");
            var tableServiceClient = new TableServiceClient(connectionString);
            tableServiceClient.CreateTableIfNotExists(TableName);
            _tableClient = tableServiceClient.GetTableClient(TableName);
        }

        public async Task<int> GetLikesAsync(string imageId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ImageLike>("images", imageId);
                return response.Value.LikeCount;
            }
            catch (Azure.RequestFailedException)
            {
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetAllLikesAsync()
        {
            var results = new Dictionary<string, int>();

            //wrong method filter called :
            // ----------------
            //var queryResults = _tableClient.QueryAsync<ImageLike>(filter: $"PartitionKey eq images");
            // ----------------
            //"The requested operation is not implemented on the specified resource.
            // RequestId:a609fcf5-5002-0057-2f14-91f0f1000000
            // Time:2025-03-09T16:58:43.2554984Z\r
            // Status: 501 (Not Implemented)\r
            // ErrorCode: NotImplemented\r
            // Content:\r
            // {\"odata.error\":{\"code\":\"NotImplemented\",\"message\":{\"lang\":\"en-US\",\"value\":\"The requested operation is not implemented on the specified resource.\
            // RequestId:a609fcf5-5002-0057-2f14-91f0f1000000\
            // Time:2025-03-09T16:58:43.2554984Z\"}}}\r
            // "parsedStack":[{"level":0,"method":"Azure.Data.Tables.TableRestClient+<QueryEntitiesAsync>d__23.MoveNext","assembly":"Azure.Data.Tables, Version=12.8.3.0, Culture=neutral, PublicKeyToken=92742159e12e44c8","line":0},{"level":1,"method":"System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw","assembly":"System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e","line":0},{"level":2,"method":"System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess","assembly":"System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e","line":0},{"level":3,"method":"System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification","assembly":"System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e","line":0},{"level":4,"method":"System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1+ConfiguredTaskAwaiter.GetResult","assembly":"System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e","line":0},{"level":5,"method":"Azure.Data.Tables.TableClient+<>c__DisplayClass55_0`1+<<QueryAsync>b__0>d.MoveNext","assembly":"Azure.Data.Tables, Version=12.8.3.0, Culture=neutral, PublicKeyToken=92742159e12e44c8","line":0},{"level":6,"method":"System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw","assembly":"System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e","line":0}
            var queryResults = _tableClient.QueryAsync<ImageLike>(filter: $"PartitionKey eq 'images'");
            
            await foreach (var like in queryResults)
            {
                results[like.RowKey] = like.LikeCount;
            }
            
            return results;
        }

        public async Task AddLikeAsync(string imageId)
        {
            var like = new ImageLike
            {
                PartitionKey = "images",
                RowKey = imageId,
                LikeCount = 1
            };

            try
            {
            //like count with wrong value (ImageLikeService.cs - Line 62 - added string to sum):
            // ----------------
            // var existingLike = await _tableClient.GetEntityAsync<ImageLike>("images", imageId+"Ca");
            // ----------------
            //Exception thrown: 'Azure.RequestFailedException' in System.Private.CoreLib.dll
                var existingLike = await _tableClient.GetEntityAsync<ImageLike>("images", imageId);
                like.LikeCount = existingLike.Value.LikeCount + 1 ;
            }
            catch (Azure.RequestFailedException)
            {
                // Entity doesn't exist, use default count of 1
            }

            await _tableClient.UpsertEntityAsync(like);
        }
    }
}