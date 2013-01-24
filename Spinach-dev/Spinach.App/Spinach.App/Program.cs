using System;

namespace Spinach.App
{
#if WINDOWS || XBOX
    static class Program
    {
        static void Main(string[] args)
        {
            using (SpinachApp game = new SpinachApp())
            {
                game.Run();
            }
        }
    }
#endif
}