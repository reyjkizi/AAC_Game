using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AAC_Game.Core;
using AAC_Game.Entities;
using AAC_Game.Managers;
using AAC_Game.UI;

namespace AAC_Game
{
    public partial class Form1 : Form
    {
        
        //ПОЛЯ И МЕНЕДЖЕРЫ

        // Менеджеры
        private ResourceManager _resources;      
        private AudioManager _audio;             
        private GameLogic _gameLogic;             
        private GameRenderer _renderer;
        private SaveManager _saveManager;

        // Управление
        private Timer _gameTimer;
        private HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        // Настройки экрана
        private bool _isFullscreen = true; 
        private string[] _resolutions = { "800x600", "1280x720", "1920x1080", "2560x1440" };
        private int _currentResIndex = 3;    
        private Bitmap _blurSnapshot; 
        
        //ВНЕШНИЕ ФУНКЦИИ
        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);
        
        //КОНСТРУКТОР
        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.DoubleBuffered = true;

            _resources = ResourceManager.Instance;
            _resources.LoadAllAssets();

            _audio = AudioManager.Instance;
            _saveManager = SaveManager.Instance;
            _gameLogic = new GameLogic();
            _renderer = new GameRenderer();

            LoadSettings();

            SetupInput();

            _gameTimer = new Timer();
            _gameTimer.Interval = 16;
            _gameTimer.Tick += GameTick;
            _gameTimer.Start();

