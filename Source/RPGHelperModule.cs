using System;

namespace Celeste.Mod.RPGHelper;

public class RPGHelperModule : EverestModule {
    public static RPGHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(RPGHelperModuleSettings);
    public static RPGHelperModuleSettings Settings => (RPGHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(RPGHelperModuleSession);
    public static RPGHelperModuleSession Session => (RPGHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(RPGHelperModuleSaveData);
    public static RPGHelperModuleSaveData SaveData => (RPGHelperModuleSaveData) Instance._SaveData;

    public RPGHelperModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(RPGHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(RPGHelperModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        HealthController.Load();
    }

    public override void Unload() {
        HealthController.Unload();
    }
}