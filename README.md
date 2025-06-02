# Udon Voice Utilities

[![Total downloads](https://img.shields.io/github/downloads/Guribo/UdonVoiceUtils/total?style=flat-square&logo=appveyor)](https://github.com/Guribo/UdonVoiceUtils/releases)

<!-- TOC -->
* [Udon Voice Utilities](#udon-voice-utilities)
  * [What is UVU?](#what-is-uvu)
    * [TL;DR](#tldr)
    * [Example use cases](#example-use-cases)
    * [Where is it being used?](#where-is-it-being-used)
  * [Getting started](#getting-started)
    * [Versioning](#versioning)
    * [Installation](#installation)
    * [Minimal scene setup](#minimal-scene-setup)
  * [Troubleshooting](#troubleshooting)
    * [Errors after installation](#errors-after-installation)
    * [*Something* is not working in the world](#something-is-not-working-in-the-world)
    * [Occlusion is not working](#occlusion-is-not-working)
  * [FAQ](#faq)
    * [Can I use MeshColliders for occlusion?](#can-i-use-meshcolliders-for-occlusion)
    * [How can I change the default/globally active settings?](#how-can-i-change-the-defaultglobally-active-settings)
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

- [Drinking Night](https://vrchat.com/home/world/wrld_4a65ba53-d8df-40a7-b67d-30c63bff0e95) by Rackshaw - *Occlusion,
  Audio zones*
- [Midnight Rooftop](https://vrchat.com/home/world/wrld_d29bb0d0-d268-42dc-8365-926f9d485505) by ImLeXz - *Occlusion,
  Voice directionality*
- [The Avali ShatterDome](https://vrchat.com/home/launch?worldId=wrld_f2c3ed62-7d02-416d-a450-753939a5f327) by
  RadioFoxWin - *Microphone, PlayerAudioController menu*
- [The Black Cat](https://vrchat.com/home/world/wrld_4cf554b4-430c-4f8f-b53e-1f294eed230b) by spookyghostboo - *Voice
  directionality*
- [The Great Pub](https://vrchat.com/home/world/wrld_6caf5200-70e1-46c2-b043-e3c4abe69e0f) by owlboy - *Occlusion, Voice
  directionality*
- [The Pool Parlor](https://vrchat.com/home/world/wrld_99bdc4c6-b80c-49f3-aae0-5d67017d8340) by Toasterly - *Voice
  directionality, Audio zones*
- [Virtual Performing Arts Theater](https://vrchat.com/home/world/wrld_f1ae5929-a881-4c21-acc0-8d5cb9bf919f) by
  DjembeDragon - *Private channels, Audio Zones, Reverb, Audience/Stage volume control*
- ...

> You can request to have your world added by sending **Guribo** a message on Discord or by creating a ticket on this
> repository.
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

### Versioning

This package is versioned using [Semantic Version](https://semver.org/).

The used pattern MAJOR.MINOR.PATCH indicates:

1. MAJOR version: incompatible API changes occurred
    - Implication: after updating backup, check and update your scenes/scripts as needed
2. MINOR version: new functionality has been added in a backward compatible manner
    - Implication: after updating check and update your usages if needed
3. PATCH version: backward compatible bug fixes were implemented
    - Implication: after updating remove potential workarounds you added

### Installation

1. Install/Add VRChat World SDK 3.7 to your project
2. Install/Add CyanPlayerObjectPool to your project: https://cyanlaser.github.io/CyanPlayerObjectPool/
3. Install/Add TLP UdonVoiceUtils to your project: https://guribo.github.io/TLP/
4. Start your project and open the scene `Packages/tlp.udonvoiceutils/Runtime/Scenes/Demo.unity`
5. With ClientSim enabled, click the Play button in Unity
6. Check for any errors in the console (only one regarding AudioListener is expected)
7. If there is any problems, please check the [Troubleshooting](#troubleshooting) section below
8. To set up your own world, start with the [Minimal Scene Setup](#minimal-scene-setup)

> **Additional Notes:**  
> Please follow the official guides created by VRChat to create a Unity World Creation project using the VRChat Create
> Companion.  
> The example scripts and prefabs can be found in `Packages/tlp.udonvoiceutils/Runtime/Prefabs/Examples`.
>
>  **Please check out the [Wiki](https://github.com/Guribo/UdonVoiceUtils/wiki) for additional information on the
available components, features and prefabs before creating your own worlds. It mentions limitations and things to take
into account when integrating UVU into a VRChat world.**
>
> To see (and hear) how any of these features can be used feel free to test the example world with a friend or visit the
> public version [in VRChat](https://vrchat.com/home/launch?worldId=wrld_7ec7bbdd-ba81-4564-985a-c79dfc9eaca7).

### Minimal scene setup

1. In your **Project** windows in the search window type in "**TLP_**"
2. Filter the search results using the **Dropdown menu**, select: **In Packages**
3. Add **TLP_Essentials** prefab to your scene to get the core components
   - Note: `WorldVersionCheck` inside the prefab is optional and can be deleted from the scene
4. Add **TLP_PlayerAudioController** - *core system of UVU (mandatory)*
   - Alternatively you can use the prefab **TLP_PlayerAudioCompleteWithUi** if you want the menu as well

5. The minimum setup is now complete, but has no effect on any player by default.
6. From here on out you can either modify the global settings or add some of the example prefabs and connect them to the
   controller to create what you need. In addition, you can also create your own custom solutions that rely on UVU's
   audio overriding capabilities.

## Troubleshooting

### Errors after installation

1. Make sure [Cyan.PlayerObjectPool](https://cyanlaser.github.io/CyanPlayerObjectPool/) is added to your project!
2. You might have updated to a version of UdonUtils that is not yet compatible with UdonVoiceUtils
    1. Remove UdonVoiceUtils and UdonUtils from your project via the Creator companion
    2. Add **ONLY** UdonVoiceUtils again *(This will install the latest compatible version of UdonUtils for you as
       well)*

### *Something* is not working in the world

1. Always playtest in the Unity editor using client sim!
2. Check in the console for any errors
    1. Note that there should only be a single error regarding an AudioListener, which is expected and by design
3. Errors produced by my TLP packages can be clicked on and they will highlight the GameObject in your scene hierarchy
   that produced the error
4. Always start with the first error at the top!
5. Read the error messages, they usually tell you what is wrong.
    1. Usually it is something like `<Variable> is not set`
    2. Check that script and look for the variable name mentioned
    3. Set the missing variable, usually the related script that needs to be set there is part of the prefab
    4. Sometimes it makes sense to just start fresh with a clean prefab!

### Occlusion is not working

1. Make sure your colliders are on the `Environment` layer!
2. Walls must have a collision surface on either side to work in both directions!
    1. This is the case when using e.g. basic BoxColliders

## FAQ

### Can I use MeshColliders for occlusion?

**YES**

### How can I change the default/globally active settings?

1. Navigate to your PlayerAudioController prefab in your scene
2. Find the GameObject `Configurations/LocalConfiguration` inside it
3. Change its settings to your liking (this GameObject holds the active settings)
4. when using the menu: apply the same settings to the `Configurations/DefaultConfiguration`
   GameObject (this allows resetting the global settings back to your particular default values)

## References

* [UdonSharp](https://udonsharp.docs.vrchat.com/)
* [VRChat player audio API docs](https://docs.vrchat.com/docs/player-audio)

## Changelog

All notable changes to this project will be documented in this file.

### [3.0.2] - 2025-06-02

#### 🚀 Features

- Ensure players are completely muted when occlusion is maxed out

#### 🐛 Bug Fixes

- Fix not working player occlusion layer enabled-check
- Correct distance factor calculation by considering height-based-multiplier

#### 🚜 Refactor

- Cleanup test a bit
- Modularize audio range reduction logic
- Extracted and moved player update notification code
- Rename methods and variables for clarity in audio range

#### 🧪 Testing

- Create runtime test for reported bug by Ziggor

#### ⚙️ Miscellaneous Tasks

- Update package version and add support for SDK 3.8

### [3.0.1] - 2024-12-22

#### ⚙️ Miscellaneous Tasks

- Support newer, compatible UdonUtils versions, add minimum supported VRC SDK version

### [3.0.0] - 2024-12-14

#### 🚀 Features

- [**breaking**] Migrate to more robust initialization, make executionOrder values unique
- [**breaking**] Deterministic execution order of all scripts, add AdjustableGain example

### [2.0.1] - 2024-12-11

#### 🐛 Bug Fixes

- Prevent update to incompatible sdks

### [2.0.0] - 2024-12-11

#### ⚙️ Miscellaneous Tasks

- Bump version

### [2.0.0-rc.2] - 2024-11-02

#### 🚀 Features

- [**breaking**] Deterministic execution order to address known VRC-bug

### [2.0.0-rc.1] - 2024-08-21

#### 🚀 Features

- *(PlayerAudioOverride)* Add validation for PlayerAudioController
- Add static VoiceUtils
- Add searching and auto setting of PlayerAudioController
- *(PlayerAudioOverride)* Move initialization to SetupAndValidate, hide PlayerAudioController reference in inspector
- *(PlayerAudioController)* Ensure gameobject has correct name after initialization
- Add setup validations
- Ensure sync is off on triggerzone
- Change visualizer to use player api getter
- Extracted occlusion into own class
- Extract ignored players into own class
- Remove startup delay, fix init errors
- Recreate VoiceOverrideRoom prefab
- Recreate main prefabs
- [**breaking**] Improve setup experience, add missing validations, refactor/update code base to latest UdonSharp features

#### 🐛 Bug Fixes

- Compile issues
- PlayerAudioController Gizmos not working

#### 🚜 Refactor

- Move PlayerList not set test to PlayerAudioOverride test
- Reduce redundant checks
- Replace some arrays with lists
- Move to subdirectory

#### 📚 Documentation

- Add additional 2.0.0 change notes and update Readme

#### 🧪 Testing

- *(PlayerAudioOverride)* PlayerList not set
- *(TestPlayerAudioOverride)* Fix warnings
- Updating of players

#### ⚙️ Miscellaneous Tasks

- Recompile

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
