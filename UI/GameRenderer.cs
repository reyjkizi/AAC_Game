using AAC_Game.Core;
using AAC_Game.Entities;
using AAC_Game.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace AAC_Game.UI
{
    public class GameRenderer
    {
        //ПРИВАТНЫЕ ПОЛЯ
        private readonly ResourceManager _resources;
        private float[] _depthBuffer;
        private Random _random = new Random();
        
        //КОНСТРУКТОР
        public GameRenderer()
        {
            _resources = ResourceManager.Instance;
        }
        
        //ОСНОВНОЙ МЕТОД РЕНДЕРИНГА
        public void RenderWorld(Graphics g, Player player, Map map, List<Particle> particles,
                               bool isShooting, int damageFlash, float walkCycle, float bobAmount,
                               int width, int height, int uiHeight)
        {
            int gameHeight = height - uiHeight;

            g.FillRectangle(new SolidBrush(Color.FromArgb(50, 50, 50)), 0, gameHeight / 2, width, gameHeight / 2);

            DrawWalls(g, player, map, width, gameHeight);

            var sortedSprites = map.Entities
                .OrderByDescending(ent => Math.Pow(player.X - ent.X, 2) + Math.Pow(player.Y - ent.Y, 2))
                .ToList();

            foreach (var entity in sortedSprites)
                DrawSprite(g, player, entity, width, gameHeight);

            DrawParticles(g, player, particles, width, gameHeight);

            DrawWeapon(g, isShooting, walkCycle, bobAmount, width, gameHeight);

            if (damageFlash > 0 && _resources.BloodOverlay != null)
            {
                float alpha = Math.Min(1.0f, damageFlash / 60f);
                ColorMatrix matrix = new ColorMatrix { Matrix33 = alpha };
                using (ImageAttributes attr = new ImageAttributes())
                {
                    attr.SetColorMatrix(matrix);
                    g.DrawImage(_resources.BloodOverlay,
                        new Rectangle(0, 0, width, gameHeight),
                        0, 0, _resources.BloodOverlay.Width, _resources.BloodOverlay.Height,
                        GraphicsUnit.Pixel, attr);
                }
            }
        }
        
        //РЕНДЕРИНГ СТЕН (РЕЙКАСТИНГ)
        private void DrawWalls(Graphics g, Player player, Map map, int width, int height)
        {
            int resolution = 2;
            int numRays = width / resolution;
            _depthBuffer = new float[numRays];
            float angleStep = player.FOV / numRays;

            for (int i = 0; i < numRays; i++)
            {
                float currentAngle = (player.Angle - player.FOV / 2) + i * angleStep;
                float distance = 0.1f;
                float eyeX = (float)Math.Cos(currentAngle);
                float eyeY = (float)Math.Sin(currentAngle);
                bool hitWall = false;

                while (distance < 16f && !hitWall)
                {
                    distance += 0.02f;
                    int testX = (int)(player.X + eyeX * distance);
                    int testY = (int)(player.Y + eyeY * distance);

                    if (testX >= 0 && testX < map.Width && testY >= 0 && testY < map.Height)
                    {
                        if (map.Grid[testY, testX] != 0)
                        {
                            hitWall = true;
                            _depthBuffer[i] = distance;

                            float correctedDist = distance * (float)Math.Cos(currentAngle - player.Angle);
                            if (correctedDist < 0.1f) correctedDist = 0.1f;

                            int columnHeight = (int)(height / correctedDist);

                            if (_resources.WallTex != null)
                            {
                                float blockX = player.X + eyeX * distance;
                                float blockY = player.Y + eyeY * distance;
                                float testX_f = blockX - (int)blockX;
                                float testY_f = blockY - (int)blockY;

                                float hitX;
                                if (Math.Abs(testY_f - 0.5f) > Math.Abs(testX_f - 0.5f))
                                    hitX = testX_f;
                                else
                                    hitX = testY_f;

                                float srcX = hitX * _resources.WallTex.Width;

                                g.DrawImage(_resources.WallTex,
                                    new RectangleF(i * resolution, height / 2 - columnHeight / 2, resolution, columnHeight),
                                    new RectangleF(srcX, 0, 1.1f, _resources.WallTex.Height),
                                    GraphicsUnit.Pixel);

                                int alpha = Math.Min(220, (int)(distance * 12));
                                if (alpha > 10)
                                {
                                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                                    {
                                        g.FillRectangle(shadow,
                                            i * resolution,
                                            height / 2 - columnHeight / 2,
                                            resolution,
                                            columnHeight);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        distance = 16f;
                        hitWall = true;
                        _depthBuffer[i] = 16f;
                    }
                }

                if (!hitWall)
                    _depthBuffer[i] = 16f;
            }
        }
        
        //РЕНДЕРИНГ СПРАЙТОВ
        private void DrawSprite(Graphics g, Player player, GameEntity entity, int width, int height)
        {
            if (_depthBuffer == null || entity?.Sprite == null || entity.HP <= 0)
                return;

            float dx = entity.X - player.X;
            float dy = entity.Y - player.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < 0.2f || dist > 15f)
                return;

            float objectAngle = (float)Math.Atan2(dy, dx) - player.Angle;
            objectAngle = NormalizeAngle(objectAngle);

            float fovMultiplier = 1.2f;
            if (Math.Abs(objectAngle) > player.FOV * fovMultiplier)
                return;

            float sizeMod = 1.0f;
            bool isItem = false;

            if (entity.Type == EntityType.Boss)
                sizeMod = 2.0f;
            else if (entity.IsEnemy)
                sizeMod = 1.0f;
            else if (entity.IsItem)
            {
                sizeMod = 0.3f;
                isItem = true;
            }

            float spriteHeight = (height / dist) * sizeMod;
            float spriteWidth = spriteHeight * ((float)entity.Sprite.Width / entity.Sprite.Height);

            float screenX = (0.5f + 0.5f * objectAngle / (player.FOV / 2)) * width;
            float wallBottom = height / 2 + (height / dist) / 2;
            float screenY = wallBottom - spriteHeight;

            int xIdx = (int)(screenX / width * _depthBuffer.Length);
            xIdx = Math.Max(0, Math.Min(_depthBuffer.Length - 1, xIdx));

            bool isVisible = IsSpriteVisible(dist, xIdx, spriteWidth);

            if (isVisible)
            {
                DrawShadow(g, screenX, screenY, spriteWidth, spriteHeight, dist, isItem);
                g.DrawImage(entity.Sprite, screenX - spriteWidth / 2, screenY, spriteWidth, spriteHeight);

                if (entity.IsEnemy)
                    DrawHealthBar(g, entity, screenX, screenY, spriteWidth, height);
            }
        }

        private bool IsSpriteVisible(float dist, int xIdx, float spriteWidth)
        {
            float wallDist = _depthBuffer[xIdx];

            if (dist < wallDist - 0.3f)
                return true;

            int checkRadius = Math.Max(1, (int)(spriteWidth / 15));
            for (int offset = -checkRadius; offset <= checkRadius; offset++)
            {
                int checkIdx = xIdx + offset;
                if (checkIdx >= 0 && checkIdx < _depthBuffer.Length)
                {
                    if (dist < _depthBuffer[checkIdx] - 0.3f)
                        return true;
                }
            }
            return false;
        }

        private float NormalizeAngle(float angle)
        {
            while (angle < -Math.PI) angle += (float)Math.PI * 2;
            while (angle > Math.PI) angle -= (float)Math.PI * 2;
            return angle;
        }

        private void DrawShadow(Graphics g, float screenX, float screenY, float width, float height, float dist, bool isItem)
        {
            int shadowSize;
            int shadowY;
            int shadowHeight;

            if (isItem)
            {
                shadowSize = (int)(width * 1.2f);
                shadowY = (int)(screenY + height * 1.5f);
                shadowHeight = shadowSize / 4;
            }
            else
            {
                shadowSize = (int)(width * 0.7f);
                shadowY = (int)(screenY + height * 0.85f);
                shadowHeight = shadowSize / 3;
            }

            int shadowX = (int)(screenX - shadowSize / 2);
            int alpha = (int)(80 / (dist * 0.5f));
            alpha = Math.Min(60, Math.Max(20, alpha));

            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, shadowX, shadowY, shadowSize, shadowHeight);
            }
        }

        private void DrawHealthBar(Graphics g, GameEntity entity, float screenX, float screenY, float spriteWidth, int height)
        {
            float barWidth = spriteWidth * 0.8f;
            float barHeight = Math.Max(2, height / 150f);
            float barX = screenX - barWidth / 2;
            float barY = screenY - barHeight - 5;

            float hpPercent = entity.GetHealthPercent();

            g.FillRectangle(Brushes.Black, barX, barY, barWidth, barHeight);
            Brush hpBrush = entity.Type == EntityType.Boss ? Brushes.DeepSkyBlue : Brushes.LimeGreen;
            g.FillRectangle(hpBrush, barX, barY, barWidth * hpPercent, barHeight);
            g.DrawRectangle(Pens.White, barX, barY, barWidth, barHeight);
        }
        
        //РЕНДЕРИНГ ЧАСТИЦ
        private void DrawParticles(Graphics g, Player player, List<Particle> particles, int width, int height)
        {
            foreach (var p in particles)
            {
                float dx = p.X - player.X;
                float dy = p.Y - player.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist < 0.1f) continue;

                float objectAngle = (float)Math.Atan2(dy, dx) - player.Angle;
                objectAngle = NormalizeAngle(objectAngle);

                if (Math.Abs(objectAngle) < player.FOV)
                {
                    float screenX = (0.5f * (objectAngle / (player.FOV / 2)) + 0.5f) * width;
                    int xIdx = (int)(screenX / (width / _depthBuffer.Length));

                    if (xIdx >= 0 && xIdx < _depthBuffer.Length && _depthBuffer[xIdx] > dist)
                    {
                        float wallHeight = height / dist;
                        float wallBottom = height / 2 + wallHeight / 2;
                        float screenY = wallBottom - (wallHeight * p.Z);

                        int size = (int)(12 / dist);
                        if (size > 0 && size < 50)
                        {
                            g.FillEllipse(Brushes.Red, screenX - size / 2, screenY - size / 2, size, size);
                        }
                    }
                }
            }
        }
        
        //РЕНДЕРИНГ ОРУЖИЯ
        private void DrawWeapon(Graphics g, bool isShooting, float walkCycle, float bobAmount, int width, int height)
        {
            if (_resources.AcceptorSprite == null) return;

            int gunHeight = (int)(height * 0.85f);
            int gunWidth = (int)(gunHeight * ((float)_resources.AcceptorSprite.Width / _resources.AcceptorSprite.Height));

            float bobX = (float)Math.Sin(walkCycle) * (bobAmount * 1.0f);
            float bobY = (float)Math.Abs((float)Math.Cos(walkCycle)) * (bobAmount * 0.8f);

            int recoil = isShooting ? 25 : 0;
            int posX = (int)((width / 2 - gunWidth / 2) + 240 + bobX);
            int posY = (int)((height - gunHeight) + 110 + recoil + bobY);

            if (isShooting && _resources.FlashSprite != null)
            {
                int fSize = 120;
                g.DrawImage(_resources.FlashSprite,
                    (int)(width / 2 - fSize / 2 + bobX),
                    (int)(height / 2 - fSize / 2 + 10 + bobY),
                    fSize, fSize);
            }

            g.DrawImage(_resources.AcceptorSprite, posX, posY, gunWidth, gunHeight);
        }
        
        //РЕНДЕРИНГ РАДАРА
        public void DrawRadar(Graphics g, Player player, Map map, int size = 6, int offset = 20)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.Grid[y, x] != 0)
                        g.FillRectangle(Brushes.DimGray, x * size + offset, y * size + offset, size, size);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            foreach (var ent in map.Entities)
            {
                if (ent.IsEnemy) continue;

                float ex = ent.X * size + offset;
                float ey = ent.Y * size + offset;
                if (ent.Type == EntityType.Health && _resources.HealthIcon != null)
                    g.DrawImage(_resources.HealthIcon, ex - 5, ey - 5, 10, 10);
                else if (ent.Type == EntityType.Ammo && _resources.AmmoIcon != null)
                    g.DrawImage(_resources.AmmoIcon, ex - 5, ey - 5, 10, 10);
            }

            foreach (var ent in map.Entities)
            {
                if (!ent.IsEnemy) continue;
                float ex = ent.X * size + offset;
                float ey = ent.Y * size + offset;
                g.FillEllipse(Brushes.Red, ex - 3, ey - 3, 6, 6);

                if (ent.Type == EntityType.Boss)
                    g.FillEllipse(Brushes.DeepSkyBlue, ex - 5, ey - 5, 10, 10);
            }

            float px = player.X * size + offset, py = player.Y * size + offset;
            float lx = px + (float)Math.Cos(player.Angle) * 12, ly = py + (float)Math.Sin(player.Angle) * 12;

            using (Pen p = new Pen(Color.Cyan, 2))
                g.DrawLine(p, px, py, lx, ly);
            g.FillEllipse(Brushes.White, px - 4, py - 4, 8, 8);
            g.DrawEllipse(Pens.Black, px - 4, py - 4, 8, 8);
        }
        
        //ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        public Bitmap CreateBlurSnapshot(Form form)
        {
            try
            {
                var blurSnapshot = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
                form.DrawToBitmap(blurSnapshot, new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                return new Bitmap(blurSnapshot, new Size(64, 48));
            }
            catch
            {
                return null;
            }
        }
    }
}