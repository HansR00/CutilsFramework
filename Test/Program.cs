using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Test
    {
        public static void Main()
        {
            bool DoMapsOn;
            string retval;

            CultureInfo thisculture;

            string Locale = "nb-NO";  // or nn-NO

            thisculture = CultureInfo.GetCultureInfo( Locale );
            CultureInfo.DefaultThreadCurrentCulture = thisculture;


            if ( DateTime.TryParse( "03.03.23", out DateTime DoneToday ) )
            {
                DoMapsOn = DateIsToday( DoneToday );
            }
            else DoMapsOn = true;

            if ( DoMapsOn )
            {
                Console.WriteLine( $"MapsOn: Must send signature: {DoneToday:dd/MM/yy} / Setting DoneToday to now." );
                Console.WriteLine( $"{DateTime.Now:dd/MM/yy}" );
            }
            else retval = $"MapsOn: Must NOT send signature, has been done already : {DoneToday:dd/MM/yy}";

            return;
        }

        static bool DateIsToday( DateTime thisDate )
        {
            bool retval = true;

            if ( Math.Abs( DateTime.Now.DayOfYear - thisDate.DayOfYear ) > 0 )
            {
                retval = false;
            }

            return retval;
        }


    }

}
