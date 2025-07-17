using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace RaylibImguiStarter
{
    internal unsafe class ImGuiController
    {
        private Vector2 mousePosition;
        private Vector2 displaySize;
        private float delta;

        static double g_Time = 0.0;
        static bool g_UnloadAtlas = false;
        static uint g_AtlasTexID = 0;

        public ImGuiController()
        {
            var io = ImGui.GetIO();
            io.MousePos = new Vector2(0, 0);
            LoadDefaultFontAtlas();
        }

        public static void Shutdown()
        {
            if (g_UnloadAtlas) ImGui.GetIO().Fonts.ClearFonts();
            g_Time = 0.0;
        }

        private void UpdateMousePosAndButtons()
        {
            var io = ImGui.GetIO();

            if (io.WantSetMousePos) Raylib.SetMousePosition((int)io.MousePos.X, (int)io.MousePos.Y);

            io.MouseDown[0] = Raylib.IsMouseButtonDown(MouseButton.Left);
            io.MouseDown[1] = Raylib.IsMouseButtonDown(MouseButton.Right);
            io.MouseDown[2] = Raylib.IsMouseButtonDown(MouseButton.Middle);

            if (!Raylib.IsWindowMinimized()) mousePosition = new Vector2(Raylib.GetMouseX(), Raylib.GetMouseY());

            io.MousePos = mousePosition;
        }

        private static void UpdateMouseCursor()
        {
            var io = ImGui.GetIO();
            if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.NoMouseCursorChange)) return;

            if (io.MouseDrawCursor || ImGui.GetMouseCursor() == ImGuiMouseCursor.None)
            {
                Raylib.HideCursor();
            }
            else
            {
                Raylib.ShowCursor();
            }
        }

        public static bool ProcessEvent()
        {
            var io = ImGui.GetIO();

            foreach (var key in KeyboardToImGuiMap)
            {
                var raylibKey = key.Key;
                var imguiKey = key.Value;

                if (Raylib.IsKeyDown(raylibKey)) // touche est enfoncée
                {
                    io.AddKeyEvent(imguiKey, true);
                }
                else if (Raylib.IsKeyReleased(raylibKey))
                {
                    io.AddKeyEvent(imguiKey, false);
                }
            }

            int length = 0;
            io.AddInputCharactersUTF8(Raylib.CodepointToUTF8(Raylib.GetCharPressed(), ref length));

            return true;
        }

        public void NewFrame()
        {
            var io = ImGui.GetIO();

            displaySize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            io.DisplaySize = displaySize;

            double current_time = Raylib.GetTime();
            delta = g_Time > 0.0 ? (float)(current_time - g_Time) : 1.0f / 60.0f;
            io.DeltaTime = delta;

            UpdateMousePosAndButtons();
            UpdateMouseCursor();

            if (Raylib.GetMouseWheelMove() > 0)
            {
                io.MouseWheel += 1;
            }
            else if (Raylib.GetMouseWheelMove() < 0)
            {
                io.MouseWheel -= 1;
            }
        }

        private static void LoadDefaultFontAtlas()
        {
            if (!g_UnloadAtlas)
            {
                var io = ImGui.GetIO();
                byte* pixels;
                int width, height, bpp;
                Image image;

                io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bpp);
                var size = Raylib.GetPixelDataSize(width, height, PixelFormat.UncompressedR8G8B8A8);
                image.Data = (void*)Marshal.AllocHGlobal(size);
                Buffer.MemoryCopy(pixels, image.Data, size, size);
                image.Width = width;
                image.Height = height;
                image.Mipmaps = 1;
                image.Format = PixelFormat.UncompressedR8G8B8A8;
                var tex = Raylib.LoadTextureFromImage(image);
                g_AtlasTexID = tex.Id;
                io.Fonts.TexID = (IntPtr)g_AtlasTexID;
                Marshal.FreeHGlobal((IntPtr)pixels);
                Marshal.FreeHGlobal((IntPtr)image.Data);
                g_UnloadAtlas = true;
            }
        }

        public static void Render(ImDrawDataPtr draw_data)
        {
            Rlgl.DisableBackfaceCulling();
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdLists[n];
                uint idx_index = 0;
                for (int i = 0; i < cmd_list.CmdBuffer.Size; i++)
                {
                    var pcmd = cmd_list.CmdBuffer[i];
                    var pos = draw_data.DisplayPos;
                    var rectX = (int)(pcmd.ClipRect.X - pos.X);
                    var rectY = (int)(pcmd.ClipRect.Y - pos.Y);
                    var rectW = (int)(pcmd.ClipRect.Z - rectX);
                    var rectH = (int)(pcmd.ClipRect.W - rectY);
                    Raylib.BeginScissorMode(rectX, rectY, rectW, rectH);
                    {
                        var ti = pcmd.TextureId;
                        for (int j = 0; j <= (pcmd.ElemCount - 3); j += 3)
                        {
                            if (pcmd.ElemCount == 0)
                            {
                                break;
                            }

                            Rlgl.PushMatrix();
                            Rlgl.Begin(DrawMode.Triangles);
                            Rlgl.SetTexture((uint)ti.ToInt32());

                            ImDrawVertPtr vertex;
                            ushort index;

                            index = cmd_list.IdxBuffer[(int)(j + idx_index)];
                            vertex = cmd_list.VtxBuffer[index];
                            DrawTriangleVertex(vertex);

                            index = cmd_list.IdxBuffer[(int)(j + 2 + idx_index)];
                            vertex = cmd_list.VtxBuffer[index];
                            DrawTriangleVertex(vertex);

                            index = cmd_list.IdxBuffer[(int)(j + 1 + idx_index)];
                            vertex = cmd_list.VtxBuffer[index];
                            DrawTriangleVertex(vertex);

                            Rlgl.DisableTexture();
                            Rlgl.End();
                            Rlgl.PopMatrix();
                        }
                    }

                    idx_index += pcmd.ElemCount;
                }
            }

            Raylib.EndScissorMode();
            Rlgl.EnableBackfaceCulling();
        }

        private static void DrawTriangleVertex(ImDrawVertPtr idx_vert)
        {
            Color c = new Color((byte)(idx_vert.col >> 0), (byte)(idx_vert.col >> 8), (byte)(idx_vert.col >> 16), (byte)(idx_vert.col >> 24));
            Rlgl.Color4ub(c.R, c.G, c.B, c.A);
            Rlgl.TexCoord2f(idx_vert.uv.X, idx_vert.uv.Y);
            Rlgl.Vertex2f(idx_vert.pos.X, idx_vert.pos.Y);
        }

        public static readonly Dictionary<KeyboardKey, ImGuiKey> KeyboardToImGuiMap = new Dictionary<KeyboardKey, ImGuiKey>
        {
            { KeyboardKey.Apostrophe, ImGuiKey.Apostrophe },
            { KeyboardKey.Comma, ImGuiKey.Comma },
            { KeyboardKey.Minus, ImGuiKey.Minus },
            { KeyboardKey.Period, ImGuiKey.Period },
            { KeyboardKey.Slash, ImGuiKey.Slash },
            { KeyboardKey.Zero, ImGuiKey._0 },
            { KeyboardKey.One, ImGuiKey._1 },
            { KeyboardKey.Two, ImGuiKey._2 },
            { KeyboardKey.Three, ImGuiKey._3 },
            { KeyboardKey.Four, ImGuiKey._4 },
            { KeyboardKey.Five, ImGuiKey._5 },
            { KeyboardKey.Six, ImGuiKey._6 },
            { KeyboardKey.Seven, ImGuiKey._7 },
            { KeyboardKey.Eight, ImGuiKey._8 },
            { KeyboardKey.Nine, ImGuiKey._9 },
            { KeyboardKey.Semicolon, ImGuiKey.Semicolon },
            { KeyboardKey.Equal, ImGuiKey.Equal },
            { KeyboardKey.A, ImGuiKey.A },
            { KeyboardKey.B, ImGuiKey.B },
            { KeyboardKey.C, ImGuiKey.C },
            { KeyboardKey.D, ImGuiKey.D },
            { KeyboardKey.E, ImGuiKey.E },
            { KeyboardKey.F, ImGuiKey.F },
            { KeyboardKey.G, ImGuiKey.G },
            { KeyboardKey.H, ImGuiKey.H },
            { KeyboardKey.I, ImGuiKey.I },
            { KeyboardKey.J, ImGuiKey.J },
            { KeyboardKey.K, ImGuiKey.K },
            { KeyboardKey.L, ImGuiKey.L },
            { KeyboardKey.M, ImGuiKey.M },
            { KeyboardKey.N, ImGuiKey.N },
            { KeyboardKey.O, ImGuiKey.O },
            { KeyboardKey.P, ImGuiKey.P },
            { KeyboardKey.Q, ImGuiKey.Q },
            { KeyboardKey.R, ImGuiKey.R },
            { KeyboardKey.S, ImGuiKey.S },
            { KeyboardKey.T, ImGuiKey.T },
            { KeyboardKey.U, ImGuiKey.U },
            { KeyboardKey.V, ImGuiKey.V },
            { KeyboardKey.W, ImGuiKey.W },
            { KeyboardKey.X, ImGuiKey.X },
            { KeyboardKey.Y, ImGuiKey.Y },
            { KeyboardKey.Z, ImGuiKey.Z },
            { KeyboardKey.Space, ImGuiKey.Space },
            { KeyboardKey.Escape, ImGuiKey.Escape },
            { KeyboardKey.Enter, ImGuiKey.Enter },
            { KeyboardKey.Tab, ImGuiKey.Tab },
            { KeyboardKey.Backspace, ImGuiKey.Backspace },
            { KeyboardKey.Insert, ImGuiKey.Insert },
            { KeyboardKey.Delete, ImGuiKey.Delete },
            { KeyboardKey.Right, ImGuiKey.RightArrow },
            { KeyboardKey.Left, ImGuiKey.LeftArrow },
            { KeyboardKey.Down, ImGuiKey.DownArrow },
            { KeyboardKey.Up, ImGuiKey.UpArrow },
            { KeyboardKey.PageUp, ImGuiKey.PageUp },
            { KeyboardKey.PageDown, ImGuiKey.PageDown },
            { KeyboardKey.Home, ImGuiKey.Home },
            { KeyboardKey.End, ImGuiKey.End },
            { KeyboardKey.CapsLock, ImGuiKey.CapsLock },
            { KeyboardKey.ScrollLock, ImGuiKey.ScrollLock },
            { KeyboardKey.NumLock, ImGuiKey.NumLock },
            { KeyboardKey.PrintScreen, ImGuiKey.PrintScreen },
            { KeyboardKey.Pause, ImGuiKey.Pause },
            { KeyboardKey.F1, ImGuiKey.F1 },
            { KeyboardKey.F2, ImGuiKey.F2 },
            { KeyboardKey.F3, ImGuiKey.F3 },
            { KeyboardKey.F4, ImGuiKey.F4 },
            { KeyboardKey.F5, ImGuiKey.F5 },
            { KeyboardKey.F6, ImGuiKey.F6 },
            { KeyboardKey.F7, ImGuiKey.F7 },
            { KeyboardKey.F8, ImGuiKey.F8 },
            { KeyboardKey.F9, ImGuiKey.F9 },
            { KeyboardKey.F10, ImGuiKey.F10 },
            { KeyboardKey.F11, ImGuiKey.F11 },
            { KeyboardKey.F12, ImGuiKey.F12 },
            { KeyboardKey.LeftShift, ImGuiKey.LeftShift },
            { KeyboardKey.LeftControl, ImGuiKey.LeftCtrl },
            { KeyboardKey.LeftAlt, ImGuiKey.LeftAlt },
            { KeyboardKey.LeftSuper, ImGuiKey.LeftSuper },
            { KeyboardKey.RightShift, ImGuiKey.RightShift },
            { KeyboardKey.RightControl, ImGuiKey.RightCtrl },
            { KeyboardKey.RightAlt, ImGuiKey.RightAlt },
            { KeyboardKey.RightSuper, ImGuiKey.RightSuper },
            { KeyboardKey.KeyboardMenu, ImGuiKey.Menu },
            { KeyboardKey.LeftBracket, ImGuiKey.LeftBracket },
            { KeyboardKey.Backslash, ImGuiKey.Backslash },
            { KeyboardKey.RightBracket, ImGuiKey.RightBracket },
            { KeyboardKey.Grave, ImGuiKey.GraveAccent },
            { KeyboardKey.Kp0, ImGuiKey.Keypad0 },
            { KeyboardKey.Kp1, ImGuiKey.Keypad1 },
            { KeyboardKey.Kp2, ImGuiKey.Keypad2 },
            { KeyboardKey.Kp3, ImGuiKey.Keypad3 },
            { KeyboardKey.Kp4, ImGuiKey.Keypad4 },
            { KeyboardKey.Kp5, ImGuiKey.Keypad5 },
            { KeyboardKey.Kp6, ImGuiKey.Keypad6 },
            { KeyboardKey.Kp7, ImGuiKey.Keypad7 },
            { KeyboardKey.Kp8, ImGuiKey.Keypad8 },
            { KeyboardKey.Kp9, ImGuiKey.Keypad9 },
            { KeyboardKey.KpDecimal, ImGuiKey.KeypadDecimal },
            { KeyboardKey.KpDivide, ImGuiKey.KeypadDivide },
            { KeyboardKey.KpMultiply, ImGuiKey.KeypadMultiply },
            { KeyboardKey.KpSubtract, ImGuiKey.KeypadSubtract },
            { KeyboardKey.KpAdd, ImGuiKey.KeypadAdd },
            { KeyboardKey.KpEnter, ImGuiKey.KeypadEnter },
            { KeyboardKey.KpEqual, ImGuiKey.KeypadEqual },
        };
    }
}