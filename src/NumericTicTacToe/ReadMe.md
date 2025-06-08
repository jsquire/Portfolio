# Numeric Tic-Tac-Toe

### Summary

Numeric Tic-Tac-Toe is a strategic variant of the classic Tic-Tac-Toe game that replaces traditional X's and O's with numbers. This mathematical twist on the timeless game was developed to add a layer of numerical strategy and mental arithmetic to the familiar 3x3 grid format.

The game originated as an educational tool to help students practice basic arithmetic while engaging in strategic thinking. Unlike traditional Tic-Tac-Toe where the objective is simply to get three symbols in a row, Numeric Tic-Tac-Toe introduces mathematical constraints that require players to think several moves ahead while considering numerical relationships.

Jesse was introduced to this variant in 1998 as part of a college assignment and wrote a version in the Ada programming language.  The C# version was originally authored in 2025 for fun and as an experiment with allowing extensible rendering and opponent implementation.

### Rules

The game is played on a standard 3x3 grid with the following rules:

1. **Player Assignment**: One player uses odd numbers (1, 3, 5, 7, 9) and the other uses even numbers (2, 4, 6, 8).

2. **Gameplay**: Players take turns placing their numbers on empty squares of the grid.

3. **Number Usage**: Each number can only be used once during the game.

4. **Winning Condition**: Instead of getting three identical symbols in a row, a player wins by creating a line (horizontal, vertical, or diagonal) where the three numbers sum to exactly 15.

5. **Strategic Element**: Since players have limited numbers and must reach the target sum of 15, careful planning is required to both create winning opportunities and block opponents.

6. **Draw Condition**: If all squares are filled and no player has achieved a sum of 15 in any line, the game is a draw.

This numerical approach transforms the simple strategy of traditional Tic-Tac-Toe into a more complex game requiring mathematical reasoning and forward planning.

### Structure

- **src**
 _The container for the project implementation._

- **tests**
  _The container for the project tests._

- **NumericTicTacToe.sln**
  _The Visual Studio solution file for the project._