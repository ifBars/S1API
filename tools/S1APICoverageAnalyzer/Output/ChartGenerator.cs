using System.Text;
using System.Text.Json;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Output;

/// <summary>
/// Generates chart visualizations from coverage history data.
/// </summary>
public sealed class ChartGenerator
{
    /// <summary>
    /// Generate a QuickChart API URL for embedding in markdown/HTML.
    /// </summary>
    public string GenerateChartUrl(CoverageHistory history, ChartOptions options)
    {
        var entries = history.GetLastEntries(options.MaxDataPoints);
        
        if (entries.Count == 0)
            return string.Empty;
        
        var labels = entries.Select(e => e.Timestamp.ToString("yyyy-MM-dd")).ToArray();
        var classData = entries.Select(e => e.ClassCoveragePercentage).ToArray();
        var memberData = entries.Select(e => e.MemberCoveragePercentage).ToArray();
        
        var datasets = new List<object>();
        
        if (options.ShowClassCoverage)
        {
            datasets.Add(new
            {
                label = "Class Coverage %",
                data = classData,
                borderColor = "rgb(75, 192, 192)",
                backgroundColor = "rgba(75, 192, 192, 0.1)",
                fill = false,
                tension = 0.1
            });
        }
        
        if (options.ShowMemberCoverage)
        {
            datasets.Add(new
            {
                label = "Member Coverage %",
                data = memberData,
                borderColor = "rgb(255, 99, 132)",
                backgroundColor = "rgba(255, 99, 132, 0.1)",
                fill = false,
                tension = 0.1
            });
        }
        
        var chartConfig = new
        {
            type = "line",
            data = new
            {
                labels = labels,
                datasets = datasets
            },
            options = new
            {
                responsive = true,
                plugins = new
                {
                    title = new
                    {
                        display = true,
                        text = options.Title
                    },
                    legend = new
                    {
                        display = true,
                        position = "top"
                    }
                },
                scales = new
                {
                    y = new
                    {
                        beginAtZero = true,
                        max = 100,
                        title = new
                        {
                            display = true,
                            text = "Coverage %"
                        }
                    },
                    x = new
                    {
                        title = new
                        {
                            display = true,
                            text = "Date"
                        }
                    }
                }
            }
        };
        
        var json = JsonSerializer.Serialize(chartConfig, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        var encoded = Uri.EscapeDataString(json);
        return $"https://quickchart.io/chart?c={encoded}&width={options.Width}&height={options.Height}";
    }
    
    /// <summary>
    /// Generate markdown with embedded chart and recent events.
    /// </summary>
    public string GenerateChartMarkdown(CoverageHistory history, ChartOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("## S1API Coverage Over Time");
        sb.AppendLine();
        
        if (history.Entries.Count == 0)
        {
            sb.AppendLine("*No history data available yet. Run the analyzer a few times to build history.*");
            return sb.ToString();
        }
        
        var chartUrl = GenerateChartUrl(history, options);
        if (!string.IsNullOrEmpty(chartUrl))
        {
            sb.AppendLine($"![Coverage Chart]({chartUrl})");
            sb.AppendLine();
        }
        
        var latest = history.LatestEntry;
        if (latest != null)
        {
            sb.AppendLine($"*Last updated: {latest.Timestamp:yyyy-MM-dd HH:mm} UTC*");
            sb.AppendLine();
        }
        
        // Show recent events
        var recentEntries = history.GetLastEntries(10)
            .Where(e => e.Events.Count > 0 || !string.IsNullOrEmpty(e.Note))
            .OrderByDescending(e => e.Timestamp)
            .Take(5)
            .ToList();
        
        if (recentEntries.Count > 0)
        {
            sb.AppendLine("### Recent Changes");
            foreach (var entry in recentEntries)
            {
                foreach (var evt in entry.Events)
                {
                    var icon = evt.Type switch
                    {
                        EventType.AnalyzerUpdate => "🔧",
                        EventType.GameUpdate => "🎮",
                        EventType.MatchingImproved => "✨",
                        EventType.ApiExpansion => "📈",
                        EventType.ManualAnnotation => "📝",
                        _ => "•"
                    };
                    
                    sb.AppendLine($"- **{entry.Timestamp:yyyy-MM-dd}**: {icon} {evt.Description}");
                    if (!string.IsNullOrEmpty(evt.Details))
                    {
                        sb.AppendLine($"  *{evt.Details}*");
                    }
                }
                
                if (!string.IsNullOrEmpty(entry.Note))
                {
                    sb.AppendLine($"- **{entry.Timestamp:yyyy-MM-dd}**: 📝 {entry.Note}");
                }
            }
            sb.AppendLine();
        }
        
        // Show current coverage stats
        if (latest != null)
        {
            sb.AppendLine("### Current Coverage");
            sb.AppendLine($"- **Class Coverage**: {latest.ClassCoveragePercentage:F2}% ({latest.CoveredClasses}/{latest.TotalClasses})");
            sb.AppendLine($"- **Member Coverage**: {latest.MemberCoveragePercentage:F2}% ({latest.CoveredMembers}/{latest.TotalMembers})");
            sb.AppendLine($"- **Excluded Types**: {latest.ExcludedClasses}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generate HTML with interactive Chart.js chart.
    /// </summary>
    public string GenerateChartHtml(CoverageHistory history, ChartOptions options)
    {
        var entries = history.GetLastEntries(options.MaxDataPoints);
        
        if (entries.Count == 0)
        {
            return "<p>No history data available yet.</p>";
        }
        
        var labels = entries.Select(e => e.Timestamp.ToString("yyyy-MM-dd")).ToArray();
        var classData = entries.Select(e => e.ClassCoveragePercentage).ToArray();
        var memberData = entries.Select(e => e.MemberCoveragePercentage).ToArray();
        
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <title>S1API Coverage Over Time</title>");
        sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js\"></script>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("        .chart-container { position: relative; height: 400px; width: 800px; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"    <h1>{options.Title}</h1>");
        sb.AppendLine("    <div class=\"chart-container\">");
        sb.AppendLine("        <canvas id=\"coverageChart\"></canvas>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <script>");
        sb.AppendLine("        const ctx = document.getElementById('coverageChart').getContext('2d');");
        sb.AppendLine("        const chart = new Chart(ctx, {");
        sb.AppendLine("            type: 'line',");
        sb.AppendLine("            data: {");
        sb.AppendLine($"                labels: {JsonSerializer.Serialize(labels)},");
        sb.AppendLine("                datasets: [");
        
        if (options.ShowClassCoverage)
        {
            sb.AppendLine("                    {");
            sb.AppendLine("                        label: 'Class Coverage %',");
            sb.AppendLine($"                        data: {JsonSerializer.Serialize(classData)},");
            sb.AppendLine("                        borderColor: 'rgb(75, 192, 192)',");
            sb.AppendLine("                        backgroundColor: 'rgba(75, 192, 192, 0.1)',");
            sb.AppendLine("                        fill: false,");
            sb.AppendLine("                        tension: 0.1");
            sb.AppendLine("                    },");
        }
        
        if (options.ShowMemberCoverage)
        {
            sb.AppendLine("                    {");
            sb.AppendLine("                        label: 'Member Coverage %',");
            sb.AppendLine($"                        data: {JsonSerializer.Serialize(memberData)},");
            sb.AppendLine("                        borderColor: 'rgb(255, 99, 132)',");
            sb.AppendLine("                        backgroundColor: 'rgba(255, 99, 132, 0.1)',");
            sb.AppendLine("                        fill: false,");
            sb.AppendLine("                        tension: 0.1");
            sb.AppendLine("                    }");
        }
        
        sb.AppendLine("                ]");
        sb.AppendLine("            },");
        sb.AppendLine("            options: {");
        sb.AppendLine("                responsive: true,");
        sb.AppendLine("                maintainAspectRatio: false,");
        sb.AppendLine("                plugins: {");
        sb.AppendLine("                    title: {");
        sb.AppendLine("                        display: true,");
        sb.AppendLine($"                        text: '{options.Title}'");
        sb.AppendLine("                    }");
        sb.AppendLine("                },");
        sb.AppendLine("                scales: {");
        sb.AppendLine("                    y: {");
        sb.AppendLine("                        beginAtZero: true,");
        sb.AppendLine("                        max: 100,");
        sb.AppendLine("                        title: {");
        sb.AppendLine("                            display: true,");
        sb.AppendLine("                            text: 'Coverage %'");
        sb.AppendLine("                        }");
        sb.AppendLine("                    },");
        sb.AppendLine("                    x: {");
        sb.AppendLine("                        title: {");
        sb.AppendLine("                            display: true,");
        sb.AppendLine("                            text: 'Date'");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine("    </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generate Mermaid chart for GitHub-native rendering.
    /// </summary>
    public string GenerateChartMermaid(CoverageHistory history, ChartOptions options)
    {
        var entries = history.GetLastEntries(options.MaxDataPoints);
        
        if (entries.Count == 0)
        {
            return "```mermaid\n%% No history data available yet\n```";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("%%{init: {'theme':'base', 'themeVariables': { 'primaryColor':'#4ECDC4'}}}%%");
        sb.AppendLine("xychart-beta");
        sb.AppendLine($"    title \"{options.Title}\"");
        
        // Format dates for x-axis (use month abbreviations if many points)
        var labels = entries.Count > 12
            ? entries.Select(e => e.Timestamp.ToString("MMM")).ToArray()
            : entries.Select(e => e.Timestamp.ToString("MM-dd")).ToArray();
        
        sb.AppendLine($"    x-axis [{string.Join(", ", labels.Select(l => $"\"{l}\""))}]");
        sb.AppendLine("    y-axis \"Coverage %\" 0 --> 100");
        
        if (options.ShowClassCoverage)
        {
            var classData = entries.Select(e => e.ClassCoveragePercentage).ToArray();
            sb.AppendLine($"    line [{string.Join(", ", classData.Select(d => d.ToString("F1")))}]");
        }
        
        sb.AppendLine("```");
        
        return sb.ToString();
    }
}

/// <summary>
/// Options for chart generation.
/// </summary>
public sealed class ChartOptions
{
    public int MaxDataPoints { get; set; } = 50;
    public bool ShowClassCoverage { get; set; } = true;
    public bool ShowMemberCoverage { get; set; } = false;
    public bool ShowEventAnnotations { get; set; } = true;
    public bool ShowTotalCounts { get; set; } = false;
    public string Title { get; set; } = "S1API Coverage Over Time";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 400;
}

