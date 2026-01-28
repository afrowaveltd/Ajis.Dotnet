# ============================
# AJIS .NET solution bootstrap
# ============================

$ErrorActionPreference = "Stop"

# ---------
# Settings
# ---------
$SolutionName = "Ajis.Dotnet"

$CoreTFM      = "net10.0;netstandard2.1"
$ModernTFM    = "net10.0"
$TestTFM      = "net10.0"
$BenchmarkTFM = "net10.0"

# -------------
# Directories
# -------------
$dirs = @(
    "src",
    "tests",
    "benchmarks",
    "test_data",
    "test_data\tiny",
    "test_data\medium",
    "test_data\large",
    "test_data\generators"
)

foreach ($dir in $dirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }
}

# ----------------
# Create solution
# ----------------
if (-not (Test-Path "$SolutionName.sln")) {
    dotnet new sln -n $SolutionName
}

# --------------------
# Source projects
# --------------------
$srcProjects = @(
    @{ Name = "Afrowave.AJIS.Core";           Path = "src\Afrowave.AJIS.Core";           Tfm = $CoreTFM },
    @{ Name = "Afrowave.AJIS.Serialization";  Path = "src\Afrowave.AJIS.Serialization";  Tfm = $CoreTFM },
    @{ Name = "Afrowave.AJIS.Records";        Path = "src\Afrowave.AJIS.Records";        Tfm = $CoreTFM },
    @{ Name = "Afrowave.AJIS.IO";              Path = "src\Afrowave.AJIS.IO";              Tfm = $CoreTFM },
    @{ Name = "Afrowave.AJIS.Net";             Path = "src\Afrowave.AJIS.Net";             Tfm = $ModernTFM },
    @{ Name = "Afrowave.AJIS";                 Path = "src\Afrowave.AJIS";                 Tfm = $ModernTFM }
)

foreach ($proj in $srcProjects) {
    if (-not (Test-Path $proj.Path)) {
        dotnet new classlib -n $proj.Name -o $proj.Path
        dotnet add "$proj.Path\$($proj.Name).csproj" package Microsoft.SourceLink.GitHub | Out-Null
    }

    dotnet sln add "$proj.Path\$($proj.Name).csproj" | Out-Null
}

# --------------------
# Test helper project
# --------------------
$testingProj = "tests\Afrowave.AJIS.Testing"
if (-not (Test-Path $testingProj)) {
    dotnet new classlib -n Afrowave.AJIS.Testing -o $testingProj
    dotnet add "$testingProj\Afrowave.AJIS.Testing.csproj" package xunit | Out-Null
    dotnet add "$testingProj\Afrowave.AJIS.Testing.csproj" package NSubstitute | Out-Null
}
dotnet sln add "$testingProj\Afrowave.AJIS.Testing.csproj" | Out-Null

# -------------
# Test projects
# -------------
$testProjects = @(
    "Afrowave.AJIS.Core.Tests",
    "Afrowave.AJIS.Records.Tests",
    "Afrowave.AJIS.Serialization.Tests"
)

foreach ($name in $testProjects) {
    $path = "tests\$name"
    if (-not (Test-Path $path)) {
        dotnet new xunit -n $name -o $path
        dotnet add "$path\$name.csproj" reference "$testingProj\Afrowave.AJIS.Testing.csproj"
        dotnet add "$path\$name.csproj" package NSubstitute | Out-Null
    }
    dotnet sln add "$path\$name.csproj" | Out-Null
}

# --------------------
# Benchmark project
# --------------------
$benchPath = "benchmarks\Afrowave.AJIS.Benchmarks"
if (-not (Test-Path $benchPath)) {
    dotnet new console -n Afrowave.AJIS.Benchmarks -o $benchPath
    dotnet add "$benchPath\Afrowave.AJIS.Benchmarks.csproj" package BenchmarkDotNet | Out-Null
    dotnet add "$benchPath\Afrowave.AJIS.Benchmarks.csproj" reference "$testingProj\Afrowave.AJIS.Testing.csproj"
}
dotnet sln add "$benchPath\Afrowave.AJIS.Benchmarks.csproj" | Out-Null

# --------------------
# Test data manifest
# --------------------
$manifest = "test_data\manifest.ajis"
if (-not (Test-Path $manifest)) {
@"
# AJIS test data manifest
# Large files are generated on demand and cached per-machine.
#
# Example entries:
# users_1m.ajis:
#   type: users
#   count: 1000000
#   seed: 42
#   expected_size_mb: 880
"@ | Set-Content $manifest -Encoding UTF8
}

Write-Host "AJIS solution structure created successfully." -ForegroundColor Green
