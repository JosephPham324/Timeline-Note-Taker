using System.Text.RegularExpressions;

namespace Timeline_Note_Taker.Services;

public class SlashCommandParser
{
    private readonly ISettingsService _settingsService;

    // Matches #"quoted topic" or #unquoted followed by content
    private static readonly Regex QuotedHashtagRegex = new Regex(
        @"^#""([^""]+)""\s+(.+)$",
        RegexOptions.Compiled
    );
    
    private static readonly Regex UnquotedHashtagRegex = new Regex(
        @"^#(\w+)\s+(.+)$",
        RegexOptions.Compiled
    );

    public SlashCommandParser(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public (bool HasCommand, string? Topic, string? Content) Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (false, null, null);
        }

        var trimmedInput = input.Trim();

        // Try quoted hashtag first (supports spaces and semicolons)
        var quotedMatch = QuotedHashtagRegex.Match(trimmedInput);
        if (quotedMatch.Success)
        {
            var topicString = quotedMatch.Groups[1].Value;
            var content = quotedMatch.Groups[2].Value;
            
            // If there are separators, take the first topic for the Topic field
            // and store all topics in Tags (for future use)
            var separator = _settingsService.TopicSeparator;
            // Handle split safely
            var split = topicString.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            var firstTopic = split.Length > 0 ? split[0].Trim() : topicString;
            
            return (true, firstTopic, content);
        }

        // Try unquoted hashtag (single word only)
        var unquotedMatch = UnquotedHashtagRegex.Match(trimmedInput);
        if (unquotedMatch.Success)
        {
            return (true, unquotedMatch.Groups[1].Value, unquotedMatch.Groups[2].Value);
        }

        return (false, null, input);
    }
    
    // Helper method to extract all topics (for future tagging feature)
    public List<string> ExtractAllTopics(string input)
    {
        var topics = new List<string>();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return topics;
        }

        var trimmedInput = input.Trim();
        var quotedMatch = QuotedHashtagRegex.Match(trimmedInput);
        
        if (quotedMatch.Success)
        {
            var topicString = quotedMatch.Groups[1].Value;
            
            // Split by user-defined separator and trim each topic
            var separator = _settingsService.TopicSeparator;
            topics = topicString.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }
        else
        {
            var unquotedMatch = UnquotedHashtagRegex.Match(trimmedInput);
            if (unquotedMatch.Success)
            {
                topics.Add(unquotedMatch.Groups[1].Value);
            }
        }
        
        return topics;
    }
}
