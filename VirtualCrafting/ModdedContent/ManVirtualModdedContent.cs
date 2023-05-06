using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VirtualCrafting.Model;
using static LocalisationEnums;

namespace VirtualCrafting.ModdedContent
{
    public class ManVirtualModdedContent : Singleton.Manager<ManVirtualModdedContent>
    {
        private readonly Dictionary<ChunkTypes, VirtualChunkDescriptor> SessionChunkDescriptors = new Dictionary<ChunkTypes, VirtualChunkDescriptor>();
        private readonly Dictionary<ChunkTypes, VirtualChunk> VanillaChunkTemplates = new Dictionary<ChunkTypes, VirtualChunk>();
        private readonly Dictionary<VirtualChunkDescriptor, VirtualChunk> ModdedChunkTemplates =
            new Dictionary<VirtualChunkDescriptor, VirtualChunk>(new VirtualChunkTypeComparer());
        private readonly Dictionary<string, VirtualChunkDescriptor> ModdedChunkDescriptors = new Dictionary<string, VirtualChunkDescriptor>();

        private readonly Dictionary<BlockTypes, VirtualBlockDescriptor> SessionBlockDescriptors = new Dictionary<BlockTypes, VirtualBlockDescriptor>();
        private readonly Dictionary<BlockTypes, VirtualBlock> VanillaBlockTemplates = new Dictionary<BlockTypes, VirtualBlock>();
        private readonly Dictionary<VirtualBlockDescriptor, VirtualBlock> ModdedBlockTemplates =
            new Dictionary<VirtualBlockDescriptor, VirtualBlock>(new VirtualBlockTypeComparer());
        private readonly Dictionary<string, VirtualBlockDescriptor> ModdedBlockDescriptors = new Dictionary<string, VirtualBlockDescriptor>();

        private readonly Dictionary<string, VirtualFabricatorDescriptor> FabricatorTypes = new Dictionary<string, VirtualFabricatorDescriptor>();
        private readonly Dictionary<int, VirtualFabricatorDescriptor> SessionFabricatorTypes = new Dictionary<int, VirtualFabricatorDescriptor>();

        private readonly Dictionary<int, string> LegacyToBlockIDs = new Dictionary<int, string>();
        private readonly Dictionary<string, int> BlockToLegacyIDs = new Dictionary<string, int>();

        private readonly Dictionary<string, int> FabricatorToSessionIDs = new Dictionary<string, int>();
        private readonly Dictionary<string, int> ChunkToSessionIDs = new Dictionary<string, int>();

        private void PopulateVanillaMaps()
        {
            throw new NotImplementedException();
        }
        private void SetupModdedBlocks()
        {
            throw new NotImplementedException();
        }
        private void SetupLegacyIDMaps()
        {

        }
        internal void Setup()
        {
            this.PopulateVanillaMaps();
        }

        private void AutoAssignIDs(List<string> idsToAssign, Dictionary<string, int> idToSessionMap, int startingIndex = 0)
        {

        }


        internal void Init()
        {
            // assign IDs, and fix ID assignments in the modded chunk types
            List<string> moddedChunkIDs = ModdedChunkDescriptors.Keys.ToList();
            this.AutoAssignIDs(moddedChunkIDs, ChunkToSessionIDs, 1000000);
            foreach (KeyValuePair<string, VirtualChunkDescriptor> pair in ModdedChunkDescriptors)
            {
                int sessionID = ChunkToSessionIDs[pair.Key];
                pair.Value.SetChunkID((ChunkTypes) sessionID);
                SessionChunkDescriptors.Add((ChunkTypes)sessionID, pair.Value);
            }

            // List<string> fabricatorIDs = FabricatorTypes.Keys.ToList();
            // this.AutoAssignIDs(fabricatorIDs, FabricatorToSessionIDs, 200);

            // Go through all blocks in the session, populate the official/legacy maps
            this.SetupLegacyIDMaps();
        }

        internal void LateInit()
        {
            // setup modded block details
            this.SetupModdedBlocks();
        }

        /// <summary>
        /// Register a new modded chunk
        /// </summary>
        /// <param name="chunkID"></param>
        public void RegisterModdedChunk(string chunkID, string name, string description, Sprite sprite, ChunkRarity rarity, ChunkCategory[] category)
        {
            // currently has invalid chunk ID. We can fix that later.
            VirtualChunkDescriptor chunkType = new VirtualChunkDescriptor(chunkID);
            VirtualChunk chunkTemplate = new VirtualChunk(name, description, sprite, rarity, category);
            chunkTemplate.LinkItemID(chunkType);
            this.ModdedChunkTemplates.Add(chunkType, chunkTemplate);
            this.ModdedChunkDescriptors.Add(chunkID, chunkType);
        }

        // TODO: fixup fabricator stuff
        /*
        /// <summary>
        /// Register a virtual fabricator
        /// </summary>
        /// <param name="fabricatorID"></param>
        /// <param name="fabricatorTemplate"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void RegisterVirtualFabricator(string fabricatorID, VirtualFabricator fabricatorTemplate)
        {
            VirtualFabricatorDescriptor fabricatorType = new VirtualFabricatorDescriptor(fabricatorID);
            fabricatorTemplate.LinkItemID(fabricatorType);
            FabricatorTypes.Add(fabricatorID, fabricatorType);
        }
        */

        internal int GetChunkID(string moddedChunkID)
        {
            return ChunkToSessionIDs.GetOrDefault(moddedChunkID, -1);
        }

