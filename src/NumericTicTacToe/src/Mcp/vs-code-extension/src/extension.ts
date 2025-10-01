import * as vscode from 'vscode';
import * as path from 'path';

/**
 * Activates the Numeric Tic-Tac-Toe MCP extension.
 */
export function activate(context: vscode.ExtensionContext): void {
    console.log('Activating Numeric Tic-Tac-Toe MCP extension');

    // Create the provider instance
    const provider = new NumericTicTacToeMcpProvider(context);

    // Register the MCP server definition provider
    const mcpProvider = vscode.lm.registerMcpServerDefinitionProvider(
        'numeric-tic-tac-toe-provider',
        provider
    );

    context.subscriptions.push(mcpProvider);

    // Notify that MCP servers are now available to trigger discovery refresh
    setTimeout(() => {
        provider.notifyServersChanged();
    }, 100);

    console.log('Numeric Tic-Tac-Toe MCP extension activated successfully');
}

/**
 * Deactivates the extension and performs cleanup.
 */
export function deactivate(): void {
    console.log('Deactivating Numeric Tic-Tac-Toe MCP extension');
}

/**
 * MCP server definition provider for Numeric Tic-Tac-Toe.
 */
class NumericTicTacToeMcpProvider implements vscode.McpServerDefinitionProvider {
    private readonly context: vscode.ExtensionContext;
    private readonly _onDidChangeMcpServerDefinitions = new vscode.EventEmitter<void>();

    constructor(context: vscode.ExtensionContext) {
        this.context = context;
    }

    /**
     * Event fired when MCP server definitions change.
     */
    readonly onDidChangeMcpServerDefinitions = this._onDidChangeMcpServerDefinitions.event;

    /**
     * Triggers the change event to notify VS Code that servers are available.
     */
    public notifyServersChanged(): void {
        this._onDidChangeMcpServerDefinitions.fire();
    }

    /**
     * Provides the list of MCP server definitions.
     */
    async provideMcpServerDefinitions(): Promise<vscode.McpServerDefinition[]> {
        const serverPath = path.join(this.context.extensionPath, 'dist', 'mcp-server', 'NumTic.Mcp.exe');

        const serverDefinition = new vscode.McpStdioServerDefinition(
            'Numeric Tic-Tac-Toe',
            serverPath,
            [], // args
            {}, // env
            '1.0.0' // version
        );

        return [serverDefinition];
    }

    /**
     * Resolves additional configuration for an MCP server definition.
     */
    async resolveMcpServerDefinition(
        serverDefinition: vscode.McpServerDefinition
    ): Promise<vscode.McpServerDefinition> {
        // No additional resolution needed for this server
        return serverDefinition;
    }
}
