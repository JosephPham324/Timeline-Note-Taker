using SQLite;
using Timeline_Note_Taker.Models;

namespace Timeline_Note_Taker.Models;

public class Note
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Content { get; set; } = string.Empty;

    // Storing enums as integers is safer for SQLite
    public int NoteTypeId { get; set; } = (int)NoteType.Text;

    // Computed property for easy enum access
    [Ignore]
    public NoteType Type
    {
        get => (NoteType)NoteTypeId;
        set => NoteTypeId = (int)value;
    }

    // Instead of storing images in the DB (bloat), store the path
    // MAUI has a 'FileSystem.AppDataDirectory' that works on all platforms
    public string? AttachmentPath { get; set; }

    public string? Topic { get; set; }

    // Store tags as comma-separated string for SQLite compatibility
    public string? Tags { get; set; }

    [Ignore]
    public List<string> TagList
    {
        get => string.IsNullOrEmpty(Tags) 
            ? new List<string>() 
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        set => Tags = string.Join(",", value);
    }
}
