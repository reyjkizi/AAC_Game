namespace AAC_Game.Entities
{
    public class Particle
    {
        //ПОЛОЖЕНИЕ В ПРОСТРАНСТВЕ 
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        //СКОРОСТЬ
        public float VX { get; set; }
        public float VY { get; set; }
        public float VZ { get; set; }
        
        //ВРЕМЯ ЖИЗНИ
        public int LifeTime { get; set; }
        
        //КОНСТРУКТОР
        public Particle(float x, float y, float z, float vx, float vy, float vz, int lifeTime)
        {
            X = x;
            Y = y;
            Z = z;

            VX = vx;
            VY = vy;
            VZ = vz;

            LifeTime = lifeTime;
        }
        
        //МЕТОДЫ
        public void Update()
        {
            X += VX;
            Y += VY;
            Z += VZ;

            VZ -= 0.015f;

            LifeTime--;
        }

        public bool IsAlive => LifeTime > 0 && Z > 0;
    }
}