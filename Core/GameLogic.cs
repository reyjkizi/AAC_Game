using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AAC_Game.Entities;
using AAC_Game.Managers;
using AAC_Game.UI;

namespace AAC_Game.Core
{
    public class GameLogic
    {
        //ИГРОВЫЕ ОБЪЕКТЫ
        public Player Player { get; private set; }

        public Map CurrentMap { get; private set; }

        public List<Particle> Particles { get; private set; }

        //СОСТОЯНИЕ ИГРЫ
        public GameScreen CurrentScreen { get; set; }
        public GameDifficulty SelectedDifficulty { get; set; }
        public int CurrentLevel { get; set; }
        public bool IsGameOver { get; set; }
        public bool IsPaused { get; set; }
        public bool GameStarted { get; set; }
        public int KillsCount { get; set; }

        //ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        public int DamageFlash { get; set; }
        public int InvulnerabilityTimer { get; set; }
        public int HitMarkerTimer { get; set; }
        public int ShootTimer { get; set; }
        public bool IsShooting { get; set; }
        public float WalkCycle { get; set; }
        public float BobAmount { get; set; }

        //ЭЛЕМЕНТЫ ИНТЕРФЕЙСА
        public Rectangle BtnRestartPos { get; set; }
        public Rectangle BtnExitPos { get; set; }

        //СОБЫТИЯ 
        public event Action OnLevelCompleted;
        public event Action OnGamePaused;

        //СОГЛАШЕНИЕ 
        public float AgreementScrollY { get; set; }
        public bool IsAgreementScrolledToEnd { get; set; }
        public int AgreementTotalHeight { get; set; }
        public const int AgreementViewHeight = 280;

        //СТАТИСТИКА И БОНУСЫ
        public int ShotsFired { get; set; }
        public int ShotsHit { get; set; }
        public int DamageTaken { get; set; }
        public int LevelStartTime { get; set; }
        public int LevelFrames { get; set; }
        public bool NoDamageBonus { get; set; }

        //НАСТРОЙКИ
        public float MouseSensitivity { get; set; } = 0.003f;

        //ПРИВАТНЫЕ ПОЛЯ
        private SaveManager _saveManager;           
        private Random _random = new Random();      
        private GameRenderer _renderer;              
        private AudioManager _audio;                  
        private ResourceManager _resources;           
        private const int INVULNERABILITY_DURATION = 20;
        private const int DAMAGE_FLASH_DURATION = 15;
        
        //КОНСТРУКТОР
        public GameLogic()
        {
            _resources = ResourceManager.Instance;
            _audio = AudioManager.Instance;
            _renderer = new GameRenderer();
            _saveManager = SaveManager.Instance;

            Player = new Player(2, 2);
            CurrentMap = new Map();
            Particles = new List<Particle>();

            CurrentScreen = GameScreen.Agreement;
            SelectedDifficulty = GameDifficulty.Easy;
            CurrentLevel = 1;
            KillsCount = 0;
        }

        //СОХРАНЕНИЕ/ЗАГРУЗКА 
        public void SaveGame()
        {
            _saveManager.SaveGame(
                CurrentLevel,
                Player.Health,
                Player.Ammo,
                Player.Score,
                KillsCount,
                Player.X,
                Player.Y,
                Player.Angle
            );

            _audio.PlayClick();
        }

        public bool LoadGame()
        {
            int level = 1;
            int health = 100;
            int ammo = 20;
            int score = 0;
            int kills = 0;
            float x = 2;
            float y = 2;
            float angle = 0;

            if (_saveManager.LoadGame(ref level, ref health, ref ammo, ref score, ref kills, ref x, ref y, ref angle))
            {
                CurrentLevel = level;
                Player.Health = health;
                Player.Ammo = ammo;
                Player.Score = score;
                KillsCount = kills;
                Player.X = x;
                Player.Y = y;
                Player.Angle = angle;

                InitializeLevel();

                GameStarted = true;
                IsGameOver = false;
                IsPaused = false;
                CurrentScreen = GameScreen.Playing;

                _audio.PlayClick();
                return true;
            }

            return false;
        }

        public void DeleteSave()
        {
            _saveManager.DeleteSavedGame();
        }
        
