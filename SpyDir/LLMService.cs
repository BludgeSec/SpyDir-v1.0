using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCrawler_v1.2.Services
{
    public class LLMService
    {
        private readonly Dictionary<string, string> _fileTypeRisks = new Dictionary<string, string>
        {
            { "exe", "High" },
            { "dll", "High" },
            { "bat", "Medium" },
            { "ps1", "Medium" },
            { "psm1", "Medium" },
            { "vbs", "Low" },
            { "js", "Low" },
            { "html", "Low" },
            { "css", "Low" },
            { "php", "Low" },
            { "py", "Low" },
            { "rb", "Low" },
            { "sh", "Low" },
            { "cmd", "Low" }
        };

        private readonly Dictionary<string, string> _riskKeywords = new Dictionary<string, string>
        {
            { "password", "High" },
            { "secret", "High" },
            { "key", "High" },
            { "token", "High" },
            { "admin", "Medium" },
            { "root", "Medium" },
            { "login", "Medium" },
            { "config", "Medium" },
            { "database", "Medium" },
            { "backup", "Low" },
            { "temp", "Low" },
            { "log", "Low" }
        };

        private int GetRiskLevelValue(string riskLevel)
        {
            return riskLevel.ToLower() switch
            {
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };
        }

        public async Task<string> AnalyzeFileContent(string content, string fileType, string ipAddress, string hostname, string keywords, bool analyzeContent)
        {
            var riskLevel = "Low";
            var keywordsList = keywords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(k => k.Trim().ToLower())
                                     .ToList();

            // Check file type risk
            if (_fileTypeRisks.TryGetValue(fileType.ToLower(), out string typeRisk))
            {
                riskLevel = typeRisk;
            }

            // Check keyword risk
            foreach (var keyword in keywordsList)
            {
                if (_riskKeywords.TryGetValue(keyword, out string keywordRisk))
                {
                    if (GetRiskLevelValue(keywordRisk) > GetRiskLevelValue(riskLevel))
                    {
                        riskLevel = keywordRisk;
                    }
                }
            }

            // If content analysis is requested
            if (analyzeContent)
            {
                try
                {
                    foreach (var keyword in _riskKeywords.Keys)
                    {
                        if (content.ToLower().Contains(keyword))
                        {
                            string keywordRisk = _riskKeywords[keyword];
                            if (GetRiskLevelValue(keywordRisk) > GetRiskLevelValue(riskLevel))
                            {
                                riskLevel = keywordRisk;
                            }
                        }
                    }
                }
                catch
                {
                    // If we can't read the content, keep the current risk level
                }
            }

            return riskLevel;
        }

        public async Task<string> GetAnalysisDetails(string filePath, string fileType, string ipAddress, string hostname, List<string> keywords, bool analyzeContent)
        {
            var details = new StringBuilder();
            details.AppendLine($"File Analysis Report");
            details.AppendLine($"-------------------");
            details.AppendLine($"File: {filePath}");
            details.AppendLine($"Type: {fileType}");
            details.AppendLine($"Location: {ipAddress} ({hostname})");
            details.AppendLine();

            // File type analysis
            if (_fileTypeRisks.TryGetValue(fileType.ToLower(), out string typeRisk))
            {
                details.AppendLine($"File type risk: {typeRisk}");
            }

            // Keyword analysis
            if (keywords.Count > 0)
            {
                details.AppendLine("Keyword analysis:");
                foreach (var keyword in keywords)
                {
                    if (_riskKeywords.TryGetValue(keyword.ToLower(), out string keywordRisk))
                    {
                        details.AppendLine($"- {keyword}: {keywordRisk} risk");
                    }
                }
            }

            return details.ToString();
        }
    }
}
 