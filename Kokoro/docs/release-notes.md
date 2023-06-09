[← back to readme](README.md)

# Release notes

## 2.1.0
Released 9 June 2023.

* Added `FarmerExt.GetDatingState()`.
* Added `FarmerExt.ConsumeItem()`.
* Improved `ItemExt.IsSameItem()`.
* Improved Harmony errors.
* Fixed an error when [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348) was not installed.

## 2.0.1
Released 30 May 2023.

* Fixed a bug where mods referencing the Shrike library failed to load, by force-referencing Shrike assemblies in Kokoro's code.

## 2.0.0
Released 29 May 2023.

* Now includes the [Shrike](https://github.com/Nanoray-pl/Shrike) sequence matching library as a dependency.
* New system for handling multiplayer mod messages, replacing old utility methods.
* New system for tracking machines.
* Improvements to the Skill utilities.
* Improvements to Harmony utilities.
* Fixed reflection utilities.

## 1.0.0
Released 28 February 2023.

* Initial release.