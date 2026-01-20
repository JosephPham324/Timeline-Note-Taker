namespace Timeline_Note_Taker.Services;

public class NoteEventService
{
    public event Action? NoteSaved;

    public void TriggerNoteSaved()
    {
        NoteSaved?.Invoke();
    }
}
