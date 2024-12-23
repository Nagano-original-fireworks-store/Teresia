﻿using YamlDotNet.Serialization;
using System;
using System.IO;
using System.Data.Common;

namespace SharedLibrary.Config
{
    public class Config
    {
        private readonly StreamWriter _yamlConfigPath = File.CreateText(ConfigBuilder.ConfigFilePath);
        private static readonly ProgramConfig ServerData = new ProgramConfig();

        public static class ConfigBuilder
        {
            public const string ConfigFilePath = "./config.yaml";

            public static ProgramConfig BuildConfig(string configFilePath)
            {
                try
                {
                    if (!File.Exists(configFilePath) || File.ReadAllText(configFilePath).Length == 0)
                    {
                        File.CreateText(configFilePath).Close();
                        Config config = new Config();
                        config.NewConfig();
                    }

                    Deserializer deserializer = new Deserializer();
                    var obj = deserializer.Deserialize<ProgramConfig>(File.ReadAllText(configFilePath));
                    return obj;
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to parse the config file", e);
                }
            }

            public static ProgramConfig BuildDefaultConfig() => BuildConfig(ConfigFilePath);
        }

        public class ProgramConfig
        {
            [YamlMember(Alias = "HttpServerAddress")]
            public string HttpServerAddress { get; set; } = "http://localhost:5000";

            [YamlMember(Alias = "DataFolder")]
            public string DataFolder { get; set; } = "./";

            [YamlMember(Alias = "MySql")]
            public Dictionary<Proto.Enum.MysqlIndex, MySqlConfig> MySqlConfig { get; set; } = new Dictionary<Proto.Enum.MysqlIndex, MySqlConfig>();


#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
            private static ProgramConfig _current;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
            public static ProgramConfig Current
            {
                get
                {
                    if (_current == null)
                    {
                        _current = ConfigBuilder.BuildDefaultConfig();
                    }
                    return _current;
                }
            }
        }
        public class MySqlConfig
        {
            [YamlMember(Alias = "Ip")]
            public string Ip { get; set; } = "";

            [YamlMember(Alias = "Port")]
            public uint Port { get; set; }

            [YamlMember(Alias = "User")]
            public string User { get; set; } = "";

            [YamlMember(Alias = "Database")]
            public string Database { get; set; } = "";

            [YamlMember(Alias = "Password")]
            public string Password { get; set; } = "";

            public string BuildConnectionString()
            {
                var player = new DbConnectionStringBuilder
                {
                    { "Server", Ip },
                    { "Port", Port },
                    { "Database", Database },
                    { "User Id", User },
                    { "Password", Password },
                };
                return player.ConnectionString.ToString();
            }
        }
        public void NewConfig()
        {
            using (StreamWriter writer = new StreamWriter(ConfigBuilder.ConfigFilePath))
            {
                ISerializer yamlSerializer = new Serializer();
                yamlSerializer.Serialize(writer, ServerData);
            }
        }
    }
}
