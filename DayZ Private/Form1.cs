using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using D2D = System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using D3D = Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using ReadWriteMemory;
using System.Security.Cryptography;

namespace DayZ_Private
{
    public partial class Form1 : Form
    {

        #region Pointless Stuff
        private Margins marg;

        //this is used to specify the boundaries of the transparent area
        internal struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]

        private static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]

        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]

        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        public const int GWL_EXSTYLE = -20;

        public const int WS_EX_LAYERED = 0x80000;

        public const int WS_EX_TRANSPARENT = 0x20;

        public const int LWA_ALPHA = 0x2;

        public const int LWA_COLORKEY = 0x1;

        [DllImport("dwmapi.dll")]
        static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);

        private D3D.Device device = null;

        //D3D Drawings
        private static D3D.Line line;
        private static D3D.Font font;


        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzåäöABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ1234567890АаБбВвГгДдЕеӘәЖжЗзИиЙйКкЛлМмНнОоÖöПпПпрСсТтуФфХхҺһҺһЧч'ШшьЭэԚԜԝ".ToCharArray();
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[maxSize];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public Form1()
        {
            InitializeComponent();
            this.Text = GetUniqueKey(20);
            //Make the window's border completely transparant
            SetWindowLong(this.Handle, GWL_EXSTYLE,
                    (IntPtr)(GetWindowLong(this.Handle, GWL_EXSTYLE) ^ WS_EX_LAYERED ^ WS_EX_TRANSPARENT));

            //Set the Alpha on the Whole Window to 255 (solid)
            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);
            
            //Init DirectX
            //This initializes the DirectX device. It needs to be done once.
            //The alpha channel in the backbuffer is critical.
            D3D.PresentParameters presentParameters = new D3D.PresentParameters();
            presentParameters.Windowed = true;
            presentParameters.SwapEffect = D3D.SwapEffect.Discard;
            presentParameters.BackBufferFormat = D3D.Format.A8R8G8B8;

            this.device = new D3D.Device(0, D3D.DeviceType.Hardware, this.Handle,
            D3D.CreateFlags.HardwareVertexProcessing, presentParameters);

            line = new D3D.Line(this.device);
            font = new D3D.Font(device, new System.Drawing.Font("Museo", 9, FontStyle.Regular));

            Thread dx = new Thread(new ThreadStart(this.dxThread));
            dx.IsBackground = true;
            dx.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //Create a margin (the whole form)
            marg.Left = 0;
            marg.Top = 0;
            marg.Right = this.Width;
            marg.Bottom = this.Height;

            //Expand the Aero Glass Effect Border to the WHOLE form.
            // since we have already had the border invisible we now
            // have a completely invisible window - apart from the DirectX
            // renders NOT in black.
            DwmExtendFrameIntoClientArea(this.Handle, ref marg);
        }
        #endregion
        #region Drawing Logic
        //Draw Text(SHADOW)
        public static void DrawShadowText(string text, Point Position, Color color)
        {
            font.DrawText(null, text, new Point(Position.X + 1, Position.Y + 1), Color.Black);
            font.DrawText(null, text, Position, color);
        }

        //Draw Line
        public static void DrawLine(float x1, float y1, float x2, float y2, float w, Color Color)
        {
            Vector2[] vLine = new Vector2[2] { new Vector2(x1, y1), new Vector2(x2, y2) };

            line.GlLines = true;
            line.Antialias = false;
            line.Width = w;

            

            line.Begin();
            line.Draw(vLine, Color.ToArgb());
            line.End();

        }

        //Draw Filled Box
        static Color SetTransparency(int A, Color color)
        {
            return Color.FromArgb(A, color.R, color.G, color.B);
        }

        public static void DrawFilledBox(float x, float y, float w, float h, System.Drawing.Color Color)
        {
            Vector2[] vLine = new Vector2[2];

            line.GlLines = true;
            line.Antialias = false;
            line.Width = w;

            vLine[0].X = x + w / 2;
            vLine[0].Y = y;
            vLine[1].X = x + w / 2;
            vLine[1].Y = y + h;

            line.Begin();
            line.Draw(vLine, Color.ToArgb());
            line.End();
        }

        public static void DrawTransparentBox(float x, float y, float w, float h, int transparency,System.Drawing.Color Color)
        {
            Vector2[] vLine = new Vector2[2];

            line.GlLines = true;
            line.Antialias = false;
            line.Width = w;

            vLine[0].X = x + w / 2;
            vLine[0].Y = y;
            vLine[1].X = x + w / 2;
            vLine[1].Y = y + h;
            Color halfTransparent = SetTransparency(transparency, Color);
            line.Begin();
            line.Draw(vLine, halfTransparent.ToArgb());
            line.End();
        }

        //Draw Box
        public static void DrawBox(float x, float y, float w, float h, float px, System.Drawing.Color Color)
        {
            DrawFilledBox(x, y + h, w, px, Color);
            DrawFilledBox(x - px, y, px, h, Color);
            DrawFilledBox(x, y - px, w, px, Color);
            DrawFilledBox(x + w, y, px, h, Color);
        }
        #endregion
        ProcessMemory Mem = new ProcessMemory("dayz");
        int dwTransData;
        VECTOR3 InvViewRight;
        VECTOR3 InvViewUp;
        VECTOR3 InvViewForward;
        VECTOR3 InvViewTranslation;
        VECTOR3 ViewPortMatrix;
        VECTOR3 ProjD1;
        VECTOR3 ProjD2;
        #region Vector3
        public struct VECTOR3
        {
            public float x;
            public float y;
            public float z;

            public float dot(VECTOR3 dot)
            {
                return (x * dot.x + y * dot.y + z * dot.z);
            }
            public float distance(VECTOR3 l, VECTOR3 e)
            {
                float dist;
                dist = (float)Math.Sqrt(Math.Pow(l.x - e.x, 2) + Math.Pow(l.y - e.y, 2) + Math.Pow(l.z - e.z, 2));
                return dist;
            }
        }

        public VECTOR3 SubVectorDist(VECTOR3 playerFrom, VECTOR3 playerTo)
        {
            return new VECTOR3()
            {
                x = playerFrom.x - playerTo.x,
                y = playerFrom.y - playerTo.y,
                z = playerFrom.z - playerTo.z
            };
        }

        public VECTOR3 ReadVECTOR3(int pOffset)
        {
            float _x = Mem.ReadFloat(pOffset);
            float _z = Mem.ReadFloat(pOffset + 0x4);
            float _y = Mem.ReadFloat(pOffset + 0x8);
            return new VECTOR3
            {
                x = _x,
                y = _y,
                z = _z
            };
        }

        public VECTOR3 WriteVECTOR3(int pOffset, float value)
        {
            Mem.WriteFloat(pOffset, value);
            Mem.WriteFloat(pOffset + 0x4, value);
            Mem.WriteFloat(pOffset + 0x8, value);

            float _x = Mem.ReadFloat(pOffset);
            float _z = Mem.ReadFloat(pOffset + 0x4);
            float _y = Mem.ReadFloat(pOffset + 0x8);
            return new VECTOR3
            {
                x = _x,
                y = _y,
                z = _z
            };
        }

        double Distance(VECTOR3 point1, VECTOR3 point2)
        {
            double distance = Math.Sqrt(((int)point1.x - (int)point2.x) * ((int)point1.x - (int)point2.x) +
                ((int)point1.y - (int)point2.y) * ((int)point1.y - (int)point2.y) +
                ((int)point1.z - (int)point2.z) * ((int)point1.z - (int)point2.z));
            distance = Math.Round(distance, 3);
            return distance;
        }

        public VECTOR3 W2SN(VECTOR3 _in)
        {
            VECTOR3 _out, temp;
            temp = SubVectorDist(_in, InvViewTranslation);
            float x = temp.dot(InvViewRight);
            float y = temp.dot(InvViewUp);
            float z = temp.dot(InvViewForward);

            _out.x = ViewPortMatrix.x * (1 + (x / ProjD1.x / z));
            _out.y = ViewPortMatrix.z * (1 - (y / ProjD2.z / z));
            _out.z = z;
            return _out;
        }
        #endregion
        #region More pointless stuff
        // ... { GLOBAL HOOK }
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;

        private LowLevelKeyboardProc _proc = hookProc;

        private static IntPtr hhook = IntPtr.Zero;

        public void SetHook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
        }

        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }
        #endregion
        public static int CurrentMenu = 1;//1 = Main, 2 = ESP, 3 = Misc

        public static float stepHop = 0.3f;
        public static bool showMenu = false;
        public static bool speedHack = false;
        public static bool ItemMagnet = false;
        public static bool SilentAim = false;
        public static bool CrosshairAim = false;
        public static bool NoGrass = false;
        public static bool t = false;
        public static bool superZoom = false;
        public static bool safeMode = false;
        public static bool alwaysDay = false;
        public static bool showPlayers = false;
        public static bool showZombies = false;
        public static bool showCorpses = false;
        public static bool showItems = false;
        public static bool showAirfields = false;
        public static bool monsterKill = false;
        public static bool flyMode = false;
        public static bool flyMode_up = false;
        public static bool flyMode_down = false;
        public static bool ShowInvitem = true;
        public static bool Instructions = false;
        public static bool ShowClothing = true;
        public static bool DrawBoxes = false;
        public static bool ShowWeapons = true;
        public static bool norec = false;
        public static bool ShowMags = true;
        public static bool nofatigue = false;
        public static bool ShowUnkown = false;
        public static bool StealItems = false;
        public static int CurrentOption = 0;
        public static bool NoFall;
        public static int CurrentY = 0;
        int lockonptr = 0;
        public static bool bdown = false;
        public static bool bup = false;
        public static bool lockon = false;
        public static int Selection = 0;
        public static int menuTimer = 0;
        public static Color PlayerColor = Color.Green;
        public static Color ZombieColor = Color.Red;
        public static Color red = Color.Red;
        public static Color green = Color.Green;
        public static Color cyan = Color.Cyan;
        public static Color blue = Color.Blue;
        public static Color yellow = Color.Yellow;
        public static Color pink = Color.Pink;
        public static int playerColor = 0;
        public static int zombieColor = 1;
        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool ValidKeyDown = false;
            if (vkCode == Keys.LControlKey.GetHashCode() || vkCode == Keys.Up.GetHashCode()
                || vkCode == Keys.Down.GetHashCode() || vkCode == Keys.Right.GetHashCode()
                || vkCode == Keys.Left.GetHashCode() || vkCode == Keys.End.GetHashCode()
                || vkCode == Keys.PageUp.GetHashCode() || vkCode == Keys.PageDown.GetHashCode())
                ValidKeyDown = true;
            //KEY BINDINGS
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN && ValidKeyDown)
            {
                //Hide/Show Menu
                if (vkCode == Keys.End.GetHashCode())
                {
                    showMenu = !showMenu;
                }
                //Speedhack
                if (vkCode == Keys.LControlKey.GetHashCode())
                {
                    speedHack = !speedHack;
                }
                //Fly Hack Binds
                if (vkCode == Keys.PageUp.GetHashCode())
                {
                    flyMode_down = false;
                    flyMode_up = true;
                }
                if (vkCode == Keys.NumPad5.GetHashCode())
                {
                    flyMode_down = false;
                    flyMode_up = false;
                }
                if (vkCode == Keys.PageDown.GetHashCode())
                {
                    flyMode_up = false;
                    flyMode_down = true;
                }
                //Go Up and Down in menu
                if (vkCode == Keys.Up.GetHashCode() && showMenu)
                {
                    if (CurrentY >= 13)
                        CurrentOption -= 1;
                }
                if (vkCode == Keys.Down.GetHashCode() && showMenu)
                {
                    CurrentOption += 1;
                    
                }
                if (vkCode == Keys.PageUp.GetHashCode() && showMenu)
                {
                    bup = true;
                   
                }
                else
                {
                    bup = false;
                }

                if (vkCode == Keys.PageDown.GetHashCode() && showMenu)
                {
                    bdown = true;

                }
                else
                {
                    bdown = false;
                }



              
                //Change values in menu
                if (vkCode == Keys.Left.GetHashCode() && showMenu || vkCode == Keys.Right.GetHashCode() && showMenu)
                {
                    //Main Menu
                    if (CurrentMenu == 1)
                    {
                        if (CurrentOption == 6)
                        {
                            if (vkCode == Keys.Left.GetHashCode())
                                stepHop -= 0.05f;
                            if (vkCode == Keys.Right.GetHashCode())
                                stepHop += 0.05f;
                        }
                        if (CurrentOption == 7)
                            ItemMagnet = !ItemMagnet;


                        if (CurrentOption == 2 && menuTimer <= 0)
                            CurrentMenu = 2;
                        if (CurrentOption == 3 && menuTimer <= 0)
                        {
                            CurrentMenu = 3;
                            CurrentOption -= 1;
                        }
                        if (CurrentOption == 4 && menuTimer <= 0)
                        {
                            CurrentMenu = 4;
                            CurrentOption -= 2;
                        }
                        if (CurrentOption == 2 || CurrentOption == 3)
                            menuTimer = 10;

                    }
                    //ESP Menu
                    if (CurrentMenu == 2)
                    {
                        if (CurrentOption == 2 && menuTimer <= 0)
                        {
                            CurrentMenu = 1;
                        }

                        if (CurrentOption == 4)
                            showPlayers = !showPlayers;
                        if (CurrentOption == 5)
                            showCorpses = !showCorpses;
                        if (CurrentOption == 6)
                            showZombies = !showZombies;
                        if (CurrentOption == 7)
                            showItems = !showItems;
                        if (CurrentOption == 9)
                            showAirfields = !showAirfields;

                    }
                    //Misc Menu
                    if (CurrentMenu == 3)
                    {
                        if (CurrentOption == 2 && menuTimer <= 0)
                        {
                            CurrentMenu = 1;
                        }

                        if (CurrentOption == 4)
                            SilentAim = !SilentAim;
                        if (CurrentOption == 5)
                            CrosshairAim = !CrosshairAim;
                        if (CurrentOption == 6)
                        {
                            t = !t;
                            //bup = false;
                        }
                        if (CurrentOption == 7)
                            NoGrass = !NoGrass;

                        if (CurrentOption == 8)
                        {
                            NoFall = !NoFall;
                        }
                        if (CurrentOption == 9)
                        {
                            norec = !norec;
                        }
                        if (CurrentOption == 10)
                        {
                            nofatigue = !nofatigue;
                        }
                    }
                    //Settings Menu
                    if (CurrentMenu == 4)
                    {
                        if (CurrentOption == 2 && menuTimer <= 0)
                        {
                            CurrentMenu = 1;
                        }
                        if (vkCode == Keys.Left.GetHashCode())
                        {
                            if (CurrentOption == 4 && playerColor > 0)
                                playerColor -= 1;
                            if (CurrentOption == 5 && zombieColor > 0)
                                zombieColor -= 1;

                            if (CurrentOption == 6)
                                ShowWeapons = !ShowWeapons;
                            if (CurrentOption == 7)
                                Instructions = !Instructions;
                            if (CurrentOption == 8)
                                ShowMags = !ShowMags;
                            if (CurrentOption == 9)
                                ShowClothing = !ShowClothing;
                            if (CurrentOption == 10)
                                ShowUnkown = !ShowUnkown;
                        }
                        if (vkCode == Keys.Right.GetHashCode())
                        {
                            if (CurrentOption == 4)
                                playerColor += 1;
                            if (CurrentOption == 5)
                                zombieColor += 1;

                            if (CurrentOption == 6)
                                ShowWeapons = !ShowWeapons;
                            if (CurrentOption == 7)
                                Instructions = !Instructions;
                            if (CurrentOption == 8)
                                ShowMags = !ShowMags;
                            if (CurrentOption == 9)
                                ShowClothing = !ShowClothing;
                            if (CurrentOption == 10)
                                ShowUnkown = !ShowUnkown;
                        }
                        
                    }
                }
                return (IntPtr)1;
            }
            else
                return CallNextHookEx(hhook, code, (int)wParam, lParam);

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            SetHook();
        }

        private void Form1_Closing(object sender, EventArgs e)
        {
            UnHook();
            this.device.Dispose();
        }

        string GetPlayerName(int playerPtr)
        {
            string playerName = "PlayerName"; 

            //int myPlayerID = Mem.ReadInt(pLocal + 0x7E0); //Example, you should already have this. 
            int entityPlayerID = Mem.ReadInt(playerPtr + 0x7E8); //Entity ID from ObjectTable iteration 
            int scoreBoardBase = 0x1095A68; //Make this a constant dude. Or you'll have fun times updating to new values. 
            int scoreBoardOffset = Mem.ReadInt(scoreBoardBase + 0x28);
            int scoreBoardSize = Mem.ReadInt(scoreBoardOffset + 0x10);
            int scoreBoard = Mem.ReadInt(scoreBoardOffset + 0xC);

            for (int ii = 0; ii < scoreBoardSize; ii++)
            {
                int iteratePlayerID = Mem.ReadInt(scoreBoard + (ii * 0xE8) + 0x4); //Get iterated player ID from Scoreboard 

                if (iteratePlayerID == entityPlayerID) //If you don't want your own name use "&& iteratePlayerID != myPlayerID" 
                {
                    int playerNamePointer = Mem.ReadInt(scoreBoard + 0x80 + (ii * 0xE8));
                    int playerNameSize = Mem.ReadInt(playerNamePointer + 0x4);
                    playerName = Mem.ReadStringAscii(playerNamePointer + 0x8, playerNameSize);

                }
            }
            return playerName;
        }
        public static Image RotateImage(Image img, float rotationAngle)
        {
            Bitmap b = new Bitmap(img.Width, img.Height);
            Graphics graphic = Graphics.FromImage(b);
            graphic.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
            graphic.RotateTransform(rotationAngle);
            graphic.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
            graphic.InterpolationMode = D2D.InterpolationMode.HighQualityBicubic;
            graphic.DrawImage(img, new Point(0, 0));
            graphic.Dispose();
            return b;
        }

        public int Players = 0;
        public int Zombies = 0;
        public int DeadPlayers = 0;
    
        private void dxThread()
        {
            while (true)
            {
                device.Clear(D3D.ClearFlags.Target, Color.FromArgb(0, 0, 0, 0), 1.0f, 0);
                device.RenderState.ZBufferEnable = false;
                device.RenderState.Lighting = false;
                device.RenderState.CullMode = D3D.Cull.None;
                device.Transform.Projection = Matrix.OrthoOffCenterLH(0, this.Width, this.Height, 0, 0, 1);
                device.BeginScene();

      
                    Mem.StartProcess();
                if (playerColor > 5)
                    playerColor = 5;

                if (zombieColor > 5)
                    zombieColor = 5;

                if (playerColor == 0)
                    PlayerColor = green;
                if (playerColor == 1)
                    PlayerColor = red;
                if (playerColor == 2)
                    PlayerColor = cyan;
                if (playerColor == 3)
                    PlayerColor = blue;
                if (playerColor == 4)
                    PlayerColor = yellow;
                if (playerColor == 5)
                    PlayerColor = pink;

                if (zombieColor == 0)
                    ZombieColor = green;
                if (zombieColor == 1)
                    ZombieColor = red;
                if (zombieColor == 2)
                    ZombieColor = cyan;
                if (zombieColor == 3)
                    ZombieColor = blue;
                if (zombieColor == 4)
                    ZombieColor = yellow;
                if (zombieColor == 5)
                    ZombieColor = pink;

                int pBase = Mem.ReadInt(0x10A3248);
                int pLocal = Mem.ReadInt(pBase + 0x15C0);
                pLocal = Mem.ReadInt(pLocal + 0x4);
                int pLocation = Mem.ReadInt(pLocal + 0x20);

                float LocX = Mem.ReadFloat(pLocation + 0x28);
                float LocY = Mem.ReadFloat(pLocation + 0x30);
                float LocZ = Mem.ReadFloat(pLocation + 0x2C);


                if (norec)
                {
                    Mem.WriteFloat(pLocal + 0xC3C, 0f);
                }
                else
                {
                    Mem.WriteFloat(pLocal + 0xC3C, 1f);
                }

                if(nofatigue)
                    Mem.WriteFloat(pLocal + 0xC58, 0f);
                else
                    Mem.WriteFloat(pLocal + 0xC5C, 0.99f);

              
                VECTOR3 g_LocalPos = new VECTOR3();
                g_LocalPos = ReadVECTOR3(Mem.ReadInt(pLocal + 0x20) + 0x28);

                int Transformations = 0x10F4244;
                int Starter = Mem.ReadInt(Transformations);
                dwTransData = Mem.ReadInt(Starter + 0x94);
                InvViewRight = ReadVECTOR3((int)dwTransData + 0x4);
                InvViewUp = ReadVECTOR3((int)dwTransData + 0x10);
                InvViewForward = ReadVECTOR3((int)dwTransData + 0x1C);
                InvViewTranslation = ReadVECTOR3((int)dwTransData + 0x28);
                ViewPortMatrix = ReadVECTOR3((int)dwTransData + 0x54);
                ProjD1 = ReadVECTOR3((int)dwTransData + 0xCC);
                ProjD2 = ReadVECTOR3((int)dwTransData + 0xD8);

                Point cursor = System.Windows.Forms.Cursor.Position;

                DrawFilledBox(cursor.X, cursor.Y, 5, 5, Color.White);


                if (NoFall)
                {
                    WriteVECTOR3(pLocation + 0x48, 0f);
                }

                if (t)
                {
                    if (bup)
                    {
                        //  DrawShadowText("UPPPP", new Point(500, 500), Color.Black);
                        WriteVECTOR3(pLocation + 0x48, 0f);
                        Mem.WriteFloat(pLocation + 0x2C, LocZ + stepHop);
                        Mem.WriteInt(pLocal + 0x198, 256);


                    }

                    else if (bdown)
                    {
                        WriteVECTOR3(pLocation + 0x48, 0f);
                        Mem.WriteFloat(pLocation + 0x2C, LocZ - stepHop);
                    }
                }

                VECTOR3 W2SN_Local_Player;
                W2SN_Local_Player = W2SN(g_LocalPos);


    
                VECTOR3 EntityPos;


                if (Instructions)
                {
                    DrawFilledBox(1750, 890, 200, 200, Color.Black);
                    DrawShadowText("Normal Speedhack: Left Control", new Point(1760, 900), Color.White);
                    DrawShadowText("FlyHack: Page Up = Up", new Point(1760, 930), Color.White);
                    DrawShadowText("FlyHack: Page Down = Down", new Point(1760, 960), Color.White);
                    DrawShadowText("FlyHack: Forward = Control", new Point(1760, 990), Color.White);


                }

                if (NoGrass)
                {
                    Mem.WriteFloat(pBase + 0x754, 0f);

                }

                else
                {
                    Mem.WriteFloat(pBase + 0x754, 10f);

                }


                if (showItems)
                {

                    int ItemTable = Mem.ReadInt(pBase + 0xFB8);
                    int SizeItem = Mem.ReadInt(pBase + 0xFBC);

                    for (int Iloot = 0; Iloot < SizeItem; Iloot++)
                    {
                        int loot = Mem.ReadInt(ItemTable + (Iloot * 0x4));
                        int lootvis = Mem.ReadInt(loot + 0x20);

                        VECTOR3 lootw2sn = W2SN(ReadVECTOR3(lootvis + 0x28));
                        double lootdistt = Distance(g_LocalPos, ReadVECTOR3(lootvis + 0x28));
                        int name1 = Mem.ReadInt(loot + 0x70);
                        int name2 = Mem.ReadInt(name1 + 0x34);
                        string classname = Mem.ReadStringAscii(name2 + 0x8, Mem.ReadInt(name2 + 0x4));

                        if (lootw2sn.z > 0.1)
                        {

                            DrawShadowText(classname + " Dist " + lootdistt.ToString(), new Point((int)lootw2sn.x, (int)lootw2sn.y), Color.Red);
                        }

                        if (ItemMagnet)
                        {


                            Mem.WriteFloat(lootvis + 0x28, g_LocalPos.x);
                            Mem.WriteFloat(lootvis + 0x30, g_LocalPos.y);
                            Mem.WriteFloat(lootvis + 0x2C, g_LocalPos.z);
                        }


                    }


                }
            
                    int TablePtr = Mem.ReadInt(pBase + 0x774);
                    int TableArray = Mem.ReadInt(TablePtr);
                    int TableSize = Mem.ReadInt(TablePtr + 0x4);
                    for (int i = 0; i < TableSize; i++)
                    {
                        int Entity = Mem.ReadInt(TableArray + (i * 0x2C));
                        int EntityPtr = Mem.ReadInt(Entity + 0x4);
                        int eVisualState = Mem.ReadInt(EntityPtr + 0x20);

               
                        EntityPos = ReadVECTOR3(eVisualState + 0x28);
                        VECTOR3 HeadPos = ReadVECTOR3(eVisualState + 0x130);
                        VECTOR3 realentitypos = ReadVECTOR3(eVisualState + 0x28);
                        VECTOR3 W2SNEntityPos = W2SN(HeadPos);
        

                        float EntX = Mem.ReadFloat(eVisualState + 0x130);
                        float EntY = Mem.ReadFloat(eVisualState + 0x140);
                        float EntZ = Mem.ReadFloat(eVisualState + 0x144);


                        int MunitionsTbl = Mem.ReadInt(pBase + 0x9C4);
                        int MunitionsTblSize = Mem.ReadInt(pBase + 0x9C8);

                        for (int iterate = 0; iterate < MunitionsTblSize; iterate++)
                        {
                            int bullet = Mem.ReadInt(MunitionsTbl + (0x4 * iterate));
                            int bulletstate = Mem.ReadInt(bullet + 0x20);

                            int bulletparent = Mem.ReadInt(bullet + 0x250);
                            bulletparent = Mem.ReadInt(bulletparent + 0x4);
                            int parentstate = Mem.ReadInt(bulletparent + 0x20);
                            VECTOR3 bulletparentpos = ReadVECTOR3(parentstate + 0x28);
                      
                             if (SilentAim)
                            {
                                if (Distance(HeadPos, g_LocalPos) < 500 && Distance(HeadPos, g_LocalPos) > 0.5)
                                {
                                    //if (bulletparentpos.x == g_LocalPos.x)
                                  //  {
                                        Mem.WriteFloat(bulletstate + 0x28, HeadPos.x);
                                        Mem.WriteFloat(bulletstate + 0x2C, HeadPos.z);
                                        Mem.WriteFloat(bulletstate + 0x30, HeadPos.y);
                                   // }

                                }
                            }
                        }


                        int name1 = Mem.ReadInt(EntityPtr + 0x70);
                        int name2 = Mem.ReadInt(name1 + 0x34);
                        string classname = Mem.ReadStringAscii(name2 + 0x8, Mem.ReadInt(name2 + 0x4));

                        int weaponptr = Mem.ReadInt(EntityPtr + 0xA2C);
                        int weaponptrname = Mem.ReadInt(weaponptr + 0x70);
                        int weaponptrclassname = Mem.ReadInt(weaponptrname + 0x34);
                        string weaponname = Mem.ReadStringAscii(weaponptrclassname + 0x8, Mem.ReadInt(weaponptrclassname + 0x4));
                     

                        if (classname.Contains("Survivor"))
                        {


                            


                            if (showPlayers)
                            {
                                if (W2SNEntityPos.z > 0.01f)
                                {
                                    if (Distance(g_LocalPos, EntityPos) < 1500 && Distance(g_LocalPos, EntityPos) > 0.1)
                                    {

                                        if (Mem.ReadByte(EntityPtr + 0x264) == 1)
                                        {
                                            if (showCorpses)
                                            {
                                                DrawShadowText("Dead Player" + " Distance: " + Distance(g_LocalPos, EntityPos).ToString(), new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y), PlayerColor);
                                            }
                                        }
                                        else
                                        {
                                            if(ShowWeapons) {
                                            DrawShadowText("Player ", new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y), PlayerColor);
                                            DrawShadowText("Distance : " + Distance(g_LocalPos, EntityPos).ToString(), new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y + 20), PlayerColor);
                                            if (weaponname != "") 
                                            DrawShadowText("Weapon : " + weaponname, new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y + 40), PlayerColor);
                                            }
                                            else {
                                                
                                                DrawShadowText("Player ", new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y), PlayerColor);
                                                DrawShadowText("Distance : " + Distance(g_LocalPos, EntityPos).ToString(), new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y + 20), PlayerColor);
                                            }
                                        }

                                    }
                                }
                            }
                        }

                        else if (classname.Contains("Zombie")) 
                        {
                            if (W2SNEntityPos.z > 0.01f)
                            {
                                if (showZombies)
                                {
                                    if (Distance(g_LocalPos, EntityPos) < 10000)
                                    {

                                        if (Mem.ReadByte(EntityPtr + 0x264) == 1)
                                        {
                                            if (showCorpses)
                                            {

                                            }
                                        }
                                        else
                                        {
                                            DrawShadowText("Zombie ", new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y), ZombieColor);
                                            DrawShadowText("Distance : " + Distance(g_LocalPos, EntityPos).ToString(), new Point((int)W2SNEntityPos.x, (int)W2SNEntityPos.y + 20), ZombieColor);
                                        }

                                    }

                                }
                            }
                        }
                    




                    



                        


                    

                }


                if (speedHack)
                {

                    VECTOR3 newPos;



                    newPos.x = g_LocalPos.x + stepHop * InvViewForward.x;
                    newPos.y = g_LocalPos.y + stepHop * InvViewRight.x;
                    newPos.z = 0.0f;
                    Mem.WriteFloat(pLocation + 0x28, newPos.x);
                    Mem.WriteFloat(pLocation + 0x30, newPos.y);
                }

                VECTOR3 Balota = new VECTOR3() { x = 5000f, y = 2000f, z = 50f };
                Balota = W2SN(Balota);

                VECTOR3 NEAF = new VECTOR3() { x = 12232f, y = 12578f, z = 200f };
                NEAF = W2SN(NEAF);

                VECTOR3 NWAF = new VECTOR3() { x = 4561, y = 9560, z = 350f };
                NWAF = W2SN(NWAF);

                float playerCoords = LocX + LocY + LocZ;

                float bx = (g_LocalPos.x - Balota.x);
                float by = (g_LocalPos.y - Balota.y);
                float bz = (g_LocalPos.z - Balota.z);
                float BalotaDist = (float)Math.Sqrt((bx * bx) + (by * by) + (bz * bz));
                if (BalotaDist < 0) { BalotaDist = Math.Abs(BalotaDist); }

                float nx = (g_LocalPos.x - NEAF.x);
                float ny = (g_LocalPos.y - NEAF.y);
                float nz = (g_LocalPos.z - NEAF.z);
                float NEAFDist = (float)Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
                if (NEAFDist < 0) { NEAFDist = Math.Abs(NEAFDist); }

                float nwx = (g_LocalPos.x - NWAF.x);
                float nwy = (g_LocalPos.y - NWAF.y);
                float nwz = (g_LocalPos.z - NWAF.z);
                float NWAFDist = (float)Math.Sqrt((nwx * nwx) + (nwy * nwy) + (nwz * nwz));
                if (NWAFDist < 0) { NWAFDist = Math.Abs(NWAFDist); }

                if (CurrentOption < 3)
                    CurrentOption = 2;
                if (stepHop < 0.05f)
                    stepHop = 0.05f;
                if (stepHop > 5f)
                    stepHop = 5f;

                int maxOptions = 0;
                if (CurrentMenu == 1)
                    maxOptions = 8;
                if (CurrentMenu == 2)
                    maxOptions = 10;
                if (CurrentMenu == 3)
                    maxOptions = 11;
                if (CurrentMenu == 4)
                    maxOptions = 11;
                if (CurrentOption == maxOptions)
                    CurrentOption = maxOptions - 1;
                CurrentY = CurrentOption * 13;

                menuTimer -= 1;
                string curMenu = "";
                if (CurrentMenu == 1)
                    curMenu = "Main";
                if (CurrentMenu == 2)
                    curMenu = "ESP";
                if (CurrentMenu == 3)
                    curMenu = "Misc";
                if (CurrentMenu == 4)
                    curMenu = "Settings";

                int TextX = 15;
                string MenuText = "Direct X Overlay by Teddi";
                string Version = " 1.5";
                DrawShadowText(MenuText + Version, new Point(this.Width / 2 - (MenuText.Length + Version.Length), 2), Color.White);
                if (showMenu == false)
                {
                  
                }
                else
                {   
                    if (curMenu == "Main")
                    {
                        DrawTransparentBox(0, 0, 200, 110, 248, Color.Black);
                        DrawShadowText(curMenu + " ", new Point(1, 0), Color.White);
                        DrawShadowText(">", new Point(0, CurrentY), Color.Red);

                        DrawShadowText("ESP ->", new Point(TextX, 26), Color.DeepSkyBlue);
                        DrawShadowText("Misc ->", new Point(TextX, 39), Color.DeepSkyBlue);
                        DrawShadowText("Settings ->", new Point(TextX, 52), Color.DeepSkyBlue);
                        if (speedHack)
                            DrawShadowText("Speedhack", new Point(TextX, 65), Color.Green);
                        else
                            DrawShadowText("Speedhack", new Point(TextX, 65), Color.Red);
                            DrawShadowText("Speed: " + stepHop, new Point(TextX, 78), Color.Red);
                        if (ItemMagnet)
                            DrawShadowText("Item Magnet", new Point(TextX, 91), Color.Green);
                        else
                            DrawShadowText("Item Magnet", new Point(TextX, 91), Color.Red);

                    }
                    if (curMenu == "ESP")
                    {
                        DrawTransparentBox(0, 0, 200, 135, 248, Color.Black);
                        DrawShadowText(curMenu + " ", new Point(1, 0), Color.White);
                        DrawShadowText(">", new Point(0, CurrentY), Color.Red);

                        DrawShadowText("<- Back", new Point(TextX, 26), Color.DeepSkyBlue);

                        if (showPlayers)
                            DrawShadowText("Show Players", new Point(TextX, 52), Color.Green);
                        else
                            DrawShadowText("Show Players", new Point(TextX, 52), Color.Red);

                        if (showCorpses)
                            DrawShadowText("Show Corpses", new Point(TextX, 65), Color.Green);
                        else
                            DrawShadowText("Show Corpses", new Point(TextX, 65), Color.Red);

                        if (showZombies)
                            DrawShadowText("Show Zombies", new Point(TextX, 78), Color.Green);
                        else
                            DrawShadowText("Show Zombies", new Point(TextX, 78), Color.Red);

                        if (showItems)
                            DrawShadowText("Show Items", new Point(TextX, 91), Color.Green);
                        else
                            DrawShadowText("Show Items", new Point(TextX, 91), Color.Red);

                        if (showAirfields)
                            DrawShadowText("Show Airfields", new Point(TextX, 117), Color.Green);
                        else
                            DrawShadowText("Show Airfields", new Point(TextX, 117), Color.Red);
                    }
                    if (curMenu == "Misc")
                    {
                        DrawTransparentBox(0, 0, 200, 200, 248, Color.Black);
                        DrawShadowText(curMenu + " ", new Point(1, 0), Color.White);
                        DrawShadowText(">", new Point(0, CurrentY), Color.Red);

                        DrawShadowText("<- Back", new Point(TextX, 26), Color.DeepSkyBlue);

                        if (SilentAim)
                            DrawShadowText("Silent Aim", new Point(TextX, 52), Color.Green);
                        else
                            DrawShadowText("Silent Aim", new Point(TextX, 52), Color.Red);

       


                        if (t)
                            DrawShadowText("Fly Hack", new Point(TextX, 78), Color.Green);
                        else
                        {
                            DrawShadowText("Fly Hack", new Point(TextX, 78), Color.Red);
                            bup = false;
                            bdown = false;
                        }

                        if (NoGrass)
                            DrawShadowText("No Grass", new Point(TextX, 91), Color.Green);
                        else
                            DrawShadowText("No Grass", new Point(TextX, 91), Color.Red);

                        if (NoFall)
                            DrawShadowText("No Fall", new Point(TextX, 104), Color.Green);
                        else
                            DrawShadowText("No Fall", new Point(TextX, 104), Color.Red);
                        
                        if(norec)
                            DrawShadowText("No Recoil", new Point(TextX, 117), Color.Green);
                        else
                            DrawShadowText("No Recoil", new Point(TextX, 117), Color.Red);

                        if (nofatigue)
                            DrawShadowText("No Fatigue", new Point(TextX, 130), Color.Green);
                        else
                            DrawShadowText("No Fatigue", new Point(TextX, 130), Color.Red);
                   
                    }
                    if (curMenu == "Settings")
                    {
                        DrawTransparentBox(0, 0, 200, 150, 248, Color.Black);
                        DrawShadowText(curMenu + " ", new Point(1, 0), Color.White);
                        DrawShadowText(">", new Point(0, CurrentY), Color.Red);

                        DrawShadowText("<- Back", new Point(TextX, 26), Color.DeepSkyBlue);
                        
                        DrawShadowText("Player Color", new Point(TextX, 52), PlayerColor);
                        DrawShadowText("Zombie Color", new Point(TextX, 65), ZombieColor);

                        if(ShowWeapons)
                            DrawShadowText("Show Weapons", new Point(TextX, 78), Color.Green);
                        else
                            DrawShadowText("Show Weapons", new Point(TextX, 78), Color.Red);

                        if (Instructions)
                            DrawShadowText("Show Instructions", new Point(TextX, 91), Color.Green);
                        else
                            DrawShadowText("Show Instructions", new Point(TextX, 91), Color.Red);


                      
                    }
                }
                if (showAirfields)
                {
                    if (Balota.z > 0.01f)
                        DrawShadowText("Balota: " + BalotaDist, new Point((int)Balota.x, (int)Balota.y), Color.Blue);
                    if (NEAF.z > 0.01f)
                        DrawShadowText("NEAF: " + NEAFDist, new Point((int)NEAF.x, (int)NEAF.y), Color.Red);
                    if (NWAF.z > 0.01f)
                        DrawShadowText("NWAF: " + NWAFDist, new Point((int)NWAF.x, (int)NWAF.y), Color.Green);        
                }
                device.EndScene();
                device.Present();
            }
        }
    }
}