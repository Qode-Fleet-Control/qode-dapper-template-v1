using App.Data;

// A console job: make sure the table exists, write a couple of rows, read them back.
// Exits 0 on success; any exception exits non-zero.
var (conn, dialect) = await Db.OpenAsync();
await using (conn)
{
    Console.WriteLine($"database: {dialect} ({conn.GetType().Name}, server {conn.ServerVersion})");
    var todos = new TodoRepository(conn, dialect);
    await todos.EnsureSchemaAsync();

    var first = await todos.AddAsync($"write the report ({DateTime.UtcNow:HH:mm:ss})");
    await todos.AddAsync("ship the template");
    await todos.CompleteAsync(first);

    foreach (var t in await todos.ListAsync(5))
    {
        Console.WriteLine($"  #{t.Id} [{(t.Done ? "x" : " ")}] {t.Title}  ({t.CreatedAt:u})");
    }
    Console.WriteLine($"open todos: {await todos.CountOpenAsync()}");
}
Console.WriteLine("done");
