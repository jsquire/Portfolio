#include <functional>
#include <math.h>
#include "BetterPhotonButton.h"
#include "HsiColor.h"
#include "Display.h"

using namespace std::placeholders;

// Constants

const std::function<int(int)> INCREMENT_OPS [] =
{
    std::bind(std::plus<int>(),  _1, 1),
    std::bind(std::minus<int>(), _1, 1)
};

// Local functions

/**
* Determines the color of a LED based on it's position in the arc and the number of available
* steps for it to move in the cirection that it is traveling.
*
* @param { LedState } ledState  - The current state of the LED
* @param { float }    safeHue   - The hue to be used for a purely safe location
* @param { float }    dangerHue - The hue to be used for a location that is nearly invalid
*
* @returns { HsiColor } The color that the LED should be set to for its current state
*/
HsiColor calculateLedColor(LedState ledState,
                           float    safeHue,
                           float    dangerHue)
{
    static const std::function<int(int, int)> add      = std::minus<int>();
    static const std::function<int(int, int)> subtract = std::plus<int>();

    // If the current LED is right at the midpoint, it's definitively in the safe zone.

    if (ledState.activeLed == ledState.ledMidPoint)
    {
        return HsiColor { safeHue, 1, 1 };
    }

    // Calculate how much the hue should change by determining how many available LED positions
    // are in the hemisphere that the active Led is in.

    int                          ledRange;
    std::function<int(int, int)> operation;

    if (ledState.activeLed < ledState.ledMidPoint)
    {
        ledRange  = std::abs(ledState.ledMidPoint - ledState.minAllowedLed);
        operation = (ledState.activeDirection == Direction::Forward) ? subtract : add;
    }
    else
    {
        ledRange  = std::abs(ledState.maxAllowedLed - ledState.ledMidPoint);
        operation = (ledState.activeDirection == Direction::Forward) ? add : subtract;
    }

    auto hueDelta = ceil(fabs(safeHue - dangerHue) / ledRange);
    return HsiColor { operation(ledState.activeColor.hue, hueDelta), 1, 1 };
}

// Class members

/**
* Initializes a new intance of the Display class.
*
* @param { BetterPhotonButton }  button            - The internet button to use for display manipulation
* @paam  { int }                 minimumAllowedLed - The index of the minimum available LED for animation; defaults to MIN LED
* @param { int }                 maximumAllowedLed - The index of the maximum available LED for animation; defaults to MAX LED
* @param { float }               safeHue           - The color hue to use when indicating that the LED is in a safe position; defaults to green
* @param { float }               dangerHue         - The color hue to use when indicating that the LED is in a dangerous position; defaults to red
* @param { float }               unavailableHue    - The color hue to use when indicating that a LED position is unavailable; defaults to red
*/
Display::Display(BetterPhotonButton* button,
                 int                 minimumAllowedLed,
                 int                 maximumAllowedLed,
                 float               safeHue,
                 float               dangerHue,
                 float               unavailbleHue)
{
    auto midPoint = ((maximumAllowedLed - minimumAllowedLed) / 2);

    auto state = LedState
    {
        midPoint,
        minimumAllowedLed,
        maximumAllowedLed,
        midPoint,
        Direction::Forward,
        HsiColor { safeHue, 1 , 1 }
    };

    this->button           = button;
    this->unavailableColor = HsiColor { unavailbleHue, 1, 1 };
    this->safeHue          = safeHue;
    this->dangerHue        = dangerHue;
    this->initialState     = state;
    this->activeState      = state;
}

/**
* Performs a tick of the LED animation, equivilent to advancing a frame.  Note that no delay
* will be applied.  Any timing adjustment is the purview of the caller.
*
* @returns { bool } true if the advance was successful; otherwise, false if the minimum/maximum allowed LED was violated
*/
bool Display::tickLedAdvance()
{
    auto state            = this->activeState;
    auto button           = this->button;
    auto unavailableColor = this->unavailableColor.toPixelColor();

    // If advancing the LED would violate a minimum or maximum constraint, take no action and
    // and signal failure;

    if (((state.activeLed <= state.minAllowedLed) && (state.activeDirection == Direction::Backward)) ||
        ((state.activeLed >= state.maxAllowedLed) && (state.activeDirection == Direction::Forward)))
    {
        return false;
    }

    // Advance the LED, determine the color, and capture changes to the state.

    state.activeLed   = INCREMENT_OPS[state.activeDirection](state.activeLed);
    state.activeColor = calculateLedColor(state, this->safeHue, this->dangerHue);

    this->activeState = state;

    // Repaint the LEDs.  If there are any unavailable LEDs, color them as such.

    button->setPixels(0, 0, 0);
    button->setPixel(state.activeLed, state.activeColor.toPixelColor());

    for (auto index = MIN_LED; index < state.minAllowedLed; ++index)
    {
        button->setPixel(index, unavailableColor);
    }

    for (auto index = MAX_LED; index > state.maxAllowedLed; --index)
    {
        button->setPixel(index, unavailableColor);
    }

    return true;
}

