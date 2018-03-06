#include "BetterPhotonButton.h"

#ifndef Audio_H
#define Audio_H

#define BUZZER_PHOTON_PIN D0

/**
* The audio artifacts and effects for the LED pong game.
*/
class Audio
{
private:
    BetterPhotonButton* button;

public:
    /**
    * Initializes a new intance of the Display class.
    *
    * @param { BetterPhotonButton }  button - The internet button to use for emitting audio effects
    */
    Audio(BetterPhotonButton* button);

    /**
    * Plays the sound effect for when the LED is "pinged" back in the other
    * direction.
    */
    void playPingEffect();

    /**
    * Plays the sound effect for when the game is lost.
    */
    void playLossEffect();

    /**
    * Plays the sound effect for when the game is won.
    */
    void playWinEffect();

    /**
    * Stops playing any and all sound effects.
    */
    void stopAll();
};

#endif
