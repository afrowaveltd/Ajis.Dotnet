$ErrorActionPreference = "Stop"

Write-Host "=== AJIS structure initialization ===" -ForegroundColor Cyan

function Ensure-Dir($path) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path | Out-Null
        Write-Host "Created dir: $path"
    }
}

function Ensure-File($path, $content = "") {
    if (-not (Test-Path $path)) {
        Set-Content -Path $path -Value $content -Encoding UTF8
        Write-Host "Created file: $path"
    }
}

# ---------- Core ----------
$core = "src/Afrowave.AJIS.Core"
Ensure-Dir "$core/Abstractions"
Ensure-Dir "$core/Configuration"
Ensure-Dir "$core/Diagnostics"
Ensure-Dir "$core/Events"
Ensure-Dir "$core/Internal"
Ensure-Dir "$core/Docs"

Ensure-File "$core/README.md" @"
# Afrowave.AJIS.Core

Core contracts, diagnostics, settings, localization, logging and event model for AJIS.

This project contains **no parsing or serialization logic**.
"@

Ensure-File "$core/Docs/Localization.md" "# Localization"
Ensure-File "$core/Docs/Diagnostics.md" "# Diagnostics"
Ensure-File "$core/Docs/Logging.md" "# Logging"
Ensure-File "$core/Docs/Events.md" "# Events"

# ---------- Streaming ----------
$streaming = "src/Afrowave.AJIS.Streaming"
Ensure-Dir "$streaming/Model"
Ensure-Dir "$streaming/Parsing"
Ensure-Dir "$streaming/Extensions"
Ensure-Dir "$streaming/Docs"

Ensure-File "$streaming/README.md" @"
# Afrowave.AJIS.Streaming

Low-memory streaming parser producing AJIS segments.
"@

Ensure-File "$streaming/Docs/StreamingModel.md" "# Streaming Model"
Ensure-File "$streaming/Docs/SegmentContract.md" "# Segment Contract"

# ---------- Serialization ----------
$serialization = "src/Afrowave.AJIS.Serialization"
Ensure-Dir "$serialization/Facade"
Ensure-Dir "$serialization/Model"
Ensure-Dir "$serialization/Docs"

Ensure-File "$serialization/README.md" @"
# Afrowave.AJIS.Serialization

AJIS serialization APIs and segment-based serializers.
"@

Ensure-File "$serialization/Docs/SerializationModes.md" "# Serialization Modes"
Ensure-File "$serialization/Docs/Canonicalization.md" "# Canonicalization"

# ---------- Optional IO ----------
$io = "src/Afrowave.AJIS.IO"
Ensure-Dir "$io/FileOps"
Ensure-Dir "$io/Docs"

Ensure-File "$io/README.md" @"
# Afrowave.AJIS.IO

File-level operations for AJIS (search, replace, partial read/write).
"@

# ---------- Optional Net ----------
$net = "src/Afrowave.AJIS.Net"
Ensure-Dir "$net/Http"
Ensure-Dir "$net/Docs"

Ensure-File "$net/README.md" @"
# Afrowave.AJIS.Net

HTTP helpers and streaming integrations for AJIS.
"@

# ---------- Records ----------
$records = "src/Afrowave.AJIS.Records"
Ensure-Dir "$records/Model"
Ensure-Dir "$records/Mapping"
Ensure-Dir "$records/Docs"

Ensure-File "$records/README.md" @"
# Afrowave.AJIS.Records

Record-based mapping helpers for AJIS.
"@

# ---------- Root Docs sanity ----------
Ensure-Dir "Docs"
Ensure-File "Docs/README.md" "# AJIS Documentation"

Write-Host "=== AJIS structure ready ===" -ForegroundColor Green
