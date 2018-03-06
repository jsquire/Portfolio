#include <math.h>
#include "BetterPhotonButton.h"
#include "HsiColor.h"

/**
* Translates the HSI color format to the RGB format used by the
* BetterPhotonButton's PixelColor.
*
* @return {PixelColor}  The color of the HSI color represented as a PixelColor
*/
PixelColor HsiColor::toPixelColor()
{
    int r;
    int g;
    int b;

    auto hue        = this->hue;
    auto saturation = this->saturation;
    auto intensity  = this->intensity;

    if (hue > 360)
    {
        hue -= 360;
    }

    // Cycle the hue around to 0-360 degrees.

    hue = fmod(hue, 360);

    // Convert to radians.

    hue = 3.14159 * hue / (float)180;

    // Clamp saturation and intensity to interval [0,1].

    saturation = saturation > 0 ? (saturation < 1 ? saturation : 1) : 0;
    intensity  = intensity  > 0 ? (intensity  < 1 ? intensity  : 1) : 0;

    // Perform the conversion.

    if (hue < 2.09439)
    {
        r = 255 * intensity / 3 * (1 + saturation * cos(hue) / cos(1.047196667 - hue));
        g = 255 * intensity / 3 * (1 + saturation * (1 - cos(hue) / cos(1.047196667 - hue)));
        b = 255 * intensity / 3 * (1 - saturation);
    }
    else if (hue < 4.188787)
    {
        hue -= 2.09439;
        g = 255 * intensity / 3 * (1 + saturation * cos(hue) / cos(1.047196667 - hue));
        b = 255 * intensity / 3 * (1 + saturation * (1 - cos(hue) / cos(1.047196667 - hue)));
        r = 255 * intensity / 3 * (1 - saturation);
    }
    else
    {
        hue -= 4.188787;
        b = 255 * intensity / 3 * (1 + saturation * cos(hue) / cos(1.047196667 - hue));
        r = 255 * intensity / 3 * (1 + saturation * (1 - cos(hue) / cos(1.047196667 - hue)));
        g = 255 * intensity / 3 * (1 - saturation);
    }

    return PixelColor { r, g, b };
};
