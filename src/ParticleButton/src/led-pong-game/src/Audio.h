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
    bool                stopRequested;
    int                 winToneCurrent;

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
    *
    * @returns { bool } true if there are more notes to play; otherwise, false.
    */
    bool playPingEffect();

    /**
    * Plays the sound effect for when the game is lost.
    *
    * @returns { bool } true if there are more notes to play; otherwise, false.
    */
    bool playLossEffect();

    /**
    * Plays the sound effect for when the game is won.
    *
    * @returns { bool } true if there are more notes to play; otherwise, false.
    */
    bool playWinEffect();

    /**
    * Stops playing any and all sound effects.
    */
    void stopAll();
};

#endif
