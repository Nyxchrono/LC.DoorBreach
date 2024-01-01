using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using Nyxchrono.LC.DoorBreach;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rethunk.LC.RadarIdentQuickSwitch;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static BepInEx.Logging.ManualLogSource LogSource { get; private set; }

    #region Configuration
    public static BepInEx.Configuration.ConfigFile config;

    public static BepInEx.Configuration.ConfigEntry<bool> configGeneralEnabled;
    /*public static BepInEx.Configuration.ConfigEntry<bool> configIdentBackfill;
    public static BepInEx.Configuration.ConfigEntry<int> configIdentLength;
    public static BepInEx.Configuration.ConfigEntry<bool> configIdentSequential;
    public static BepInEx.Configuration.ConfigEntry<string> configIdentStart;
    public static BepInEx.Configuration.ConfigEntry<string> configIdentType;*/

    private static void LoadConfig()
    {
        configGeneralEnabled = config.Bind<bool>(
            new BepInEx.Configuration.ConfigDefinition(
                "General",
                "Enabled"
            ),
            true,
            new BepInEx.Configuration.ConfigDescription(
                "Whether or not to enable the plugin. If disabled, <strong>None</strong> of the following options do anything.",
                new BepInEx.Configuration.AcceptableValueList<bool>(true, false)
            )
        );

        /*configIdentBackfill = config.Bind<bool>(
            new BepInEx.Configuration.ConfigDefinition(
                "Identifiers",
                "Backfill"
            ),
            true,
            new BepInEx.Configuration.ConfigDescription(
                "Whether or not to backfill the identifiers of players that join mid-round.\n\nIf false, idents are not re-used.",
                new BepInEx.Configuration.AcceptableValueList<bool>(true, false)
            )
        );

        configIdentLength = config.Bind<int>(
            new BepInEx.Configuration.ConfigDefinition(
                "Identifiers",
                "Length"
            ),
            3,
            new BepInEx.Configuration.ConfigDescription(
                "The length of the identifiers to use.\n\nIf sequential is enabled, this is the maximum length of the identifier.\n\nIf sequential is disabled, this is the exact length of the identifier.",
                new BepInEx.Configuration.AcceptableValueRange<int>(1, 10)
            )
        );

        configIdentSequential = config.Bind<bool>(
            new BepInEx.Configuration.ConfigDefinition(
                "Identifiers",
                "Sequential"
            ),
            true,
            new BepInEx.Configuration.ConfigDescription(
                "Whether or not to use sequential identifiers.\n\nIf true, identifiers will be assigned in order of joining.\n\nIf false, identifiers will be assigned randomly.",
                new BepInEx.Configuration.AcceptableValueList<bool>(true, false)
            )
        );

        configIdentStart = config.Bind<string>(
            new BepInEx.Configuration.ConfigDefinition(
                "Identifiers",
                "Start"
            ),
            "0",
            new BepInEx.Configuration.ConfigDescription(
                "The first identifier to use.\n\nIf sequential is enabled, this is the first identifier to use.\n\nIf sequential is disabled, this is the only identifier to use.",
                new BepInEx.Configuration.AcceptableValueList<string>(
                    "0 (Numeric)", "1 (Numeric)",
                    "A (Latin)",
                    "α (Greek)",
                    "Alpha (NATO)",
                    "Alfa (IPA)"
                )
            )
        );

        configIdentType = config.Bind<string>(
            new BepInEx.Configuration.ConfigDefinition(
                "Identifiers",
                "Type"
            ),
            "Numeric",
            new BepInEx.Configuration.ConfigDescription(
                "The type of identifier to use.\n\nIf sequential is enabled, this is the type of identifier to use.\n\nIf sequential is disabled, this is ignored.",
                new BepInEx.Configuration.AcceptableValueList<string>(
                    "Numeric",
                    "Latin Alphabet",
                    "Greek Alphabet",
                    "Phonetic (NATO)",
                    "Phonetic (IPA)"
                )
            )
        );*/
    }
    #endregion

    /// <summary>
    /// Uses reflection to get the field value from an object.
    /// </summary>
    ///
    /// <param name="type">The instance type.</param>
    /// <param name="instance">The instance object.</param>
    /// <param name="fieldName">The field's name which is to be fetched.</param>
    ///
    /// <returns>The field value from the object.</returns>
    internal static object GetInstanceField(Type type, object instance, string fieldName)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static;
        FieldInfo field = type.GetField(fieldName, bindFlags);
        return field.GetValue(instance);
    }

    internal static object GetInstanceField(object instance, string fieldName)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static;
        FieldInfo field = instance.GetType().GetField(fieldName, bindFlags);
        return field.GetValue(instance);
    }

    #region Unity Methods
    private void Awake()
    {
        config = Config;
        LogSource = Logger;
        LoadConfig();

        On.Shovel.HitShovel += Shovel_HitShovel;
        On.RoundManager.SetLockedDoors += RoundManager_SetLockedDoors;

        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} was loaded!");
    }

    public class DoorHitInfo : MonoBehaviour
    {
        int NumOfHits;

        public DoorLock DoorLock;

        public void Start()
        {
            NumOfHits = 0;
            DoorLock = GetComponent<DoorLock>();

            if (DoorLock.isLocked)
            {
                NumOfHits = new System.Random().Next(5, 15);
                LogSource.LogDebug($"Door {DoorLock.gameObject.GetInstanceID()} will take {NumOfHits} hits");
                //LogSource.LogDebug
            }
        }

        public void OnHit()
        {
            if (NumOfHits-- <= 0)
            {
                LogSource.LogMessage("Door should open now");
                OpenDoor(DoorLock);
            }
            else
            {
                LogSource.LogMessage($"NumOfHits remaining: {NumOfHits}");
            }
        }

        public void OpenDoor(DoorLock door)
        {
            LogSource.LogMessage($"OpenDoor() called!");

            // Unlock door if locked
            if (door.isLocked)
            {
                door.UnlockDoorSyncWithServer(); // Also unlocks locally
                door.OpenDoorAsEnemyServerRpc(); // Tell other clients to open door

                // Open the door locally
                door.OpenOrCloseDoor(StartOfRound.Instance.localPlayerController);
                LogSource.LogMessage($"Unlocking door {door.gameObject.GetInstanceID()}");
            }

            // Open door if closed
            var isDoorOpened = (bool)GetInstanceField(typeof(DoorLock), door, "isDoorOpened");
            if (!isDoorOpened)
            {
                door.OpenDoorAsEnemyServerRpc();
                door.OpenOrCloseDoor(StartOfRound.Instance.localPlayerController);
                LogSource.LogMessage($"Opening door {door.gameObject.GetInstanceID()}");
            }
        }
    }

    private void RoundManager_SetLockedDoors(On.RoundManager.orig_SetLockedDoors orig, RoundManager self, Vector3 mainEntrancePosition)
    {
        // Call the original game function
        orig(self, mainEntrancePosition);

        // Add our cust om component to the door
        foreach(var door in FindObjectsByType<DoorLock>(FindObjectsSortMode.None))
        {
            door.gameObject.AddComponent<DoorHitInfo>();

            // Debugging stuffs:
            // For each door, set them as locked
            if ((bool)GetInstanceField(typeof(DoorLock), door, "isDoorOpened"))
            {
                door.OpenOrCloseDoor(StartOfRound.Instance.localPlayerController);
                door.LockDoor();
            }
        }
    }

    private void Shovel_HitShovel(On.Shovel.orig_HitShovel orig, Shovel self, bool cancel)
    {
        LogSource.LogInfo($"{self?.GetType()?.Name} Shovel: {self?.ToString()}");

        // Call the original game function
        orig(self, cancel);

        // Raycast in front of the plyaer
        var hits = Physics.OverlapSphere(self.transform.position, 1f); // in v45 door layer is 9, but not all doors??

        // For each object in that sphere, check if contains the DoorHitInfo component we added
        // at the beginning of the round
        foreach (var hit in hits)
        {
            // check the collider has locked door
            var lockedDoorInfo = hit.gameObject.GetComponent<DoorHitInfo>();
            if (lockedDoorInfo != null)
            {
                string lockTypeString = (lockedDoorInfo.DoorLock.isLocked ? "LOCKED" : "UNLOCKED");
                LogSource.LogMessage($"IT'S A {lockTypeString} DOOR; LAYER = {lockedDoorInfo.gameObject.layer}");

                lockedDoorInfo.OnHit();
            }
        }
    }

    private void OnDestroy()
    {
        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} was unloaded!");
    }
    #endregion

}