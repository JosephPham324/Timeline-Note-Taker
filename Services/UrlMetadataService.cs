using HtmlAgilityPack;

namespace Timeline_Note_Taker.Services;

public class UrlMetadataService : IUrlMetadataService
{
    private readonly HttpClient _httpClient;

    public UrlMetadataService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            AutomaticDecompression = System.Net.DecompressionMethods.All, // Handle Gzip/Brotli
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true // Bypass SSL errors for sites like Nyaa
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        
        // Use a generic, common User-Agent that doesn't look suspicious
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        // Add standard browser headers
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?0");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"Windows\"");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
    }

    public async Task<string?> FetchPageTitleAsync(string url)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Fetching title for: {url}");
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Response: {response.StatusCode} for {url}");
            
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Failed status code: {response.StatusCode}");
                return null;
            }
            
            // Read as byte array and decode manually to handle charset correctly if needed
            // But ReadAsStringAsync usually handles charset from Content-Type header
            var html = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(html))
            {
                System.Diagnostics.Debug.WriteLine("[UrlMetadata] Empty HTML received");
                return null;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            
            string? title = null;

            // Method 1: Open Graph meta tag (often better/cleaner than title tag)
            var ogTitleNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            if (ogTitleNode != null)
            {
                title = HtmlEntity.DeEntitize(ogTitleNode.GetAttributeValue("content", "")).Trim();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Found OG title: {title}");
                    return title;
                }
            }

            // Method 2: Standard <title> tag
            var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
            {
                title = HtmlEntity.DeEntitize(titleNode.InnerText).Trim();
                System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Found <title>: {title}");
                return title;
            }
            
            // Method 3: Twitter card meta tag
            var twitterTitleNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='twitter:title']");
            if (twitterTitleNode != null)
            {
                title = HtmlEntity.DeEntitize(twitterTitleNode.GetAttributeValue("content", "")).Trim();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Found Twitter title: {title}");
                    return title;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[UrlMetadata] No title found in HTML for: {url}");
            // Optional: dump first 500 chars of HTML to debug
            // System.Diagnostics.Debug.WriteLine($"[UrlMetadata] HTML start: {html.Substring(0, Math.Min(html.Length, 500))}");
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UrlMetadata] Error fetching title: {ex.Message}");
            return null;
        }
    }
}
