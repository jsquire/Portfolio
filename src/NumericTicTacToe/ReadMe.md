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

### Experiments and Exploration

Beyond the game itself, this project also serves as a playground for trying out new ideas and stretching into areas that Jesse doesn’t always get to explore in day-to-day work.  It provides a platform to test concepts, benchmark patterns, and have a bit of fun exploring:

- **Efficiency Patterns in .NET**  
  Experimenting with modern low-allocation techniques and performance-oriented coding.  Even when the practical gains are small, it is rewarding to explore how far efficiency can be pushed while still balancing clarity and maintainability.

- **Benchmarking Design Patterns**  
  Comparing design approaches through benchmarks to see how they hold up in practice.  The focus is less on squeezing out every last microsecond and more on understanding where different patterns are most effective.

- **AI-Assisted Development**  
  Using AI tools as a partner in the process. Most of the code is still handwritten, but AI has been helpful as a "rubber duck," for brainstorming ideas, identifying potential improvements, and generating much of the test coverage.  It has also been a chance to explore instruction-tuning and automation files.

- **AI Agent Players**  
  Trying out AI services as autonomous game players, more for curiosity than competitive edge.  This creates an opportunity to see how well agent-driven decision making compares to traditional algorithmic strategies.

- **Exploring UI Platforms**  
  After several years of SDK library work, this has been a chance to experiment with frameworks like Spectre.Console, .NET MAUI, and Godot to see how the same game feels across different environments.

This combination of efficiency work, pattern exploration, AI experimentation, and UI tinkering makes the project both a professional exercise and an enjoyable space for creative engineering.