        //ИНИЦИАЛИЗАЦИЯ УРОВНЯ
        public void InitializeLevel()
        {
            CurrentMap.Load(CurrentLevel);
            CurrentMap.ClearEntities();
            SpawnEntitiesForLevel();
        }
        private void SpawnEntitiesForLevel()
        {
            switch (CurrentLevel)
            {
                case 1:
                    SpawnSafeEntities(EntityType.Enemy, 3);
                    SpawnSafeEntities(EntityType.Ammo, 3);
                    SpawnSafeEntities(EntityType.Health, 2);
                    break;
                case 2:
                    SpawnSafeEntities(EntityType.Enemy, 4);
                    SpawnSafeEntities(EntityType.FastEnemy, 2);
                    SpawnSafeEntities(EntityType.Ammo, 4);
                    SpawnSafeEntities(EntityType.Health, 2);
                    break;
                case 3:
                    SpawnSafeEntities(EntityType.Enemy, 5);
                    SpawnSafeEntities(EntityType.FastEnemy, 3);
                    SpawnSafeEntities(EntityType.Ammo, 5);
                    SpawnSafeEntities(EntityType.Health, 3);
                    break;
                case 4:
                    SpawnSafeEntities(EntityType.TankEnemy, 2);
                    SpawnSafeEntities(EntityType.FastEnemy, 4);
                    SpawnSafeEntities(EntityType.Ammo, 6);
                    SpawnSafeEntities(EntityType.Health, 3);
                    break;
                case 5:
                    SpawnSafeEntities(EntityType.Boss, 1);
                    SpawnSafeEntities(EntityType.FastEnemy, 6);
                    SpawnSafeEntities(EntityType.Ammo, 8);
                    SpawnSafeEntities(EntityType.Health, 4);
                    break;
            }
        }
        private void SpawnSafeEntities(EntityType type, int count)
        {
            List<Point> validSpots = new List<Point>();

            for (int y = 1; y < CurrentMap.Height - 1; y++)
            {
                for (int x = 1; x < CurrentMap.Width - 1; x++)
                {
                    if (IsAreaClear(x, y))
                    {
                        validSpots.Add(new Point(x, y));
                    }
                }
            }

            if (validSpots.Count < count)
            {
                for (int y = 1; y < CurrentMap.Height - 1; y++)
                    for (int x = 1; x < CurrentMap.Width - 1; x++)
                        if (CurrentMap.Grid[y, x] == 0 && !validSpots.Contains(new Point(x, y)))
                            validSpots.Add(new Point(x, y));
            }

            int spawned = 0;
            while (spawned < count && validSpots.Count > 0)
            {
                int idx = _random.Next(validSpots.Count);
                Point p = validSpots[idx];

                float posX = p.X + 0.5f;
                float posY = p.Y + 0.5f;

                double dist = Math.Sqrt(Math.Pow(posX - Player.X, 2) + Math.Pow(posY - Player.Y, 2));

                if (dist > 4.0 || validSpots.Count < 5)
                {
                    var entity = new GameEntity(posX, posY, type, SelectedDifficulty, CurrentLevel);
                    CurrentMap.Entities.Add(entity);
                    spawned++;
                }

                validSpots.RemoveAt(idx);
            }
        }

        private bool IsAreaClear(int cx, int cy)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
                for (int x = cx - 1; x <= cx + 1; x++)
                    if (CurrentMap.Grid[y, x] != 0) return false;
            return true;
        }

        //ИГРОВОЙ ЦИКЛ 
        public void Update(HashSet<Keys> pressedKeys, Point mousePosition, int formWidth, int formHeight)
        {
            ManageMusic();

            if (CurrentScreen != GameScreen.Playing || IsGameOver)
                return;

            if (pressedKeys.Contains(Keys.Escape))
            {
                IsPaused = true;
                CurrentScreen = GameScreen.Pause;
                _audio.StopStep();
                OnGamePaused?.Invoke();
                pressedKeys.Remove(Keys.Escape);
                return;
            }

            if (IsPaused)
                return;

            int deltaX = mousePosition.X - (formWidth / 2);
            Player.Angle += deltaX * MouseSensitivity;

            HandleMovement(pressedKeys);
            UpdateEffects();
            UpdateEntities();
            UpdateParticles();
            CheckLevelCompletion();
        }

