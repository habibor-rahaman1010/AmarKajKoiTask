using AmarKajKoi.Database;
using AmarKajKoi.RepositoriesInterface;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace AmarKajKoi.RepositoriesImplement
{
    public class DapperGenericRepository<TEntity> : IDapperGenericRepository<TEntity> where TEntity : class, new()
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DapperGenericRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsyncFromErp(string query, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var result = await connection.QueryAsync<TEntity>(query, param);

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<IList<TEntity>> GetAllAsync(string query, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                var result = await connection.QueryAsync<TEntity>(query, param);

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<IList<T>> GetAllAsync<T>(string query, object param = null) where T : class, new()
        {
            try
            {
                if (typeof(T).IsInterface)
                {
                    throw new ArgumentException("T cannot be an interface. Use a concrete class.");
                }

                using var connection = _dbConnectionFactory.CreateConnection();

                var result = await connection.QueryAsync<T>(query, param, commandTimeout: 120);

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<TResult> GetAllSingleAsync<TResult>(string query, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                var result = await connection.QueryAsync<TResult>(query, param);

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<int> GetCountAsync(string tableName, string columnName, dynamic columnData)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                string query = "select Count(" + columnName + ") from " + tableName + " where " + columnName + " = @ColumnData ";
                var result = await connection.QueryAsync<int>(query, new { ColumnData = columnData });

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<int> GetCountAsync(string tableName, string columnName, string whereColumnName, dynamic columnData)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                string query = "select Count(" + columnName + ") from " + tableName + " where " + whereColumnName + " = @ColumnData ";
                var result = await connection.QueryAsync<int>(query, new { ColumnData = columnData });

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<DataTable> GetDataInDataTableAsync(string query, object selector = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                DataTable table = new DataTable();
                table.Load(await connection.ExecuteReaderAsync(query, selector));

                return table;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<DataSet> GetDataInDataSetAsync(string query, object selector)
        {
            try
            {
                DataSet dsList = new DataSet();
                using var connection = _dbConnectionFactory.CreateConnection();

                using (connection)
                {
                    IDataReader ds = await connection.ExecuteReaderAsync(query, selector);
                    dsList = ConvertDataReaderToDataSet(ds);
                }
                return dsList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        private DataSet ConvertDataReaderToDataSet(IDataReader dataReader)
        {
            try
            {
                DataSet ds = new DataSet();
                int i = 0;
                while (!dataReader.IsClosed)
                {
                    ds.Tables.Add("dt" + (i + 1));
                    ds.EnforceConstraints = false;

                    try
                    {
                        ds.Tables[i].Load(dataReader);
                        i++;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<int> ExecuteAsync(string query, IDbConnection connection, IDbTransaction transaction, object selector = null)
        {
            try
            {
                var affectedRows = await connection.ExecuteAsync(query, selector, transaction);

                return affectedRows;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<DataTable> GetDataInDataTableWithConnectionAsync(string query, SqlConnection connection, object selector = null)
        {
            try
            {
                DataTable table = new DataTable();
                table.Load(await connection.ExecuteReaderAsync(query, selector));

                return table;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }
        public async Task<DataSet> GetDataInDataSetWithConnectionAsync(string query, SqlConnection connection, object selector = null)
        {
            try
            {
                DataSet dsList = new DataSet();
                IDataReader ds = await connection.ExecuteReaderAsync(query, selector);
                dsList = ConvertDataReaderToDataSet(ds);

                return dsList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<int> ExecuteAsync(string query, object selector = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                using (connection)
                {
                    var affectedRows = await connection.ExecuteAsync(query, selector);

                    return affectedRows;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<int> ExecuteScalarAsync(string sql, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var count = await connection.ExecuteScalarAsync<int>(sql, param);
                return count;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<TResult> GetDataByIdAsync<TResult>(string query, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var data = await connection.QuerySingleOrDefaultAsync<TResult>(query, param);
                return data;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<bool> ItemIsExist(string query, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var isExist = await connection.ExecuteScalarAsync<bool>(query, param);
                return isExist;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public async Task<IList<T>> GetExistingIds<T>(string query, object? param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var existingIds = await connection.QueryAsync<T>(query, param);
                return existingIds.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }

        public Task<DataTable> ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in props)
            {
                dataTable.Columns.Add(
                    prop.Name,
                    Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType
                );
            }

            foreach (T item in items)
            {
                object[] values = new object[props.Length];

                for (int i = 0; i < props.Length; i++)
                {
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                }

                dataTable.Rows.Add(values);
            }

            return Task.FromResult(dataTable);
        }

        public async Task<TResult?> QuerySingleAsync<TResult>(string query, object? param = null, CommandType? commandType = CommandType.Text)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                return await connection.QueryFirstOrDefaultAsync<TResult>(
                    query,
                    param,
                    commandType: commandType);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured.", ex);
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object param = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                return await connection.ExecuteScalarAsync<T>(sql, param);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception Occured: ", ex);
            }
        }
    }
}
