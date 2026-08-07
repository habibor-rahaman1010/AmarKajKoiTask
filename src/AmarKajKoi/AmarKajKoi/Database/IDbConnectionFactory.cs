using System.Data;

namespace AmarKajKoi.Database
{
    public interface IDbConnectionFactory
    {
        public IDbConnection CreateConnection();
    }
}