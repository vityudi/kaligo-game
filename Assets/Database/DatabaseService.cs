using System;
using System.Collections.Generic;
using Dapper;
using Npgsql;
using UnityEngine;

namespace Kaligo.Database {
    public class DatabaseService : IDisposable {
        NpgsqlConnection _conn;

        public const string LocalConnectionString =
            "Host=localhost;Port=5432;Database=kaligo_dev;Username=kaligo;Password=localdev";

        public void Initialize(string connectionString = null) {
            _conn = new NpgsqlConnection(connectionString ?? LocalConnectionString);
            _conn.Open();
            Debug.Log("[Database] Connected to PostgreSQL.");
        }

        public NpgsqlConnection Connection => _conn;

        public IEnumerable<T> Query<T>(string sql, object param = null) =>
            _conn.Query<T>(sql, param);

        public T QueryFirst<T>(string sql, object param = null) =>
            _conn.QueryFirstOrDefault<T>(sql, param);

        public int Execute(string sql, object param = null) =>
            _conn.Execute(sql, param);

        public NpgsqlTransaction BeginTransaction() =>
            _conn.BeginTransaction();

        public void Dispose() => _conn?.Dispose();
    }
}
