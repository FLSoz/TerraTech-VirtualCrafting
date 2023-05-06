using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace VirtualCrafting.Networking
{
    internal class NetworkingManager
    {
        #region Networking (custom)
        /*
         * Custom networking solution. Unity's builtin NetworkBehaviour is used to sync up the actual inventory contents
         * This is used for access controls, as well as sending out order requests
         */
        internal static void ReceiveAsClient(VirtualCraftingMessage obj, NetworkMessage netmsg)
        {
        }
        internal static void ReceiveAsHost(VirtualCraftingMessage obj, NetworkMessage netmsg)
        {
        }
        #endregion
    }
}
