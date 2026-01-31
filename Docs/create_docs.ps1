# Create /docs/api structure with empty markdown files
# Run this script FROM the Docs directory

$apiDir = Join-Path (Get-Location) "api"

if (-not (Test-Path $apiDir)) {
    New-Item -ItemType Directory -Path $apiDir | Out-Null
    Write-Host "Created directory: api"
} else {
    Write-Host "Directory already exists: api"
}

$files = @(
    "README.md",
    "streamwalk.md",
    "reader.md",
    "stream-reader.md",
    "visitor.md",
    "slices.md",
    "options.md",
    "errors.md"
)

foreach ($file in $files) {
    $path = Join-Path $apiDir $file
    if (-not (Test-Path $path)) {
        New-Item -ItemType File -Path $path | Out-Null
        Write-Host "Created file: api/$file"
    } else {
        Write-Host "File already exists: api/$file"
    }
}

Write-Host "Docs API structure ready."
