using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AAC_Game.Core
{
    // ЭКРАНЫ ИГРЫ
    public enum GameScreen
    {
        Agreement,
        MainMenu,
        DifficultySelect,
        Settings,
        Playing,
        Pause,
        LevelComplete,
        Victory,
        Help       
    }
    // УРОВНИ СЛОЖНОСТИ
    public enum GameDifficulty
    {
        Easy = 1,
        Normal = 2,
        Hard = 3
    }
    // РАСЧЁТ СЛОЖНОСТИ
    public static class DifficultyHelper
    {
        public static float GetHpMultiplier(GameDifficulty difficulty)
        {
            if (difficulty == GameDifficulty.Easy)
                return 1f;
            if (difficulty == GameDifficulty.Normal)
                return 2f;
            if (difficulty == GameDifficulty.Hard)
                return 3.5f;
            return 1f;
        }
        public static float GetDamageMultiplier(GameDifficulty difficulty)
        {
            if (difficulty == GameDifficulty.Easy)
                return 0.7f;
            if (difficulty == GameDifficulty.Normal)
                return 1f;
            if (difficulty == GameDifficulty.Hard)
                return 1.5f;
            return 1f;
        }
    }
}