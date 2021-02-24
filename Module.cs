using System;
using System.Collections.Generic;
using System.Linq;

using Celeste.Mod;

namespace NyahHelper
{
    public class Module : EverestModule
    {
        public Module()
        {
            #region Hooks
            Entities.Refills.CoreSpiritHook.Hook();
            Entities.Blocks.CoreKevinHook.Hook();
            Entities.Blocks.SyncedMoveBlockHook.Hook();
            #endregion
        }

        public override void Load()
        {
        }

        public override void Unload()
        {
        }
    }
}
