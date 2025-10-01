#!/bin/bash

#
# Builds the Numeric Tic-Tac-Toe MCP extension with dependencies.
#
# This script builds the C# MCP server in Release configuration and then
# compiles the VS Code extension. The C# build automatically copies the
# executable to the extension's dist/mcp-server folder via post-build action.
#
# Usage:
#   ./build.sh [configuration]
#
# Parameters:
#   configuration: Build configuration (Debug|Release). Defaults to Release.
#
# Examples:
#   ./build.sh          # Builds with Release configuration
#   ./build.sh Debug    # Builds with Debug configuration
#

set -e  # Exit on any error

# Parse parameters.

CONFIGURATION="${1:-Release}"

if [[ "$CONFIGURATION" != "Debug" && "$CONFIGURATION" != "Release" ]]; then
    echo "Error: Invalid configuration '$CONFIGURATION'. Must be 'Debug' or 'Release'." >&2
    exit 1
fi

# Determine paths.

EXTENSION_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MCP_PROJECT_PATH="$EXTENSION_ROOT/../NumTic.Mcp.csproj"

echo "Building Numeric Tic-Tac-Toe MCP Extension"
echo "Extension Root: $EXTENSION_ROOT"
echo "MCP Project: $MCP_PROJECT_PATH"
echo ""

# Step 1: Build the C# MCP server.

echo "Step 1: Building C# MCP server ($CONFIGURATION)..."
if [[ ! -f "$MCP_PROJECT_PATH" ]]; then
    echo "Error: MCP project file not found: $MCP_PROJECT_PATH" >&2
    exit 1
fi

dotnet build "$MCP_PROJECT_PATH" --configuration "$CONFIGURATION" --nologo
echo "✓ C# MCP server built successfully"
echo ""

# Step 2: Verify the executable was copied to dist folder.

EXPECTED_EXECUTABLE="$EXTENSION_ROOT/dist/mcp-server/NumTic.Mcp.exe"
if [[ ! -f "$EXPECTED_EXECUTABLE" ]]; then
    echo "Error: MCP server executable not found at expected location: $EXPECTED_EXECUTABLE" >&2
    exit 1
fi

echo "✓ MCP server executable found: $EXPECTED_EXECUTABLE"
echo ""

# Step 3: Install npm dependencies if needed.

echo "Step 2: Installing npm dependencies..."
if [[ ! -d "$EXTENSION_ROOT/node_modules" ]]; then
    npm install
    echo "✓ npm dependencies installed"
else
    echo "✓ npm dependencies already installed"
fi
echo ""

# Step 4: Compile the TypeScript extension.

echo "Step 3: Compiling TypeScript extension..."
npm run compile
echo "✓ TypeScript extension compiled successfully"
echo ""

# Success!

echo "🎉 Build completed successfully!"
echo "Extension is ready for testing or packaging."