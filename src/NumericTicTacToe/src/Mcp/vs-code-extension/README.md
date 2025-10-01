# Numeric Tic-Tac-Toe MCP Extension

This VS Code extension provides MCP (Model Context Protocol) integration for playing Numeric Tic-Tac-Toe with natural language through VS Code's Chat interface.

## Overview

Numeric Tic-Tac-Toe is a strategic variant of classic Tic-Tac-Toe that uses numbers instead of X's and O's. Players use odd (1,3,5,7,9) or even (2,4,6,8) numbers and win by creating lines that sum to exactly 15.

This extension registers an MCP server that allows you to:

- Start new games with natural language commands
- Make moves by describing positions and tokens
- View rich board visualizations with Unicode and color
- Get game status and available moves
- Analyze winning strategies

## Installation

This extension is designed for local development and testing. The build process requires building the C# MCP server before the extension can function properly.

**Key Features:**
- **Simple Installation**: No manual VS Code settings configuration required
- **Self-Contained**: All necessary files are bundled with the extension
- **Automatic Registration**: MCP server is registered through static `package.json` configuration

### Build Order Dependencies

The extension depends on the compiled MCP server executable, which is automatically copied to the extension's `dist/mcp-server` folder during the C# project build process.

**Automated Build (Recommended):**

Use the provided build scripts that handle the complete build process:

```bash
# PowerShell (Windows/Cross-platform)
npm run build                    # Builds with Release configuration
npm run build:release            # Explicitly builds Release
npm run build:debug              # Builds with Debug configuration

# Bash (Linux/macOS/WSL)
npm run build:bash               # Builds with Release configuration  
npm run build:bash:release       # Explicitly builds Release
npm run build:bash:debug         # Builds with Debug configuration

# Direct script execution
./build.ps1                      # PowerShell script
./build.sh                       # Bash script
```

**Manual Build Steps:**

If you prefer to build manually, follow these steps in order:

1. **Build the MCP server first**:

   ```bash
   cd ../..
   dotnet build src/Mcp/NumTic.Mcp.csproj --configuration Release
   ```

   This step is required because the post-build action copies the compiled executable to `dist/mcp-server/NumTic.Mcp.exe`, which the extension references.

2. **Install npm dependencies**:

   ```bash
   npm install
   ```

3. **Compile the extension**:

   ```bash
   npm run compile
   ```

4. **Package the extension**:
   ```bash
   npx @vscode/vsce package
   ```

5. **Install the .vsix file in VS Code**:
   - Open VS Code
   - Go to Extensions view (Ctrl+Shift+X)
   - Click the "..." menu and select "Install from VSIX..."
   - Select the generated .vsix file

### Development Workflow

During development, if you modify the C# MCP server code, rebuild the server project before testing the extension:

```bash
dotnet build src/Mcp/NumTic.Mcp.csproj
```

The post-build action will automatically update the executable in the extension's dist folder.

## Usage

Once installed, the extension automatically registers the Numeric Tic-Tac-Toe MCP server with VS Code through static configuration in `package.json`. **No manual settings.json configuration is required.**

After installing the extension and restarting VS Code, you can immediately use VS Code Chat to interact with the game using natural language commands:

**Starting Games:**
- "Start a new numeric tic-tac-toe game"
- "Begin a new game"

**Making Moves:**
- "Place token 3 at position 1,2"
- "Put 5 in the center"
- "Use odd number 7 in the top left"

**Game Information:**
- "Show me the current board"
- "What moves are available?"
- "Get the current game status"

**Strategy Help:**
- "Analyze the current position"
- "What are my best moves?"

The MCP server handles natural language parsing and provides rich visual feedback through ASCII board representations optimized for the chat interface.

## MCP Tool API Reference

The following tools are available for programmatic interaction:

### start_new_game
- **Parameters**: 
  - `humanPlayerType`: PlayerToken (Odd|Even, default: Odd)
  - `selectedDifficulty`: Difficulty (Easy|Medium|Hard, default: Medium)
- **Example**: `humanPlayerType=Odd, selectedDifficulty=Medium`

### make_move
- **Parameters**:
  - `position`: int (1-9, board position)
  - `token`: byte (Odd: 1,3,5,7,9 | Even: 2,4,6,8)
- **Example**: `position=5, token=3`

### display_board
- **Parameters**: None
- **Description**: Shows the current game state with board visualization

### get_available_tokens  
- **Parameters**:
  - `playerType`: PlayerToken (Odd|Even, default: Odd)
- **Example**: `playerType=Odd`

### list_commands
- **Parameters**: None
- **Description**: Returns detailed information about all available tools and their usage

## Development

To work on this extension during development:

### Setup
1. **Install dependencies**: `npm install`
2. **Start watching mode**: `npm run watch`
3. **Press F5 in VS Code** to launch Extension Development Host
4. **Test the extension** in the new VS Code window

### Development Cycle

When making changes to either the extension or the C# MCP server:

1. **For TypeScript changes**: The watch mode will automatically recompile
2. **For C# MCP server changes**: Use the automated build scripts:
   ```bash
   npm run build               # Full rebuild (recommended)
   ```
   Or manually rebuild just the server:
   ```bash
   dotnet build ../NumTic.Mcp.csproj
   ```
3. **Reload the Extension Development Host** (Ctrl+R) to pick up changes

### Debugging
- Use VS Code's built-in debugger to set breakpoints in the TypeScript extension code
- The C# MCP server can be debugged separately by attaching to its process
- Console output from both the extension and MCP server appears in the Debug Console

## Requirements

- VS Code 1.95.0 or later with MCP support
- .NET 9.0 runtime
- Compiled NumTic.Mcp.exe server

## Architecture

This extension serves as a bridge between VS Code's MCP integration and the Numeric Tic-Tac-Toe MCP server. It:

1. **Static Registration**: Declares the MCP server in `package.json` using the `contributes.mcpServers` configuration
2. **Relative Path Resolution**: Uses `./dist/mcp-server/NumTic.Mcp.exe` relative to the extension directory
3. **Automatic Discovery**: VS Code automatically discovers and connects to the server when the extension is activated
4. **Self-Contained**: The extension includes all necessary server files in its `dist/mcp-server` directory

The actual game logic and MCP protocol implementation are handled by the separate NumTic.Mcp server project, which gets copied into the extension during the build process.

### MCP Server Configuration

The MCP server is configured in `package.json`:

```json
{
  "contributes": {
    "mcpServers": {
      "numeric-tic-tac-toe": {
        "command": "./dist/mcp-server/NumTic.Mcp.exe",
        "args": [],
        "env": {}
      }
    }
  }
}
```

This approach ensures:

- **Portability**: No absolute paths or manual VS Code settings required
- **Simplicity**: No complex registration code needed in the extension
- **Reliability**: Uses VS Code's native MCP server discovery mechanism
