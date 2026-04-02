using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduSyncAI.Services
{
    public class Model3DAssetDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Discipline { get; set; }
        public string ModelUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class RepositoryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;
        private readonly string _apiBaseUrl = $"{AppConfig.ApiUrl}/modelassets";

        public RepositoryService()
        {
            _httpClient = new HttpClient();
            _cacheDirectory = Path.Combine(AppConfig.DataDir, "Cache", "3DModels");
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<List<Model3DAssetDto>> GetModelsByDisciplineAsync(string discipline)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/discipline/{discipline}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<Model3DAssetDto>>(json, options) ?? new List<Model3DAssetDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RepoService] Error fetching models: {ex.Message}");
            }

            return new List<Model3DAssetDto>();
        }

        public async Task<string> DownloadAndCacheModelAsync(Model3DAssetDto asset)
        {
            try
            {
                // Create a clean filename from the URL or title
                var fileName = Path.GetFileName(new Uri(asset.ModelUrl).LocalPath);
                if (string.IsNullOrEmpty(fileName)) 
                {
                    fileName = $"{asset.Id}.obj";
                }

                var extension = Path.GetExtension(fileName).ToLower();
                var localFilePath = Path.Combine(_cacheDirectory, $"{asset.Id}_{asset.Title.Replace(" ", "_")}{extension}");

                // If already cached, return immediately
                if (File.Exists(localFilePath))
                {
                    Console.WriteLine($"[RepoService] Found {asset.Title} in cache.");
                    return localFilePath;
                }

                // Download the file
                Console.WriteLine($"[RepoService] Downloading {asset.Title} from {asset.ModelUrl}...");
                var response = await _httpClient.GetAsync(asset.ModelUrl);
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                return localFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RepoService] Error downloading model '{asset.Title}': {ex.Message}");
                return string.Empty;
            }
        }
    }
}