        private void HandleMovement(HashSet<Keys> pressedKeys)
        {
            float moveSpeed = 0.08f;
            float radius = 0.3f;
            bool isMoving = false;
            float nx = Player.X, ny = Player.Y;

            if (pressedKeys.Contains(Keys.W))
            {
                nx += (float)Math.Cos(Player.Angle) * moveSpeed;
                ny += (float)Math.Sin(Player.Angle) * moveSpeed;
                isMoving = true;
            }
            if (pressedKeys.Contains(Keys.S))
            {
                nx -= (float)Math.Cos(Player.Angle) * moveSpeed;
                ny -= (float)Math.Sin(Player.Angle) * moveSpeed;
                isMoving = true;
            }
            if (pressedKeys.Contains(Keys.A))
            {
                nx += (float)Math.Cos(Player.Angle - Math.PI / 2) * moveSpeed;
                ny += (float)Math.Sin(Player.Angle - Math.PI / 2) * moveSpeed;
                isMoving = true;
            }
            if (pressedKeys.Contains(Keys.D))
            {
                nx += (float)Math.Cos(Player.Angle + Math.PI / 2) * moveSpeed;
                ny += (float)Math.Sin(Player.Angle + Math.PI / 2) * moveSpeed;
                isMoving = true;
            }

            if (CurrentMap.CanMoveTo((int)(nx + (nx > Player.X ? radius : -radius)), (int)Player.Y))
                Player.X = nx;
            if (CurrentMap.CanMoveTo((int)Player.X, (int)(ny + (ny > Player.Y ? radius : -radius))))
                Player.Y = ny;

            if (isMoving)
            {
                WalkCycle += 0.2f;
                BobAmount = 2 + (float)Math.Sin(WalkCycle) * 2;

                if (WalkCycle % 1.0f < 0.2f && WalkCycle > 0.1f)
                {
                    _audio.PlayStep();
                }
            }
            else
            {
                WalkCycle = 0;
                BobAmount = 0;
                _audio.StopStep();
            }
        }

        private void UpdateEffects()
        {
            if (DamageFlash > 0)
                DamageFlash--;
            if (InvulnerabilityTimer > 0)
                InvulnerabilityTimer--;
            if (IsShooting)
            {
                ShootTimer--;
                if (ShootTimer <= 0)
                    IsShooting = false;
            }
        }

        private void UpdateEntities()
        {
            bool tookDamageThisFrame = false;
            int totalDamage = 0;

            foreach (var entity in CurrentMap.Entities.ToList())
            {
                float dx = Player.X - entity.X;
                float dy = Player.Y - entity.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (entity.IsEnemy)
                {
                    UpdateEnemyMovement(entity, dx, dy, dist);

                    if (dist < 0.8f)
                    {
                        int damage = entity.Type == EntityType.Boss ? 30 : 10;
                        totalDamage += damage;
                        tookDamageThisFrame = true;
                        NoDamageBonus = false;
                    }
                }

                if (entity.IsItem && dist < 0.7f)
                {
                    if (entity.Type == EntityType.Health)
                    {
                        if (Player.Health < 100)
                        {
                            Player.Heal(25);
                            CurrentMap.Entities.Remove(entity);
                            _audio.PlayClick();
                        }
                    }
                    else if (entity.Type == EntityType.Ammo)
                    {
                        Player.AddAmmo(20);
                        CurrentMap.Entities.Remove(entity);
                        _audio.PlayClick();
                    }
                }
            }

            if (tookDamageThisFrame && InvulnerabilityTimer <= 0)
            {
                Player.TakeDamage(totalDamage);
                DamageTaken += totalDamage;
                DamageFlash = DAMAGE_FLASH_DURATION;
                InvulnerabilityTimer = INVULNERABILITY_DURATION;
                _audio.PlayHit();

                if (Player.Health <= 0)
                {
                    Player.Health = 0;
                    IsGameOver = true;
                    _audio.PlayDeath();
                    _audio.StopStep();
                }
            }
        }

        private void UpdateEnemyMovement(GameEntity enemy, float dx, float dy, float dist)
        {
            if (dist < 15f && dist > 0.6f)
            {
                float nextX = enemy.X + (dx / dist) * enemy.Speed;
                float nextY = enemy.Y + (dy / dist) * enemy.Speed;
                float r = 0.35f;

                bool canMoveX = CurrentMap.CanMoveTo((int)(nextX + (dx > 0 ? r : -r)), (int)enemy.Y);
                bool canMoveY = CurrentMap.CanMoveTo((int)enemy.X, (int)(nextY + (dy > 0 ? r : -r)));

                if (canMoveX) enemy.X = nextX;
                if (canMoveY) enemy.Y = nextY;

                foreach (var other in CurrentMap.Entities)
                {
                    if (enemy == other || !other.IsEnemy) continue;
                    float ex = enemy.X - other.X;
                    float ey = enemy.Y - other.Y;
                    float edist = (float)Math.Sqrt(ex * ex + ey * ey);
                    if (edist < 0.6f && edist > 0.01f)
                    {
                        enemy.X += (ex / edist) * 0.02f;
                        enemy.Y += (ey / edist) * 0.02f;
                    }
                }
            }
        }

