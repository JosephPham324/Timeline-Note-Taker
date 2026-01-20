using HtmlAgilityPack;

namespace Timeline_Note_Taker.Services;

public class UrlMetadataService : IUrlMetadataService
{
    private readonly HttpClient _httpClient;

    public UrlMetadataService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<string?> FetchPageTitleAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Try to get the title from the <title> tag
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
            {
                return titleNode.InnerText.Trim();
            }

            // Fallback to og:title meta tag
            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            if (ogTitle != null)
            {
                return ogTitle.GetAttributeValue("content", null)?.Trim();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
