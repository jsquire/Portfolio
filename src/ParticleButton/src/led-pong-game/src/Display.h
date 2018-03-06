#include "BetterPhotonButton.h"
#include "Direction.h"
#include "HsiColor.h"
#include "LedState.h"

#ifndef Display_H
#define Display_H

#define MIN_LED  0
#define MAX_LED  10

/**
* The side of the range that an operation is being performed on.  This is relative to
* the manufacturer-specified LED numbers, to avoid ambiguity due to orientation of the device.
*/
enum LedSide
{
    Neither,
    Minimum,
    Maximum
};

/**
* The display artifacts and effects for the LED pong game.
*/
class Display
{
private:
    BetterPhotonButton* button;
    LedState            activeState;
    LedState            initialState;
    HsiColor            unavailableColor;
    float               safeHue;
    float               dangerHue;

public:
    /**
    * Initializes a new intance of the Display class.
    *
    * @param { BetterPhotonButton }  button            - The internet button to use for display manipulation
    * @paam  { int }                 minimumAllowedLed - The index of the minimum available LED for animation; defaults to MIN LED
    * @param { int }                 maximuAllowedLed  - The index of the maximum available LED for animation; defaults to MAX LED
    * @param { float }               safeHue           - The color hue to use when indicating that the LED is in a safe position; defaults to green
    * @param { float }               dangerHue         - The color hue to use when indicating that the LED is in a dangerous position; defaults to red
    * @param { float }               unavailableHue    - The color hue to use when indicating that a LED position is unavailable; defaults to red
    */
    Display(BetterPhotonButton* button,
            int                 minimumAllowedLed = MIN_LED,
            int                 maximuAllowedLed  = MAX_LED,
            float               safeHue           = 120,
            float               dangerHue         = 0,
            float               unavailbleHue     = 0);

    /**
    * Performs a tick of the LED animation, equivilent to advancing a frame.  Note that no delay
    * will be applied.  Any timing adjustment is the purview of the caller.
    *
    * @returns { bool } true if the advance was successful; otherwise, false if the minimum/maximum allowed LED was violated
    */
    bool tickLedAdvance();

    /**
    * Reverses the direction of the LED animation, to be applied when next a tick is
    * performed.
    */
    void reverseLedDirection();

    /**
    * Performs a tick of the LED animation for demonstrating a loss.  Note that no delay
    * will be applied.  Any timing adjustment is purview of the caller.
    *
    * @param { LedSide } side      - Indicates the side of the range to reduce; if set to Neither, the animation has no effect
    * @param { int }     tickCount - The current tick count for the animation
    */
    void tickLossDisplayAnimation(LedSide side,
                                  int     tickCount);

    /**
    * Sets the LED state to indicate a winner.  Note that there is no animation for this state.
    *
    * @param { LedSide } side      - Indicates the side of the range to reduce; if set to Neither, the animation has no effect
    */
    void activateWinDisplay(LedSide side);

    /**
    * Allows the current LED state to be retrieved.
    *
    * @returns { LedState } The current state of the LED display
    */
    LedState getLedState();

    /**
    * Reduces the available range of LEDs legal for animation by one unit.
    *
    * @param { LedSide } side - Indicates the side of the range to reduce; defaults to Neither, which determines the side based on the current animation direction
    */
    bool reduceAvailableLeds(LedSide side = LedSide::Neither);

    /**
    * Determines which side of the device the active LED is on.
    *
    * @param { LedState } ledState - The current state of the LED
    *
    * @returns { LedSide } The side that the LED is currently on
    */
    LedSide determineLedSide(LedState ledState);

    /**
    * Resets the state of the display.
    */
    void reset();

    /**
    * Clears all LEDs, returning them to an "off" state.
    */
    void clearLeds();
};

#endif
