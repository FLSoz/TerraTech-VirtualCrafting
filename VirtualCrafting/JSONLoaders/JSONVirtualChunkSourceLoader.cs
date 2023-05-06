using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VirtualCrafting.Modules;

namespace VirtualCrafting.JSONLoaders
{
    public class JSONVirtualChunkSourceLoader : JSONModuleLoader
    {
        public override bool CreateModuleForBlock(int blockID, ModdedBlockDefinition def, TankBlock block, JToken data)
        {
            ModuleVirtualChunkSource source = base.GetOrAddComponent<ModuleVirtualChunkSource>(block);
            return true;
        }

        public override string GetModuleKey()
        {
            return "ModuleVirtualChunkSource";
        }
    }
}
