using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace QuestProModule;

public class QuestProMod : ResoniteMod
{
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float> EyeOpennessExponent =
      new("quest_pro_eye_open_exponent",
        "Exponent to apply to eye openness.  Can be updated at runtime.  Useful for applying different curves for how open your eyes are.",
        () => 1.0f);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float> EyeWideMultiplier =
      new("quest_pro_eye_wide_multiplier",
        "Multiplier to apply to eye wideness.  Can be updated at runtime.  Useful for multiplying the amount your eyes can widen by.",
        () => 1.0f);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float> EyeMovementMultiplier =
      new("quest_pro_eye_movement_multiplier",
        "Multiplier to adjust the movement range of the user's eyes.  Can be updated at runtime.", () => 1.0f);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float> EyeExpressionMultiplier =
      new("quest_pro_eye_expression_multiplier",
        "Multiplier to adjust the range of the user's eye expressions.  Can be updated at runtime.", () => 1.0f);

    private static ModConfiguration? _config;

    private static SyncCell<FbMessage>? _store;
    private static AlvrConnection? _connection;
    private static FbInputDriver? _driver;

    private const int PORT = 0xA1F7; // 41463

    public override string Name => "QuestPro4Reso";
    public override string Author => "Noble, dfgHiatus, Geenz, Earthmark";
    public override string Version => "3.2.0";
    public override string Link => "https://github.com/noblereign/QuestPro4Reso";

    public override void OnEngineInit()
    {
        _config = GetConfiguration()!;
        _config.OnThisConfigurationChanged += OnConfigurationChanged;

        Harmony harmony = new Harmony("dog.glacier.QuestPro4Reso");
        harmony.PatchAll();

        var engine = Engine.Current;
        if (engine != null)
        {
            engine.RunPostInit(() => RegisterDriver(engine));
        }
        else
        {
            Error($"[VRCFTReceiver] OnEngineInit failed: Engine.Current is null");
        }
    }


    private static void RegisterDriver(Engine engine)
    {
        try
        {
            if (engine.InputInterface != null)
            {
                _store = new SyncCell<FbMessage>();
                _connection = new AlvrConnection(PORT, _store);
                _driver = new FbInputDriver(_store, _connection);
                engine.InputInterface.RegisterInputDriver(_driver);
                Msg("[QuestPro4Reso] Driver initialized successfully!");
            }
            else
            {
                Error($"RegisterDriver failed: Engine.InputInterface is null");
            }
        }
        catch (Exception ex)
        {
            Warn($"[QuestPro4Reso] Driver initialization failed: {ex}");
        }
    }

    private void OnConfigurationChanged(ConfigurationChangedEvent @event)
    {
        if (@event.Key == EyeOpennessExponent)
        {
            if (@event.Config.TryGetValue(EyeOpennessExponent, out var openExp))
            {
                _driver!.EyeOpenExponent = openExp;
            }
        }

        if (@event.Key == EyeWideMultiplier)
        {
            if (@event.Config.TryGetValue(EyeWideMultiplier, out var wideMulti))
            {
                _driver!.EyeWideMulti = wideMulti;
            }
        }

        if (@event.Key == EyeMovementMultiplier)
        {
            if (@event.Config.TryGetValue(EyeMovementMultiplier, out var moveMulti))
            {
                _driver!.EyeMoveMulti = moveMulti;
            }
        }

        if (@event.Key == EyeExpressionMultiplier)
        {
            if (@event.Config.TryGetValue(EyeExpressionMultiplier, out var eyeExpressionMulti))
            {
                _driver!.EyeExpressionMulti = eyeExpressionMulti;
            }
        }
    }
}