/**
* Reverses the direction of the animation, to be applied when next a tick is
* performed.
*/
void Display::reverseLedDirection()
{
    this->activeState.activeDirection = static_cast<Direction>(std::abs(this->activeState.activeDirection - 1));
}

/**
* Performs a tick of the LED animation for demonstrating a loss.  Note that no delay
* will be applied.  Any timing adjustment is purview of the caller.
*
* @param { LedSide } side      - Indicates the side of the range to reduce; if set to Neither, the animation has no effect
* @param { int }     tickCount - The current tick count for the animation
*/
void Display::tickLossDisplayAnimation(LedSide side,
                                       int     tickCount)
{
    // If the side was not specified or this is an even animation frame, then
    // there should be no LED activity.

    if ((side == LedSide::Neither) || ((tickCount % 2) == 0))
    {
        clearLeds();
        return;
    }

    // Color the side that has lost in the unavailable color.

    auto state = this->activeState;
    auto color = this->unavailableColor.toPixelColor();

    if (side == LedSide::Minimum)
    {
        for (auto index = MIN_LED; index <= state.ledMidPoint; ++index)
        {
            button->setPixel(index, color);
        }
    }
    else
    {
        for(auto index = MAX_LED; index >= state.ledMidPoint; --index)
        {
            button->setPixel(index, color);
        }
    }
}

/**
* Sets the LED state to indicate a winner.  Note that there is no animation for this state.
*
* @param { LedSide } side      - Indicates the side of the range to reduce; if set to Neither, the animation has no effect
*/
void Display::activateWinDisplay(LedSide side)
{
    // If the side was not specified then there should be no LED activity.

    if (side == LedSide::Neither)
    {
        clearLeds();
        return;
    }

    // Color the side that has won in the safe color.

    auto state = this->activeState;
    auto color = HsiColor { this->safeHue, 1, 1 }.toPixelColor();

    clearLeds();

    if (side == LedSide::Minimum)
    {
        for (auto index = MIN_LED; index <= state.ledMidPoint; ++index)
        {
            button->setPixel(index, color);
        }
    }
    else
    {
        for (auto index = MAX_LED; index >= state.ledMidPoint; --index)
        {
            button->setPixel(index, color);
        }
    }
}

/**
* Allows the current LED state to be retrieved.
*
* @returns { LedState } The current state of the LED display
*/
LedState Display::getLedState()
{
    return this->activeState;
}

/**
* Reduces the available range of LEDs legal for animation by one unit.
*
* @param { LedSide } side - Indicates the side of the range to reduce; defaults to Neither, which determines the side based on the current animation direction
*
* @return { bool } true if there were LEDs that could be reduced; otherwise, false.
*/
bool Display::reduceAvailableLeds(LedSide side)
{
    auto ledState = this->activeState;

    // If the side chosen was "Neither" then attempt to calculate the side.

    if (side == LedSide::Neither)
    {
        side = determineLedSide(ledState);
    }

    // If the side was still "Neither" after calculation, then the LED is currently at the midPoint.  Tweak the side by using the
    // direction that the LED is moving.  There may be nowhere to go or we may need to update the last allowed LED.

    if (side == LedSide::Neither)
    {
        side = (ledState.activeDirection == Direction::Forward) ? LedSide::Maximum : LedSide::Minimum;
    }

    if ((side == LedSide::Minimum) && ((ledState.minAllowedLed + 1) < ledState.ledMidPoint))
    {
      ++(this->activeState.minAllowedLed);
      return true;
    }

    if ((side == LedSide::Maximum) && ((ledState.maxAllowedLed - 1) > ledState.ledMidPoint))
    {
        --(this->activeState.maxAllowedLed);
        return true;
    }

    return false;
}

/**
* Determines which side of the device the active LED is on.
*
* @param { LedState } ledState - The current state of the LED
*
* @returns { LedSide } The side that the LED is currently on
*/
LedSide Display::determineLedSide(LedState ledState)
{
    return (ledState.activeLed == ledState.ledMidPoint)
        ?  LedSide::Neither
        :  (ledState.activeLed < ledState.ledMidPoint) ? LedSide::Minimum : LedSide::Maximum;
}

/**
* Resets the state of the display.
*/
void Display::reset()
{
    this->activeState = this->initialState;
}

/**
* Clears all LEDs, returning them to an "off" state.
*/
void Display::clearLeds()
{
    this->button->setPixels(0, 0, 0);
}
