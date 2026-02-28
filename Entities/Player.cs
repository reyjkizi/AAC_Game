using System;

namespace AAC_Game.Entities
{
    public class Player
    {
        //ПОЗИЦИЯ И ОРИЕНТАЦИЯ
        public float X { get; set; }
        public float Y { get; set; }
        public float Angle { get; set; }
        public float FOV { get; set; }
        
        //ХАРАКТЕРИСТИКИ
        public int Health { get; set; }
        public int Ammo { get; set; }
        public int Score { get; set; }
        
        //СВОЙСТВА
        public bool IsAlive
        {
            get { return Health > 0; }
        }
        
        //КОНСТРУКТОР
        public Player(float x, float y)
        {
            X = x;
            Y = y;
            Angle = 0;
            FOV = (float)(Math.PI / 3);
            Ammo = 20;
            Score = 0;
        }
        
        //МЕТОДЫ УПРАВЛЕНИЯ СОСТОЯНИЕМ
        public void Reset(int startX = 2, int startY = 2)
        {
            X = startX;
            Y = startY;
            Angle = 0;
            Health = 100;
            Ammo = 20;
            Score = 0;
        }

        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }

        public void Heal(int amount)
        {
            Health = Math.Min(100, Health + amount);
        }

        public void AddAmmo(int amount)
        {
            Ammo += amount;
        }
        
        //МЕТОДЫ СТРЕЛЬБЫ
        public bool CanShoot()
        {
            return Ammo > 0;
        }

        public void UseAmmo()
        {
            if (Ammo > 0)
                Ammo--;
        }
    }
}