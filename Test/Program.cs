using System;
using System.Globalization;

namespace Test
{
    class Test
    {
        public static void Main()
        {
            bool DoMapsOn;
            string Locale, tmp;

            CultureInfo thisculture;

            do
            {
                Console.WriteLine( "" );
                Console.Write( "Give a locale: " );
                tmp = Console.ReadLine();

                if ( !string.IsNullOrEmpty( tmp ) )
                {
                    Locale = tmp;
                }
                else Locale = "nb-NO";  // or nn-NO... the problem is with nb-NO under mono

                //string Locale = "hr-HR";  // or nn-NO... the problem is with nb-NO under mono

                try
                {
                    thisculture = CultureInfo.GetCultureInfo( Locale );
                    CultureInfo.DefaultThreadCurrentCulture = thisculture;
                }
                catch
                {
                    continue;
                }

                tmp = "2023-03-03T13:45:30";
                //tmp = "03.03.23 00:00";
                Console.WriteLine( $"MapsOn: Before testing DoneToday string tmp: {tmp} " );

                string thisFormat = "s";
                Console.WriteLine($"Try ParseExact: {DateTime.ParseExact( tmp, thisFormat, thisculture )}" );

                if ( DateTime.TryParse( tmp, out DateTime DoneToday ) )
                {
                    Console.WriteLine( $"MapsOn: Before testing DoneToday after parsing: {DoneToday} " );
                    DoMapsOn = !DateIsToday( DoneToday );
                    Console.WriteLine( $"MapsOn: After testing DoneToday: {DoneToday} DoMApsOn = {DoMapsOn}" );
                }
                else DoMapsOn = true;

                if ( DoMapsOn )
                {
                    Console.WriteLine( $"MapsOn: Must send signature: {DoneToday:s} / Setting DoneToday to now." );
                }
                else Console.WriteLine( $"MapsOn: Must NOT send signature, has been done already : {DoneToday:s}" );
            } while ( true );
        }

        static bool DateIsToday( DateTime thisDate )
        {
            Console.WriteLine( $"DateIsToday for thisDate: {thisDate} | thisDate.DateOfYear: {thisDate.DayOfYear} versus Now.DayOfYear: {DateTime.Now.DayOfYear})" );

            bool retval = true;

            if ( Math.Abs( DateTime.Now.DayOfYear - thisDate.DayOfYear ) > 0 )
            {
                retval = false;
            }

            Console.WriteLine( $"DateIsToday returning {retval}" );

            return retval;
        }


    }

}
