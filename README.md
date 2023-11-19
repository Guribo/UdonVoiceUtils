# Udon Voice Utilities

> IMPORTANT:
> Tutorials for individual components and WIKI are not yet added/updated.  
> If you want to know about something specifically right away feel free to contact me on Discord (name: Guribo).  
> You can find me in the official VRChat Discord Server or in e.g. the Prefabs Discord Server.  
> This initial release is a release candidate but is feature complete.  
> Once the documentation is updated and the new tutorial world is ready it will switch to a full release.  
> 
> Feel free to give UVU 1.0.0-rc1 a test and create issue tickets if you run into any problems. I will get these sorted out before the final release.

[![Total downloads](https://img.shields.io/github/downloads/Guribo/UdonVoiceUtils/total?style=flat-square&logo=appveyor)](https://github.com/Guribo/UdonVoiceUtils/releases)

<!-- TOC -->
* [Udon Voice Utilities](#udon-voice-utilities)
  * [What is UVU?](#what-is-uvu)
    * [TL;DR](#tldr)
    * [Example use cases](#example-use-cases)
    * [Where is it being used?](#where-is-it-being-used)
  * [Getting started](#getting-started)
    * [Dependencies](#dependencies)
    * [Installation](#installation)
    * [Minimal scene setup](#minimal-scene-setup)
  * [Upgrading from BPAv0.8 to UVU](#upgrading-from-bpav08-to-uvu)
  * [1.0.0 Change Notes](#100-change-notes)
    * [Additions](#additions)
    * [Fixes](#fixes)
    * [Deletions](#deletions)
    * [Other](#other)
  * [References](#references)
<!-- TOC -->

## What is UVU?

**UdonVoiceUtils** is a selection of scripts and prefabs that can modify player audio settings in real time.

### TL;DR

Jump to [Installation](#installation) to get started... but I really recommend you read the given information here :)

### Example use cases

1. Increasing players voice range with e.g. a microphone
2. Separating players using audio zones/rooms
3. Muffling players behind walls
4. Muffling players standing behind other players
5. Scaling voice ranges depending on avatar height
6. Adding reverb to player voices and all audible audio
7. Creating complex stage setups with private backstage areas
8. Focusing player voices when they face you or you face them
9. Dividing players for competitive games using voice channels

### Where is it being used?

- Midnight Rooftop by ImLeXz - *Occlusion, Voice directionality*
- The Black Cat by spookyghostboo - *Voice directionality*
- The Great Pub by owlboy - *Occlusion, Voice directionality*
- Pool Parlor by Toasterly - *Voice directionality, Audio zones*
- ...


> You can request to have your world added by sending **Guribo** a message on Discord or by creating a ticket on this repository.
> Please provide the following information or use the template here on GitHub:
>
> 1. World name
> 2. World ID (optional, may be used for placing a portal in a tutorial world)
> 3. Your name to be displayed (optional if the current VRChat name shall be used)
> 
> Note: I won't accept worlds that are not submitted by the world creator themselves

## Getting started

Since **UdonVoiceUtils 1.0.0** the package can be added to your projects
via the **VRChat Creator Companion**.

### Dependencies
During installation via the creator companion the following dependencies are installed as well if not yet present:
- [tlp.udonutils](https://github.com/Guribo/UdonUtils)

> Note that dependencies may install additional packages!

### Installation
1. Navigate to **https://guribo.github.io/TLP**
2. In the list of packages look for **UdonVoiceUtils**
3. Click **Add to VCC** followed by **Open Creator Companion**  to add the repository to your
   **CreatorCompanion**
4. Confirm adding of the repository in the **CreatorCompanion** application
5. Navigate to the **Projects** list
6. Click on **Manage Project** on the project where you want to add **UdonVoiceUtils**
7. Locate **UdonVoiceUtils** in the list and click on the **+** icon to add it
    - **This will delete old BetterPlayerAudio files from your project in case you had these imported! This is a necessary step.**
8. Start your project to begin setting up **UdonVoiceUtils** or have a look at the demo scene `Packages/tlp.udonvoiceutils/Runtime/Scenes/Demo.unity`

> **Note:**  
> Please follow the official guides created by VRChat to create a Unity World Creation project using the VRChat Create Companion.  
> The example scripts and prefabs can be found in `Packages/tlp.udonvoiceutils/Runtime/Prefabs/Examples`.  
> 
>  **Please check out the [Wiki](https://github.com/Guribo/UdonVoiceUtils/wiki) for additional information on the available components, features and prefabs before creating your own worlds. It mentions limitations and things to take into account when integrating UVU into a VRChat world.**  
> 
> To see (and hear) how any of these features can be used feel free to test the example world with a friend or visit the public version [in VRChat](https://vrchat.com/home/launch?worldId=wrld_7ec7bbdd-ba81-4564-985a-c79dfc9eaca7).

### Minimal scene setup

1. In your **Project** windows in the search window type in "**TLP_**"
2. Filter the search results using the **Dropdown menu**, select: **In Packages**
3. From the now displayed search result add two prefabs to your scene:

    1. **TLP_Logger** - *universal logger for all TLP packages*
    2. **TLP_PlayerAudioController** - *core system of UVU*
        - Alternatively you can use the prefab **TLP_PlayerAudioCompleteWithUi** if you want the menu as well

4. The minimum setup is now complete, but has no effect on any player by default.
5. From here on out you can either modify the global settings or add some of the prefabs and connect them to the controller  to create what you need. In addition, you can also create your own custom solutions that rely on UVU's audio overriding capabilities.



## Upgrading from BPAv0.8 to UVU

Please follow these steps in order and read all the instructions first before executing on them

1. **Backup your project and ensure that you can revert to the backed up version without breaking anything!**
2. If you have custom override values either make screenshots or duplicate the project so that you can easily set the same values again (note that occlusion values have changed)
3. Open a new empty scene
4. Import the latest release from the creator companion
5. Check if there is any errors on the console after importing. This can help diagnose problems that have occurred.
    1. If you get the error:
       ````
       (...) does not belong to a U# assembly, have you made a U# assembly definition for the assembly the script is a part of?
       ````
       - restart the Unity editor and they should be gone
   
6. Open your scene that used BPAv0.8
7. Check the console, there is most likely messages like:
   1. `[UdonSharp] Scene behaviour 'SyncedPlayers' needed UnityEngine.Object upgrade pass`. This means the upgrade was successful.
   2. If you see the error `Can't remove AudioListener because AudioReverbFilter depends on it` please ignore it, it is an "exploit" to get reverb to work on voices.

8. If there is warnings like `[UdonSharp] Empty UdonBehaviour found on (...)`
    1. Click on the message to navigate to the GameObject that caused the warning
    2. Find the UdonBehaviours that are empty (Program Source is set to `None`)
    3. Delete these UdonBehaviours
9. Add the prefab `Assets/TLP/UdonUtils/Prefabs/TLP_Logger.prefab` to your scene
10. Find the `PlayerAudioController` and if you had it unpacked replace the entire GameObject with the prefab `TLP_PlayerAudioController`
11. Open the hierarchy of the prefab and navigate to the `Configurations` GameObject
12. Make your changes that shall be applied by default to every player to both the `LocalConfiguration` and `DefaultConfiguration`
13. The most painful part:
    1. Navigate through your hierarchy and check every `UVU` component for unset variables
    2. During testing, I experienced that some references to e.g. `SyncedIntegerArray` were empty after the upgrade
    3. If these variables are part of a prefab right-click on them and try the `Revert` option if available, otherwise,
       select the corresponding component from the child GameObjects or replace the entire GameObject with one of the prefabs that come with `UVU` and set it up again.
    4. Finding may be simplified by using ClientSim and testing every AudioZone/-Room in your world and checking the console for errors.
14. Lastly, there is some scripts that no longer exist in the shape of UdonBehaviours, thus there might be some empty UdonBehaviours in your scene. The console will tell you about them.
    1. UdonMath
    2. UdonCommon
    3. UdonDebug
       Unless any prefab was unpacked they should have been removed automatically during the upgrade process.


## 1.0.0 Change Notes

### Additions
* Add `PlayerAudioConfigurationModel` component
    * One is used for current global configuration
    * One is used as default settings
        * *used when the `Reset All` button on the example menu is pressed*
    * One is used to synchronize the master with all other players
        * *when UVU is used combination with the example menu*
* Rely on new Execution Order defined in `UdonUtils`
* Add AnimationCurve `HeightToVoiceCorrelation` to change voice range automatically in relation to avatar height (off by default)
    * Add to global `PlayerAudioConfigurationModel`
    * Add to `PlayerAudioOverride`
* Add support for de-/activating UVU at runtime
    * Disable/Enable either the GameObject with the `PlayerAudioController` or the `PlayerAudioController` component itself
* Add `DataDictionary` of UdonBehaviours mapped to PlayerIds (`int`) that can be used to get notified whenever a player is updated
    * DataDictionary name: `PlayerUpdateListeners`
    * Keys: playerIds (integer)
    * Values: UdonBehaviour
    * Expected public Variables on listening UdonBehaviours:
        * `public bool VoiceLowpass`
        * `public float VoiceGain`
        * `public float VoiceDistanceFar`
        * `public float VoiceDistanceNear`
        * `public float VoiceVolumetricRadius`
    * Expected functions on listening UdonBehaviours:
        *  `public void VoiceValuesUpdate()`
* Add `PlayerAudioView` component which takes care of receiving input from the example menu and takes care of updating the menu in return
* Add `VoiceRangeVisualizer` prefab that updates and displays each players voice range in real time for debugging/testing purposes
* Add `DynamicPrivacy` script that can update the privacy channel of another `PlayerAudioOverride` upon receiving a `LocalPlayerAdded` or `LocalPlayerRemoved` event from a given `PlayeraudioOverride`
    * Allows creating complex privacy channel setup, e.g. stages with private production areas (please check the demo world for such an example)
### Fixes
- Fix "memory leak" in which new GameObjects would get instantiated every time a player entered a zone leading to decreased performance in long running instances which relied on zones
    - The objects are now pooled and re-used
- Fix bug in `VoiceOverrideRoom` that caused in rare cases the list of players to not being updated
- Fix issue making players already inside of triggers not being affected by `PlayerAudioOverride` when entering the world by briefly toggling all relevant triggers off and on again upon `Start`
### Deletions
- Remove `UdonCommon` and other, similar library scripts  from prefabs and use code statically
- Remove `AutoPlayerRange` script (now part of `UdonUtils`)
- Remove `CustomAudioFalloff` script (now part of `UdonUtils`)
### Other
- Rename from **UdonBetterAudio** to **UdonVoiceUtils**
- Move from `Assets/Guribo/UdonBetterAudio` to `Packages/tlp.udonvoiceutils`
- Convert to VRChat Creator Companion package
- Update to latest Udon API where appropriate
- Rely on GUIDs instead of Assembly names in Assembly Definitions
- Rename `PrivateZones` demo scene into `Demo` and move to `Packages/tlp.udonvoiceutils/Runtime/Scenes/Demo`
- Rework of example Menu and underlying architecture
    - Relies on MVC pattern introduced in `UdonUtils`
- Move settings from former `BetterPlayerAudio` component to `PlayerAudioConfigurationModel`
- Simplify usage and explanations of occlusion values
- Change update rate to no longer depend on time but on rendered frames --> more predictable performance on slower PCs/Quest
- Extensive rework of main `PlayerAudiController`
- Remove workaround to determine avatar height and use avatar eye height of `VrcPlayerApi` instead
- Change behaviour of private channels to only mute people on the outside if the boolean `muteOutsiders` is set to true
    - *previously just being in a private channel muted all players on the outside by default*
- Change previous event handling to use new class `UdonEvent` from `UdonUtils`
- Renamed core prefabs to start with "`TLP_`"


## References
* [UdonSharp](https://udonsharp.docs.vrchat.com/)
* [VRChat player audio API docs](https://docs.vrchat.com/docs/player-audio)