        internal int GetLegacyBlockID(string blockID)
        {
            return BlockToLegacyIDs.GetOrDefault(blockID, -1);
        }
        internal int GetSessionIDForBlock(string blockID)
        {
            return Singleton.Manager<ManMods>.inst.GetBlockID(blockID);
        }
        internal int GetSessionIDForBlock(int legacyID)
        {
            int sessionID = Singleton.Manager<ManMods>.inst.GetBlockID(LegacyToBlockIDs.GetOrNull(legacyID));
            if (sessionID > 3)
            {
                return sessionID;
            }
            return -1;
        }
        public BlockTypes GetSessionIDForBlock(VirtualBlockDescriptor block)
        {
            return block.SessionID;
        }

        internal int GetSessionIDForFabricator(string fabricatorID)
        {
            return this.FabricatorToSessionIDs[fabricatorID];
        }

        // Get templates
        internal IVirtualItem GetItemFromDescriptor(IVirtualItemDescriptor itemType) {
            switch(itemType.ItemType)
            {
                case VirtualItemType.BLOCK:
                    VirtualBlockDescriptor BlockType = (VirtualBlockDescriptor)itemType;
                    if (BlockType.ModdedType == VirtualBlockModdedType.VANILLA)
                    {
                        return VanillaBlockTemplates[BlockType.SessionID];
                    }
                    else
                    {
                        return ModdedBlockTemplates[BlockType];
                    }
                case VirtualItemType.CHUNK:
                    VirtualChunkDescriptor chunkType = (VirtualChunkDescriptor)itemType;
                    if (chunkType.ModdedType == VirtualChunkModdedType.VANILLA)
                    {
                        return VanillaChunkTemplates[chunkType.ChunkID];
                    }
                    else
                    {
                        return ModdedChunkTemplates[chunkType];
                    }
                default:
                    VirtualCraftingMod.logger.Fatal($"Invalid item type detected {itemType.ItemType}");
                    return null;
            }
        }
        public VirtualChunk GetChunkTemplate(VirtualChunkDescriptor chunkType)
        {
            return (VirtualChunk)this.GetItemFromDescriptor(chunkType);
        }
        // TODO: add virutal fabricators
        /*
        public VirtualFabricator GetFabricatorTemplate(VirtualFabricatorDescriptor fabricatorType)
        {
            return (VirtualFabricator)this.GetItemFromItemType(fabricatorType);
        }
        */
        internal IVirtualItemDescriptor GetItemDescriptorFromHash(VirtualItemType itemType, int itemHash)
        {
            switch(itemType)
            {
                case VirtualItemType.BLOCK:
                    return SessionBlockDescriptors.GetOrNull((BlockTypes)itemHash);
                case VirtualItemType.CHUNK:
                    return SessionChunkDescriptors.GetOrNull((ChunkTypes)itemHash);
                default:
                    VirtualCraftingMod.logger.Fatal($"Trying to get item of hash {itemHash} for invalid item type {itemType}");
                    return null;
            }
        }

        internal IVirtualItemDescriptor GetItemDescriptorFromID(string ID)
        {
            string[] tokens = ID.Split(new char[] { ':' }, 2);
            VirtualItemType itemType = (VirtualItemType)Enum.Parse(typeof(VirtualItemType), tokens[0]);
            switch(itemType)
            {
                case VirtualItemType.BLOCK:
                    if (Enum.TryParse<BlockTypes>(tokens[1], out BlockTypes blockID))
                    {
                        return SessionBlockDescriptors[(BlockTypes)blockID];
                    }
                    return ModdedBlockDescriptors.GetOrNull(tokens[1]);
                case VirtualItemType.CHUNK:
                    if (Enum.TryParse<ChunkTypes>(tokens[1], out ChunkTypes chunkID))
                    {
                        return SessionChunkDescriptors[(ChunkTypes)chunkID];
                    }
                    return ModdedChunkDescriptors.GetOrNull(tokens[1]);
                default:
                    VirtualCraftingMod.logger.Error($"Could not parse descriptor for {ID}");
                    return null;
            }
        }

        internal void Reset()
        {
            foreach (VirtualChunkDescriptor chunkType in this.SessionChunkDescriptors.Values)
            {
                if (chunkType.ModdedType != VirtualChunkModdedType.VANILLA)
                {
                    this.SessionChunkDescriptors.Remove(chunkType.ChunkID);
                }
            }
            this.ChunkToSessionIDs.Clear();
            foreach (VirtualBlockDescriptor blockType in this.ModdedBlockDescriptors.Values)
            {
                if (blockType.ModdedType != VirtualBlockModdedType.VANILLA)
                {
                    this.SessionBlockDescriptors.Remove(blockType.SessionID);
                }
            }
            this.BlockToLegacyIDs.Clear();
            this.LegacyToBlockIDs.Clear();

            this.ModdedChunkDescriptors.Clear();
            this.ModdedBlockDescriptors.Clear();
            this.ModdedChunkTemplates.Clear();
            this.ModdedBlockTemplates.Clear();

            this.FabricatorTypes.Clear();
            this.SessionFabricatorTypes.Clear();

            this.FabricatorToSessionIDs.Clear();
        }
    }

    internal static class MyExtensions
    {
        public static T2 GetOrNull<T1, T2>(this Dictionary<T1, T2> dict, T1 key)
        {
            if (key != null && dict.TryGetValue(key, out T2 value))
            {
                return value;
            }
            return default(T2);
        }
        public static T2 GetOrDefault<T1, T2>(this Dictionary<T1, T2> dict, T1 key, T2 defaultValue)
        {
            if (key != null && dict.TryGetValue(key, out T2 value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}
