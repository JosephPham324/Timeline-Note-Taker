namespace Timeline_Note_Taker.Services;

public interface ISettingsService
{
    string AttachmentsDirectory { get; set; }
    string Language { get; set; }
    string TopicSeparator { get; set; }
}
