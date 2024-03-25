using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using static QAPI.GameModes;

namespace QAPI.Components
{
    [RegisterTypeInIl2Cpp]
    public class GameModeReferences : MonoBehaviour
    {
        internal Dictionary<string, CustomGameModeInfo> CustomGameModes = new();
    }
}
