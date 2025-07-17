using Raylib_cs;
using ImGuiNET;

namespace RaylibImguiStarter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Raylib.InitWindow(1000, 720, "Raylib GUI Title");
            Raylib.SetTargetFPS(60);

            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            var controller = new ImGuiController();
            string textAreaContent = "Enter your text here...";
            int textBufferSize = 1024; // Taille du buffer pour le textarea (taille max du texte)

            while (!Raylib.WindowShouldClose()) // GameLoop
            {
                controller.NewFrame();
                ImGuiController.ProcessEvent();
                ImGui.NewFrame();
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);
                ImGui.Begin("IMGUI GUI Title");

                // Ajouter un textarea au-dessus du bouton
                ImGui.InputTextMultiline("##TextArea", ref textAreaContent, (uint)textBufferSize, new System.Numerics.Vector2(500, 200));

                // Ajouter le bouton en dessous du textarea
                if (ImGui.Button("Click me!")) ImGui.OpenPopup("Alert");
                
                if (ImGui.BeginPopup("Alert")) // Fenêtre pop-up d'alerte
                {
                    ImGui.Text($"Button clicked,\nTextArea content : {textAreaContent}.");
                    ImGui.EndPopup();
                }

                ImGui.End();
                ImGui.Render();
                ImGuiController.Render(ImGui.GetDrawData());
                Raylib.EndDrawing();
            }
            ImGuiController.Shutdown();
        }
    }
}
