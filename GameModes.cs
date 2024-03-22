using HarmonyLib;
using Il2Cpp;
using Il2CppSilica.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QAPI
{
    public static class GameModes
    {
        #region Variables
        private static readonly string SANDBOX_NAME = "MP_Sandbox";

        public static GameObject? CurrentGameMode
        {
            get { return currentGameMode; }
        }

        private static Dictionary<string, CustomGameModeInfo> CustomGameModes = new();
        private static Action<string, GameModeInfo>? OnLevelEndLoadDelegate;
        private static Action<string, GameModeInfo>? OnLevelBeginLoadDelegate;
        private static string? queuedGameMode;
        private static GameObject? currentGameMode;
        private static bool reinitializePlaystyleMenu = true;
        #endregion

        #region Initialization
        internal static void Initialize()
        {
            OnLevelEndLoadDelegate = new Action<string, GameModeInfo>(OnLevelEndLoad);
            GameEvents.OnLevelEndLoad += OnLevelEndLoadDelegate;
            OnLevelBeginLoadDelegate = new Action<string, GameModeInfo>(OnLevelBeginLoad);
            GameEvents.OnLevelBeginLoad += OnLevelBeginLoadDelegate;
        }
        #endregion

        #region GameModes
        public static bool QueueGameMode(string? gameMode)
        {
            if (String.IsNullOrEmpty(gameMode))
            {
                queuedGameMode = string.Empty;
                return true;
            }

            if (!CustomGameModes.ContainsKey(gameMode))
            {
                Log.LogOutput(
                    $"QueueGameMode: GameMode '{gameMode}' not found",
                    Log.ELevel.Warning
                );
                return false;
            }

            queuedGameMode = gameMode;
            return true;
        }

        public static bool RegisterGameMode(CustomGameModeInfo info)
        {
            if (!GameDatabase.Database)
            {
                Log.LogOutput($"RegisterGameMode: Database not ready yet!");
                return false;
            }

            if (!ValidateInfo(info))
                return false;

            info.Object.transform.SetParent(QAPIMod.ParentContainer);
            info.Object.SetActive(false);
            CustomGameModes.Add(info.DisplayName, info);

            Log.LogOutput($"Registered new game mode '{info.DisplayName}'");

            return true;
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(ServerSettingsForm), nameof(ServerSettingsForm.LoadLevelsList))]
        internal static class Patch_SetLevelDeveloperMode
        {
            public static void Prefix()
            {
                if (String.IsNullOrEmpty(queuedGameMode))
                    return;

                ToggleDevModeOptions(false);
            }

            public static void Postfix()
            {
                if (String.IsNullOrEmpty(queuedGameMode))
                    return;

                ToggleDevModeOptions(true);
            }
        }

        [HarmonyPatch(
            typeof(ServerSettingsForm),
            nameof(ServerSettingsForm.Refresh),
            new Type[] { typeof(GameModeInfo) }
        )]
        internal static class Patch_SetCustomGameModeName
        {
            public static void Postfix(ServerSettingsForm __instance, GameModeInfo __0)
            {
                if (
                    String.IsNullOrEmpty(queuedGameMode)
                    || !__0.DisplayName.ToLower().Contains("sandbox")
                    || !CustomGameModes.ContainsKey(queuedGameMode)
                )
                    return;

                Log.LogOutput($"Setting custom GameMode text");
                var customGameMode = CustomGameModes[queuedGameMode];

                __instance.GameModeName.text = customGameMode.DisplayName;
            }
        }

        [HarmonyPatch(typeof(Playstyle), nameof(Playstyle.Load))]
        internal static class Patch_DisableCustomPlaystyleInfo
        {
            public static bool Prefix(Playstyle __instance, PlaystyleButton __0)
            {
                if (__0.GameMode.ObjectName.Equals(SANDBOX_NAME))
                {
                    if (!__instance.animator)
                        __instance.animator = __instance.GetComponent<Animator>();

                    __instance.IsEnabled = __0.GameMode.Enabled;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlaystyleMenu), nameof(PlaystyleMenu.OnEnable))]
        internal static class Patch_InitializePlaystyleMenu
        {
            public static void Prefix(PlaystyleMenu __instance)
            {
                queuedGameMode = null;

                if (reinitializePlaystyleMenu)
                {
                    var buttonTemplate = __instance.Buttons[0].Button.gameObject;
                    var buttonList = __instance.Buttons.ToList();

                    foreach (var gameMode in CustomGameModes)
                        if (gameMode.Value.Enabled)
                            buttonList.Add(CreatePlaystyleButton(buttonTemplate, gameMode.Value));

                    __instance.Buttons = buttonList.ToArray();

                    reinitializePlaystyleMenu = false;
                }
            }
        }
        #endregion

        #region Events
        private static void OnSelectCustomGameMode(string customGamemode)
        {
            queuedGameMode = customGamemode;
        }

        // Destroy current GameMode on level change
        private static void OnLevelBeginLoad(string sceneName, GameModeInfo info)
        {
            var oldScene = SceneManager.GetActiveScene().name.ToLower();

            if (oldScene.Contains("main") || oldScene.Contains("intro"))
                return;

            if (currentGameMode != null)
                GameObject.Destroy(currentGameMode);

            reinitializePlaystyleMenu = true;
        }

        // Load a requested GameMode if a map is loaded in sandbox mode
        private static void OnLevelEndLoad(string sceneName, GameModeInfo info)
        {
            sceneName = sceneName.ToLower();

            if (
                String.IsNullOrEmpty(queuedGameMode)
                || sceneName.Contains("main")
                || sceneName.Contains("intro")
                || info != null
                || queuedGameMode == null
            )
                return;

            if (
                !CustomGameModes.ContainsKey(queuedGameMode)
                || CustomGameModes[queuedGameMode].Object == null
            )
            {
                Log.LogOutput(
                    $"OnLevelEndLoad: GameMode not found or CustomGameModeInfo.Object is null",
                    Log.ELevel.Warning
                );
                return;
            }

            currentGameMode = GameObject.Instantiate(CustomGameModes[queuedGameMode].Object);
            currentGameMode.SetActive(true);
            Log.LogOutput($"Level loaded with Sandbox GameMode!");
        }
        #endregion

        #region Helpers
        private static void ToggleDevModeOptions(bool state)
        {
            GameModeInfo.GetByName(SANDBOX_NAME).IsDeveloperOnly = state;

            foreach (var level in GameDatabase.Database.AllLevels)
            {
                var levelName = level.DisplayName.ToLower();

                if (
                    levelName.Contains("siege")
                    || levelName.Contains("dome")
                    || levelName.Contains("proving")
                )
                    level.IsDeveloperOnly = state;
            }
        }

        private static PlaystyleButton CreatePlaystyleButton(
            GameObject template,
            CustomGameModeInfo info
        )
        {
            var newPlayStyleButton = new PlaystyleButton();

            var newGameObject = GameObject.Instantiate(template);
            newGameObject.name = info.DisplayName;
            newGameObject.transform.SetParent(template.transform.parent);
            var newButton = newGameObject.GetComponent<Button>();
            var newAnimator = newGameObject.GetComponent<Animator>();
            var newPlaystyle = newGameObject.GetComponent<Playstyle>();

            for (int e = 0; e < newPlaystyle.labels.Length; e++)
                newPlaystyle.labels[e].text = info.DisplayName;

            newPlaystyle.description.text = info.DisplayDescription;

            newPlayStyleButton.Button = newButton;
            newPlayStyleButton.Button.onClick.AddListener(
                ((UnityAction)(() => OnSelectCustomGameMode(info.DisplayName)))
            );

            newPlayStyleButton.Animator = newAnimator;
            newPlayStyleButton.Playstyle = newPlaystyle;
            newPlayStyleButton.GameMode = GameModeInfo.GetByName(SANDBOX_NAME);

            if (info.DisplaySprite != null)
                newPlaystyle.image.sprite = info.DisplaySprite;

            return newPlayStyleButton;
        }

        private static bool ValidateInfo(CustomGameModeInfo info)
        {
            if (info.DisplayName == null || info.DisplayName.Length == 0)
            {
                Log.LogOutput($"RegisterGameMode: Info.DisplayName must be set");
                return false;
            }
            else if (CustomGameModes.ContainsKey(info.DisplayName))
            {
                Log.LogOutput(
                    $"RegisterGameMode: Unable to add '{info.DisplayName}': GameMode already exists"
                );
                return false;
            }
            else if (info.DisplayDescription == null || info.DisplayDescription.Length == 0)
            {
                Log.LogOutput($"RegisterGameMode: Info.DisplayDescription must be set");
                return false;
            }
            /*else if (info.DisplayImage == null)
            {
                Log.LogOutput($"RegisterGameMode: Info.DisplayImage must not be null");
                return false;
            }*/
            else if (info.Object == null)
            {
                Log.LogOutput($"RegisterGameMode: Info.DisplayImage must not be null");
                return false;
            } /*
            else if (info.MaxPlayers > 64 || info.MaxPlayers < 1)
            {
                Log.LogOutput($"RegisterGameMode: Info.MaxPlayers range must fall within 1 and 64");
                return false;
            }
            else if (info.StandardPlayers > 64 || info.StandardPlayers < 1)
            {
                Log.LogOutput(
                    $"RegisterGameMode: Info.StandardPlayers range must fall within 1 and 64"
                );
                return false;
            }*/
            else if (info.Object == null)
            {
                Log.LogOutput($"RegisterGameMode: Info.Object must not be null");
                return false;
            }

            return true;
        }
        #endregion

        #region Definitions
        public class CustomGameModeInfo
        {
            /// <summary>
            /// Show this GameMode in the UI?
            /// </summary>
            public bool Enabled = true;

            //public bool HiddenMaps = true;

            /// <summary>
            /// A unique name shown in the UI and what is passed to QueueGameMode()
            /// </summary>
            public string? DisplayName;
            public string? DisplayDescription;
            public Sprite? DisplaySprite;

            /// <summary>
            /// Prefab to be instantiated on level load
            /// </summary>
            public GameObject? Object;
        }
        #endregion
    }
}
