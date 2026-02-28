using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace AAC_Game.Managers
{
    public class SaveManager
    {
        private static SaveManager _instance;
        public static SaveManager Instance => _instance ?? (_instance = new SaveManager());
        
        //ПУТИ К ФАЙЛАМ
        private readonly string _settingsPath = "settings.json";
        private readonly string _savePath = "save.json";
        
        //КОНСТРУКТОР
        private SaveManager() { }
        
        //СОХРАНЕНИЕ НАСТРОЕК
        public void SaveSettings(float mouseSens, double musicVol, double sfxVol, bool fullscreen, int resIndex)
        {
            try
            {
                var settings = new
                {
                    MouseSensitivity = mouseSens,
                    MusicVolume = musicVol,
                    SfxVolume = sfxVol,
                    IsFullscreen = fullscreen,
                    ResolutionIndex = resIndex,
                    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        public bool LoadSettings(ref float mouseSens, ref double musicVol, ref double sfxVol, ref bool fullscreen, ref int resIndex)
        {
            if (!File.Exists(_settingsPath))
                return false;

            try
            {
                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<JsonElement>(json);

                if (settings.TryGetProperty("MouseSensitivity", out var sens))
                    mouseSens = sens.GetSingle();
                if (settings.TryGetProperty("MusicVolume", out var music))
                    musicVol = music.GetDouble();
                if (settings.TryGetProperty("SfxVolume", out var sfx))
                    sfxVol = sfx.GetDouble();
                if (settings.TryGetProperty("IsFullscreen", out var full))
                    fullscreen = full.GetBoolean();
                if (settings.TryGetProperty("ResolutionIndex", out var res))
                    resIndex = res.GetInt32();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                return false;
            }
        }
        
        //СОХРАНЕНИЕ ПРОГРЕССА
        public void SaveGame(int level, int health, int ammo, int score, int kills,
                            float playerX, float playerY, float playerAngle)
        {
            try
            {
                var saveData = new
                {
                    CurrentLevel = level,
                    PlayerHealth = health,
                    PlayerAmmo = ammo,
                    PlayerScore = score,
                    PlayerKills = kills,
                    PlayerX = playerX,
                    PlayerY = playerY,
                    PlayerAngle = playerAngle,
                    SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_savePath, json);

                Console.WriteLine($"Игра сохранена. Уровень {level}, Счет: {score}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения игры: {ex.Message}");
            }
        }

        public bool LoadGame(ref int level, ref int health, ref int ammo, ref int score, ref int kills,
                            ref float playerX, ref float playerY, ref float playerAngle)
        {
            if (!File.Exists(_savePath))
                return false;

            try
            {
                string json = File.ReadAllText(_savePath);
                var saveData = JsonSerializer.Deserialize<JsonElement>(json);

                if (saveData.TryGetProperty("CurrentLevel", out var lvl))
                    level = lvl.GetInt32();
                if (saveData.TryGetProperty("PlayerHealth", out var hp))
                    health = hp.GetInt32();
                if (saveData.TryGetProperty("PlayerAmmo", out var am))
                    ammo = am.GetInt32();
                if (saveData.TryGetProperty("PlayerScore", out var sc))
                    score = sc.GetInt32();
                if (saveData.TryGetProperty("PlayerKills", out var kl))
                    kills = kl.GetInt32();
                if (saveData.TryGetProperty("PlayerX", out var x))
                    playerX = x.GetSingle();
                if (saveData.TryGetProperty("PlayerY", out var y))
                    playerY = y.GetSingle();
                if (saveData.TryGetProperty("PlayerAngle", out var ang))
                    playerAngle = ang.GetSingle();

                Console.WriteLine($"Загружена игра. Уровень {level}, Счет: {score}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки игры: {ex.Message}");
                return false;
            }
        }

        public bool HasSavedGame()
        {
            return File.Exists(_savePath);
        }

        public void DeleteSavedGame()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
                Console.WriteLine("Сохранение удалено");
            }
        }

        public string GetSaveInfo()
        {
            if (!File.Exists(_savePath))
                return "Нет сохранений";

            try
            {
                string json = File.ReadAllText(_savePath);
                var saveData = JsonSerializer.Deserialize<JsonElement>(json);

                int level = 0;
                int score = 0;
                string date = "Неизвестно";

                if (saveData.TryGetProperty("CurrentLevel", out var lvl))
                    level = lvl.GetInt32();
                if (saveData.TryGetProperty("PlayerScore", out var sc))
                    score = sc.GetInt32();
                if (saveData.TryGetProperty("SaveDate", out var dt))
                    date = dt.GetString();

                return $"Уровень {level}/5, Счет: {score} ({date})";
            }
            catch
            {
                return "Ошибка чтения сохранения";
            }
        }
    }
}