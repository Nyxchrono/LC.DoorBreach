using BepInEx;

namespace Nyxchrono.DoorBreach;

// Suggestions/Ideas:
// Add durability to shovel, delete after 30ish hits?
// Add option to generate more locked doors
// Add option to make heavier melee weapons do more hits per swing against doors

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static BepInEx.Logging.ManualLogSource LogSource { get; set; }
    
    #region Config
    
    internal static BepInEx.Configuration.ConfigEntry<bool> _configGeneralEnabled;
    internal static BepInEx.Configuration.ConfigEntry<int>  _configGeneralMinHits;
    internal static BepInEx.Configuration.ConfigEntry<int>  _configGeneralMaxHits;
    
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
        
        //ApplyPatches();
        ApplyHooks();
        
        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has initialized");
    }
    
    private void OnDestroy()
    {
        LogSource.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} was unloaded!");
    }

    #endregion
    
    //public void ApplyPatches()
    //{
    //    
    //}
    
    public void ApplyHooks()
    {
        On.Shovel.HitShovel += Hooks.Shovel_HitShovel;
        On.RoundManager.SetLockedDoors += Hooks.RoundManager_SetLockedDoors;
        On.DoorLock.LockDoor += Hooks.DoorLock_LockDoor;
    }
    
}
