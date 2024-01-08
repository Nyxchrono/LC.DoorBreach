# Changelog for DoorBreach

## [1.1.0] - 2024-01-08
- Added/Improved compatibility for mods that allow locking of doors after round has started
- For the nerds:
  - Cleaned up `Plugin.cs`
  - Added `Components.cs`, `Hooks.cs`, `Utils.cs`
  - Added a hook for `DoorLock.LockDoor`
  - Only call `UnlockDoorServerRpc` as that will eventually unlock it clientside

## [1.0.1] - 2024-01-05
- Update README.md

## [1.0.0] - 2024-01-05
- Finished initial release
