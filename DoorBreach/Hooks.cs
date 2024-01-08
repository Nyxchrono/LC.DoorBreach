using System.Reflection;
using MonoMod.Utils;
using UnityEngine;
using static UnityEngine.Object;
using static Nyxchrono.DoorBreach.Plugin;
using static Nyxchrono.DoorBreach.Components;

namespace Nyxchrono.DoorBreach;

public class Hooks
{
    // For mods that allow locking of doors after round has started
    internal static bool IsStartOfRoundLockingDoors;
    
    internal static void RoundManager_SetLockedDoors(On.RoundManager.orig_SetLockedDoors orig, RoundManager self, Vector3 mainEntrancePosition)
    {
        IsStartOfRoundLockingDoors = true;
        
        // Call the original game function
        orig(self, mainEntrancePosition);

        IsStartOfRoundLockingDoors = false;
        
        // Add our custom component to the door
        int numOfDoors = 0;
        foreach(DoorLock door in FindObjectsByType<DoorLock>(FindObjectsSortMode.None))
        {
            door.gameObject.AddComponent<DoorHitInfo>();
            numOfDoors++;
            
            // Debug to make all doors locked at beginning
            //door.LockDoor();
        }
        
        LogSource.LogDebug($"Added {nameof(DoorHitInfo)} to {numOfDoors} doors");
    }
    
    internal static void Shovel_HitShovel(On.Shovel.orig_HitShovel orig, Shovel self, bool cancel)
    {
        // Call the original game function
        orig(self, cancel);

        // Raycast in front of the player // This is exactly what the game does, except I've added a mask to get only doors
        Transform gpCamTransform = self.playerHeldBy.gameplayCamera.transform;
        var hits = Physics.SphereCastAll(
            gpCamTransform.position + gpCamTransform.right * -0.35f,
            0.75f,
            gpCamTransform.forward,
            1f,
            1 << 9,
            QueryTriggerInteraction.Collide
        );
        
        // For each object in that sphere, check if contains the DoorHitInfo component we added
        // at the beginning of the round
        foreach (RaycastHit hit in hits)
        {
            // check the collider has locked door
            DoorHitInfo lockedDoorInfo = hit.collider.gameObject.GetComponent<DoorHitInfo>();
            if (lockedDoorInfo != null)
            {
                //string lockTypeString = (lockedDoorInfo.doorLock.isLocked ? "locked" : "unlocked");
                //LogSource.LogDebug($"Hit a {lockTypeString} door ({lockedDoorInfo.doorLock.gameObject.GetInstanceID()})");
                lockedDoorInfo.OnHit();
                break;
            }
        }
    }

    internal static void DoorLock_LockDoor(On.DoorLock.orig_LockDoor orig, DoorLock self, float timeToLockPick = 30f)
    {
        orig(self, timeToLockPick);

        if (IsStartOfRoundLockingDoors)
            return;
        
        // For mods that allow locking of doors after round has started:
        LogSource.LogDebug($"Locking door after RoundManager.SetLockedDoors");
        DoorHitInfo doorHitInfo = self.GetComponent<DoorHitInfo>();
        if (doorHitInfo is not null)
            doorHitInfo.NumOfHits = new System.Random().Next(_configGeneralMinHits.Value, _configGeneralMaxHits.Value);
    }
}