using System.Globalization;
using MelonLoader;
using QAPI;
using UnityEngine;

[assembly: MelonInfo(typeof(QAPIMod), "QAPI", "0.0.1", "dodad")]
[assembly: MelonGame("Bohemia Interactive", "Silica")]
[assembly: MelonOptionalDependencies("QList")]
[assembly: MelonPriority(-99)]
[assembly: MelonColor(100, 255, 180, 100)]

namespace QAPI;

public class QAPIMod : MelonMod
{
    #region Variables
    internal static Transform ParentContainer
    {
        get
        {
            if (parentContainer == null)
            {
                parentContainer = new GameObject("QAPI Container").transform;
                GameObject.DontDestroyOnLoad(ParentContainer.gameObject);
            }

            return parentContainer;
        }
    }

    private static Transform? parentContainer;
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
