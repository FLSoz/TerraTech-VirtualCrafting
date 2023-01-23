using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VirtualCrafting
{
    public class VirtualCraftingMod : ModBase
    {
        internal const string HarmonyID = "flsoz.ttmm.weaponaim.mod";
        internal static readonly Harmony harmony = new Harmony(HarmonyID);
        private static bool Inited = false;

        internal static bool DEBUG = true;

        internal static Logger logger;
        internal static void ConfigureLogger()
        {
            logger = new Logger("VirtualCrafting");
            logger.Info("Logger is setup");
        }

        public override void EarlyInit()
        {
            if (!Inited)
            {
                ConfigureLogger();
            }
        }

        public override bool HasEarlyInit()
        {
            return true;
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }

        public override void DeInit()
        {
            throw new NotImplementedException();
        }
    }
}
