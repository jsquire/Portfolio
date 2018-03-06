# LED Pong #

### Overview ###
A basic game for the Particle internet button where the objective is to protect your side of the button from losing LEDs by bouncing the animation back to your opponent's side.  Each time the LED makes it past your defense, you lose a LED until there are none remaining.  

### Playing the Game ###

The game is intended for two local players, each holding one side of the internet button such that they can easily push the button at 3 and 9 o'clock.  The internet button should be oriented with the USB port facing up.  The controls are:

- _**Top button:**_ Starts, resets, or stops a game
- _**Bottom button:**_ Stops a game or clears the previous winner, shutting down the LEDs.
- _**Left button:**_ Pings the LED back at your opponent when it is on your side and moving toward your scoring area
- _**Right button:**_ Pings the LED back at your opponent when it is on your side and moving toward your scoring area  

### Structure ###
* #### ```/src```
  _This is the source folder that contains the firmware files for the game project. It should *not* be renamed.
Anything that is in this folder when compliling will be sent to our Particle cloud service and compiled into a firmware binary for the Particle device that is currently have targeted._

  - #### ```/src/led-pong-game.ino```
_This is the firmware that will run as the primary application, containing the game loop, initialization, and utility functionality._

  - #### ```/src/project.properties```  
_This is the file that specifies the name and version number of the libraries that the game project depends on. This metadata is used by the Particle cloud when compiling the project._

  - #### ```/src/Audio.*```
_These are the class items for audio feedback in the game, responsible for playing sounds._

  - #### ```/src/Display.*```
_These are the classes items for the game UI, responsible for animation and other LED manipulations._

  - #### ```/src/HsiColor.*```
_These are the structures for providing a hue, saturation, and intensity color space and for translation to the RGB format used for the LEDs.  This construct allows for smoother color transitions when animating._

  - #### ```/src/LedState.h```
_This structure provides the current state of the LEDs on the button, used by the display and main game constructs._

  - #### ```/src/Direction.h```
_This structure is used to denote the direction that the LEDs are animating and moving, used by the display and main game constructs._

### Compiling ###

To compile, make sure that you have the correct Particle device target selected and run `particle compile <platform>` in the CLI or click the Compile button in the Desktop IDE. The files in the project folder will be sent to the Particle cloud service and the resulting output flashed to the device.
