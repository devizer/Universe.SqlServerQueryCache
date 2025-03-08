using System.Data.SqlClient;
using Dapper;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Tests;

public static class SqlServerReferenceExtensions
{
    public static bool HasSystemObject(this SqlServerRef server, string objectName)
    {
        var cs = GetConnectionString(server);
        var mediumVersion = GetMediumVersion(cs);
        var con = SqlClientFactory.Instance.CreateConnection(cs);
        // 
        var sql = "If Exists (Select 1 From sys.all_objects Where [name] = @name and is_ms_shipped = 1) Select Cast(1 as bit) Else Select Cast(0 as bit);";
        var ret = con.Query<bool>(sql, new { name = objectName }).FirstOrDefault();
        return ret;
    }

    public static string GetSafeFileOnlyName(this SqlServerRef server)
    {
        var cs = GetConnectionString(server);
        var mediumVersion = GetMediumVersion(cs);
        var platform = SqlClientFactory.Instance.CreateConnection(cs).Manage().HostPlatform;
        return SafeFileName.Get($"{server.DataSource}: v{mediumVersion} on {platform}");
    }

    public static string GetMediumVersion(string cs)
    {
        var mediumVersion = SqlClientFactory.Instance.CreateConnection(cs).Manage().MediumServerVersion;
        return mediumVersion;
    }

    public static string GetConnectionString(this SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        return cs;
    }
}