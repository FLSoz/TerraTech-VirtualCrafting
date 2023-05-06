using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCrafting.Modules;

namespace VirtualCrafting.JSONLoaders
{
    internal class JSONChunkVirtualizerLoader : JSONModuleLoader
    {
        public override bool CreateModuleForBlock(int blockID, ModdedBlockDefinition def, TankBlock block, JToken data)
        {
            ModuleChunkVirtualizer source = base.GetOrAddComponent<ModuleChunkVirtualizer>(block);
            return true;
        }

        public override string GetModuleKey()
        {
            return "ModuleChunkVirtualizer";
        }
    }
}
