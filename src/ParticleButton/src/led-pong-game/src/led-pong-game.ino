#include <math.h>
#include "BetterPhotonButton.h"
#include "HsiColor.h"
#include "Display.h"
#include "Audio.h"

// Type definitions

enum ButtonPosition
{
    Top    = 0,
    Right  = 1,
    Bottom = 2,
    Left   = 3
};

enum Activity
{
    Idle,
    Interactive,
    LossNotification,
    WinNotification
};

struct GameState
{
    Activity activity;
    int      activityTickCount;
    int      ticksPerInteractiveMove;
    int      ticksPerLossNotification;
};

// Constants

static const int LOOP_DELAY = 15;

// Globals

auto internetButton = BetterPhotonButton();
auto display        = new Display(&internetButton);
auto audio          = new Audio(&internetButton);
auto gameState      = GameState { Activity::Idle, 0, 10, 25 };

// Function signatures

void buttonHandler(int button, bool pressed);

/**
* This function runs once, when the device is flashed or powered-on.  It is intended
* to allow for initialization.
*/
void setup()
{
    internetButton.setup();
    internetButton.setReleasedHandler(&buttonHandler);
    display->clearLeds();

    Serial.begin();
}

/**
* This function runs continuously, as quickly as it can be executed.  This is the main loop where
* any program logic should live.
*/
void loop()
{
    switch (gameState.activity)
    {
        case Activity::Interactive:
            // Attempt to advance the LED animation if enough ticks have elapsed since we last did so.

            if (++gameState.activityTickCount == gameState.ticksPerInteractiveMove)
            {
                gameState.activityTickCount = 0;

                // If the LED animiation cannot advance, then it "hit" the limit and the player for that
                // side loses a LED position.

                if (!display->tickLedAdvance())
                {
                    // If we can no longer reduce the number of available LEDs, then the current player has lost the
                    // game.  Advance the game state to showing the loss notification.

                    if (!display->reduceAvailableLeds())
                    {
                        gameState.activity          = Activity::LossNotification;
                        gameState.activityTickCount = 0;
                    }
                    else
                    {
                        display->reverseLedDirection();
                    }
                }
            }

            break;

        case Activity::LossNotification:
            // If the state just transitioned, play the loss sound
            // effect.

            if (gameState.activityTickCount)
            {
                audio->playLossEffect();
            }

            // If there are ticks remaining on the loss notification, display it;  otherwise,
            // transition to the win notification.

            if (++gameState.activityTickCount <= gameState.ticksPerLossNotification)
            {
                auto side = display->determineLedSide(display->getLedState());
                display->tickLossDisplayAnimation(side, gameState.activityTickCount);
            }
            else
            {
                gameState.activity          = Activity::WinNotification;
                gameState.activityTickCount = 0;
            }

            break;

        case Activity::WinNotification:
            // Display the Win notification

            if (gameState.activityTickCount == 0)
            {
                // The current side that will be determined is the side that was last unaable to
                // reduce the LEDs - in other words the losing side.  This needs to be reversed to identify
                // the winner.

                auto side = display->determineLedSide(display->getLedState());
                side = (side == LedSide::Minimum) ? LedSide::Maximum : LedSide::Minimum;

                ++gameState.activityTickCount;
                display->activateWinDisplay(side);

            }
            else if (gameState.activityTickCount == 1)
            {
                // The audio needs an extra tick to ensure that the loss effect has cleared.
                // Run the win effect on a tick count of 1 to be sure.
                
                ++gameState.activityTickCount;
                audio->playWinEffect();
            }

            break;

        default:
            // No activity is current. I haven't been able to figure out why, but if we don't do
            // this for at least two loops at the outset, LED 1 wants to light up green before we
            // do any animation.  Makes sense to keep forcing it off per-loop when the game state
            // isn't actively updating LEDs.
            //
            // ¯\_(ツ)_/¯
            //
            display->clearLeds();
            break;
    }

    delay(LOOP_DELAY);
    internetButton.update(millis());
}

/**
* This function is responsible for interpreting the button presses captured by
* the device and taking the appopriate response action.
*
* @param { int }  button  - The index of the button that the event was captured for
* @param { bool } pressed - true if the button is currently in the "pressed" state; otherwise, false
*/
void buttonHandler(int  button,
                   bool pressed)
{
    // Button positions are determined based on the USB port being oriented to the "Top" of the button.  If the top
    // or bottom buttons were pressed, then toggle or stop the game.

    switch (button)
    {
        case ButtonPosition::Top:
            gameState.activity = (gameState.activity == Activity::Idle) ? Activity::Interactive : Activity::Idle;

            if (gameState.activity == Activity::Interactive)
            {
                audio->stopAll();
                display->reset();
            }

            return;

        case ButtonPosition::Bottom:
            gameState.activity =  Activity::Idle;
            audio->stopAll();
            display->clearLeds();
            return;

    }

    // If one of the side buttons was pressed, then a ping of the LED was intended;  ensure that the LED is in the
    // proper hemisphere and moving in the proper direction for the button that was pressed.  If so, then consider it
    // a ping and reverse the LED direction.

    auto state = display->getLedState();
    auto side  = display->determineLedSide(state);

    if (((button == ButtonPosition::Right) && (side == LedSide::Minimum) && (state.activeDirection == Direction::Backward)) ||
        ((button == ButtonPosition::Left)  && (side == LedSide::Maximum) && (state.activeDirection == Direction::Forward)))
    {
        audio->playPingEffect();
        display->reverseLedDirection();
    }
}
