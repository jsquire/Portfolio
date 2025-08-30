using Spectre.Console;

namespace Squire.NumTic.Console;

/// <summary>
///   Handles the display of the splash screen and collection of initial game settings.
/// </summary>
///
public static class SplashScreen
{
    /// <summary>
    ///   Displays the splash screen with game rules and collects player preferences.
    /// </summary>
    ///
    /// <param name="console">The console instance for rendering. If null, uses AnsiConsole.Console.</param>
    ///
    /// <returns>A tuple containing the selected player token and difficulty level.</returns>
    ///
    public static (PlayerToken PlayerToken, Difficulty Difficulty) Show(IAnsiConsole? console = null)
    {
        var ansiConsole = console ?? AnsiConsole.Console;

        // Clear the screen.

        ansiConsole.Clear();

        // Display the banner.

        var banner = new FigletText("NUMERIC TIC-TAC-TOE")
            .LeftJustified()
            .Color(Color.Blue);

        ansiConsole.Write(banner);
        ansiConsole.WriteLine();

        // Display game rules.

        var rulesPanel = new Panel(
            new Markup("""
                - Players take turns placing numbered tokens on a 3×3 grid
                - [bold cyan]Odd Player[/] uses numbers: 1, 3, 5, 7, 9
                - [bold magenta]Even Player[/] uses numbers: 2, 4, 6, 8
                - Each number can only be used once in the game
                - Win by getting three tokens in a row that sum to [bold green]15[/]
                - Rows, columns, and diagonals all count
                - Use position numbers 1-9 to place tokens

                [dim]Good luck and have fun![/]
             """))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(3, 1),
            Header = new PanelHeader("Game Rules")
        };

        ansiConsole.Write(rulesPanel);
        ansiConsole.WriteLine();

        // Get player token choice.

        var playerToken = ansiConsole.Prompt(
            new SelectionPrompt<PlayerToken>()
                .Title("[bold white]Choose your tokens:[/]")
                .AddChoices(PlayerToken.Odd, PlayerToken.Even)
                .UseConverter(token => token switch
                {
                    PlayerToken.Odd => "Odd Numbers (1, 3, 5, 7, 9)",
                    PlayerToken.Even => "Even Numbers (2, 4, 6, 8)",
                    _ => token.ToString()
                })
        );

        ansiConsole.WriteLine();

        // Get difficulty choice.

        var difficulty = ansiConsole.Prompt(
            new SelectionPrompt<Difficulty>()
                .Title("[bold white]Choose difficulty level:[/]")
                .AddChoices(Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Perfect)
                .UseConverter(diff => diff switch
                {
                    Difficulty.Easy => "Easy - Bot makes random moves often",
                    Difficulty.Medium => "Medium - Bot uses moderate strategy",
                    Difficulty.Hard => "Hard - Bot uses advanced strategy",
                    Difficulty.Perfect => "Perfect - Bot plays optimally",
                    _ => diff.ToString()
                })
        );

        ansiConsole.WriteLine();
        ansiConsole.Write(new Markup("[green]Starting game...[/]"));
        ansiConsole.WriteLine();

        return (playerToken, difficulty);
    }
}