        private void UpdateParticles()
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                Particles[i].Update();
                if (!Particles[i].IsAlive)
                {
                    Particles.RemoveAt(i);
                }
            }
        }

        private void CheckLevelCompletion()
        {
            int enemiesCount = CurrentMap.GetEnemyCount();

            if (enemiesCount == 0 && CurrentScreen == GameScreen.Playing && GameStarted && !IsGameOver)
            {
                _audio.StopStep();
                OnLevelCompleted?.Invoke();

                if (CurrentLevel < 5)
                {
                    CurrentScreen = GameScreen.LevelComplete;
                }
                else
                {
                    CurrentScreen = GameScreen.Victory;
                }
            }
        }

        //СТРЕЛЬБА 
        public void Shoot()
        {
            if (Player.Ammo <= 0 || IsShooting) return;

            _audio.PlayShoot();
            Player.UseAmmo();
            IsShooting = true;
            ShootTimer = 6;
            ShotsFired++;

            var enemies = CurrentMap.Entities
                .Where(e => e.IsEnemy && e.HP > 0)
                .Select(e => new
                {
                    Entity = e,
                    Dist = (float)Math.Sqrt(Math.Pow(e.X - Player.X, 2) + Math.Pow(e.Y - Player.Y, 2)),
                    Angle = (float)Math.Atan2(e.Y - Player.Y, e.X - Player.X)
                })
                .Where(e => e.Dist < 15f)
                .ToList();

            if (enemies.Count == 0) return;

            var enemiesInSight = enemies
                .Select(e => new
                {
                    e.Entity,
                    e.Dist,
                    e.Angle,
                    AngleDiff = Math.Abs(NormalizeAngle(e.Angle - Player.Angle))
                })
                .Where(e => e.AngleDiff < 0.25f)
                .ToList();

            if (enemiesInSight.Count == 0) return;

            var sortedByDistance = enemiesInSight
                .OrderBy(e => e.Dist)
                .ToList();

            foreach (var target in sortedByDistance)
            {
                float wallDist = GetDistToWall(target.Angle);

                if (target.Dist < wallDist - 0.2f)
                {
                    if (IsDirectLineOfSight(target.Entity))
                    {
                        ShotsHit++;
                        target.Entity.TakeDamage(1);
                        CreateBloodSplatter(target.Entity);
                        HitMarkerTimer = 5;

                        if (target.Entity.HP <= 0)
                        {
                            // ===== РАЗНЫЕ ОЧКИ ЗА РАЗНЫХ ВРАГОВ =====
                            int score = 0;

                            switch (target.Entity.Type)
                            {
                                case EntityType.Enemy:
                                    score = 150;      // Обычный враг
                                    break;
                                case EntityType.FastEnemy:
                                    score = 150;      // Быстрый враг (можно изменить)
                                    break;
                                case EntityType.TankEnemy:
                                    score = 500;      // Враг-танк
                                    break;
                                case EntityType.Boss:
                                    score = 1000;     // БОСС
                                    break;
                                default:
                                    score = 100;      // На всякий случай
                                    break;
                            }

                            Player.Score += score;
                            KillsCount++;

                            for (int i = 0; i < 30; i++)
                            {
                                double pAngle = _random.NextDouble() * Math.PI * 2;
                                float speed = (float)(_random.NextDouble() * 0.2f + 0.1f);
                                float vx = (float)Math.Cos(pAngle) * speed;
                                float vy = (float)Math.Sin(pAngle) * speed;
                                float vz = (float)(_random.NextDouble() * 0.2f + 0.1f);
                                Particles.Add(new Particle(
                                    target.Entity.X, target.Entity.Y, 0.5f,
                                    vx, vy, vz,
                                    _random.Next(15, 40)
                                ));
                            }

                            CurrentMap.Entities.Remove(target.Entity);
                        }

                        break;
                    }
                }
            }
        }
        private float NormalizeAngle(float angle)
        {
            while (angle < -Math.PI) angle += (float)Math.PI * 2;
            while (angle > Math.PI) angle -= (float)Math.PI * 2;
            return angle;
        }

        private bool IsDirectLineOfSight(GameEntity target)
        {
            float dx = target.X - Player.X;
            float dy = target.Y - Player.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            int steps = (int)(dist * 10);
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                float checkX = Player.X + dx * t;
                float checkY = Player.Y + dy * t;

                int gridX = (int)checkX;
                int gridY = (int)checkY;

                if (CurrentMap.IsWall(gridX, gridY))
                {
                    return false;
                }
            }

            return true;
        }
        private float GetDistToWall(float angle)
        {
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);

            for (float dist = 0.1f; dist < 15f; dist += 0.05f)
            {
                int testX = (int)(Player.X + cos * dist);
                int testY = (int)(Player.Y + sin * dist);

                if (testX < 0 || testX >= CurrentMap.Width ||
                    testY < 0 || testY >= CurrentMap.Height)
                {
                    return dist;
                }

                if (CurrentMap.Grid[testY, testX] != 0)
                {
                    return dist;
                }
            }

            return 15f;
        }
        private void CreateBloodSplatter(GameEntity e)
        {
            int particleCount = 15;
            for (int i = 0; i < particleCount; i++)
            {
                double pAngle = _random.NextDouble() * Math.PI * 2;
                float speed = (float)(_random.NextDouble() * 0.15f + 0.05f);
                float vx = (float)Math.Cos(pAngle) * speed;
                float vy = (float)Math.Sin(pAngle) * speed;
                float vz = (float)(_random.NextDouble() * 0.15f + 0.05f);
                Particles.Add(new Particle(
                    e.X, e.Y, 0.5f,
                    vx, vy, vz,
                    _random.Next(10, 30)
                ));
            }
        }

        //УПРАВЛЕНИЕ ИГРОЙ
        public void StartNewGame()
        {
            Player.Reset();
            GameStarted = true;
            IsGameOver = false;
            IsPaused = false;
            CurrentLevel = 1;
            KillsCount = 0;

            ShotsFired = 0;
            ShotsHit = 0;
            DamageTaken = 0;
            NoDamageBonus = true;

            InitializeLevel();
            LevelStartTime = Environment.TickCount;
            CurrentScreen = GameScreen.Playing;
        }
        public void RestartLevel()
        {
            IsGameOver = false;

            Player.Score = Math.Max(0, Player.Score - 1000);

            Player.Health = 100;
            Player.Ammo = 20;
            Player.X = 2;
            Player.Y = 2;
            Player.Angle = 0;

            InitializeLevel();
            CurrentScreen = GameScreen.Playing;
            _audio.StopStep();
        }

        public void NextLevel()
        {
            CalculateLevelBonus();

            CurrentLevel++;
            Player.Health = 100;
            Player.Ammo = 20;
            Player.X = 2;
            Player.Y = 2;
            Player.Angle = 0;

            ShotsFired = 0;
            ShotsHit = 0;
            DamageTaken = 0;
            NoDamageBonus = true;

            InitializeLevel();
            LevelStartTime = Environment.TickCount;
            CurrentScreen = GameScreen.Playing;
        }

        public void ReturnToMainMenu()
        {
            GameStarted = false;
            IsPaused = false;
            CurrentScreen = GameScreen.MainMenu;
        }

        private void CalculateLevelBonus()
        {
            int timeTaken = (Environment.TickCount - LevelStartTime) / 1000;
            int timeBonus = Math.Max(0, 500 - timeTaken * 10);

            int accuracyBonus = 0;
            if (ShotsFired > 0)
            {
                float accuracy = (float)ShotsHit / ShotsFired * 100f;
                accuracyBonus = (int)(accuracy * 5);
            }

            int damagePenalty = DamageTaken * 5;
            int noDamageBonus = NoDamageBonus ? 1000 : 0;

            Player.Score += timeBonus + accuracyBonus + noDamageBonus - damagePenalty;
            if (Player.Score < 0) Player.Score = 0;
        }

        //МУЗЫКА 
        private void ManageMusic()
        {
            if (CurrentScreen == GameScreen.MainMenu || CurrentScreen == GameScreen.Agreement ||
                CurrentScreen == GameScreen.DifficultySelect || CurrentScreen == GameScreen.Help)
            {
                _audio.PlayMenuMusic();
            }
            else if (CurrentScreen == GameScreen.Playing || CurrentScreen == GameScreen.Pause)
            {
                _audio.PlayGameMusic();
            }
        }

        //ОТРИСОВКА
        public void Render(Graphics g, int width, int height)
        {
            int uiHeight = 80;

            switch (CurrentScreen)
            {
                case GameScreen.Settings:
                case GameScreen.Agreement:
                case GameScreen.LevelComplete:
                    break;

                case GameScreen.MainMenu:
                    DrawMainMenu(g);
                    break;

                case GameScreen.Help:
                    DrawHelpScreen(g);
                    break;

                case GameScreen.DifficultySelect:
                    DrawDifficultyMenu(g, width, height);
                    break;

                case GameScreen.Playing:
                    _renderer.RenderWorld(g, Player, CurrentMap, Particles, IsShooting,
                        DamageFlash, WalkCycle, BobAmount, width, height, uiHeight);
                    DrawUI(g, width, height, uiHeight);
                    _renderer.DrawRadar(g, Player, CurrentMap);

                    if (IsGameOver)
                        DrawGameOverScreen(g, width, height);
                    break;

                case GameScreen.Victory:
                    DrawVictoryScreen(g);
                    break;

                case GameScreen.Pause:
                    _renderer.RenderWorld(g, Player, CurrentMap, Particles, IsShooting,
                        DamageFlash, WalkCycle, BobAmount, width, height, uiHeight);
                    DrawUI(g, width, height, uiHeight);
                    _renderer.DrawRadar(g, Player, CurrentMap);
                    DrawPauseMenu(g);
                    break;
            }
        }
        private void DrawMainMenu(Graphics g)
        {
            if (_resources.BgMenu != null)
            {
                g.DrawImage(_resources.BgMenu, 0, 0, 640, 480);
            }
            else
            {
                g.Clear(Color.Black);
            }

            using (Font titleFont = new Font("Impact", 35))
            {
                g.DrawString("ГЛАВНОЕ МЕНЮ", titleFont, Brushes.White, 170, 40);
            }

            bool hasSave = _saveManager.HasSavedGame();
            Color continueBtnColor = hasSave ? Color.DarkBlue : Color.FromArgb(60, 60, 60);

            DrawButton(g, "ПРОДОЛЖИТЬ", 220, 130, 200, 45, continueBtnColor);
            DrawButton(g, "НОВАЯ ИГРА", 220, 190, 200, 45, Color.DarkBlue);
            DrawButton(g, "СЛОЖНОСТЬ", 220, 250, 200, 45, Color.DarkBlue);
            DrawButton(g, "НАСТРОЙКИ", 220, 310, 200, 45, Color.DarkBlue);
            DrawButton(g, "ПОМОЩЬ", 220, 370, 200, 45, Color.DarkBlue);
            DrawButton(g, "ВЫХОД", 220, 430, 200, 45, Color.Maroon);

            if (hasSave)
            {
                string saveInfo = _saveManager.GetSaveInfo();
                using (Font infoFont = new Font("Arial", 8))
                {
                    g.DrawString(saveInfo, infoFont, Brushes.Gray, 220, 480);
                }
            }
        }
        private void DrawDifficultyMenu(Graphics g, int width, int height)
        {
            g.Clear(Color.Black);
            using (Font titleFont = new Font("Arial", 24, FontStyle.Bold))
            {
                g.DrawString("ВЫБОР СЛОЖНОСТИ", titleFont, Brushes.White, 160, 50);
            }

            DrawButton(g, "ПРИНИМАЮ ВСЕ КУКИ", 220, 120, 200, 45,
                SelectedDifficulty == GameDifficulty.Easy ? Color.Green : Color.DarkBlue);
            DrawButton(g, "ПРИНИМАЮ ЧАСТИЧНО", 220, 180, 200, 45,
                SelectedDifficulty == GameDifficulty.Normal ? Color.Green : Color.DarkBlue);
            DrawButton(g, "НЕ ПРИНИМАЮ", 220, 240, 200, 45,
                SelectedDifficulty == GameDifficulty.Hard ? Color.Green : Color.DarkBlue);
            DrawButton(g, "НАЗАД", 220, 320, 200, 45, Color.Gray);
        }
        private void DrawGameOverScreen(Graphics g, int width, int height)
        {
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(180, 100, 0, 0)))
            {
                g.FillRectangle(shadowBrush, 0, 0, width, height);
            }

            if (_resources.DeathImage != null)
                g.DrawImage(_resources.DeathImage, (width - 200) / 2, 20, 200, 150);

            using (Font gameOverFont = new Font("Impact", 45))
            {
                g.DrawString("ИГРА ОКОНЧЕНА", gameOverFont, Brushes.White, 140, 170);
            }

            BtnRestartPos = new Rectangle(220, 260, 200, 45);
            BtnExitPos = new Rectangle(220, 320, 200, 45);

            DrawButton(g, "ИГРАТЬ СНОВА", BtnRestartPos.X, BtnRestartPos.Y,
                BtnRestartPos.Width, BtnRestartPos.Height, Color.DarkGreen);
            DrawButton(g, "В МЕНЮ", BtnExitPos.X, BtnExitPos.Y,
                BtnExitPos.Width, BtnExitPos.Height, Color.Maroon);
        }
        private void DrawVictoryScreen(Graphics g)
        {
            if (_resources.VictoryImg != null)
            {
                g.DrawImage(_resources.VictoryImg, 0, 0, 640, 480);
            }
            else
            {
                g.Clear(Color.MidnightBlue);
            }

            using (Font titleFont = new Font("Impact", 40))
            {
                g.DrawString("ПОБЕДА!", titleFont, Brushes.Gold, 210, 40);
            }

            using (Font scoreFont = new Font("Arial", 20, FontStyle.Bold))
            using (Font valueFont = new Font("Impact", 48))
            {
                g.DrawString("ИТОГОВЫЙ СЧЕТ:", scoreFont, Brushes.White, 200, 120);

                string scoreText = $"{Player.Score}";
                SizeF scoreSize = g.MeasureString(scoreText, valueFont);
                g.DrawString(scoreText, valueFont, Brushes.Gold,
                    (640 - scoreSize.Width) / 2, 140);
            }

            DrawButton(g, "В МЕНЮ", 220, 320, 200, 45, Color.DarkBlue);
        }
        private void DrawPauseMenu(Graphics g)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), 0, 0, 640, 480);

            using (Font titleFont = new Font("Arial", 36, FontStyle.Bold))
            {
                g.DrawString("ПАУЗА", titleFont, Brushes.White, 240, 50);
            }

            DrawButton(g, "ВЕРНУТЬСЯ", 220, 120, 200, 35, Color.DarkBlue);
            DrawButton(g, "НАСТРОЙКИ", 220, 165, 200, 35, Color.DarkBlue);
            DrawButton(g, "ПОМОЩЬ", 220, 210, 200, 35, Color.DarkBlue);
            DrawButton(g, "В МЕНЮ", 220, 255, 200, 35, Color.Maroon);
        }
        private void DrawHelpScreen(Graphics g)
        {
            g.Clear(Color.FromArgb(20, 20, 40));

            using (Font titleFont = new Font("Impact", 36))
            using (Font headerFont = new Font("Arial", 14, FontStyle.Bold))
            using (Font textFont = new Font("Arial", 10))
            using (Font smallFont = new Font("Arial", 9))
            {
                g.DrawString("ПОМОЩЬ", titleFont, Brushes.Gold, 240, 20);

                int leftX = 40;     
                int rightX = 340;    
                int yPos = 80;        

                g.DrawString("УПРАВЛЕНИЕ:", headerFont, Brushes.Cyan, leftX, yPos); yPos += 22;
                g.DrawString("• W, A, S, D - движение", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("• ЛКМ - стрельба", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("• ESC - пауза", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("• Мышь - поворот камеры", textFont, Brushes.White, leftX + 15, yPos); yPos += 22;

                g.DrawString("ИГРОВЫЕ ОБЪЕКТЫ:", headerFont, Brushes.Cyan, leftX, yPos); yPos += 22;
                g.DrawString("• Белая точка - игрок", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("• Красные точки - враги", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("• Синяя точка - босс", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("• Зеленый крест - аптечка", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("  (+25 HP)", textFont, Brushes.LightGreen, leftX + 30, yPos); yPos += 18;
                g.DrawString("• Желтый патрон - патроны", textFont, Brushes.White, leftX + 15, yPos); yPos += 18;
                g.DrawString("  (+20)", textFont, Brushes.Yellow, leftX + 30, yPos); yPos += 22;

                yPos = 80;

                g.DrawString("НАЧИСЛЕНИЕ ОЧКОВ:", headerFont, Brushes.Cyan, rightX, yPos); yPos += 22;
                g.DrawString("• Обычный враг: +150", textFont, Brushes.LightGreen, rightX + 15, yPos); yPos += 18;
                g.DrawString("• Быстрый враг: +150", textFont, Brushes.LightGreen, rightX + 15, yPos); yPos += 18;
                g.DrawString("• Враг-танк: +500", textFont, Brushes.LightGreen, rightX + 15, yPos); yPos += 18;
                g.DrawString("• БОСС: +1000", textFont, Brushes.Gold, rightX + 15, yPos); yPos += 22;

                g.DrawString("ШТРАФЫ:", headerFont, Brushes.Orange, rightX, yPos); yPos += 22;
                g.DrawString("• Смерть: -1000 очков", textFont, Brushes.OrangeRed, rightX + 15, yPos); yPos += 18;
                g.DrawString("• Получение урона:", textFont, Brushes.OrangeRed, rightX + 15, yPos); yPos += 18;
                g.DrawString("  отключает бонус", textFont, Brushes.OrangeRed, rightX + 30, yPos); yPos += 18;
                g.DrawString("  'Без урона'", textFont, Brushes.OrangeRed, rightX + 30, yPos); yPos += 22;

                g.DrawString("СОВЕТЫ:", headerFont, Brushes.Cyan, rightX, yPos); yPos += 22;
                g.DrawString("• Следите за радаром", smallFont, Brushes.White, rightX + 15, yPos); yPos += 16;
                g.DrawString("• Полоска здоровья врагов", smallFont, Brushes.White, rightX + 15, yPos); yPos += 16;
                g.DrawString("• Ищите аптечки и патроны", smallFont, Brushes.White, rightX + 15, yPos); yPos += 16;
                g.DrawString("• Уклоняйтесь от урона", smallFont, Brushes.White, rightX + 15, yPos); yPos += 16;

                g.DrawString("• Если враг застрял в стене:", smallFont, Brushes.Yellow, rightX + 15, yPos); yPos += 16;
                g.DrawString("  выйдите в меню и заново", smallFont, Brushes.Yellow, rightX + 30, yPos); yPos += 16;
                g.DrawString("  запустите уровень через", smallFont, Brushes.Yellow, rightX + 30, yPos); yPos += 16;
                g.DrawString("  кнопку 'ПРОДОЛЖИТЬ'", smallFont, Brushes.Yellow, rightX + 30, yPos);

            }

            DrawButton(g, "НАЗАД", 220, 430, 200, 40, Color.DarkBlue);
        }
        private void DrawUI(Graphics g, int width, int height, int uiHeight)
        {
            int uiTop = height - uiHeight;
            g.FillRectangle(Brushes.DarkSlateGray, 0, uiTop, width, uiHeight);
            g.DrawLine(Pens.Silver, 0, uiTop, width, uiTop);

            Bitmap face = Player.Health >= 70 ? _resources.FaceHealthy :
                         (Player.Health >= 30 ? _resources.FaceHurt : _resources.FaceDead);
            if (face != null)
                g.DrawImage(face, width / 2 - 35, uiTop + 5, 70, 70);

            using (Font f = new Font("Impact", 25))
            using (Font labelFont = new Font("Arial", 10, FontStyle.Bold))
            {
                int levelX = width / 2 - 150;
                g.DrawString("LEVEL", labelFont, Brushes.White, levelX, uiTop + 10);
                g.DrawString($"{CurrentLevel}/5", f, Brushes.Cyan, levelX - 5, uiTop + 25);

                int hpX = width / 2 + 60;
                g.DrawString("HEALTH", labelFont, Brushes.White, hpX, uiTop + 10);
                g.DrawString($"{Player.Health}%", f, Brushes.Red, hpX - 5, uiTop + 25);

                g.DrawString("AMMO", labelFont, Brushes.White, width - 100, uiTop + 10);
                g.DrawString($"{Player.Ammo}", f, Brushes.Yellow, width - 110, uiTop + 25);

                g.DrawString("SCORE", labelFont, Brushes.White, 40, uiTop + 10);
                g.DrawString($"{Player.Score}", f, Brushes.Orange, 30, uiTop + 25);

                if (CurrentLevel == 5 && CurrentScreen == GameScreen.Playing)
                {
                    var boss = CurrentMap.Entities.FirstOrDefault(e => e.Type == EntityType.Boss && e.HP > 0);
                    if (boss != null)
                    {
                        float barW = width * 0.4f;
                        float barH = 12;
                        float barX = (width - barW) / 2;
                        float barY = 30;

                        float hpPercent = boss.GetHealthPercent();

                        g.FillRectangle(new SolidBrush(Color.FromArgb(150, 0, 0, 0)), barX, barY, barW, barH);
                        g.FillRectangle(Brushes.DeepSkyBlue, barX, barY, barW * hpPercent, barH);
                        g.DrawRectangle(Pens.White, barX, barY, barW, barH);

                        string bossName = "БОСС ЭТИХ ПЕЧЕНЕК";
                        SizeF textSize = g.MeasureString(bossName, labelFont);
                        g.DrawString(bossName, labelFont, Brushes.DeepSkyBlue,
                            (width - textSize.Width) / 2, barY - 18);
                    }
                }
            }
        }
        private void DrawButton(Graphics g, string text, int x, int y, int w, int h, Color col)
        {
            using (SolidBrush br = new SolidBrush(col))
            {
                g.FillRectangle(br, x, y, w, h);
            }
            g.DrawRectangle(Pens.White, x, y, w, h);
            using (Font f = new Font("Arial", 10, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.White,
                    x + (w - sz.Width) / 2,
                    y + (h - sz.Height) / 2);
            }
        }
        //ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ 
        public Point ScaleMousePosition(int mouseX, int mouseY, int formWidth, int formHeight)
        {
            float scaleX = (float)640 / formWidth;
            float scaleY = (float)480 / formHeight;
            return new Point((int)(mouseX * scaleX), (int)(mouseY * scaleY));
        }
        public bool NeedsCursor()
        {
            return CurrentScreen == GameScreen.Agreement ||
                   CurrentScreen == GameScreen.MainMenu ||
                   CurrentScreen == GameScreen.DifficultySelect ||
                   CurrentScreen == GameScreen.Settings ||
                   CurrentScreen == GameScreen.Victory ||
                   CurrentScreen == GameScreen.LevelComplete ||
                   CurrentScreen == GameScreen.Help ||
                   IsPaused || IsGameOver;
        }
    }
}