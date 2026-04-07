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

                using (var fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                // For OBJ files, also try to download the companion .mtl file
                // so HelixToolkit can load the model's original materials/colors
                if (extension == ".obj")
                {
                    await TryDownloadCompanionMtlAsync(asset.ModelUrl, localFilePath);
                }

                return localFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RepoService] Error downloading model '{asset.Title}': {ex.Message}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Attempts to download the .mtl companion file for an OBJ model.
        /// Tries two strategies: replacing .obj with .mtl in the URL, and parsing
        /// the OBJ file's mtllib directive for the actual MTL filename.
        /// </summary>
        private async Task TryDownloadCompanionMtlAsync(string objUrl, string localObjPath)
        {
            try
            {
                var localDir = Path.GetDirectoryName(localObjPath) ?? "";

                // Strategy 1: Read the OBJ file and look for "mtllib <filename>.mtl"
                string? mtlFileName = null;
                foreach (var line in File.ReadLines(localObjPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("mtllib ", StringComparison.OrdinalIgnoreCase))
                    {
                        mtlFileName = trimmed.Substring(7).Trim();
                        break;
                    }
                }

                // Strategy 2: If no mtllib directive, try replacing .obj with .mtl in the URL
                var urlsToTry = new List<(string url, string localName)>();
                if (!string.IsNullOrEmpty(mtlFileName))
                {
                    // Build URL relative to the OBJ URL
                    var baseUrl = objUrl.Substring(0, objUrl.LastIndexOf('/') + 1);
                    urlsToTry.Add((baseUrl + mtlFileName, mtlFileName));
                }
                // Always also try the simple .obj -> .mtl replacement
                var simpleMtlUrl = System.Text.RegularExpressions.Regex.Replace(objUrl, @"\.obj$", ".mtl", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (simpleMtlUrl != objUrl)
                {
                    var simpleMtlName = Path.GetFileNameWithoutExtension(localObjPath) + ".mtl";
                    urlsToTry.Add((simpleMtlUrl, simpleMtlName));
                }

                foreach (var (mtlUrl, localName) in urlsToTry)
                {
                    var localMtlPath = Path.Combine(localDir, localName);
                    if (File.Exists(localMtlPath)) break; // Already have it

                    try
                    {
                        var mtlResponse = await _httpClient.GetAsync(mtlUrl);
                        if (mtlResponse.IsSuccessStatusCode)
                        {
                            using var fs = new FileStream(localMtlPath, FileMode.Create, FileAccess.Write, FileShare.None);
                            await mtlResponse.Content.CopyToAsync(fs);
                            Console.WriteLine($"[RepoService] Downloaded companion MTL: {localName}");
                            break; // Got it, no need to try more URLs
                        }
                    }
                    catch { /* MTL not available at this URL, try next */ }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RepoService] Could not download MTL companion: {ex.Message}");
            }
        }
    }
}
