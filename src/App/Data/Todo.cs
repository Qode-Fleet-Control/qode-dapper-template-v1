namespace App.Data;

/// <summary>A row of the todos table. Dapper maps columns to properties by name.</summary>
public sealed class Todo
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; }
}
