using UnityEngine;
using static Nyxchrono.DoorBreach.Plugin;

namespace Nyxchrono.DoorBreach;

public class Components
{
    public class DoorHitInfo : MonoBehaviour
    {
        public int NumOfHits;
        public DoorLock Lock;

        public void Start()
        {
            NumOfHits = 0;
            Lock = GetComponent<DoorLock>();

            if (Lock is not null && Lock.isLocked)
            {
                NumOfHits = new System.Random().Next(_configGeneralMinHits.Value, _configGeneralMaxHits.Value);
                LogSource.LogDebug($"Door ({Lock.gameObject.GetInstanceID()}) will take {NumOfHits} hits");
            }
        }
        
        public void OnHit()
        {
            if (--NumOfHits <= 0)
                OpenDoor(Lock);
            else
                LogSource.LogDebug($"Door ({Lock.gameObject.GetInstanceID()}) has {nameof(NumOfHits)} remaining: {NumOfHits}");
        }
        
        public void OpenDoor(DoorLock door)
        {
            // Unlock door if locked
            if (door.isLocked)
            {
                LogSource.LogDebug($"Unlocking door ({door.gameObject.GetInstanceID()})");
                door.UnlockDoorServerRpc(); // Eventually also calls unlock on client side
            }

            // Door is already open, do nothing
            if (Utils.GetInstanceField<bool>(typeof(DoorLock), door, "isDoorOpened")) 
                return;
            
            LogSource.LogDebug($"Opening door ({door.gameObject.GetInstanceID()})");
            door.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(true, true);
            door.OpenDoorAsEnemyServerRpc();
        }
    }

}