using System;
using System.Globalization;

namespace Test
{
    class Test
    {
        public static void Main()
        {
            int DayOfYear = 150;
            int value = 10;
            bool thisBool = false;
            if ( DayOfYear % value == 0 ) thisBool = true;
            Console.WriteLine( $"Value = {thisBool}" );
        }
    }
}
