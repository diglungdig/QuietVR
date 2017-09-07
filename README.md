# QuietVR
> A quiet place in VR.


![](https://github.com/diglungdig/QuietVR/blob/master/Screenshots/1.png)

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

1. **Basic geometry**, which is summoned by clicky sound.(3 kinds in total)

2. **Advanced 3D model**, which is summonned by continuous sound. You can at most have two of them simultaneously surrounding you.(18 kinds in total)

3. **Animated creature**, which requires 30+ secs of continuous sound to trigger :) (2 kinds in total)

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
5. [Killer Whale by dandi(licensed under CC Attribution)](https://sketchfab.com/models/eb8079f41fe34550887f666a83173cdb)
6. [Stingray by dandi(licensed under CC Attribution)](https://sketchfab.com/models/804378af005f4dc38ddc7355d3eb3779)
7. Background Music by Tomppabeats - [You're Cute](https://www.youtube.com/watch?v=039QyF-zwWA)

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

## Limitation, Future Roadmap and Possible Expansion

QuietVR currently is a simple demo with a less than 10 mins playthrough. It takes simple audio commends and generate 3D objects from a randomized predefined set. 

Its whole purpose for now is to become a testground for audio reactive behavior in VR and further related implementation.

The bigger picture might include things like:

1. Voice recognition with VR.

2. [Procedural content generation by audio input](https://creators.vice.com/en_us/article/8qvgbx/heres-how-you-turn-sounds-into-3d-sculptures) 

3. Education in VR with voice and audio commends

From a gameplay perspective, the following things might be added with future updates in a long run:

1. Deeper analysis of audio input for expanding more sorts of audio commends

2. Instead of pooling 3D objects locally, a dedicated backend server that will allow any third party users to upload their own 3D models and share across the platform.

Short term speaking, things like customizable background music and better UI will also be implemented step by step.

## License

QuietVR is an open source project under a MIT license which allows for third party modification and expansion.

