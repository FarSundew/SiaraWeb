using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SIARAWEB.Models
{
    public class PeriodConfig
    {
        public string Periodo1 { get; set; } = "Enero - Junio";
        public string Periodo2 { get; set; } = "Julio - Diciembre";
        public string? PeriodoExtra { get; set; } = "";

        private static string GetFilePath() =>
            Path.Combine(Directory.GetCurrentDirectory(), "period_settings.json");

        public static PeriodConfig Load()
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<PeriodConfig>(json);
                    if (cfg != null) return cfg;
                }
                catch { }
            }
            return new PeriodConfig();
        }

        public void Save()
        {
            var path = GetFilePath();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public List<string> GetOpciones()
        {
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(Periodo1)) list.Add(Periodo1.Trim());
            if (!string.IsNullOrWhiteSpace(Periodo2)) list.Add(Periodo2.Trim());
            if (!string.IsNullOrWhiteSpace(PeriodoExtra))
            {
                foreach (var item in PeriodoExtra.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                        list.Add(item.Trim());
                }
            }
            return list.Distinct().ToList();
        }
    }
}

