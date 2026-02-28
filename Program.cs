using System;
using System.Windows.Forms;

namespace AAC_Game
{
    // Главная точка входа в приложение.
    internal static class Program
    {
        // Главная точка входа в приложение.
        [STAThread] 
        static void Main()
        {
            // Включает визуальные стили для элементов управления
            Application.EnableVisualStyles();

            // Отключает совместимый с предыдущими версиями рендеринг текста
            Application.SetCompatibleTextRenderingDefault(false);

            // Запускает главную форму игры
            Application.Run(new Form1());
        }
    }
}