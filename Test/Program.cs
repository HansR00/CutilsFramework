using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

[assembly: AssemblyVersion( "1.0.0" )]

namespace Simulate
{
    class Simulate
    {
        public static void Main()
        {
            TimeSpan interval = new TimeSpan( 0, 1, 0 ); // hrs, min, sec
            Random thisRand = new Random();
            CultureInfo Inv = CultureInfo.InvariantCulture;

            while ( true )
            {
                using ( StreamWriter rt = new StreamWriter( $"data/test202508.txt", true, Encoding.UTF8 ) )
                {
                    rt.AutoFlush = true;
                    rt.WriteLine( $"{DateTime.Now.ToString( "dd/MM/yy,HH:mm", Inv )},{( thisRand.NextDouble() * 10 ).ToString( "F2", Inv )}" );
                }

                Thread.Sleep( interval );
            }

            Environment.Exit( 0 );
        }
    }
}
