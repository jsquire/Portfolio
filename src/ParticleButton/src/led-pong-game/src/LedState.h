#include "Direction.h"
#include "HsiColor.h"

#ifndef LedState_H
#define LedState_H

/**
* Represents the current state of the Leds for display.
*/
struct LedState
{
    int       activeLed;
    int       minAllowedLed;
    int       maxAllowedLed;
    int       ledMidPoint;
    Direction activeDirection;
    HsiColor  activeColor;
};

#endif
