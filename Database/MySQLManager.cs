using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

public class MySQLManager
{
    // 单例实例
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
    private static MySQLManager _instance;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
    private static readonly object _lock = new object();

    // 数据库连接列表
    private List<MySqlConnection> connections;

    // 已添加的连接字符串集合
    private HashSet<string> connectionStringsSet;

    // 私有构造函数，防止外部实例化
    private MySQLManager()
    {
        connections = new List<MySqlConnection>();
        connectionStringsSet = new HashSet<string>();
    }

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static MySQLManager Instance
    {
        get
        {
            // 双重检查锁定，确保线程安全
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MySQLManager();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 初始化数据库连接。
    /// 可以多次调用此方法，新的连接将被添加到已有的连接列表中。
    /// </summary>
    /// <param name="connectionStrings">MySQL 连接字符串数组。</param>
    public void Init(string[] connectionStrings)
    {
        foreach (var connStr in connectionStrings)
        {
            // 检查连接字符串是否已经存在，防止重复连接
            if (connectionStringsSet.Contains(connStr))
            {
                Console.WriteLine("连接字符串已存在，跳过：" + connStr);
                continue;
            }

            try
            {
                var connection = new MySqlConnection(connStr);
                connection.Open();
                connections.Add(connection);
                connectionStringsSet.Add(connStr);
                Console.WriteLine("成功连接到数据库：" + connection.Database);
            }
            catch (Exception ex)
            {
                Console.WriteLine("连接失败：" + ex.Message);
            }
        }
    }

    /// <summary>
    /// 执行参数化的 SQL 查询，并返回数据读取器。
    /// </summary>
    /// <param name="commandText">SQL 查询文本。</param>
    /// <param name="parameters">SQL 参数列表。</param>
    /// <param name="connectionIndex">要在其上执行查询的数据库连接的索引。</param>
    /// <returns>查询结果的 MySqlDataReader。</returns>
    public MySqlDataReader? SqlExec(string commandText, List<MySqlParameter> parameters, int connectionIndex)
    {
        if (connectionIndex < 0 || connectionIndex >= connections.Count)
        {
            throw new IndexOutOfRangeException("无效的连接索引。");
        }

        var connection = connections[connectionIndex];

        try
        {
            var command = new MySqlCommand(commandText, connection);

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }

            // 当关闭 reader 时，连接也会关闭
            var reader = command.ExecuteReader();

            Console.WriteLine($"在数据库 {connection.Database} 上成功执行查询。");

            return reader;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"在数据库 {connection.Database} 上执行查询失败：" + ex);
            return null;
        }
    }

    /// <summary>
    /// 获取指定索引的数据库名称。
    /// </summary>
    /// <param name="index">连接的索引。</param>
    /// <returns>数据库名称。</returns>
    public string GetDatabaseName(int index)
    {
        if (index >= 0 && index < connections.Count)
        {
            return connections[index].Database;
        }
        else
        {
            return "未知数据库";
        }
    }

    /// <summary>
    /// 关闭所有数据库连接。
    /// </summary>
    public void CloseConnections()
    {
        foreach (var connection in connections)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
                Console.WriteLine("已关闭数据库连接：" + connection.Database);
            }
        }
        // 清空连接列表和连接字符串集合
        connections.Clear();
        connectionStringsSet.Clear();
    }
}
