using System.Globalization;
using MelonLoader;
using QAPI;
using UnityEngine;

[assembly: MelonInfo(typeof(QAPIMod), "QAPI", "0.0.2", "dodad")]
[assembly: MelonGame("Bohemia Interactive", "Silica")]
[assembly: MelonOptionalDependencies("QList")]
[assembly: MelonPriority(-99)]
[assembly: MelonColor(100, 255, 180, 100)]

namespace QAPI;

public class QAPIMod : MelonMod
{
    #region Variables
    internal static Transform PersistentContainer
    {
        get
        {
            if (persistentContainer == null)
            {
                persistentContainer = new GameObject("QAPI Persistent Container").transform;
                GameObject.DontDestroyOnLoad(PersistentContainer.gameObject);
            }

            return persistentContainer;
        }
    }

    private static Transform? persistentContainer;
    #endregion

    #region Melon
    public override void OnInitializeMelon()
    {
        Config.SetFilePath(this);
        RegisterOptions();
        Log.Enable(this);
        GameModes.Initialize();
    }
    #endregion

    #region QList
    private void RegisterOptions()
    {
        if (!Config.QListPresent())
            return;

        QList.Options.RegisterMod(this);
    }
    #endregion
}
