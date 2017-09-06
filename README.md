# QuietVR
> A quiet place in VR.

## What is QuietVR

A place in VR where only you should be the one to make noises. And when you do, your noises summon objects that will decorate your surrounding.

## Inspiration and Motivation

This project is inspired by Armin van Buuren's YouTube video of [Intense](https://www.youtube.com/watch?v=6UoNXz0Ox-g).

The initial thought was to create a simple VR experience where the player is surrounded by blocks and low polygon geometries. After a few iterations of implementation, it eventually turns into VR demo that focuses on audio reactive interaction.

The whole purpose of this project from a development perspective is to seek harmonic correlation between VR and audio input and how to create fun VR gameplay with simple vocal elements.

## Installation

Please go to the release page to download the latest version.

https://github.com/diglungdig/QuietVR/releases

Each release contains a VR version that works with HTC Vive/Oculus Rift and a non-VR version that works with normal PC setups.

Please note that it's recommended to play in VR just like beer has to be put into fridge to be good.

## System Requirement and Specification
  
Window 10 with GTX 970 or higher.

DirectX 11 or higher required.

VR version works with HTC Vive and Ocules Rift CV1.

Audio recording device is required in order to play QuietVR.

## How to Play
**QuietVR requires a quiet environment to start with.** 

### Setup
If you are not in a peaceful place, gameplay will most likely be disrupted for game's solely relying on audio input. 

Make sure you have your audio recording device like microphone set up before start the game.

### Game Mechanics
QuietVR currently allows for two kinds of audio input behavior:

1. **Clicky sound**(behaviors like fingersnap)

2. **Continuous sound**(behaviors like yelling or singing)

When entering the game, look for a white circle in the sky which serves as an indicator for your audio input.

The white circle can summon various objects based on the audio input behavior you provide as described above.

These summoned objects can stay around you as long as they don't get kicked out by the later ones.

Current build provides three categories of objects:

1. **Basic geometry**, which is summoned by clicky sound.

2. **Advanced 3D model**, which is summonned by continuous sound. You can at most have two of them simultaneously surrounding you.

3. **Animated creature**, which requires 30+ secs of continuous sound to trigger :) 

### Additional Commend

To exit the game, press ESC on keyboard.

## Credits and Acknowledgement

Currently, QuietVR's core functionality of audio input processing relies on the famous Japanese visualization artist and Unity engineer Keijiro Takahashi's [Lasp](https://github.com/keijiro/Lasp) project. Lasp's low latency audio processing(less than 16 ms) gives QuietVR its responsiveness and robustness which are much needed in VR environment to create the feeling of realism.

Besides Lasp, QuietVR also uses third party 3d models and assets.

The following is a full list of credits:

1. [low poly head free! by hexonian(licensed under CC Attribution)](https://sketchfab.com/models/988a1ffdb6244eaab9b293d296c6e868#)
2. [low poly space ship by chrisonciuconcepts(licensed under CC Attribution)](https://sketchfab.com/models/587941c9c11742c6b82dfb99e7b210b9)
3. [Flamingo by ryemartin(licensed under CC Attribution)](https://sketchfab.com/models/237fc4e8ca004c83ae20a1db08e2e661#)
4. [[3D Printable] Bricktown Low-Poly Collection #6 by Y3DS(licensed under CC Attribution)](https://sketchfab.com/models/a73486c6e6a640dc856ff6624ffeae97)

Various 3D model assets from Unity asset store were also used during the development of this project.

## Artistic Design and Code Structure

### Artisitic Design

The artistic design of QuietVR follows the code of minimalism. The 3D assets that exist in the game are mostly of low polygon counts.

The post processing techniques that are used in game are [sun shafts](https://docs.unity3d.com/550/Documentation/Manual/script-SunShafts.html), [bloom](https://docs.unity3d.com/550/Documentation/Manual/script-Bloom.html), and [color grading](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html).

QuietVR uses Unity's standard shader to create fade in/out effect on objects in game.

### Scripts

QuietVR's core behaviors are implemented in Quiet.cs, ObjectManager.cs and RoomObject.cs three classes.  

Quiet.cs communicates with Lasp and gives signal to ObjectManager. ObjectManager receives the signal and instantiate/pooling 3D objects accordingly. RoomObject.cs controls the rotation of objects.

The visual effect of audio reactive white circle is implemented in VoiceRipple.cs.

These classes can be found in Asset/-Scripts folder.

Note: Quite a lot of these scripts are not well documented. Further documentation will be added as this project gets its update later on.

## Future Roadmap and Possible Expansion

## License

QuietVR is an open source project under a MIT license which allows for third party modification and expansion.
