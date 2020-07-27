using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CSGOHack
{
    class Program
    {
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(System.Int32 vKey);

        public static Process csgo = Process.GetProcessesByName("csgo")[0];
        public static VAMemory memory = new VAMemory("csgo");
        public static int module = 0;
        public static int LocalPlayer;
        public static int PlayerTeam;

        struct GlowStruct
        {
            public int r;
            public int g;
            public int b;
            public int a;
            public bool rwo;
            public bool rwuo;
        }


        static void Main(string[] args)
        {
            foreach (ProcessModule mod in csgo.Modules)
            {
                if (mod.ModuleName == "client.dll")
                {
                    module = (int)mod.BaseAddress;
                }
            }
            int cur;
            int curTeam;
            int curGlowIndex;
            GlowStruct Team = new GlowStruct()
            {
                r = 0,
                g = 0,
                b = 1,
                a = 1,
                rwo = true,
                rwuo = false
            };
            GlowStruct Enemy = new GlowStruct()
            {
                r = 1,
                g = 0,
                b = 0,
                a = 1,
                rwo = true,
                rwuo = false
            };
            Thread trigger = new Thread(Trigger);
            trigger.Start();
            Thread bhop = new Thread(Bhop);
            bhop.Start();
            while (true)
            {
                LocalPlayer = memory.ReadInt32((IntPtr)(module + Offsets.dwLocalPlayer));
                PlayerTeam = memory.ReadInt32((IntPtr)(LocalPlayer + Offsets.m_iTeamNum));
                for (int i = 0; i < 64; i++)
                {
                    cur = memory.ReadInt32((IntPtr)(module + Offsets.dwEntityList + i * 0x10));
                    curTeam = memory.ReadInt32((IntPtr)(cur + Offsets.m_iTeamNum));
                    curGlowIndex = memory.ReadInt32((IntPtr)(cur + Offsets.m_iGlowIndex));
                    if (!memory.ReadBoolean((IntPtr)cur + Offsets.m_bDormant))
                    {
                        if (curTeam == PlayerTeam)
                        {
                            DrawGlow(curGlowIndex, Team);
                        }
                        else
                        {
                            DrawGlow(curGlowIndex, Enemy);
                        }
                    }
                }
            }
        }

        private static void Bhop()
        {
            while (true)
            {
                int i = GetAsyncKeyState((int) Keys.Space);
                if ((memory.ReadInt32((IntPtr)LocalPlayer + Offsets.dwForceJump) == 0) && ((i == 1) || (i == Int16.MinValue)))
                {
                    memory.WriteInt32((IntPtr) module + Offsets.dwForceJump, 6);
                }
            }
        }

        private static void Trigger()
        {
            while (true)
            {
                int i = GetAsyncKeyState((int)Keys.X);
                if ((i == 1) || (i == Int16.MinValue))
                {
                    int triggerId = memory.ReadInt32((IntPtr) (LocalPlayer + Offsets.m_iCrosshairId));
                    if (triggerId > 0)
                    {
                        memory.WriteInt32((IntPtr) module + Offsets.dwForceAttack, 6);
                    }
                }
            }
        }

        static void DrawGlow(int index, GlowStruct glow)
        {
            int obj = memory.ReadInt32((IntPtr)(module + Offsets.dwGlowObjectManager));
            memory.WriteFloat((IntPtr)(obj + (index * 0x38) + 4), glow.r);
            memory.WriteFloat((IntPtr)(obj + (index * 0x38) + 8), glow.g);
            memory.WriteFloat((IntPtr)(obj + (index * 0x38) + 12), glow.b);
            memory.WriteFloat((IntPtr)(obj + (index * 0x38) + 0x10), 255 / 100f);
            memory.WriteBoolean((IntPtr)(obj + (index * 0x38) + 0x24), glow.rwo);
            memory.WriteBoolean((IntPtr)(obj + (index * 0x38) + 0x25), glow.rwuo);
        }
    }
}
