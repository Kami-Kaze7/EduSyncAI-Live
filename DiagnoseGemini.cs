using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Gemini API Diagnostic Tool ===");
        
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("❌ ERROR: GEMINI_API_KEY environment variable is not set.");
            return;
        }

        Console.WriteLine($"🔍 Testing API Key: {apiKey.Substring(0, Math.Min(6, apiKey.Length))}...{apiKey.Substring(Math.Max(0, apiKey.Length - 4))}");
        Console.WriteLine($"📏 Key Length: {apiKey.Length} characters");

        using var client = new HttpClient();

        // 1. Test ListModels (Most reliable way to check key validity)
        Console.WriteLine("\n--- Testing Model Access ---");
        try 
        {
            var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ SUCCESS: API Key is valid and can list models.");
                Console.WriteLine("Available Models (Partial list):");
                if (content.Contains("gemini-1.5-flash")) Console.WriteLine("  - gemini-1.5-flash");
                if (content.Contains("gemini-1.5-pro")) Console.WriteLine("  - gemini-1.5-pro");
                if (content.Contains("gemini-pro")) Console.WriteLine("  - gemini-pro");
            }
            else
            {
                Console.WriteLine($"❌ FAILED: API Key check failed. Status: {response.StatusCode}");
                Console.WriteLine($"Error Details: {content}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CRITICAL ERROR: {ex.Message}");
        }

        Console.WriteLine("\n=== Troubleshooting Tips ===");
        Console.WriteLine("1. If you see 'API_KEY_INVALID', double check the key in AI Studio.");
        Console.WriteLine("2. Make sure no extra spaces were copied.");
        Console.WriteLine("3. Ensure 'Generative Language API' is enabled in Google Cloud Console.");
        Console.WriteLine("============================");
    }
}
