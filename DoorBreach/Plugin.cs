using System;
using System.Globalization;
using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nyxchrono.DoorBreach;

// Suggestions/Ideas:
// Add durability to shovel, delete after 30ish hits?
// Add option to generate more locked doors
// Add option to make heavier melee weapons do more hits per swing against doors

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static BepInEx.Logging.ManualLogSource LogSource { get; set; }
    
    #region Config
    
    private static BepInEx.Configuration.ConfigEntry<bool> _configGeneralEnabled;
    private static BepInEx.Configuration.ConfigEntry<int>  _configGeneralMinHits;
    private static BepInEx.Configuration.ConfigEntry<int>  _configGeneralMaxHits;
    
    /*
    private static BepInEx.Configuration.ConfigEntry<bool>   configIdentBackfill;
    private static BepInEx.Configuration.ConfigEntry<int>    configIdentLength;
    private static BepInEx.Configuration.ConfigEntry<bool>   configIdentSequential;
    private static BepInEx.Configuration.ConfigEntry<string> configIdentStart;
    private static BepInEx.Configuration.ConfigEntry<string> configIdentType;
    */
    
    private void LoadConfig()
    {
       _configGeneralEnabled = Config.Bind(
            new BepInEx.Configuration.ConfigDefinition("General", "Enabled"), 
            true, 
            new BepInEx.Configuration.ConfigDescription(
                "Whether to enable the mod or not",
       new BepInEx.Configuration.AcceptableValueList<bool>(true, false)
            )
        );
        
        _configGeneralMinHits = Config.Bind<int>(
            new BepInEx.Configuration.ConfigDefinition("General", "MinHits"),
            10,
            new BepInEx.Configuration.ConfigDescription(
                "The minimum number of hits required to open a locked door",
                new BepInEx.Configuration.AcceptableValueRange<int>(1, 999)
            )
        );

        _configGeneralMaxHits = Config.Bind<int>(
            new BepInEx.Configuration.ConfigDefinition("General","MaxHits"),
            20,
            new BepInEx.Configuration.ConfigDescription(
                "The maximum number of hits required to open a locked door\nMAKE SURE THIS VALUE IS GREATER THAN MinHits!!!",
                new BepInEx.Configuration.AcceptableValueRange<int>(1, 999)
            )
        );
        
        // Prevent min being lower than the max
        if (_configGeneralMinHits.Value > _configGeneralMaxHits.Value)
        {
            LogSource.LogWarning($"{nameof(_configGeneralMaxHits)} was less than {nameof(_configGeneralMinHits)}!");
            LogSource.LogWarning($"Setting {nameof(_configGeneralMaxHits)} to ({nameof(_configGeneralMinHits)} * 2)");
            _configGeneralMaxHits.Value = _configGeneralMinHits.Value * 2;
        }
    }
    
    #endregion Config
    
    #region Utils
    
    private static object GetInstanceField(Type type, object instance, string fieldName)
    {
        const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        FieldInfo field = type.GetField(fieldName, bindFlags);
        return field?.GetValue(instance);
    }
    
    #endregion Utils
    
    #region Unity Methods
    
    private void Awake()
    {
        LogSource = Logger;
        LoadConfig();
        
        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is initializing");

        if (_configGeneralEnabled.Value == false)
        {
            LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is disabled due to config setting");
            return;
        }
        
        On.Shovel.HitShovel += Shovel_HitShovel;
        On.RoundManager.SetLockedDoors += RoundManager_SetLockedDoors;

        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has initialized");
    }
    
    private void OnDestroy()
    {
        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} was unloaded!");
    }

    #endregion

    #region Custom Components

    public class DoorHitInfo : MonoBehaviour
    {
        public int NumOfHits;
        public DoorLock Lock;

        public void Start()
        {
            NumOfHits = 0;
            Lock = GetComponent<DoorLock>();

            if (Lock.isLocked)
            {
                NumOfHits = new System.Random().Next(_configGeneralMinHits.Value, _configGeneralMaxHits.Value);
                LogSource.LogDebug($"Door ({Lock.gameObject.GetInstanceID()}) will take {NumOfHits} hits");
            }
        }
        
        public void OnHit()
        {
            if (--NumOfHits <= 0)
            {
                OpenDoor(Lock);
            }
            else
            {
                LogSource.LogDebug($"Door ({Lock.gameObject.GetInstanceID()}) has {nameof(NumOfHits)} remaining: {NumOfHits}");
            }
        }

        public void OpenDoor(DoorLock door)
        {
            // Unlock door if locked
            if (door.isLocked)
            {
                LogSource.LogDebug($"Unlocking door ({door.gameObject.GetInstanceID()})");
                door.UnlockDoorSyncWithServer(); // Also unlocks locally
            }

            // Door is already open, do nothing
            if ((bool)GetInstanceField(typeof(DoorLock), door, "isDoorOpened")) 
                return;
            
            LogSource.LogDebug($"Opening door ({door.gameObject.GetInstanceID()})");
            // door.OpenOrCloseDoor(StartOfRound.Instance.localPlayerController);
            door.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(true, true);
            door.OpenDoorAsEnemyServerRpc();
        }
    }

    #endregion Custom Components

    #region Hooks

    private static void RoundManager_SetLockedDoors(On.RoundManager.orig_SetLockedDoors orig, RoundManager self, Vector3 mainEntrancePosition)
    {
        // Call the original game function
        orig(self, mainEntrancePosition);

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
    
    private static void Shovel_HitShovel(On.Shovel.orig_HitShovel orig, Shovel self, bool cancel)
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
    
    #endregion Hooks
    
}
