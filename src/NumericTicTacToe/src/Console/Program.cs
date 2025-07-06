using Squire.NumTic.Console;
using Squire.NumTic.Game;

using var cancellationSource = new CancellationTokenSource();

// Hook up cancellation to the console cancel key press event.

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Cancellation requested.  Shutting down...");
    cancellationSource.Cancel();

    // Prevent the process from terminating immediately.

    eventArgs.Cancel = true;
};


// Create a game with two console players and the default game board.

var gameInterface = new ConsoleGameInterface();
var oddPlayer = new ConsolePlayer(gameInterface);
var evenPlayer = new ConsolePlayer(gameInterface);
var game = new Game(oddPlayer, evenPlayer, gameInterface);

try
{
    await game.PlayAsync(cancellationSource.Token);
}
catch (OperationCanceledException)
{
  // Expected cancellation, just exit gracefully.
}
