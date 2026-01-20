namespace Timeline_Note_Taker.Services;

public interface IUrlMetadataService
{
    Task<string?> FetchPageTitleAsync(string url);
}
