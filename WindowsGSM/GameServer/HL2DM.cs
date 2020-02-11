﻿namespace WindowsGSM.GameServer
{
    class HL2DM : Valve.SRCDS
    {
        public const string FullName = "Half-Life 2: Deathmatch Dedicated Server";
        public override string defaultmap { get { return "dm_runoff"; } }
        public override string additional { get { return "+mp_teamplay 1"; } }
        public override string game { get { return "hl2mp"; } }
        public override string appId { get { return "232370"; } }

        public HL2DM(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
