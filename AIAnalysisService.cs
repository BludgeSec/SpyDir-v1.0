using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCrawler
{
    public class RiskAnalysisResult
    {
        public string IPAddress { get; set; }
        public string Hostname { get; set; }
        public string FilePath { get; set; }
        public int RiskLevel { get; set; }
        public string AnalysisDetails { get; set; }
        public long FileSize { get; set; }
    }

    public class AIAnalysisService : IDisposable
    {
        private readonly string _outputDirectory;
        private bool _disposed = false;
        private const int MAX_RESULTS_PER_TYPE = 1000; // Limit results per file type
        private const int MIN_RISK_LEVEL = 2; // Minimum risk level to include in results

        public AIAnalysisService(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
            Directory.CreateDirectory(outputDirectory);
        }

        public async Task<List<RiskAnalysisResult>> AnalyzeFiles(string auditFilePath, List<string> keywords, List<string> fileTypes, bool analyzeContent)
        {
            var results = new Dictionary<string, List<RiskAnalysisResult>>(); // Group by extension
            try
            {
                var fileContent = System.IO.File.ReadAllText(auditFilePath);
                var lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine($"Processing {lines.Length} files...");
                int processedCount = 0;

                foreach (var line in lines)
                {
                    processedCount++;
                    if (processedCount % 1000 == 0)
                    {
                        Console.WriteLine($"Processed {processedCount} of {lines.Length} files...");
                    }

                    var fileInfo = ParseFileLine(line);
                    if (fileInfo != null)
                    {
                        var riskLevel = CalculateRiskLevel(fileInfo, keywords, fileTypes, analyzeContent);
                        if (riskLevel >= MIN_RISK_LEVEL)
                        {
                            var extension = Path.GetExtension(fileInfo.Path).ToLower();
                            if (string.IsNullOrEmpty(extension)) extension = "(no extension)";

                            if (!results.ContainsKey(extension))
                            {
                                results[extension] = new List<RiskAnalysisResult>();
                            }

                            // Only add if we haven't reached the limit for this type
                            if (results[extension].Count < MAX_RESULTS_PER_TYPE)
                            {
                                var details = GenerateAnalysisDetails(fileInfo, riskLevel, keywords, fileTypes);
                                results[extension].Add(new RiskAnalysisResult
                                {
                                    IPAddress = fileInfo.IPAddress ?? "Unknown",
                                    Hostname = fileInfo.Hostname ?? "Unknown",
                                    FilePath = fileInfo.Path,
                                    RiskLevel = riskLevel,
                                    AnalysisDetails = details,
                                    FileSize = fileInfo.Size
                                });
                            }
                        }
                    }
                }

                // Flatten and sort results
                var finalResults = results.Values
                    .SelectMany(x => x)
                    .OrderByDescending(x => x.RiskLevel)
                    .ToList();

                Console.WriteLine($"Found {finalResults.Count} high-risk files across {results.Count} file types");

                // Write summary to console
                foreach (var group in results)
                {
                    Console.WriteLine($"{group.Key}: {group.Value.Count} files (avg risk: {group.Value.Average(x => x.RiskLevel):F1})");
                }

                return finalResults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing files: {ex.Message}");
                throw;
            }
        }

        private AuditFileInfo ParseFileLine(string line)
        {
            try
            {
                var parts = line.Split(new[] { '|', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var filePath = parts[0].Trim();
                    var sizeStr = parts[1].Trim();
                    long size = 0;

                    // Extract hostname and IP from UNC path
                    string hostname = null;
                    string ipAddress = null;
                    
                    if (filePath.StartsWith("\\\\"))
                    {
                        var pathParts = filePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        if (pathParts.Length >= 1)
                        {
                            var serverPart = pathParts[0];
                            // Check if it's an IP address
                            if (System.Net.IPAddress.TryParse(serverPart, out _))
                            {
                                ipAddress = serverPart;
                                // Try to get hostname from parts if available
                                hostname = parts.Length >= 3 ? parts[2].Trim() : null;
                            }
                            else
                            {
                                hostname = serverPart;
                                // Try to get IP from parts if available
                                ipAddress = parts.Length >= 3 ? parts[2].Trim() : null;
                            }
                        }
                    }

                    // Try to parse size, handling different formats
                    if (sizeStr.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
                    {
                        double kb;
                        if (double.TryParse(sizeStr.Substring(0, sizeStr.Length - 2), out kb))
                        {
                            size = (long)(kb * 1024);
                        }
                    }
                    else if (sizeStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                    {
                        double mb;
                        if (double.TryParse(sizeStr.Substring(0, sizeStr.Length - 2), out mb))
                        {
                            size = (long)(mb * 1024 * 1024);
                        }
                    }
                    else if (sizeStr.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                    {
                        double gb;
                        if (double.TryParse(sizeStr.Substring(0, sizeStr.Length - 2), out gb))
                        {
                            size = (long)(gb * 1024 * 1024 * 1024);
                        }
                    }
                    else
                    {
                        // Try parsing as bytes
                        long.TryParse(sizeStr.Replace(",", ""), out size);
                    }

                    // If file exists, get actual size
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            size = new FileInfo(filePath).Length;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting file size for {filePath}: {ex.Message}");
                        }
                    }

                    return new AuditFileInfo
                    {
                        Path = filePath,
                        Size = size,
                        IPAddress = ipAddress,
                        Hostname = hostname
                    };
                }
                else if (parts.Length == 1)
                {
                    var filePath = parts[0].Trim();
                    long size = 0;
                    string hostname = null;
                    string ipAddress = null;

                    // Extract hostname and IP from UNC path
                    if (filePath.StartsWith("\\\\"))
                    {
                        var pathParts = filePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        if (pathParts.Length >= 1)
                        {
                            var serverPart = pathParts[0];
                            // Check if it's an IP address
                            if (System.Net.IPAddress.TryParse(serverPart, out _))
                            {
                                ipAddress = serverPart;
                            }
                            else
                            {
                                hostname = serverPart;
                            }
                        }
                    }

                    // Try to get actual file size if file exists
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            size = new FileInfo(filePath).Length;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting file size for {filePath}: {ex.Message}");
                        }
                    }

                    return new AuditFileInfo
                    {
                        Path = filePath,
                        Size = size,
                        IPAddress = ipAddress,
                        Hostname = hostname
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing line: {line} - {ex.Message}");
            }
            return null;
        }

        private int CalculateRiskLevel(AuditFileInfo file, List<string> keywords, List<string> fileTypes, bool analyzeContent)
        {
            var riskLevel = 0;
            var extension = Path.GetExtension(file.Path).ToLower();
            var fileName = Path.GetFileName(file.Path).ToLower();
            var filePath = file.Path.ToLower();

            // Clean and prepare keywords
            var processedKeywords = keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim().ToLower())
                .ToList();

            // If "all" is specified in file types, check all files
            bool checkFileType = fileTypes.Contains("all", StringComparer.OrdinalIgnoreCase) || 
                               fileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase);

            if (checkFileType)
            {
                riskLevel += 2; // Base risk for matching file type
            }

            // Check keywords in filename and path
            foreach (var keyword in processedKeywords)
            {
                // Higher risk for filename matches
                if (fileName.Contains(keyword))
                {
                    riskLevel += 3;
                    Console.WriteLine($"Keyword match in filename: {keyword} in {fileName}");
                }

                // Lower risk for path matches
                if (filePath.Contains(keyword))
                {
                    riskLevel += 1;
                    Console.WriteLine($"Keyword match in path: {keyword} in {filePath}");
                }
            }

            // Size-based risk (for large files)
            if (file.Size > 10 * 1024 * 1024) // 10MB
            {
                riskLevel += 1;
            }

            // Content analysis
            if (analyzeContent && File.Exists(file.Path))
            {
                try
                {
                    var content = File.ReadAllText(file.Path).ToLower();
                    foreach (var keyword in processedKeywords)
                    {
                        if (content.Contains(keyword))
                        {
                            riskLevel += 1;
                            Console.WriteLine($"Keyword match in content: {keyword} in {file.Path}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing content of {file.Path}: {ex.Message}");
                }
            }

            return Math.Min(riskLevel, 5);
        }

        private string GenerateAnalysisDetails(AuditFileInfo file, int riskLevel, List<string> keywords, List<string> fileTypes)
        {
            var details = new List<string>();
            var extension = Path.GetExtension(file.Path).ToLower();
            var fileName = Path.GetFileName(file.Path).ToLower();

            // Clean and prepare keywords
            var processedKeywords = keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim().ToLower())
                .ToList();

            // Add file size in human-readable format
            details.Add($"Size: {FormatFileSize(file.Size)}");

            // Add file type info
            bool checkFileType = fileTypes.Contains("all", StringComparer.OrdinalIgnoreCase) || 
                               fileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase);
            if (checkFileType)
            {
                details.Add($"Type: {extension}");
            }

            // List matching keywords
            var matchingKeywords = processedKeywords
                .Where(k => fileName.Contains(k) || file.Path.ToLower().Contains(k))
                .Select(k => k.Trim());

            if (matchingKeywords.Any())
            {
                details.Add($"Matches: {string.Join(", ", matchingKeywords)}");
            }

            return string.Join(" | ", details);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        public void GenerateHtmlReport(List<RiskAnalysisResult> results, string outputPath)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='en'>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Risk Analysis Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
            html.AppendLine(".container { max-width: 1200px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 10px; background-color: white; }");
            html.AppendLine("th, td { padding: 12px; text-align: left; border: 1px solid #ddd; }");
            html.AppendLine("th { background-color: #f8f9fa; font-weight: 600; }");
            html.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
            html.AppendLine(".risk-5 { background-color: #ffebee; }"); // High risk - light red
            html.AppendLine(".risk-4 { background-color: #fff3e0; }"); // Orange
            html.AppendLine(".risk-3 { background-color: #fff8e1; }"); // Yellow
            html.AppendLine(".summary { margin-bottom: 20px; padding: 15px; background-color: #e3f2fd; border-radius: 5px; }");
            html.AppendLine(".collapsible { background-color: #f8f9fa; color: #444; cursor: pointer; padding: 18px; width: 100%; border: none; text-align: left; outline: none; font-size: 15px; margin-top: 10px; border-radius: 4px; display: flex; justify-content: space-between; align-items: center; }");
            html.AppendLine(".active, .collapsible:hover { background-color: #e9ecef; }");
            html.AppendLine(".content { display: none; overflow: hidden; background-color: white; border-radius: 0 0 4px 4px; }");
            html.AppendLine(".badge { background-color: #007bff; color: white; padding: 4px 8px; border-radius: 12px; font-size: 12px; margin-left: 8px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='container'>");

            // Add summary section
            html.AppendLine("<div class='summary'>");
            html.AppendLine($"<h2>Risk Analysis Summary</h2>");
            html.AppendLine($"<p>Total files analyzed: {results.Count}</p>");
            html.AppendLine($"<p>High risk files (Risk Level 5): {results.Count(r => r.RiskLevel == 5)}</p>");
            html.AppendLine($"<p>Medium risk files (Risk Level 3-4): {results.Count(r => r.RiskLevel >= 3 && r.RiskLevel <= 4)}</p>");
            html.AppendLine("</div>");

            // Group by IP Address
            var ipGroups = results.GroupBy(r => r.IPAddress ?? "Unknown Host")
                                .OrderBy(g => g.Key);

            foreach (var ipGroup in ipGroups)
            {
                string ipAddress = ipGroup.Key;
                var hostName = ipGroup.First().Hostname ?? "Unknown";
                
                html.AppendLine($"<button class='collapsible'>{ipAddress} ({hostName}) <span class='badge'>{ipGroup.Count()} files</span></button>");
                html.AppendLine("<div class='content'>");

                // Group by file extension within each IP
                var extensionGroups = ipGroup.GroupBy(r => Path.GetExtension(r.FilePath).ToLower())
                                           .OrderByDescending(g => g.Count());

                foreach (var extGroup in extensionGroups)
                {
                    string ext = string.IsNullOrEmpty(extGroup.Key) ? "(no extension)" : extGroup.Key;
                    html.AppendLine($"<button class='collapsible' style='background-color: #f1f3f5;'>{ext} <span class='badge'>{extGroup.Count()} files</span></button>");
                    html.AppendLine("<div class='content'>");
                    
                    // Add table for this extension group
                    html.AppendLine("<table>");
                    html.AppendLine("<tr>");
                    html.AppendLine("<th>Risk Level</th>");
                    html.AppendLine("<th>File Path</th>");
                    html.AppendLine("<th>Size</th>");
                    html.AppendLine("<th>Details</th>");
                    html.AppendLine("</tr>");

                    foreach (var result in extGroup.OrderByDescending(r => r.RiskLevel))
                    {
                        html.AppendLine($"<tr class='risk-{result.RiskLevel}'>");
                        html.AppendLine($"<td>{result.RiskLevel}</td>");
                        html.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(result.FilePath)}</td>");
                        html.AppendLine($"<td>{FormatFileSize(result.FileSize)}</td>");
                        html.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(result.AnalysisDetails)}</td>");
                        html.AppendLine("</tr>");
                    }

                    html.AppendLine("</table>");
                    html.AppendLine("</div>");
                }

                html.AppendLine("</div>");
            }

            // Add JavaScript for collapsible functionality
            html.AppendLine("<script>");
            html.AppendLine(@"
                var coll = document.getElementsByClassName('collapsible');
                for (var i = 0; i < coll.length; i++) {
                    coll[i].addEventListener('click', function() {
                        this.classList.toggle('active');
                        var content = this.nextElementSibling;
                        if (content.style.display === 'block') {
                            content.style.display = 'none';
                        } else {
                            content.style.display = 'block';
                        }
                    });
                }
            ");
            html.AppendLine("</script>");

            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            File.WriteAllText(outputPath, html.ToString());
            Console.WriteLine($"HTML report generated: {outputPath}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }
                _disposed = true;
            }
        }
    }

    public class AuditFileInfo
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public string IPAddress { get; set; }
        public string Hostname { get; set; }
    }
} 