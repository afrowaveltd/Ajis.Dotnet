<#
.SYNOPSIS
  Creates a starter set of INVALID StreamWalk .case files (deterministic errors)
  that match the AJIS StreamWalk contract in Docs/tests/streamwalk.md.

.DESCRIPTION
  - Safe to run multiple times.
  - By default it will NOT overwrite existing files.
  - Use -Force to overwrite.

  It also auto-detects whether your documentation folder is "Docs" or "docs".

.USAGE
  From repo root:
    pwsh ./tools/docs_tests_add_invalid_streamwalk_cases.ps1

  Or from anywhere:
    pwsh <path>/docs_tests_add_invalid_streamwalk_cases.ps1

  If scripts are blocked:
    pwsh -ExecutionPolicy Bypass -File ./tools/docs_tests_add_invalid_streamwalk_cases.ps1
#>

[CmdletBinding()]
param(
  [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Write-FileUtf8NoBom {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Content,
    [switch]$Force
  )

  $dir = Split-Path -Parent $Path
  if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

  if ((-not $Force) -and (Test-Path $Path)) {
    Write-Host "SKIP  $Path" -ForegroundColor DarkGray
    return
  }

  # PowerShell 7+: utf8NoBOM is supported.
  # Windows PowerShell 5.1: fallback to UTF8 (BOM) is possible, but we try to avoid BOM.
  try {
    Set-Content -Path $Path -Value $Content -Encoding utf8NoBOM -NoNewline
  }
  catch {
    # Fallback: write bytes manually without BOM
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($Content)
    [System.IO.File]::WriteAllBytes($Path, $bytes)
  }

  Write-Host "WRITE $Path" -ForegroundColor Green
}

# Resolve repo root (script is expected in tools/)
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

# Detect Docs folder casing (Docs vs docs)
$docsDir = Join-Path $root 'Docs'
if (-not (Test-Path $docsDir)) {
  $docsDir = Join-Path $root 'docs'
}
if (-not (Test-Path $docsDir)) {
  throw "Docs folder not found. Expected '$($root)\\Docs' or '$($root)\\docs'."
}

$testDataRoot = Join-Path $root 'test_data'
if (-not (Test-Path $testDataRoot)) {
  New-Item -ItemType Directory -Path $testDataRoot -Force | Out-Null
}

$base = Join-Path $testDataRoot 'streamwalk/invalid'

# Common OPTIONS block (AJIS-first, strict-ish)
$opts = @(
  '# OPTIONS',
  '',
  'MODE: AJIS',
  'COMMENTS: off',
  'DIRECTIVES: off',
  'IDENTIFIERS: off',
  'MAX_DEPTH: 64',
  'MAX_TOKEN_BYTES: 1048576',
  ''
) -join "`n"

function CaseText {
  param(
    [string]$InputText,
    [string[]]$ExpectedLines
  )

  $expected = ($ExpectedLines -join "`n") + "`n"
  return @(
    $opts,
    '# INPUT',
    '',
    $InputText,
    '',
    '# EXPECTED',
    '',
    $expected
  ) -join "`n"
}

# Case 1: Unexpected end of input (missing closing brace)
$case1 = CaseText -InputText '{"a":1' -ExpectedLines @(
  'ERROR UnexpectedEndOfInput offset=6'
)

# Case 2: Trailing garbage after a complete document
$case2 = CaseText -InputText '{}x' -ExpectedLines @(
  'BEGIN_OBJECT',
  'END_OBJECT',
  'ERROR TrailingGarbage offset=2'
)

# Case 3: Invalid escape sequence inside string (\q)
$case3 = CaseText -InputText '{"s":"a\q"}' -ExpectedLines @(
  'BEGIN_OBJECT',
  'PROPERTY_NAME s',
  'ERROR InvalidEscapeSequence offset=8'
)

# Case 4: Unexpected character at root
$case4 = CaseText -InputText ']' -ExpectedLines @(
  'ERROR UnexpectedCharacter offset=0'
)

# Case 5: Unexpected token (value expected, got } )
$case5 = CaseText -InputText '{"a":}' -ExpectedLines @(
  'BEGIN_OBJECT',
  'PROPERTY_NAME a',
  'ERROR UnexpectedToken offset=5'
)

Write-FileUtf8NoBom -Path (Join-Path $base 'syntax/01_unexpected_eof_object.case') -Content $case1 -Force:$Force
Write-FileUtf8NoBom -Path (Join-Path $base 'syntax/02_trailing_garbage.case') -Content $case2 -Force:$Force
Write-FileUtf8NoBom -Path (Join-Path $base 'strings/01_invalid_escape.case') -Content $case3 -Force:$Force
Write-FileUtf8NoBom -Path (Join-Path $base 'syntax/03_unexpected_character.case') -Content $case4 -Force:$Force
Write-FileUtf8NoBom -Path (Join-Path $base 'syntax/04_unexpected_token.case') -Content $case5 -Force:$Force

Write-Host ''
Write-Host 'Done. Added invalid StreamWalk cases under:' -ForegroundColor Cyan
Write-Host "  $base" -ForegroundColor Cyan
