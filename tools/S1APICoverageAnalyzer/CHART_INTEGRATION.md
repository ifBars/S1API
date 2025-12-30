# Coverage Chart Integration

## Overview

The S1API Coverage Analyzer now generates historical coverage charts that are automatically embedded in the main README.md file. This provides a visual representation of how S1API's coverage of Schedule One's game types improves over time.

## How It Works

### 1. Coverage History Tracking

Every time the analyzer runs, it:
- Calculates current coverage metrics (class %, member %)
- Compares with previous runs to detect changes
- Generates events for significant changes (game updates, analyzer improvements, coverage increases)
- Appends a new entry to `coverage-history.json`

### 2. Chart Generation

The analyzer uses QuickChart.io to generate embeddable chart images:
- **Format**: Line chart showing class coverage over time
- **Data Points**: All historical entries from `coverage-history.json`
- **Output**: Markdown with embedded chart URL
- **File**: `coverage-chart.md` (generated but not committed)
- **Note**: Member coverage is currently disabled in charts (set to 0% until implemented)

### 3. README Integration

The GitHub Actions workflow automatically:
- Runs the coverage analyzer with history and chart generation enabled
- Extracts the chart URL from `coverage-chart.md`
- Updates the chart image in `README.md`
- Commits both `README.md` and `coverage-history.json` back to the repository

## Files

### Tracked Files (Committed to Git)
- `tools/S1APICoverageAnalyzer/coverage-history.json` - Historical coverage data
- `README.md` - Contains the embedded chart

### Generated Files (Not Committed)
- `coverage-chart.md` - Temporary chart markdown (regenerated each run)
- `coverage-report.json` - Detailed coverage report
- `coverage-badge.md` - Coverage badge markdown

## GitHub Actions Workflow

The `coverage.yml` workflow now includes:

```yaml
- name: Run Coverage Analysis
  run: |
    dotnet run --project tools/S1APICoverageAnalyzer/S1APICoverageAnalyzer.csproj -- \
      --game-assembly S1API/ScheduleOneAssemblies/Managed/Assembly-CSharp.dll \
      --api-assembly S1API/bin/MonoMelon/netstandard2.1/S1API.dll \
      --output coverage-report.json \
      --badge-output coverage-badge.md \
      --history-file tools/S1APICoverageAnalyzer/coverage-history.json \
      --chart-output coverage-chart.md \
      --chart-format markdown \
      --verbose

- name: Update README Badge and Chart
  run: |
    # Updates line 5 with new coverage badge
    # Updates the chart image URL in the "Coverage History" section
    # Commits both README.md and coverage-history.json
```

## Manual Usage

To generate the chart locally:

```bash
cd tools/S1APICoverageAnalyzer

dotnet run -- \
  -g Assembly-CSharp.dll \
  -a S1API.Mono.dll \
  -o coverage-report.json \
  --history-file coverage-history.json \
  --chart-output coverage-chart.md \
  --chart-format markdown
```

The chart will be written to `coverage-chart.md` and can be previewed locally.

## Chart Formats

The analyzer supports multiple chart output formats:

1. **markdown** (default) - Embeddable QuickChart image
2. **url** - Plain chart URL
3. **html** - Interactive HTML with Chart.js
4. **mermaid** - GitHub-native Mermaid diagram

For the README integration, we use **markdown** format for maximum compatibility.

## Benefits

1. **Visual Progress Tracking**: See coverage improvements at a glance
2. **Historical Context**: Understand when major changes occurred
3. **Transparency**: Show the community S1API's development progress
4. **Motivation**: Visual feedback encourages contributions
5. **Event Annotations**: Charts can show when game updates or analyzer improvements happened

## Future Enhancements

Potential improvements:
- Add event markers to the chart (game updates, analyzer changes)
- Show trend lines and projections
- Break down coverage by namespace
- Add interactive tooltips with event details
- Generate weekly/monthly summary reports

## Troubleshooting

### Chart Not Updating
- Check that `coverage-history.json` is being committed
- Verify the workflow has write permissions
- Ensure QuickChart.io is accessible

### Chart Shows Old Data
- The chart URL is generated from the history file
- If history isn't updated, the chart won't change
- Run the analyzer with `--history-file` to update

### Chart Image Not Loading
- QuickChart.io may have rate limits
- The URL may be too long (history too large)
- Try regenerating with fewer data points

## Related Files

- `tools/S1APICoverageAnalyzer/Program.cs` - Main analyzer entry point
- `tools/S1APICoverageAnalyzer/Output/ChartGenerator.cs` - Chart generation logic
- `tools/S1APICoverageAnalyzer/Analysis/HistoryManager.cs` - History persistence
- `tools/S1APICoverageAnalyzer/Analysis/VersionTracker.cs` - Change detection
- `.github/workflows/coverage.yml` - CI/CD integration

