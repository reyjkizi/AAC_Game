using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace AAC_Game.Managers
{
    public class AudioManager
    {

        private static AudioManager _instance;

        public static AudioManager Instance => _instance ?? (_instance = new AudioManager());
        
        //МЕДИА-ПЛЕЕРЫ

        private MediaPlayer _musicMenu = new MediaPlayer();
        private MediaPlayer _musicGame = new MediaPlayer();

        private MediaPlayer _sfxDeath = new MediaPlayer();
        private MediaPlayer _sfxShoot = new MediaPlayer();
        private MediaPlayer _sfxStep = new MediaPlayer();  
        private MediaPlayer _sfxHit = new MediaPlayer();   
        private MediaPlayer _sfxClick = new MediaPlayer();
        
        //НАСТРОЙКИ ГРОМКОСТИ 
        private double _musicVolume = 0.5;
        private double _sfxVolume = 0.7;
        public double MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Math.Max(0, Math.Min(1, value));
                ApplyVolume();
            }
        }
        public double SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Math.Max(0, Math.Min(1, value));
                ApplyVolume();
            }
        }
        
        //СОСТОЯНИЕ
        public bool IsMenuMusicPlaying { get; private set; }

        public bool IsGameMusicPlaying { get; private set; }
        
        //КОНСТРУКТОР
        private AudioManager()
        {
            InitializeAudio();
        }
        
        //ИНИЦИАЛИЗАЦИЯ 
        private void InitializeAudio()
        {
            try
            {
                string basePath = Application.StartupPath;

                string menuMusicPath = Path.Combine(basePath, "Assets", "Music", "menu_music.mp3");
                string gameMusicPath = Path.Combine(basePath, "Assets", "Music", "game_music.mp3");

                if (File.Exists(menuMusicPath))
                    _musicMenu.Open(new Uri(menuMusicPath));

                if (File.Exists(gameMusicPath))
                    _musicGame.Open(new Uri(gameMusicPath));

                LoadSound(_sfxDeath, "death.wav");
                LoadSound(_sfxShoot, "shoot.wav");
                LoadSound(_sfxStep, "step.wav");
                LoadSound(_sfxHit, "hit.wav");   
                LoadSound(_sfxClick, "click.wav");

                _musicMenu.MediaEnded += (s, e) => { _musicMenu.Position = TimeSpan.Zero; _musicMenu.Play(); };
                _musicGame.MediaEnded += (s, e) => { _musicGame.Position = TimeSpan.Zero; _musicGame.Play(); };

                ApplyVolume();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации аудио: {ex.Message}");
            }
        }

        private void LoadSound(MediaPlayer player, string filename)
        {
            string basePath = Application.StartupPath;
            string path = Path.Combine(basePath, "Assets", "Sounds", filename);

            if (File.Exists(path))
                player.Open(new Uri(path));
        }

        private void ApplyVolume()
        {
            _musicMenu.Volume = _musicVolume * 0.3;
            _musicGame.Volume = _musicVolume * 0.3;

            _sfxShoot.Volume = _sfxVolume * 1.0;
            _sfxStep.Volume = _sfxVolume * 1.2;
            _sfxHit.Volume = _sfxVolume * 1.0;
            _sfxClick.Volume = _sfxVolume * 0.8;
            _sfxDeath.Volume = _sfxVolume * 1.0;
        }
        
        //УПРАВЛЕНИЕ МУЗЫКОЙ
        public void PlayMenuMusic()
        {
            try
            {
                if (!IsMenuMusicPlaying)
                {
                    _musicGame.Stop();
                    _musicMenu.Play();
                    IsMenuMusicPlaying = true;
                    IsGameMusicPlaying = false;
                }
            }
            catch { }
        }

        public void PlayGameMusic()
        {
            try
            {
                if (!IsGameMusicPlaying)
                {
                    _musicMenu.Stop();
                    _musicGame.Play();
                    IsGameMusicPlaying = true;
                    IsMenuMusicPlaying = false;
                }
            }
            catch { }
        }

        public void StopAll()
        {
            _musicMenu.Stop();
            _musicGame.Stop();
            IsMenuMusicPlaying = false;
            IsGameMusicPlaying = false;
        }
        
        //ЗВУКОВЫЕ ЭФФЕКТЫ
        public void PlayShoot()
        {
            try { _sfxShoot.Stop(); _sfxShoot.Play(); } catch { }
        }
        public void PlayStep()
        {
            try { _sfxStep.Stop(); _sfxStep.Play(); } catch { }
        }
        public void PlayHit()
        {
            try { _sfxHit.Stop(); _sfxHit.Play(); } catch { }
        }
        public void PlayClick()
        {
            try { _sfxClick.Stop(); _sfxClick.Play(); } catch { }
        }
        public void PlayDeath()
        {
            try { _sfxDeath.Stop(); _sfxDeath.Play(); } catch { }
        }
        public void StopStep()
        {
            try { _sfxStep.Stop(); } catch { }
        }
    }
}