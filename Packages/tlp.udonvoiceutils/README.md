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

- [Drinking Night](https://vrchat.com/home/world/wrld_4a65ba53-d8df-40a7-b67d-30c63bff0e95) by Rackshaw - *Occlusion, Audio zones*
- [Midnight Rooftop](https://vrchat.com/home/world/wrld_d29bb0d0-d268-42dc-8365-926f9d485505) by ImLeXz - *Occlusion, Voice directionality*
- [The Avali ShatterDome](https://vrchat.com/home/launch?worldId=wrld_f2c3ed62-7d02-416d-a450-753939a5f327) by RadioFoxWin - *Microphone, PlayerAudioController menu*
- [The Black Cat](https://vrchat.com/home/world/wrld_4cf554b4-430c-4f8f-b53e-1f294eed230b) by spookyghostboo - *Voice directionality*
- [The Great Pub](https://vrchat.com/home/world/wrld_6caf5200-70e1-46c2-b043-e3c4abe69e0f) by owlboy - *Occlusion, Voice directionality*
- [The Pool Parlor](https://vrchat.com/home/world/wrld_99bdc4c6-b80c-49f3-aae0-5d67017d8340) by Toasterly - *Voice directionality, Audio zones*
- [Virtual Performing Arts Theater](https://vrchat.com/home/world/wrld_f1ae5929-a881-4c21-acc0-8d5cb9bf919f) by DjembeDragon - *Private channels, Audio Zones, Reverb, Audience/Stage volume control*
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

- [Cyan.PlayerObjectPool](https://cyanlaser.github.io/CyanPlayerObjectPool/) - repository needs to be added to VCC manually first via `Add to VCC` Button!
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
- Upgrade to Unity 2022.3, dropping support for Unity 2019


## References
* [UdonSharp](https://udonsharp.docs.vrchat.com/)
* [VRChat player audio API docs](https://docs.vrchat.com/docs/player-audio)
## Changelog

All notable changes to this project will be documented in this file.

### [1.0.1] - 2024-08-18

#### 📚 Documentation

- *(Readme)* Add The Avali ShatterDome by RadioFoxWin to list of worlds

#### ⚙️ Miscellaneous Tasks

- Support UdonUtils 8.2.0
- Add support for com.vrchat.worlds 3.7.x

### [1.0.0] - 2024-05-25

#### ⚙️ Miscellaneous Tasks

- Migrate to UdonUtils 8.1.0, go into full release state

### [1.0.0-rc.6] - 2024-05-16

#### 🚀 Features

- *(Gizmos)* Display entire GameObject path instead of just name in scene view when PlayerAudioController is selected

#### 🐛 Bug Fixes

- *(PlayerAudioView)* Fix custom default settings not being displayed after init
- *(PlayerAudioController)* Fix PlayerAudioOverride occlusion values being applied inverted
- *(Prefabs)* Fix main prefab with UI having broken references in new scenes

#### 🎨 Styling

- *(Demo)* Bake light of demo scene, add light probes

### [1.0.0-rc.5] - 2024-05-12

#### 📚 Documentation

- *(Readme)* Add new world listings, add links to VRC worlds

### [1.0.0-rc.4] - 2024-05-10

#### ⚙️ Miscellaneous Tasks

- Support com.vrchat.worlds 3.6.x and Unity 2022.3.22

### [1.0.0-rc.3] - 2024-05-03

#### 🚀 Features

- Update to Unity 2022.3
- Migrate to UdonUtils 6.1.x

#### 📚 Documentation

- Add missing hint about cyan dependency

#### ⚙️ Miscellaneous Tasks

- Update git configuration and setup script

### [1.0.0-rc.1] - 2023-11-19

#### 🚀 Features

- Update assets to lfs
- Rename prefabs
- Change layers and make objects static that can be
- Update to U## 1.0 and client sim
- Fix loglevels, assert and perf limit warning
- Jitterfree pickups
- Make few functions static
- Disallow multiple instances of same override in lists
- Add gamemode, update vr components, test improvements, add serialization retry to base behaviour
- Add logging of all logs in frame to profiler
- Add execution order, fix runtime tests
- Move runtime test code
- Update base behaviour
- Fix up scenes and broken event callbacks
- Display data in leaderboard entry
- Start converting playeraudio controllers
- Add more events for different executionorder sections, refactor executionorder on most scripts
- Update tribes scene, create leaderboard prefab
- Convert basic performance stats to model view controller
- Update profiler ui, update leaderboard scene
- Fix players spawning in triggers not being detected by audio zone
- Add multiple sorting algorithms, update privacy zones example scene
- Add dirty property to event
- Create factories for avl tree, factory with pool
- Reduce type spam in logs, add execution order to logs
- Add comparer creation, update exectionorders, move pooleable code to base behaviour
- Add new data source using leaderboard model
- Deinit on destroy, selectable categories with view
- Update use of static class
- Remove template from prefab
- Update after utils restructure
- Update UVU exporter and readme
- Remove 'Better' references
- Update file structure
- Rename and add udonutils dependency
- Add package.json
- Fix default settings on prefabs, support muting outsiders even without privacy channel
- Don't fail if player already added
- Update scene
- Replace synchronize playerlist with new version
- Update
- Add component for dynamic privacy channel
- Update namespaces
- Add support for blacklists, update demo world
- Update assets
- Add gitconfig
- Update to support latest vrc sdk
- Fix enabling/disabling of entire hierarchy with controller on it
- Add pooling of PlayerOverrideLists
- Add toggle for menu and voice controller
- Simplify fallback head position calculations
- Implement height based range scaling using an animation curve
- Add fallback position handling to tracking data tracker
- Simplify visualizer by using cyan pool
- Update runtime tests
- Add test for eye height updates coming from master
- Fix audio range issue, add test for eye height updates coming from other player
- Update
- Change assertion to error
- Create basic image downloader with automatic aspect ratio adjustment
- Add new demo world for uvu (wip)
- Create intro tutorial
- Update dependency
- Update exporter
- Cleanup scene, change editor script parent, rename demo world
- Add debug content to exporter
- Disable runtime tests and add hint
- Delete old releases folder
- Remove old gif and old remplates

#### 🐛 Bug Fixes

- Lfs for assets
- Get rid of all files to fix lfs issues
- Add all files again without lfs

#### 🚜 Refactor

- Update variable name
- Cleanup and more test coverage
- Use Tlpbasebehaviour
- Rename PlayerAudioMenu

#### 📚 Documentation

- Update readme
- Add release notes to readme
- Update readme

#### 🧪 Testing

- Fix tests with debug enabled
- Fix errors
- Fix errors in production mode
- Update tests to use TestWithLogger, reduce log spam
- Fix missing view in setup, restructure
- Fixed missing reference
- Extract setup into base class

#### ⚙️ Miscellaneous Tasks

- Update package.json and add release issue template
- Move serializedUdonPrograms
- Remove programs

### [0.8.0] - 2022-02-27

#### 🚀 Features

- Move ExtendedAudio to own repo, change "Better" names

#### 📚 Documentation

- Add link to downloads

#### 🧪 Testing

- Increase test coverage, refactoring

#### ⚙️ Miscellaneous Tasks

- Add mic setup example
- Update
- Convert to lfs
- Update package.json and Readme

### [BPAv0.8] - 2021-09-26

#### 🚀 Features

- Support disable/enable
- Remove players from trigger zone on disable

#### 🐛 Bug Fixes

- Add temp. workaround for some avatars not having a head position/rotation
- Export of example scene

### [BPAv0.7] - 2021-08-06

#### 🚀 Features

- Single audio filters active
- Finish update to Unity 2019
- Support toggle of BPA, draw room-door relation in editor
- Display behaviour relations, cleanup

#### 🐛 Bug Fixes

- Prevent popping noises when entering reverb zone
- Reverb not activating when alone

#### 🧪 Testing

- Increase coverage

### [BPAv0.7_beta] - 2021-07-12

#### 🚀 Features

- Add privacy settings to voice override
- Add private channel support
- Priority/channel based overriding
- Override gates/doors
- Debugging, refactoring, cleanup
- Add option to talk but not listen to players in private zone
- Add network stress test, cleanup
- Split enter button from room
- Add debug tools, custom editors, cleanup

#### 🐛 Bug Fixes

- Player check
- Update on deserialization not working reliably
- Nullpointer exception
- Missing assembly reference

#### 🚜 Refactor

- Restructure content

### [BPAv0.6] - 2021-06-12

#### 🚀 Features

- Add betterPlayerAudio testing
- Add falloff analysis test scene, audio tests
- Add VoiceOverrideZone from tutorial
- V0.6

#### 🚜 Refactor

- Use udonUtil libraries, audio link testing

### [BPAv0.5] - 2021-04-10

#### 🚀 Features

- Adapt new network features
- Update example scenes

#### 🐛 Bug Fixes

- Ownership transfer of hierarchy
- Minor issues and synchronization rate
- Reset all not resetting for receiver

#### ⚙️ Miscellaneous Tasks

- Prepare next version
- Rename

### [BPAv0.4] - 2021-03-15

#### 🚀 Features

- Add is valid checks, refactoring
- Add exporter, update mic with override
- V0.4

### [BPAv0.3] - 2021-01-15

#### 🚀 Features

- Add BetterPlayerAudio
- Change default values of voice to match VRC settings
- Add pickup microphone example
- Add separate player occlusion slider

#### 🐛 Bug Fixes

- Prevent exception when menu is not is use

### [BPAv0.2] - 2020-12-19

#### 🚀 Features

- Add update rate option, add example world
- Experimental player exclusion list

#### 🚜 Refactor

- Add log coloring, add sample audio

### [BPAv0.1] - 2020-11-21

#### 🚀 Features

- Add everything except sample audio
- Add player audio controls with example ui menu
- Add default values and expose to unity UI
- Refactoring, improve default value handling, performance
- Prevent initializing UI from potentially uninitialized BetterPlayerAudio
- Use camera position instead of avatar head bone
- Allow up to 100% directionality for both listener and player
- Update hints, correct default values
- Add master control (locally toggleable)

#### 📚 Documentation

- Fix variable name

<!-- generated by git-cliff -->
