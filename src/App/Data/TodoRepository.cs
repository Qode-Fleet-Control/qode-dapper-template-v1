using System.Data.Common;
using Dapper;

namespace App.Data;

/// <summary>Plain SQL through Dapper. Only the DDL differs between the two databases.</summary>
public sealed class TodoRepository(DbConnection db, Dialect dialect)
{
    public Task EnsureSchemaAsync() => db.ExecuteAsync(dialect switch
    {
        Dialect.Postgres => """
            CREATE TABLE IF NOT EXISTS todos (
                id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                title      TEXT        NOT NULL,
                done       BOOLEAN     NOT NULL DEFAULT FALSE,
                created_at TIMESTAMPTZ NOT NULL
            )
            """,
        _ => """
            CREATE TABLE IF NOT EXISTS todos (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                title      TEXT    NOT NULL,
                done       INTEGER NOT NULL DEFAULT 0,
                created_at TEXT    NOT NULL
            )
            """,
    });

    public Task<long> AddAsync(string title) => db.ExecuteScalarAsync<long>(
        "INSERT INTO todos (title, done, created_at) VALUES (@title, @done, @createdAt) RETURNING id",
        new { title, done = false, createdAt = DateTime.UtcNow });

    public async Task<bool> CompleteAsync(long id) =>
        await db.ExecuteAsync("UPDATE todos SET done = @done WHERE id = @id", new { id, done = true }) == 1;

    public async Task<IReadOnlyList<Todo>> ListAsync(int limit = 10) => (await db.QueryAsync<Todo>(
        """
        SELECT id AS Id, title AS Title, done AS Done, created_at AS CreatedAt
        FROM todos ORDER BY id DESC LIMIT @limit
        """, new { limit })).AsList();

    public Task<long> CountOpenAsync() =>
        db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM todos WHERE done = @done", new { done = false });
}
