# Competitive Action Multiplayer template

The Competitive Action Multiplayer game contains a first-person-shooter (FPS) game loop where you can fire a weapon at other players, die, re-spawn, and restart.

Use this template’s assets and structure as a starting point to build your own game.

## Key bindings

The key bindings in this project change depending on the scene you are currently in. This template supports key bindings on desktop and mobile devices, but some inputs only work on desktop platforms.

### Any scene

| Action                                | Keyboard input | Mobile input     |
|---------------------------------------|----------------|------------------|
| Toggle the session information banner | I              | Three-finger tap |

### Key bindings in the GameScene

| Action                         | Keyboard input     | Mobile input                  |
|--------------------------------|--------------------|-------------------------------|
| Move                           | W, A, S, D         | Left virtual joystick         |
| Look around                    | Move Mouse         | Right virtual joystick        |
| Jump                           | Space              | Left virtual Jump button      |
| Aim                            | Right mouse button | Left virtual aim button       |
| Shoot                          | Left mouse button  | Left virtual Shoot button     |
| Display or hide the pause menu | Esc                | Left upper corner Menu button |

### Key bindings in the GameScene (desktop only)

| Action                   | Keyboard input |
|--------------------------|----------------|
| Destroy player character | K              |
| Return to Main Menu      | F1             |

### Key bindings in the MainMenu scene (desktop only)

| Action      | Keyboard input |
|-------------|----------------|
| Quit        | F1             |
| Host a game | 1              |
| Join a game | 2              |

## Session info
The ping value is being calculated with an EstimatedRTT (see documentation: https://docs.unity3d.com/Packages/com.unity.netcode@1.3/api/Unity.NetCode.NetworkSnapshotAck.html#Unity_NetCode_NetworkSnapshotAck_EstimatedRTT). 
It can therefore increase due to network latency, tick rate and the Transport processing time.
