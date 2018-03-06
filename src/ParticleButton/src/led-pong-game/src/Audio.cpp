#include "BetterPhotonButton.h"
#include "Audio.h"

/**
* Initializes a new intance of the Display class.
*
* @param { BetterPhotonButton }  button - The internet button to use for emitting audio effects
*/
Audio::Audio(BetterPhotonButton* button)
{
    this->button = button;
}

/**
* Plays the sound effect for when the LED is "pinged" back in the other
* direction.
*/
void Audio::playPingEffect()
{
    // The API offered by the button library makes is difficult to do a short tone,
    // so drop to the Partcle firmware for direct access.

    tone(BUZZER_PHOTON_PIN, 254, 15);
}

/**
* Plays the sound effect for when the game is lost.
*/
void Audio::playLossEffect()
{
    // The API offered by the button library makes is difficult to do a short tone,
    // so drop to the Partcle firmware for direct access.

    tone(BUZZER_PHOTON_PIN, 54, 50);
}

/**
* Plays the sound effect for when the game is won.
*/
void Audio::playWinEffect()
{
    // Notes for the winnning music gratefully borrowed from:
    //    https://www.reddit.com/r/arduino/comments/10l2pk/question_about_creating_a_melody_with_a_small/

    this->button->stopPlayingNotes();
    this->button->playNotes("");
    this->button->playNotes("d=4,o=6,b=125:2c5,d_5,2g5,d_5,2f5");
}

/**
* Stops playing any and all sound effects.
*/
void Audio::stopAll()
{
    this->button->stopPlayingNotes();
}
