using Newtonsoft.Json;

#nullable disable

public class ProgramConfig
{
    public string FlareSolverrApiUrl { get; set; } = "http://localhost:8191/";

    public List<string> IgnoreCreateList { get; set; } = new List<string>() { "dikko", "minamiaki" };

    public void InitProgramConfig()
    {
        try { File.WriteAllText("ProgramConfigExample.json", JsonConvert.SerializeObject(new ProgramConfig(), Formatting.Indented)); } catch { }
        if (!File.Exists("ProgramConfig.json"))
        {
            Log.Error($"ProgramConfig.json 遺失，請依照 {Path.GetFullPath("ProgramConfigExample.json")} 內的格式填入正確的數值");
            if (!Console.IsInputRedirected)
                Console.ReadKey();
            Environment.Exit(3);
        }

        try
        {
            var config = JsonConvert.DeserializeObject<ProgramConfig>(File.ReadAllText("ProgramConfig.json"));

            if (string.IsNullOrWhiteSpace(config.FlareSolverrApiUrl))
            {
                Log.Error($"{nameof(FlareSolverrApiUrl)} 遺失，請輸入至 ProgramConfig.json 後重開程式");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            FlareSolverrApiUrl = config.FlareSolverrApiUrl;
            IgnoreCreateList = config.IgnoreCreateList;
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            throw;
        }
    }
}