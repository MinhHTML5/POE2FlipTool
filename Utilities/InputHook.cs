using System.Runtime.InteropServices;

namespace POE2FlipTool.Utilities
{
    public class InputHook
    {
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;

        private Action<MouseButtons, bool> _onMouseEvent;
        private Action<Keys, bool, bool> _onKeyEvent;
        private bool _controlPressing = false;


        [DllImport("User32.dll")]
        static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct RAWMOUSE
        {
            [FieldOffset(0)] public ushort usFlags;
            [FieldOffset(4)] public uint ulButtons;
            [FieldOffset(4)] public ushort usButtonFlags;
            [FieldOffset(6)] public ushort usButtonData;
            [FieldOffset(8)] public uint ulRawButtons;
            [FieldOffset(12)] public int lLastX;
            [FieldOffset(16)] public int lLastY;
            [FieldOffset(20)] public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
        }







        [DllImport("User32.dll")]
        static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        public void RegisterRawInputDevices(IntPtr hwnd, Action<MouseButtons, bool> onMouseEvent, Action<Keys, bool, bool> onKeyEvent)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[2];

            rid[0].usUsagePage = 0x01;    // Generic desktop controls
            rid[0].usUsage = 0x02;        // Mouse
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = hwnd;

            rid[1].usUsagePage = 0x01;    // Generic desktop controls
            rid[1].usUsage = 0x06;        // Keyboard
            rid[1].dwFlags = RIDEV_INPUTSINK;
            rid[1].hwndTarget = hwnd;

            _onMouseEvent = onMouseEvent;
            _onKeyEvent = onKeyEvent;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                MessageBox.Show("Failed to register raw input devices.");
            }
        }


        public void ProcessRawInput(IntPtr hRawInput)
        {
            uint dwSize = 0;
            GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize)
                    return;

                RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);

                if (raw.header.dwType == 0) // Mouse
                {
                    ushort buttonFlags = raw.mouse.usButtonFlags;
                    if ((buttonFlags & 0x0001) != 0) _onMouseEvent(MouseButtons.Left, true);
                    if ((buttonFlags & 0x0002) != 0) _onMouseEvent(MouseButtons.Left, false);
                    if ((buttonFlags & 0x0004) != 0) _onMouseEvent(MouseButtons.Right, true);
                    if ((buttonFlags & 0x0008) != 0) _onMouseEvent(MouseButtons.Right, false);
                    if ((buttonFlags & 0x0010) != 0) _onMouseEvent(MouseButtons.Middle, true);
                    if ((buttonFlags & 0x0020) != 0) _onMouseEvent(MouseButtons.Middle, false);
                    if ((buttonFlags & 0x0040) != 0) _onMouseEvent(MouseButtons.XButton1, true);
                    if ((buttonFlags & 0x0080) != 0) _onMouseEvent(MouseButtons.XButton1, false);
                    if ((buttonFlags & 0x0100) != 0) _onMouseEvent(MouseButtons.XButton2, true);
                    if ((buttonFlags & 0x0200) != 0) _onMouseEvent(MouseButtons.XButton2, false);
                }
                else if (raw.header.dwType == 1) // Keyboard
                {
                    IntPtr keyboardPtr = IntPtr.Add(buffer, Marshal.SizeOf<RAWINPUTHEADER>());
                    RAWKEYBOARD keyboard = Marshal.PtrToStructure<RAWKEYBOARD>(keyboardPtr);

                    ushort vk = keyboard.VKey;
                    bool isDown = (keyboard.Flags & 0x01) == 0;
                    bool isUp = !isDown;
                    if ((Keys)vk == Keys.ControlKey)
                    {
                        _controlPressing = isDown;
                    }
                    else
                    {
                        _onKeyEvent((Keys)vk, isDown, _controlPressing);
                    }

                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }



        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        public void PressKey(Keys key, bool control = false)
        {
            if (control && !_controlPressing)
            {
                keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            }
            System.Threading.Thread.Sleep(20);
            keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            System.Threading.Thread.Sleep(20);
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            System.Threading.Thread.Sleep(20);
            if (control && !_controlPressing)
            {
                keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        public void SendKeyDown(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        }

        public void SendKeyUp(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }





        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const int INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern bool ShowCursor(bool bShow);

        public PointF GetCurrentMousePosition()
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            GetCursorPos(out POINT originalPos);
            PointF result = new PointF((float)originalPos.X / screenWidth, (float)originalPos.Y / screenHeight);
            return result;
        }
        public void SendLeftClick(bool control = false)
        {
            INPUT[] inputs = new INPUT[2];

            // Click down
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

            // Click up
            inputs[1].type = INPUT_MOUSE;
            inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

            if (control && !_controlPressing)
            {
                keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            }
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (control && !_controlPressing)
            {
                keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        public void MoveMouse(int x, int y)
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            int absX = x * 65535 / screenWidth;
            int absY = y * 65535 / screenHeight;

            INPUT[] inputs = new INPUT[1];

            // Move to target (invisibly if you want)
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = absX;
            inputs[0].mi.dy = absY;
            inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public void MoveMouseSmooth(int targetX, int targetY, int durationMs = 250)
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            Point start = Cursor.Position;

            int steps = Math.Max(15, durationMs / 8);
            const int JITTER_THRESHOLD = 300; // pixels

            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;

                // Smoothstep easing (human-like)
                double eased = t * t * (3 - 2 * t);

                int x = (int)(start.X + (targetX - start.X) * eased);
                int y = (int)(start.Y + (targetY - start.Y) * eased);

                // ---- distance-based jitter gate ----
                int dxRemain = targetX - x;
                int dyRemain = targetY - y;
                double distance = Math.Sqrt(dxRemain * dxRemain + dyRemain * dyRemain);

                if (distance >= JITTER_THRESHOLD)
                {
                    x += Random.Shared.Next(0, 2);
                    y += Random.Shared.Next(0, 2);
                }

                int absX = x * 65535 / screenWidth;
                int absY = y * 65535 / screenHeight;

                INPUT input = new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = absX,
                        dy = absY,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                    }
                };

                SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

                Thread.Sleep(Random.Shared.Next(5, 12));
            }
        }


        public void ShowMouseCursor(bool show)
        {
            ShowCursor(show);
        }
    }
}
