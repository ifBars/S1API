# S1API Coverage Analyzer

A tool to analyze how much of the Schedule One game's public API is covered by the S1API modding framework.

## Features

- **Type Coverage Analysis**: Identifies which game types are wrapped by S1API
- **Member Coverage Analysis**: Tracks which members (fields, properties, methods) are exposed
- **Smart Type Matching**: Uses multiple strategies to match game types to S1API types
  - Exact matching
  - Normalized matching (handles nested type separators)
  - Fuzzy matching (handles naming variations)
- **Configurable Exclusions**: Excludes internal/infrastructure types from analysis
- **Multiple Output Formats**: JSON, plain text, and badge markdown

## Usage

```bash
dotnet run --project S1APICoverageAnalyzer.csproj \
  --game-assembly "path/to/Assembly-CSharp.dll" \
  --api-assembly "path/to/S1API.dll" \
  --output "coverage-report.json" \
  --text-output "coverage-report.txt" \
  --badge-output "coverage-badge.md" \
  --verbose
```

### Options

- `-g, --game-assembly` (required): Path to the game assembly (Assembly-CSharp.dll)
- `-a, --api-assembly` (required): Path to the S1API assembly (S1API.dll)
- `-o, --output`: Path to write JSON coverage report
- `-t, --text-output`: Path to write plain text coverage report
- `-b, --badge-output`: Path to write badge markdown
- `-v, --verbose`: Show detailed output including covered/uncovered types

## Type Matching Strategies

The analyzer uses multiple strategies to match game types to S1API types, in order of priority:

### 1. Exact Match
Direct full name match: `ScheduleOne.NPCs.NPC` == `ScheduleOne.NPCs.NPC`

### 2. Normalized Match
Handles nested type separator differences:
- Game: `ScheduleOne.Casino.SlotMachine+ESymbol`
- Matches: `ScheduleOne.Casino.SlotMachine.ESymbol`

### 3. Fuzzy Match
Handles common naming variations:

#### Enum Prefix Differences
- Game: `ScheduleOne.Vehicles.Modification.EVehicleColor`
- S1API: `S1API.Vehicles.VehicleColor`
- Match: ✅ (removes 'E' prefix, compares simple names)

#### Plural/Singular Variations
- Game: `ScheduleOne.Vehicles.Modification.VehicleColors`
- S1API: `S1API.Vehicles.VehicleColor`
- Match: ✅ (detects plural/singular relationship)

#### Interface Prefix Differences
- Game: `ScheduleOne.Services.IPlayerService`
- S1API: `S1API.Services.PlayerService`
- Match: ✅ (removes 'I' prefix)

#### Close Matches
Uses Levenshtein distance to find types with similar names (configurable threshold)

## Configuration

### Matching Configuration

Edit `Configuration/MatchingConfig.cs` to adjust matching behavior:

```csharp
// Enable/disable fuzzy matching
MatchingConfig.EnableFuzzyMatching = true;

// Adjust similarity threshold (0.0 to 1.0)
// Higher = stricter, Lower = more lenient
MatchingConfig.FuzzySimilarityThreshold = 0.75;

// Enable verbose logging for debugging
MatchingConfig.VerboseFuzzyMatching = false;
```

### Exclusion Configuration

Edit `Configuration/ExclusionConfig.cs` to adjust which types are excluded from analysis:

- `ExcludedNamespaces`: Entire namespaces to exclude (e.g., internal systems)
- `ExcludedTypePatterns`: Specific type name patterns to exclude
- `WrapperFieldPrefixes`: Field name prefixes that indicate a primary wrapper

## Output Formats

### JSON Report
Detailed coverage data including:
- Class and member coverage percentages
- List of covered types with their covering API types
- List of uncovered types
- Excluded namespace information

### Text Report
Human-readable summary with:
- Coverage statistics
- Top uncovered types by member count
- Excluded namespace list

### Badge Markdown
Shields.io badge for README files:
```markdown
![Coverage](https://img.shields.io/badge/coverage-18.71%25-red)
```

## Architecture

### Analysis Flow

1. **Load Assemblies**: Load both game and API assemblies using shared load context
2. **Extract Game Types**: Scan game assembly for eligible ScheduleOne types
3. **Analyze API Types**: Scan API assembly for wrapped game types using multiple strategies
4. **Calculate Coverage**: Match game types to API types using multi-strategy matching
5. **Generate Reports**: Output results in requested formats

### Key Components

- `GameAssemblyAnalyzer`: Extracts types from the game assembly
- `ApiAssemblyAnalyzer`: Identifies which game types are wrapped by S1API
- `CoverageCalculator`: Matches game types to API types and calculates coverage
- `TypeNameMatcher`: Provides fuzzy matching logic for type names
- `ReportGenerator`: Generates output in various formats

## Development

### Building

```bash
dotnet build
```

### Testing

Run against actual assemblies:

```bash
dotnet run -- \
  -g "path/to/game/Assembly-CSharp.dll" \
  -a "path/to/S1API.dll" \
  -v
```

## License

Part of the S1API project. See main repository for license information.

