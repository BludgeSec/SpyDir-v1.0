using System;
using System.IO;
using System.Threading.Tasks;

namespace NetCrawler_v1.2.Services
{
    public class AIAnalysisService
    {
        private readonly ILLMService _llmService;

        public AIAnalysisService(ILLMService llmService)
        {
            _llmService = llmService;
        }

        public async Task<string> AnalyzeFile(string filePath, string fileType, string ipAddress, string hostname, string keywords, bool analyzeContent)
        {
            try
            {
                // Read the file content using synchronous method
                string content = System.IO.File.ReadAllText(filePath);

                // Analyze the content asynchronously
                return await _llmService.AnalyzeFileContent(content, fileType, ipAddress, hostname, keywords, analyzeContent);
            }
            catch (Exception ex)
            {
                return $"Error analyzing file: {ex.Message}";
            }
        }
    }
} 