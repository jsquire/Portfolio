#include "BetterPhotonButton.h"
#include "Audio.h"
#include <application.h>

// Constants

static const int WIN_EFFECT_NOTES [] =
{
    0,
    50,
    100,
    200,
    400,
    800
};

static const int WIN_EFFECT_NOTES_LENGTH = (sizeof(WIN_EFFECT_NOTES) / sizeof(*WIN_EFFECT_NOTES));

/**
* Initializes a new intance of the Display class.
*
* @param { BetterPhotonButton }  button - The internet button to use for emitting audio effects
*/
Audio::Audio(BetterPhotonButton* button)
{
    this->button         = button;
    this->stopRequested  = false;
    this->winToneCurrent = 0;
}

/**
* Plays the sound effect for when the LED is "pinged" back in the other
* direction.
*/
bool Audio::playPingEffect()
{
    // The API offered by the button library makes is difficult to do a short tone,
    // so drop to the Partcle firmware for direct access.

    this->stopRequested = false;
    tone(BUZZER_PHOTON_PIN, 254, 15);

    return false;
}

/**
* Plays the sound effect for when the game is lost.
*/
bool Audio::playLossEffect()
{
    // The API offered by the button library makes is difficult to do a short tone,
    // so drop to the Partcle firmware for direct access.

    this->stopRequested = false;
    tone(BUZZER_PHOTON_PIN, 54, 50);

    return false;
}

/**
* Plays the sound effect for when the game is won.
*/
bool Audio::playWinEffect()
{
    if (this->winToneCurrent == 0)
    {
        this->stopRequested = false;
    }

    if ((!this->stopRequested) && (this->winToneCurrent < (WIN_EFFECT_NOTES_LENGTH * 4)))
    {
        tone(BUZZER_PHOTON_PIN, WIN_EFFECT_NOTES[(this->winToneCurrent % WIN_EFFECT_NOTES_LENGTH)], 50);
        ++(this->winToneCurrent);

        return true;
    }

    this->stopRequested  = false;
    this->winToneCurrent = 0;

    return false;
}

/**
* Stops playing any and all sound effects.
*/
void Audio::stopAll()
{
    this->stopRequested = true;
}
