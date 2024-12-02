using Microsoft.Extensions.Caching.Memory;

namespace AutoTag.Core;

public interface ICoverArtFetcher
{
    Task<byte[]?> GetCoverArtAsync(string url);
}

public class CoverArtFetcher(HttpClient client, IMemoryCache cache) : ICoverArtFetcher
{
    public async Task<byte[]?> GetCoverArtAsync(string url)
    {
        if (!cache.TryGetValue(url, out byte[]? imgBytes))
        {
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                imgBytes = await response.Content.ReadAsByteArrayAsync();
                cache.Set(url, imgBytes);
            }
        }

        return imgBytes;
    }
}