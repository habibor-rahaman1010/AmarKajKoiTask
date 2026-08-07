using System.Data;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface IDapperGenericRepository<TEntity> where TEntity : class, new()
    {
        public Task<IReadOnlyList<TEntity>> GetAllAsyncFromErp(string query, object param = null);
        public Task<IList<TEntity>> GetAllAsync(string query, object param = null);
        public Task<int> GetCountAsync(string tableName, string columnName, dynamic columnData);
        public Task<TResult> GetAllSingleAsync<TResult>(string query, object param = null);
        public Task<DataTable> GetDataInDataTableAsync(string query, object selector);
        public Task<DataSet> GetDataInDataSetAsync(string query, object selector);
        public Task<IList<T>> GetAllAsync<T>(string query, object param = null) where T : class, new();
        public Task<IList<T>> GetExistingIds<T>(string query, object? param = null);
        public Task<TResult> GetDataByIdAsync<TResult>(string query, object param = null);
        public Task<int> ExecuteScalarAsync(string sql, object param = null);
        public Task<T> ExecuteScalarAsync<T>(string sql, object param = null);
        public Task<int> ExecuteAsync(string query, IDbConnection connection, IDbTransaction transaction, object selector = null);
        public Task<int> ExecuteAsync(string query, object selector = null);
        public Task<bool> ItemIsExist(string query, object param = null);
        public Task<DataTable> ToDataTable<T>(List<T> items);
        public Task<TResult?> QuerySingleAsync<TResult>(string query, object? param = null, CommandType? commandType = CommandType.Text);
    }
}