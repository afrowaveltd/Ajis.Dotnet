# Build and pack all NuGet packages
Write-Host "Building Ajis packages..."

$packages = @(
    "src\Afrowave.AJIS.Core",
    "src\Afrowave.AJIS.Streaming",
    "src\Afrowave.AJIS.Serialization",
    "src\Afrowave.AJIS.IO",
    "Afrowave.AJIS.EntityFramework",
    "Afrowave.AJIS.MongoDB"
)

# Build and pack each package
foreach ($pkg in $packages) {
    Write-Host "Processing $pkg..."
    dotnet build "$pkg\$pkg.csproj" --configuration Release --no-restore
    
    if ($LASTEXITCODE -eq 0) {
        dotnet pack "$pkg\$pkg.csproj" --configuration Release --no-build --output nupkgs
    } else {
        Write-Host "Build failed for $pkg" -ForegroundColor Red
    }
}

Write-Host "Done! Packages are in nupkgs folder"