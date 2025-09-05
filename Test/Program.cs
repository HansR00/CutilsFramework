using System;
using System.Reflection;

[assembly: AssemblyVersion( "1.0.0" )]

namespace Simulate
{
    class Simulate
    {
        public static void Main()
        {
            //TimeSpan interval = new TimeSpan( 0, 1, 0 ); // hrs, min, sec
            //Random thisRand = new Random();
            //CultureInfo Inv = CultureInfo.InvariantCulture;

            //while ( true )
            //{
            //    using ( StreamWriter rt = new StreamWriter( $"data/test202508.txt", true, Encoding.UTF8 ) )
            //    {
            //        rt.AutoFlush = true;
            //        rt.WriteLine( $"{DateTime.Now.ToString( "dd/MM/yy,HH:mm", Inv )},{( thisRand.NextDouble() * 10 ).ToString( "F2", Inv )}" );
            //    }

            //    Thread.Sleep( interval );
            //}

            string tz;

            if ( TimeZoneInfo.Local.HasIanaId )
            {
                tz = TimeZoneInfo.Local.Id;
                Console.WriteLine( "Found Timezone.local.id = " + tz );
            }
            else if ( !TimeZoneInfo.TryConvertWindowsIdToIanaId( TimeZoneInfo.Local.Id, out tz ) )
            {
                Console.WriteLine( "Warning, not IANA TZ code found for " + TimeZoneInfo.Local.Id );
                tz = "UTC";
            }

            if ( TimeZoneInfo.TryFindSystemTimeZoneById( "Europe/Amsterdam", out TimeZoneInfo? TZinfo ) )
            {
                Console.WriteLine( $"{TZinfo.StandardName} " );
                Console.WriteLine( $"{TZinfo.DaylightName} " );
                Console.WriteLine( $"{TZinfo.DisplayName} " );
                Console.WriteLine( $"{TZinfo.Id} " );
            }
            Console.ReadKey();

            Environment.Exit( 0 );
        }
    }
}
