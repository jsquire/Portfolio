#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Builds the Numeric Tic-Tac-Toe MCP extension with dependencies.

.DESCRIPTION
    This script builds the C# MCP server in Release configuration and then
    compiles the VS Code extension. The C# build automatically copies the
    executable to the extension's dist/mcp-server folder via post-build action.

.PARAMETER Configuration
    The build configuration for the C# project. Defaults to "Release".

.EXAMPLE
    .\build.ps1
    Builds with Release configuration.

.EXAMPLE
    .\build.ps1 -Configuration Debug
    Builds with Debug configuration.
#>

param(
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

# Set error handling.

$ErrorActionPreference = "Stop"

# Determine paths.

$ExtensionRoot = $PSScriptRoot
$McpProjectPath = Join-Path $ExtensionRoot ".." "NumTic.Mcp.csproj"

Write-Host "Building Numeric Tic-Tac-Toe MCP Extension" -ForegroundColor Green
Write-Host "Extension Root: $ExtensionRoot" -ForegroundColor Gray
Write-Host "MCP Project: $McpProjectPath" -ForegroundColor Gray
Write-Host ""

# Step 1: Build the C# MCP server.

Write-Host "Step 1: Building C# MCP server ($Configuration) with self-contained deployment..." -ForegroundColor Yellow
if (-not (Test-Path $McpProjectPath)) {
    throw "MCP project file not found: $McpProjectPath"
}

# Clear the existing mcp-server directory to ensure clean deployment
$McpServerDir = Join-Path $ExtensionRoot "dist" "mcp-server"
if (Test-Path $McpServerDir) {
    Write-Host "Clearing existing MCP server directory: $McpServerDir" -ForegroundColor Gray
    Remove-Item $McpServerDir -Recurse -Force
}

dotnet publish $McpProjectPath --configuration $Configuration --runtime win-x64 --self-contained true --nologo
if ($LASTEXITCODE -ne 0) {
    throw "C# MCP server publish failed with exit code $LASTEXITCODE"
}

Write-Host "✓ C# MCP server built successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Verify the executable was copied to dist folder.

$ExpectedExecutable = Join-Path $ExtensionRoot "dist" "mcp-server" "NumTic.Mcp.exe"
if (-not (Test-Path $ExpectedExecutable)) {
    throw "MCP server executable not found at expected location: $ExpectedExecutable"
}

Write-Host "✓ MCP server executable found: $ExpectedExecutable" -ForegroundColor Green
Write-Host ""

# Step 3: Install npm dependencies if needed.

Write-Host "Step 2: Installing npm dependencies..." -ForegroundColor Yellow
if (-not (Test-Path (Join-Path $ExtensionRoot "node_modules"))) {
    npm install
    if ($LASTEXITCODE -ne 0) {
        throw "npm install failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ npm dependencies installed" -ForegroundColor Green
} else {
    Write-Host "✓ npm dependencies already installed" -ForegroundColor Green
}
Write-Host ""

# Step 4: Compile the TypeScript extension.

Write-Host "Step 3: Compiling TypeScript extension..." -ForegroundColor Yellow
npm run compile
if ($LASTEXITCODE -ne 0) {
    throw "TypeScript compilation failed with exit code $LASTEXITCODE"
}

Write-Host "✓ TypeScript extension compiled successfully" -ForegroundColor Green
Write-Host ""

# Success!

Write-Host "🎉 Build completed successfully!" -ForegroundColor Green
Write-Host "Extension is ready for testing or packaging." -ForegroundColor Gray