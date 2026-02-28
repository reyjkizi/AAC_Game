using System;
using System.Drawing;
using AAC_Game.Core;
using AAC_Game.Managers;

namespace AAC_Game.Entities
{
    
    //ТИПЫ СУЩНОСТЕЙ
    public enum EntityType
    {
        Enemy,
        FastEnemy,
        TankEnemy,
        Boss,
        Health,
        Ammo
    }

    public class GameEntity
    {
        //ОСНОВНЫЕ СВОЙСТВА 
        public float X { get; set; }
        public float Y { get; set; }
        public EntityType Type { get; private set; }
        public Bitmap Sprite { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; private set; }
        public float Speed { get; set; }
        
        //ВСПОМОГАТЕЛЬНЫЕ СВОЙСТВА
        public bool IsEnemy
        {
            get
            {
                return Type == EntityType.Enemy ||
                       Type == EntityType.FastEnemy ||
                       Type == EntityType.TankEnemy ||
                       Type == EntityType.Boss;
            }
        }

        public bool IsItem
        {
            get
            {
                return Type == EntityType.Health || Type == EntityType.Ammo;
            }
        }

        //КОНСТРУКТОР
        public GameEntity(float x, float y, EntityType type, GameDifficulty difficulty, int currentLevel)
        {
            X = x;
            Y = y;
            Type = type;

            InitializeSprite();
            InitializeStats(difficulty, currentLevel);
        }
        
        //ИНИЦИАЛИЗАЦИЯ
        private void InitializeSprite()
        {
            var resources = ResourceManager.Instance;

            switch (Type)
            {
                case EntityType.Enemy:
                    Sprite = resources.EnemySprite;
                    break;
                case EntityType.FastEnemy:
                    Sprite = resources.EnemyFastSprite;
                    break;
                case EntityType.TankEnemy:
                    Sprite = resources.EnemyTankSprite;
                    break;
                case EntityType.Boss:
                    Sprite = resources.BossSprite;
                    break;
                case EntityType.Health:
                    Sprite = resources.HealthIcon;
                    break;
                case EntityType.Ammo:
                    Sprite = resources.AmmoIcon;
                    break;
                default:
                    Sprite = resources.EnemySprite;
                    break;
            }
        }
        private void InitializeStats(GameDifficulty difficulty, int currentLevel)
        {
            float levelMultiplier = 1.0f + (currentLevel * 0.2f);

            float difficultyMultiplier = DifficultyHelper.GetHpMultiplier(difficulty);

            switch (Type)
            {
                case EntityType.Enemy:
                    MaxHP = (int)(2 * levelMultiplier * difficultyMultiplier);
                    Speed = 0.025f * levelMultiplier;
                    break;

                case EntityType.FastEnemy:
                    MaxHP = (int)(1 * levelMultiplier * difficultyMultiplier);
                    Speed = 0.05f * levelMultiplier;
                    break;

                case EntityType.TankEnemy:
                    MaxHP = (int)(4 * levelMultiplier * difficultyMultiplier);
                    Speed = 0.015f * levelMultiplier;
                    break;

                case EntityType.Boss:
                    MaxHP = (int)(20 * difficultyMultiplier);
                    Speed = 0.02f * levelMultiplier;
                    break;

                default:
                    MaxHP = 1;
                    Speed = 0;
                    break;
            }

            HP = MaxHP;
        }
        
        //МЕТОДЫ
        public void TakeDamage(int damage)
        {
            HP = Math.Max(0, HP - damage);
        }
        public float GetHealthPercent()
        {
            if (MaxHP <= 0) return 0;
            return (float)HP / MaxHP;
        }
    }
}