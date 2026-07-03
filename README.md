# Udon Voice Utilities

[![Total downloads](https://img.shields.io/github/downloads/Guribo/UdonVoiceUtils/total?style=flat-square&logo=appveyor)](https://github.com/Guribo/UdonVoiceUtils/releases)

## What is UVU?

**UdonVoiceUtils** is a selection of scripts and prefabs that can modify player audio settings in real time.

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
  directionality, Occlusion*
- [The Great Pub](https://vrchat.com/home/world/wrld_6caf5200-70e1-46c2-b043-e3c4abe69e0f) by owlboy - *Occlusion, Voice
  directionality*
- [The Pool Parlor](https://vrchat.com/home/world/wrld_99bdc4c6-b80c-49f3-aae0-5d67017d8340) by Toasterly - *Voice
  directionality, Audio zones*
- [Virtual Performing Arts Theater](https://vrchat.com/home/world/wrld_f1ae5929-a881-4c21-acc0-8d5cb9bf919f) by
  DjembeDragon - *Private channels, Audio Zones, Reverb, Audience/Stage volume control*
- [Hexahedron Estate](https://vrchat.com/home/world/wrld_b477c764-43a2-43f0-8355-d2871af3881d) by Ziggor - *Occlusion*
- [Cool Party](https://vrchat.com/home/world/wrld_2a038385-c524-45f1-abbe-7c5d648bf0c0) by 
  n8bits - *Private audio zones, Wall occlusion, custom default player audio settings*
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

## Getting Started

1. Install via [VPM](https://guribo.github.io/TLP/) (VRChat Creator Companion or ALCOM) → [Installation Guide](https://github.com/Guribo/UdonVoiceUtils/wiki/Installation)
2. Add to scene → [Minimal Scene Setup](https://github.com/Guribo/UdonVoiceUtils/wiki/Minimal-Scene-Setup)
3. Play in editor with ClientSim

## Documentation

Full documentation lives on the [Wiki](https://github.com/Guribo/UdonVoiceUtils/wiki):

- [Installation](https://github.com/Guribo/UdonVoiceUtils/wiki/Installation) | [Scene Setup](https://github.com/Guribo/UdonVoiceUtils/wiki/Minimal-Scene-Setup) | [Troubleshooting](https://github.com/Guribo/UdonVoiceUtils/wiki/Troubleshooting)
- [Architecture](https://github.com/Guribo/UdonVoiceUtils/wiki/Architecture) | [Occlusion](https://github.com/Guribo/UdonVoiceUtils/wiki/Occlusion) | [Voice Directionality](https://github.com/Guribo/UdonVoiceUtils/wiki/Voice-Directionality)
- [Private Voice Channels](https://github.com/Guribo/UdonVoiceUtils/wiki/Private-Voice-Channels) | [Reverb](https://github.com/Guribo/UdonVoiceUtils/wiki/Reverb-in-VRChat) | [Performance](https://github.com/Guribo/UdonVoiceUtils/wiki/Performance)
- [Tutorial #1: Voice Override Zone](https://github.com/Guribo/UdonVoiceUtils/wiki/Tutorial-%231-How-to-create-a-voice-override-zone) | [Tutorial #2: Private Channels](https://github.com/Guribo/UdonVoiceUtils/wiki/Tutorial-%232-Setting-up-private-voice-channels)
- [Debug and Test Mode](https://github.com/Guribo/UdonVoiceUtils/wiki/Debug-and-Test-Mode) | [Updating](https://github.com/Guribo/UdonVoiceUtils/wiki/Updating) | [Uninstall](https://github.com/Guribo/UdonVoiceUtils/wiki/Uninstall) | [FAQ](https://github.com/Guribo/UdonVoiceUtils/wiki/FAQ)

## References

* [UdonSharp](https://udonsharp.docs.vrchat.com/)
* [VRChat player audio API docs](https://docs.vrchat.com/docs/player-audio)

## Changelog


## Changelog

### [5.1.4] - 2026-05-02

#### 🐛 Bug Fixes

- *(inspectors)* Remove redundant warnings in PlayerAudioControllerEditor (closes #44)
### [5.1.3] - 2026-05-01

#### 🐛 Bug Fixes

- Update field definitions and references for consistency with UdonUtils update

#### ⚙️ Miscellaneous Tasks

- *(dependencies)* Update VRChat SDK and TLP UdonUtils versions
### [5.1.2] - 2026-04-25

#### ⚙️ Miscellaneous Tasks

- Update to UdonUtils 13.0.0
- Bump version to 5.1.2
### [5.1.1] - 2026-04-25

#### ⚙️ Miscellaneous Tasks

- Bump version to 5.1.1 for automatic release testing
### [5.1.0] - 2026-04-25

#### 🚀 Features

- *(Editor)* Add TLP_UDONVOICETILS define whenever UdonUtils is added to a project

#### ⚙️ Miscellaneous Tasks

- *(PlayerAudioOverride)* Make PrivacyChannelIds public
- Change MicModel field to public in MicActivation (#43)
- Improve release automation
### [5.0.1] - 2025-11-15

#### 🐛 Bug Fixes

- *(Microphone)* Reduce log spam during pickup and drop

#### 🎨 Styling

- *(MicActivation)* Cleanup sections
### [5.0.0] - 2025-11-11

#### 🚀 Features

- *(Microphone)* [**breaking**] Add new mic activation (push-to-talk, toggle), rework/simplify mic logic, add 3 new mic prefabs
### [4.0.0] - 2025-11-09

#### 🚀 Features

- *(PlayerAudioOverride)* Allow players to be part of multiple privacy channels
- *(PlayerAudioOverrideList)* [**breaking**] Change signature of GetMaxPriority
- *(VoiceRangeVisualizer)* [**breaking**] Simplify logic and improve head position accuracy
- *(PlayerAudioController)* [**breaking**] Remove player update event functionality, refactor
- *(PlayerAudioView)* Change event handling to UdonEvents, add debug logging for variable changes
- *(PlayerAudioConfigurationModel)* Convert to safer sync approach with dedicated copies of network values
- *(VoiceRangeVisualizer)* Simplify tracking logic

#### 🐛 Bug Fixes

- Broken prefab reference to UiButton
- Broken mic prefab due to incorrect sync serialization (reported Manual despite being NoVariableSync)
- Correct mute behaviour in demo scene to match description of bathroom
- *(TestMaxOcclusionMutesPlayers)* Ensure test reverts to initially encountered Audio Configuration, move modification to test setup
- *(PlayerAudioOverride)* Fix sanity check
- *(Menu)* Fix warnings caused by new event not being pre-initialized
- *(Prefabs)* Dissallow multiple PlayerAudioController prefabs in the scene

#### 🚜 Refactor

- *(core)* Remove debug logging from GetMaxPriority method

#### 📚 Documentation

- *(Readme)* Update to VRC SDK 3.9.0
- Add world by n8bits

#### 🧪 Testing

- *(TestMaxOcclusionMutesPlayers)* Improve logging of errors
- *(TestZoneEntering)* At protection agains infinite loop
- Update to test latest changes

#### ⚙️ Miscellaneous Tasks

- *(docs)* Update README with new world entry and SDK version
- Bump version and minimum VRC World SDK version to 3.8.2
- Fix missing separator in package.json
- Disable some log spam in release mode
- Add script descriptions, update scene
- Update demo scene and prefabs
- Speed up
- Improve log message about vrc default audio settings
- Update License
- Update to UdonUtils 12.0.0
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
