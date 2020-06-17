using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class DatabaseHelper
{
    // Retrieve a response from an url.
    public static async Task<HttpResponseMessage> GetHTTPAsync(string url)
    {
        using(var client = new HttpClient())
        {
            return await client.GetAsync(url);
        }
    }

    // Retrieve content from an url.
    public static async Task<string> GetContentAsync(string url)
    {
        var result = await GetHTTPAsync(url);

        return !result.IsSuccessStatusCode ? default : await result.Content.ReadAsStringAsync();
    }

    // Calls the url to insert a name and a score into the CaveQuestDatabase.
    public static async Task InsertScore(string name, int score)
    {
        if(string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name), $"{nameof(name)} is invalid");
        }
        if(score < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(score), score, $"{nameof(score)} is out of range: has to be 0 or greater.");
        }

        string insertScoreUrl = $"https://studenthome.hku.nl/~casey.hofland/Database/CaveQuestInsertScore?playerName={name}&score={score}";
        await GetContentAsync(insertScoreUrl);
    }
}
