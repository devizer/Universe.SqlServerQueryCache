using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlSysInfoReader
{
    public class Info
    {
        public string Name;
        public object Value;
        public string Title;
    }

    public static ICollection<Info> Query(DbProviderFactory dbProvider, string connectionString)
    {
        var con = dbProvider.CreateConnection();
        con.ConnectionString = connectionString;
        object osSysInfoRaw = con.Query<object>("Select * from sys.dm_os_sys_info", null).FirstOrDefault();
        IDictionary<string, object> osSysInfoRawDictionary = (IDictionary<string, object>)osSysInfoRaw;
        List<Info> ret = new List<Info>();
        var currentCultureTextInfo = CultureInfo.CurrentCulture.TextInfo;
        foreach (var pair in osSysInfoRawDictionary)
        {
            var name = pair.Key;
            var title = currentCultureTextInfo.ToTitleCase(name.Replace("_", " "));
            ret.Add(new Info() { Name = name, Value = pair.Value, Title = title});
        }
        // var props2 = TypeDescriptor.GetProperties(osSysInfoRaw);


        return ret;
    }

}

public static class SqlSysInfoExtensions 
{
    public static string Format(this IEnumerable<SqlSysInfoReader.Info> infoList)
    {
        return Format(infoList, "\t");
    }

    public static string Format(this IEnumerable<SqlSysInfoReader.Info> infoList, string padding)
    {
        StringBuilder ret = new StringBuilder();
        foreach (var info in infoList)
        {
            var val = info.Value is long l ? l.ToString("n0") : Convert.ToString(info.Value);
            ret.AppendLine($"{padding}{info.Title}: {val}");
        }

        return ret.ToString();
    }

}