using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AAC_Game.Managers
{
    public class ResourceManager
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ResourceManager();
                return _instance;
            }
        }
        
        //ТЕКСТУРЫ И ИЗОБРАЖЕНИЯ
        //Стены и окружение 
        public Bitmap WallTex { get; private set; }
        public Bitmap BloodOverlay { get; private set; }
        public Bitmap AcceptorSprite { get; private set; }
        public Bitmap FlashSprite { get; private set; }

        //Враги
        public Bitmap EnemySprite { get; private set; }
        public Bitmap EnemyFastSprite { get; private set; }
        public Bitmap EnemyTankSprite { get; private set; }
        public Bitmap BossSprite { get; private set; }

        //Предметы
        public Bitmap HealthIcon { get; private set; }
        public Bitmap AmmoIcon { get; private set; }

        //Интерфейс игрока
        public Bitmap FaceHealthy { get; private set; }
        public Bitmap FaceHurt { get; private set; }
        public Bitmap FaceDead { get; private set; }
        public Bitmap DeathImage { get; private set; }

        //Меню
        public Bitmap BgMenu { get; private set; }
        public Bitmap LevelWinImg { get; private set; }
        public Bitmap VictoryImg { get; private set; }
        
        //КОНСТРУКТОР
        private ResourceManager() { }
        
        //ЗАГРУЗКА РЕСУРСОВ
        public void LoadAllAssets()
        {
            try
            {
                string basePath = Application.StartupPath;

                BossSprite = LoadImage(Path.Combine(basePath, "Assets", "Images", "boss.png"));
                BgMenu = LoadImage(Path.Combine(basePath, "Assets", "Images", "menu_bg.png"));
                LevelWinImg = LoadImage(Path.Combine(basePath, "Assets", "Images", "level_complete.png"));
                VictoryImg = LoadImage(Path.Combine(basePath, "Assets", "Images", "victory_screen.png"));
                BloodOverlay = LoadImage(Path.Combine(basePath, "Assets", "Images", "blood_overlay.png"));
                WallTex = LoadImage(Path.Combine(basePath, "Assets", "Images", "wall.png"));
                AcceptorSprite = LoadImage(Path.Combine(basePath, "Assets", "Images", "acceptorSprite.png"));
                FlashSprite = LoadImage(Path.Combine(basePath, "Assets", "Images", "flash.png"));
                EnemySprite = LoadImage(Path.Combine(basePath, "Assets", "Images", "enemy.png"));
                EnemyFastSprite = LoadImage(Path.Combine(basePath, "Assets", "Images", "enemy_fast.png"));
                EnemyTankSprite = LoadImage(Path.Combine(basePath, "Assets", "Images", "enemy_tank.png"));
                HealthIcon = LoadImage(Path.Combine(basePath, "Assets", "Images", "health.png"));
                AmmoIcon = LoadImage(Path.Combine(basePath, "Assets", "Images", "ammo.png"));
                FaceHealthy = LoadImage(Path.Combine(basePath, "Assets", "Images", "face_1.png"));
                FaceHurt = LoadImage(Path.Combine(basePath, "Assets", "Images", "face_2.png"));
                FaceDead = LoadImage(Path.Combine(basePath, "Assets", "Images", "face_3.png"));
                DeathImage = LoadImage(Path.Combine(basePath, "Assets", "Images", "death.png"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке ресурсов: " + ex.Message);
            }
        }
        
        //ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private Bitmap LoadImage(string path)
        {
            if (File.Exists(path))
                return new Bitmap(path);

            return CreatePlaceholder();
        }

        private Bitmap CreatePlaceholder()
        {
            var bmp = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Magenta);
            }
            return bmp;
        }
    }
}