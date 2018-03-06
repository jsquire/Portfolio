#include "BetterPhotonButton.h"

#ifndef HsiColor_H
#define HsiColor_H

/**
* Allows a color to be specified in the form of a hue, saturation, and intensity.
*/
struct HsiColor
{
    float hue;
    float saturation;
    float intensity;

    /**
    * Translates the HSI color format to the RGB format used by the
    * BetterPhotonButton's PixelColor.
    *
    * @return {PixelColor}  The color of the HSI color represented as a PixelColor
    */
    PixelColor toPixelColor();
};

#endif
