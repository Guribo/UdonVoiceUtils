# BetterAudio
'Better' audio for UDON (VRChat) based worlds.

Everything here work in progress and will evolve with time.

Currently released are only the BetterPlayerAudio parts.

You can find the v0.7 release here: https://github.com/Guribo/BetterAudio/releases/tag/BPAv0.7
>*In order to use it you need the VRChat Udon SDK, [UdonSharp by Merlin](https://github.com/MerlinVR/UdonSharp/wiki/setup) and Unity 2019.4.*
>*Afterwards simply import the v0.7 unitypackage and have a look at the example scene `Assets/Guribo/UdonBetterAudio/Examples/PrivateZones/PrivateZones.unity`.*

**Please check out the [Wiki](https://github.com/Guribo/BetterAudio/wiki) for additional information on the available components, features and prefabs.**

It contains:
- player voice control
- private channels
- player voice/avatar audio occlusion
- player voice/avatar audio directionality
- reverb filter support

A second version the focuses on audio effects in worlds will be released later, the code already includes:
- speed of sound
- sonic booms
- doppler effect
- occlusion
> *Due to the specifically targeted usecase (fast vehicles/objects) there is not much use for it currently and is also quite expensive in terms of performance.
> Thus it is not released yet but you are free to donwload the code from the development branch and give it a go.*


## References
* [UdonSharp by Merlin](https://github.com/MerlinVR/UdonSharp)
* [VRChat player audio API docs](https://docs.vrchat.com/docs/player-audio)