            this.MouseWheel += Form1_MouseWheel;
        }
        
        //ИНИЦИАЛИЗАЦИЯ
        private void SetupInput()
        {
            this.KeyDown += (s, e) =>
            {
                if (!_pressedKeys.Contains(e.KeyCode))
                    _pressedKeys.Add(e.KeyCode);
            };

            this.KeyUp += (s, e) => _pressedKeys.Remove(e.KeyCode);
            this.MouseDown += OnMouseDown;

            _gameLogic.OnLevelCompleted += CaptureBlur;
            _gameLogic.OnGamePaused += CaptureBlur;
        }

        private void LoadSettings()
        {
            float sens = _gameLogic.MouseSensitivity;
            double musicVol = _audio.MusicVolume;
            double sfxVol = _audio.SfxVolume;
            bool fullscreen = _isFullscreen;
            int resIndex = _currentResIndex;

            if (_saveManager.LoadSettings(ref sens, ref musicVol, ref sfxVol, ref fullscreen, ref resIndex))
            {
                _gameLogic.MouseSensitivity = sens;
                _audio.MusicVolume = musicVol;
                _audio.SfxVolume = sfxVol;
                _isFullscreen = fullscreen;
                _currentResIndex = resIndex;

                ApplyScreenMode();
            }
        }

        private void ApplyScreenMode()
        {
            if (_isFullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                string[] res = _resolutions[_currentResIndex].Split('x');
                this.ClientSize = new Size(int.Parse(res[0]), int.Parse(res[1]));
            }
        }
        
        //ИГРОВОЙ ЦИКЛ
        private void GameTick(object sender, EventArgs e)
        {
            Point mousePos = this.PointToClient(Cursor.Position);
            _gameLogic.Update(_pressedKeys, mousePos, this.Width, this.Height);

            ManageCursor();

            if (_gameLogic.CurrentScreen == GameScreen.Playing &&
                !_gameLogic.IsPaused && !_gameLogic.IsGameOver)
            {
                Cursor.Position = this.PointToScreen(new Point(this.Width / 2, this.Height / 2));
            }

            this.Invalidate();
        }

        private void ManageCursor()
        {
            if (_gameLogic.NeedsCursor())
            {
                if (this.Cursor != Cursors.Arrow)
                    this.Cursor = Cursors.Arrow;
                while (ShowCursor(true) < 0) ;
            }
            else
            {
                if (this.Cursor != null)
                    this.Cursor = null;
                while (ShowCursor(false) >= 0) ;
            }
        }
        
        //ЭФФЕКТЫ
        private void CaptureBlur()
        {
            try
            {
                if (_blurSnapshot != null)
                {
                    _blurSnapshot.Dispose();
                    _blurSnapshot = null;
                }

                _blurSnapshot = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                this.DrawToBitmap(_blurSnapshot, new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height));
                _blurSnapshot = new Bitmap(_blurSnapshot, new Size(64, 48));
            }
            catch
            {
                _blurSnapshot = null;
            }
        }
        
        //ОБРАБОТЧИКИ ВВОДА
        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_gameLogic.CurrentScreen == GameScreen.Agreement)
            {
                _gameLogic.AgreementScrollY -= (e.Delta / 120) * 20;
                _gameLogic.AgreementScrollY = Math.Max(0, Math.Min(
                    _gameLogic.AgreementScrollY,
                    _gameLogic.AgreementTotalHeight - GameLogic.AgreementViewHeight));
                this.Invalidate();
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            Point mousePos = _gameLogic.ScaleMousePosition(e.X, e.Y, this.Width, this.Height);

            switch (_gameLogic.CurrentScreen)
            {
                case GameScreen.Settings:
                    HandleSettingsClick(mousePos);
                    break;
                case GameScreen.Agreement:
                    HandleAgreementClick(mousePos);
                    break;
                case GameScreen.Help:
                    HandleHelpClick(mousePos);
                    break;
                case GameScreen.MainMenu:
                    HandleMainMenuClick(mousePos);
                    break;
                case GameScreen.DifficultySelect:
                    HandleDifficultyClick(mousePos);
                    break;
                case GameScreen.Pause:
                    HandlePauseClick(mousePos);
                    break;
                case GameScreen.Playing:
                    HandlePlayingClick(e, mousePos);
                    break;
                case GameScreen.Victory:
                    HandleVictoryClick(mousePos);
                    break;
                case GameScreen.LevelComplete:
                    HandleLevelCompleteClick(mousePos);
                    break;
            }
        }

        private void HandlePlayingClick(MouseEventArgs e, Point mousePos)
        {
            if (_gameLogic.IsGameOver)
                HandleGameOverClick(mousePos);
            else if (e.Button == MouseButtons.Left && !_gameLogic.IsPaused)
                _gameLogic.Shoot();
        }
        
        //ОБРАБОТЧИКИ КЛИКОВ ПО ЭКРАНАМ
        private void HandleHelpClick(Point mousePos)
        {
            _audio.PlayClick();

            if (new Rectangle(220, 430, 200, 45).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = _gameLogic.IsPaused ? GameScreen.Pause : GameScreen.MainMenu;
            }
        }

        private void HandleSettingsClick(Point mousePos)
        {
            _audio.PlayClick();
            if (new Rectangle(350, 95, 40, 30).Contains(mousePos))
                _gameLogic.MouseSensitivity = Math.Max(0.0005f, _gameLogic.MouseSensitivity - 0.0005f);
            if (new Rectangle(400, 95, 40, 30).Contains(mousePos))
                _gameLogic.MouseSensitivity += 0.0005f;

            if (new Rectangle(350, 135, 40, 30).Contains(mousePos))
                _audio.MusicVolume = Math.Max(0, _audio.MusicVolume - 0.1);
            if (new Rectangle(400, 135, 40, 30).Contains(mousePos))
                _audio.MusicVolume = Math.Min(1, _audio.MusicVolume + 0.1);

            if (new Rectangle(350, 175, 40, 30).Contains(mousePos))
                _audio.SfxVolume = Math.Max(0, _audio.SfxVolume - 0.1);
            if (new Rectangle(400, 175, 40, 30).Contains(mousePos))
                _audio.SfxVolume = Math.Min(1, _audio.SfxVolume + 0.1);

            if (new Rectangle(150, 220, 190, 35).Contains(mousePos))
            {
                _isFullscreen = !_isFullscreen;
                ApplyScreenMode();
            }

            if (new Rectangle(350, 220, 120, 35).Contains(mousePos))
            {
                _currentResIndex = (_currentResIndex + 1) % _resolutions.Length;
                if (!_isFullscreen)
                {
                    string[] res = _resolutions[_currentResIndex].Split('x');
                    this.ClientSize = new Size(int.Parse(res[0]), int.Parse(res[1]));
                }
            }

            // Кнопка СОХРАНИТЬ И ВЫЙТИ
            if (new Rectangle(220, 400, 200, 45).Contains(mousePos))
            {
                _saveManager.SaveSettings(
                    _gameLogic.MouseSensitivity,
                    _audio.MusicVolume,
                    _audio.SfxVolume,
                    _isFullscreen,
                    _currentResIndex
                );

                _gameLogic.CurrentScreen = _gameLogic.IsPaused ? GameScreen.Pause : GameScreen.MainMenu;
            }
        }

        private void HandleAgreementClick(Point mousePos)
        {
            if (new Rectangle(170, 380, 300, 50).Contains(mousePos) && _gameLogic.IsAgreementScrolledToEnd)
            {
                _gameLogic.CurrentScreen = GameScreen.MainMenu;
            }
        }

        private void HandleMainMenuClick(Point mousePos)
        {
            _audio.PlayClick();

            if (new Rectangle(220, 130, 200, 45).Contains(mousePos))
            {
                if (_saveManager.HasSavedGame())
                {
                    if (_gameLogic.LoadGame())
                    {
                        _gameLogic.CurrentScreen = GameScreen.Playing;
                        _gameLogic.IsPaused = false;
                    }
                }
                else
                {
                    MessageBox.Show("Нет сохраненной игры!");
                }
            }
            else if (new Rectangle(220, 190, 200, 45).Contains(mousePos))
            {
                _gameLogic.StartNewGame();
            }
            else if (new Rectangle(220, 250, 200, 45).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = GameScreen.DifficultySelect;
            }
            else if (new Rectangle(220, 310, 200, 45).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = GameScreen.Settings;
            }
            else if (new Rectangle(220, 370, 200, 45).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = GameScreen.Help;
            }
            else if (new Rectangle(220, 430, 200, 45).Contains(mousePos))
            {
                Application.Exit();
            }
        }

        private void HandleDifficultyClick(Point mousePos)
        {
            if (new Rectangle(220, 120, 200, 45).Contains(mousePos))
                _gameLogic.SelectedDifficulty = GameDifficulty.Easy;
            else if (new Rectangle(220, 180, 200, 45).Contains(mousePos))
                _gameLogic.SelectedDifficulty = GameDifficulty.Normal;
            else if (new Rectangle(220, 240, 200, 45).Contains(mousePos))
                _gameLogic.SelectedDifficulty = GameDifficulty.Hard;
            else if (new Rectangle(220, 320, 200, 45).Contains(mousePos))
                _gameLogic.CurrentScreen = GameScreen.MainMenu;
        }

        private void HandlePauseClick(Point mousePos)
        {
            _audio.PlayClick();

            if (new Rectangle(220, 120, 200, 35).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = GameScreen.Playing;
                _gameLogic.IsPaused = false;
            }
            else if (new Rectangle(220, 165, 200, 35).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = GameScreen.Settings;
            }
            else if (new Rectangle(220, 210, 200, 35).Contains(mousePos))
            {
                _gameLogic.CurrentScreen = GameScreen.Help;
            }
            else if (new Rectangle(220, 255, 200, 35).Contains(mousePos))
            {
                _gameLogic.SaveGame();
                _audio.StopStep();
                _gameLogic.ReturnToMainMenu();
            }
        }

        private void HandleGameOverClick(Point mousePos)
        {
            _audio.PlayClick();

            if (_gameLogic.BtnRestartPos.Contains(mousePos))
            {
                _gameLogic.RestartLevel();
            }
            else if (_gameLogic.BtnExitPos.Contains(mousePos))
            {
                _gameLogic.ReturnToMainMenu();
            }
        }

        private void HandleVictoryClick(Point mousePos)
        {
            if (new Rectangle(220, 320, 200, 45).Contains(mousePos))
            {
                _blurSnapshot?.Dispose();
                _blurSnapshot = null;
                _gameLogic.GameStarted = false;
                _gameLogic.CurrentScreen = GameScreen.MainMenu;
            }
        }

        private void HandleLevelCompleteClick(Point mousePos)
        {
            if (new Rectangle(220, 320, 200, 45).Contains(mousePos))
            {
                _blurSnapshot?.Dispose();
                _blurSnapshot = null;
                _gameLogic.NextLevel();
            }
        }
        
        //ОТРИСОВКА
        protected override void OnPaint(PaintEventArgs e)
        {
            int bufferW = 640;
            int bufferH = 480;

            using (Bitmap offscreen = new Bitmap(bufferW, bufferH))
            using (Graphics g = Graphics.FromImage(offscreen))
            {
                g.Clear(Color.Black);

                switch (_gameLogic.CurrentScreen)
                {
                    case GameScreen.Settings:
                        DrawSettingsMenu(g, bufferW, bufferH);
                        break;
                    case GameScreen.Agreement:
                        DrawAgreement(g, bufferW, bufferH);
                        break;
                    case GameScreen.LevelComplete:
                        DrawLevelComplete(g);
                        break;
                    default:
                        _gameLogic.Render(g, bufferW, bufferH);
                        break;
                }

                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.DrawImage(offscreen, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
        }
        
        //ОТРИСОВКА МЕНЮ
        private void DrawSettingsMenu(Graphics g, int w, int h)
        {
            g.Clear(Color.FromArgb(30, 30, 30));
            using (Font titleF = new Font("Impact", 28))
            using (Font textF = new Font("Arial", 11, FontStyle.Bold))
            {
                g.DrawString("НАСТРОЙКИ", titleF, Brushes.White, 210, 30);

                int yPos = 100;
                DrawSettingRow(g, textF, "СЕНСА:", $"{(_gameLogic.MouseSensitivity * 1000):F1}", ref yPos, 95);
                DrawSettingRow(g, textF, "МУЗЫКА:", $"{(int)(_audio.MusicVolume * 100)}%", ref yPos, 135);
                DrawSettingRow(g, textF, "ЗВУКИ:", $"{(int)(_audio.SfxVolume * 100)}%", ref yPos, 175);

                string modeText = _isFullscreen ? "ПОЛНЫЙ ЭКРАН" : "ОКОННЫЙ РЕЖИМ";
                DrawButton(g, modeText, 150, 220, 190, 35, Color.DarkBlue);
                DrawButton(g, _resolutions[_currentResIndex], 350, 220, 120, 35, Color.DarkBlue);

                DrawButton(g, "СОХРАНИТЬ И ВЫЙТИ", 220, 400, 200, 45, Color.DarkBlue);
            }
        }

        private void DrawSettingRow(Graphics g, Font font, string label, string value, ref int y, int btnY)
        {
            g.DrawString(label + " " + value, font, Brushes.LightGray, 150, y);
            DrawButton(g, "-", 350, btnY, 40, 30, Color.DarkBlue);
            DrawButton(g, "+", 400, btnY, 40, 30, Color.DarkBlue);
            y += 40;
        }

        private void DrawAgreement(Graphics g, int w, int h)
        {
            g.Clear(Color.FromArgb(20, 20, 20));

            using (Font titleF = new Font("Impact", 18))
            using (Font textF = new Font("Arial", 10))
            {
                g.DrawString("ПОЛЬЗОВАТЕЛЬСКОЕ СОГЛАШЕНИЕ", titleF, Brushes.White, 130, 20);

                Rectangle agreementRect = new Rectangle(50, 70, w - 100, GameLogic.AgreementViewHeight);
                g.DrawRectangle(Pens.Gray, agreementRect);

                string fullText = GetAgreementText();

                SizeF size = g.MeasureString(fullText, textF, agreementRect.Width);
                _gameLogic.AgreementTotalHeight = (int)size.Height;

                var oldClip = g.Clip;
                g.SetClip(agreementRect);
                g.DrawString(fullText, textF, Brushes.LightGray,
                    new RectangleF(50, 70 - _gameLogic.AgreementScrollY,
                    agreementRect.Width, _gameLogic.AgreementTotalHeight));
                g.Clip = oldClip;

                DrawScrollbar(g, agreementRect);

                if (_gameLogic.AgreementScrollY >= _gameLogic.AgreementTotalHeight - agreementRect.Height - 5)
                    _gameLogic.IsAgreementScrolledToEnd = true;
            }

            Color btnColor = _gameLogic.IsAgreementScrolledToEnd ? Color.DarkBlue : Color.FromArgb(60, 60, 60);
            DrawButton(g, "ПРИНЯТЬ", 170, 380, 300, 50, btnColor);

            if (!_gameLogic.IsAgreementScrolledToEnd)
                g.DrawString("Прокрутите текст вниз, чтобы принять",
                    new Font("Arial", 8), Brushes.White, 215, 435);
        }

        private string GetAgreementText()
        {
            return "Настоящее Соглашение заключено между вами и Администрацией...\n\n" +
                "1. ОБЩИЕ ПОЛОЖЕНИЯ:\n" +
                "1.1. Перед использованием данной программы вы обязаны прочитать этот текст до конца.\n" +
                "1.2. Используя программу, вы подтверждаете, что готовы к игре.\n\n" +
                "2. ПРАВИЛА ИГРЫ:\n" +
                "2.1. Первое правило игры. Вы никому не рассказываете об Игре. Это значит, что вы не имеете права  упоминать название этой игры в Твиттере, Инстаграме , Телеграмме , за ужином с родителями или на свидании.\n" +
                "2.2. Второе правило игры. САМОЕ ГЛАВНОЕ ПРАВИЛО: вы НИКОГДА и НИГДЕ не рассказываете об игре. Особенно под пытками. Даже если очень хочется похвастаться, что вы дошли до третьего уровня.\n" +
                "2.3. Правило \"Стоп\". Если в процессе использования Программы ваш компьютер издал синий экран смерти, завис или крикнул «Стоп, я устал» — игра немедленно прекращается.\n" +
                "2.4. Правило дуэли. В игре участвуют только двое: вы и ваше эго. Читы и приглашение старшего брата «показать, как надо» категорически запрещены.\n\n" +
                "3. ТЕХНИЧЕСКИЕ ТРЕБОВАНИЯ И ЭКИПИРОВКА:\n" +
                "3.1. Запрещено использовать баги, эксплойты и клавиатуру как метательное оружие в монитор.\n\n" +
                "4. ПРИНЯТИЕ РИСКОВ:\n" +
                "4.1. Первое — ээээээ. Кхм.  Забыл, что хотели написать в первом пункте, но это не освобождает вас от ответственности.\n" +
                "4.2. Второе — вы берёте на себя все риски.\n" +
                "4.2. Третье — вы берёте на себя все риски. \n\n" +
                "5. ТЕРРИТОРИАЛЬНЫЕ ОГРАНИЧЕНИЯ:\n" +
                "5.1. Соглашение распространяется за пределы игры. Если вы закрыли игру или глаза, вы все еще обязаны соблюдать правила.\n\n" +
                "6. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ:\n" +
                "6.1. Администрация оставляет за собой право в любой момент переписать это соглашение, забыть обновить его на сайте и сделать вид, что так и было.\n" +
                "6.2. Если вы не согласны с условиями, вам следует немедленно удалить игру и начать вязать крючком. Но учтите: в вязании тоже есть свои правила, и там правила еще жестче\n" +
                "6.3. Настоящее соглашение не является договором публичной оферты, а является просто криком души.\n\n" +
                "С Уважением, Администрация.";
        }

        private void DrawLevelComplete(Graphics g)
        {
            if (_blurSnapshot != null)
            {
                g.DrawImage(_blurSnapshot, 0, 0, 640, 480);
            }

            g.FillRectangle(new SolidBrush(Color.FromArgb(150, 0, 0, 0)), 0, 0, 640, 480);

            if (_resources.LevelWinImg != null)
            {
                g.DrawImage(_resources.LevelWinImg, 170, 100, 300, 200);
            }

            DrawButton(g, "ПРИНЯТЬ", 220, 320, 200, 45, Color.DarkBlue);
        }

        private void DrawScrollbar(Graphics g, Rectangle rect)
        {
            float scrollRatio = (float)GameLogic.AgreementViewHeight / _gameLogic.AgreementTotalHeight;
            int thumbHeight = (int)(GameLogic.AgreementViewHeight * scrollRatio);
            int thumbY = rect.Y + (int)((_gameLogic.AgreementScrollY / _gameLogic.AgreementTotalHeight) * GameLogic.AgreementViewHeight);

            g.FillRectangle(Brushes.DimGray, rect.Right + 5, rect.Y, 10, rect.Height);
            g.FillRectangle(Brushes.LightGray, rect.Right + 5, thumbY, 10, thumbHeight);
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
    }
}