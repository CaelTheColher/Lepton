using Harmony;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cael.Lepton
{
    public class ModEntry : Mod
    {
        public static Mod ModInstance;
        public static IModHelper ModHelper;

        public override void Entry(IModHelper helper)
        {
            ModInstance = this;
            ModHelper = helper;
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("Cael.Lepton");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
