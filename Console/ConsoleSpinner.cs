using System.Drawing;
using Window = Colorful.Console;

namespace Console
{
    internal class ConsoleSpinner(Color color, int delay)
    {
        private int counter;

        public void Turn()
        {
            counter++;

            Thread.Sleep(delay);

            switch (counter % 4)
            {
                case 0: Window.Write("/", color); break;
                case 1: Window.Write("-", color); break;
                case 2: Window.Write(@"\", color); break;
                case 3: Window.Write("|", color); break;
            }
            Window.SetCursorPosition(Window.CursorLeft - 1, Window.CursorTop);
        }
    }
}