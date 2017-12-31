# QuietVR
> A quiet place in VR.

![QuietVR](https://github.com/diglungdig/QuietVR/blob/master/Screenshots/QuietVRDemo.gif)

Please go to the end of this README for version update logs

## What is QuietVR

QuietVR is an experimental demo of VR interaction via voice input. It connects audio recognition interfaces with Google’s newly introduced Poly Api.

## Inspiration and Motivation

This project is inspired by Armin van Buuren's YouTube video of [Intense](https://www.youtube.com/watch?v=6UoNXz0Ox-g).

The initial thought was to create a simple VR experience where the player is surrounded by blocks and low polygon geometries. After a few iterations of implementation, it eventually turns into VR demo that focuses on audio reactive interaction.

The whole purpose of this project from a development perspective is to implement a harmonic coordination between VR and audio input, creating fun VR gameplay with simple vocal elements.

## Installation

Please go to the release page to download the latest version.

https://github.com/diglungdig/QuietVR/releases

Each release contains a VR version that works with HTC Vive/Oculus Rift and a non-VR version that works with normal PC setups.

It's recommended to play in VR.

## System Requirement and Specification
  
Window 10 with GTX 970 or higher.

DirectX 11 or higher required.

VR version works with HTC Vive and Ocules Rift CV1.

Audio recording device is required in order to play QuietVR.

## How to Play

### Setup

Currently QuietVR can only be run on Windows. MacOS is not supported.

QuietVR uses Windows’ speech recognition feature. [To enable it on your OS,  go to Speech, inking & typing on your Windows 10 Settings, then turn on speech services and typing suggestions.](https://privacy.microsoft.com/en-us/windows-10-speech-inking-typing-and-privacy-faq)

It’s better to play this demo in a quiet environment. Otherwise gameplay will most likely be disrupted for game's solely relying on audio input. Make sure you have your audio recording device like microphone or headset set up before start the game.


### Game Mechanics

QuietVR currently has two modes. Each of these modes support different kinds of voice input behaviors.

**[Search Mode]:** 
Search Mode takes advantage of Windows Speech Recognition feature to communicate with Google’s Poly server.
In this mode, you are given the option to request a 3D model via voice command.
To initialize requesting process,
Make sure you have your environment setup ready(See the above section).
Towards your microphone, say “Quiet”
Then, say a word representing an object you want to fetch from the server(“Elephant”, “Pizza”, “Book”, etc)
If the process succeeds, you should see your object pop in front of you.

**[Random Mode]:**
Random mode is first introduced in QuietVR version 1.0(the OG mode so to speak :D). It uses Keijiro Takahashi’s Lasp((Low-latency Audio Signal Processing plugin for Unity) to give responsive voice feedbacks to the user.
In this mode, you are given the option to...yell...as long as you want. An object will be fetched after your voice falls off.


### Additional Commends

1. To exit the game, press ESC on keyboard.

2. Press Space on keyboard to switch between two modes.

## Credits and Acknowledgement

For its use of Google's Poly Api, all of the 3D models appearing in the game are basically from Google's community(Tiltbrush, Google Blocks, etc). The majority of these 3D resources have the Creaive Commons License. 
**Game gives credits to these amazing authors by printing out the credit in the back of the player right after the 3D object appears in front.**

QuietVR also relies on the famous Japanese visualization artist and Unity engineer Keijiro Takahashi's [Lasp](https://github.com/keijiro/Lasp) project to update the visual cue when listening to voice input. Lasp's low latency audio processing(less than 16 ms) gives QuietVR its responsiveness and robustness which are much needed in VR environment to create the feeling of realism.

Background Music by Bill Evans - Waltz For Debby

## Artistic Design and Code Structure

### Artisitic Design

The artistic design of QuietVR follows the code of minimalism. 

Shader for generic objects used in the game is a simple vertex color shader. 

The post processing techniques that are used in game are from [Unity's Post Processing Stack](https://github.com/Unity-Technologies/PostProcessing).

### Scripts

QuietVR's core behaviors are implemented in Quiet.cs(child class of PolyVRPort.cs), CommandRecognition.cs, and PolyManager.cs.  

PolyVRPort.cs is a singleton abstract class set to coordinate between voice responsive interfaces, like Windows Speech and Lasp, and Google's Poly Api. It has serveral abstract functions that are crucial for the process.

Quiet.cs is a derived child class from PolyVRPort.cs. Besides overriding functions from parent class and hooking up interfaces, it also coordinates all the cosmetics and event triggering for the game.

CommandRecognition.cs uses Windows.Speech's KeywordRecognizer and DictationRecognizer to allow for voice command input. The voice command follows the format of "Quiet"(Keyword) + Content(Dictation).

PolyManager.cs, as the name suggests, communicates with Poly Api and gives feedback to PolyVRPort.cs

The visual effect of audio reactive white circle is implemented in VoiceRipple.cs.

These classes can be found in Asset/-Scripts folder.

Note: Quite a lot of these scripts are not well documented. Further documentation will be added as this project gets its update later on.

## Limitation, Future Roadmap and Possible Expansion

QuietVR's purpose for now is to become a testground for audio reactive behavior in VR and further related implementation.

The bigger picture might include things like:

1. ~~Voice recognition with VR(implemented in version 2.0. However I'm thinking about migrating from the sloppy Windows Speech to IBM Speech Sandbox)~~.

2. [Procedural content generation by audio input](https://creators.vice.com/en_us/article/8qvgbx/heres-how-you-turn-sounds-into-3d-sculptures) 

3. Education in VR with voice and audio commends

From a gameplay perspective, the following things might be added with future updates in a long run:

1. Deeper analysis of audio input for expanding more sorts of audio commends

2. ~~Instead of pooling 3D objects locally, a dedicated backend server that will allow any third party users to upload their own 3D models and share across the platform.(implemented in version 2.0 via Google Poly)~~


## License

QuietVR is an open source project under a MIT license which allows for third party modification and expansion.

**Because QuietVR uses Google Poly Api, you need to have a Api key in order to work within the Unity environment. A detailed instruction on how to obtain and insert the key can be found [here](https://developers.google.com/poly/develop/unity).** 


## Update Logs

### Version 2.0
Updates:
**1.** Google Poly Api integration. Program now pulls 3D models at runtime from Google Poly.

**2.** A new mode "Search Mode" has been added. In this mode, user can use voice recogition to fetch 3D models.

**3.** A new program logo

**4.** Various minor comestics updates


### Version 1.1
Updates:

**1.** A countdown timer before each voice input

**2.** A title text

**3.** Restrcuturing, redesign and optimization on classes and codes

**4.** Minor improvements include things like disabling voice input when user is not facing at the indicator

**5.** Generic shapes in the scene now use a unlit color shader for simplitity

**6.** Voice input now can generate more special 3D models!

