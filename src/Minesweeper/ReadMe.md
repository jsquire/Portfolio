# Minesweeper #

### Summary ###

An implementation of the classic game Minesweeper in TypeScript with a web front end.  The game of Minesweeper presents a board of tiles, each of which may or may not contain a hidden mine.  The object of the game is to uncover every tile that does not contain a mine, while leaving those that do covered.  The player navigates the board, choosing to uncover tiles, mark a covered tile as a suspected mine, or remove a previously applied mark.  

If the player chooses to uncover a tile that contains a mine, the game immediately ends and the player is considered to have lost.  If the player uncovers a tile that does not hide a mine, the number of adjacent tiles (including diagonals) that are hiding a mine will be revealed in the tile.  Should there be no hidden mines adjacent to the tile being uncovered, each adjacent tile will be automatically uncovered, following the same rules.

The game ends when a player has uncovered all tiles that do not hide mines, or has requested to validate that all hidden mines have been correctly marked as such.  Should the validation prove that all hidden mines have been marked by the player, the game ends in victory; otherwise, the game ends in failure.

My goal was to use the project as a vehicle for learning TypeScript and AngularJS.  

### Structure ###

* **src**
  <br />_The container for project source code._
  
* **lib**
  <br />_The container for external libraries referenced by the project._
  
* **web**
  <br />_The container for project build output intended for use as a directory with static web content that can be loaded from the file system or served by an HTTP server._

