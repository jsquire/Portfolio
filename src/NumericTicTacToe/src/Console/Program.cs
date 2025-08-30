using Squire.NumTic;
using Squire.NumTic.Console;
using Squire.NumTic.Contracts;
using Squire.NumTic.Players;

using var cancellationSource = new CancellationTokenSource();

// Hook up cancellation to the console cancel key press event.

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Cancellation requested.  Shutting down...");
    cancellationSource.Cancel();

    // Prevent the process from terminating immediately.

    eventArgs.Cancel = true;
};

// Show splash screen and get player preferences.

var (selectedPlayerToken, selectedDifficulty) = SplashScreen.Show();

// Create game interface and players based on user choices.

var gameState = GameState.CreateDefault();
var gameInterface = new ConsoleGameInterface(gameState);
var humanPlayer = new ConsolePlayer(gameInterface);
var botOptions = new BotPlayerOptions { Difficulty = selectedDifficulty };
var botPlayer = new BotPlayer(gameInterface, botOptions);

// Assign players based on token selection.

IPlayer oddPlayer;
IPlayer evenPlayer;

switch (selectedPlayerToken)
{
    case PlayerToken.Odd:
        oddPlayer = humanPlayer;
        evenPlayer = botPlayer;
        break;

    case PlayerToken.Even:
        oddPlayer = botPlayer;
        evenPlayer = humanPlayer;
        break;

    default:
        throw new ArgumentOutOfRangeException(nameof(selectedPlayerToken), "Invalid player token selected.");
};


var game = new Game(oddPlayer, evenPlayer, gameInterface, gameState);

try
{
    await game.PlayAsync(cancellationSource.Token);
}
catch (OperationCanceledException)
{
    // Expected cancellation, just exit gracefully.
}