using SQLite;
using Timeline_Note_Taker.Models;

namespace Timeline_Note_Taker.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "timelinenotes.db");
    }

    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<Note>();
    }

    // Create
    public async Task<int> SaveNoteAsync(Note note)
    {
        await InitAsync();
        if (note.Id == 0)
        {
            note.CreatedAt = DateTime.Now;
            return await _database!.InsertAsync(note);
        }
        else
        {
            return await _database!.UpdateAsync(note);
        }
    }

    // Read - All notes
    public async Task<List<Note>> GetAllNotesAsync()
    {
        await InitAsync();
        return await _database!.Table<Note>()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // Read - By date range
    public async Task<List<Note>> GetNotesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await InitAsync();
        return await _database!.Table<Note>()
            .Where(n => n.CreatedAt >= startDate && n.CreatedAt <= endDate)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // Read - Today's notes
    public async Task<List<Note>> GetTodaysNotesAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await GetNotesByDateRangeAsync(today, tomorrow);
    }

    // Read - This week's notes
    public async Task<List<Note>> GetThisWeeksNotesAsync()
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        return await GetNotesByDateRangeAsync(startOfWeek, endOfWeek);
    }

    // Read - By topic
    public async Task<List<Note>> GetNotesByTopicAsync(string topic)
    {
        await InitAsync();
        return await _database!.Table<Note>()
            .Where(n => n.Topic == topic)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // Read - By type
    public async Task<List<Note>> GetNotesByTypeAsync(NoteType type)
    {
        await InitAsync();
        return await _database!.Table<Note>()
            .Where(n => n.NoteTypeId == (int)type)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // Read - Search
    public async Task<List<Note>> SearchNotesAsync(string searchTerm)
    {
        await InitAsync();
        var lowerSearch = searchTerm.ToLower();
        var allNotes = await _database!.Table<Note>()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        
        return allNotes.Where(n => 
            (n.Content?.ToLower().Contains(lowerSearch) ?? false) ||
            (n.Topic?.ToLower().Contains(lowerSearch) ?? false) ||
            (n.Tags?.ToLower().Contains(lowerSearch) ?? false)
        ).ToList();
    }

    // Delete
    public async Task<int> DeleteNoteAsync(Note note)
    {
        await InitAsync();
        return await _database!.DeleteAsync(note);
    }

    // Get distinct topics
    public async Task<List<string>> GetAllTopicsAsync()
    {
        await InitAsync();
        var notes = await _database!.Table<Note>()
            .Where(n => n.Topic != null && n.Topic != "")
            .ToListAsync();
        return notes.Select(n => n.Topic!)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }
}
