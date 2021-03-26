using System.IO;
using System.Text.Json;
using System.Threading;

namespace True_Mining_Desktop.Core.NextStart
{
    internal class Instructions
    {
        public bool useThisInstructions { get; set; } = false;

        public bool startMining { get; set; } = false;
        public bool ignoreUpdates { get; set; } = true;
        public bool startHiden { get; set; } = false;
    }

    internal class Actions
    {
        public static void Save(Instructions instructions)
        {
            string text = JsonSerializer.Serialize<Instructions>(instructions);

            while (Tools.IsFileLocked(new FileInfo("NextStartInstructions.json"))) { Thread.Sleep(500); }
            File.WriteAllText("NextStartInstructions.json", text);
        }

        public static Instructions loadedNextStartInstructions = new Instructions();

        public static void Load()
        {
            if (File.Exists("NextStartInstructions.json"))
            {
                while (Tools.IsFileLocked(new FileInfo("NextStartInstructions.json"))) { Thread.Sleep(500); }
                loadedNextStartInstructions = JsonSerializer.Deserialize<Instructions>(File.ReadAllText("NextStartInstructions.json"));

                DeleteInstructions();
            }
        }

        public static void DeleteInstructions()
        {
            while (Tools.IsFileLocked(new FileInfo("NextStartInstructions.json"))) { Thread.Sleep(500); }
            File.Delete("NextStartInstructions.json");
        }
    }
}