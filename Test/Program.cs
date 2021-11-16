using System;
using System.Globalization;

namespace TestSpace
{
    public class Test
    {
        Test()
        {
        }

        static void Main( /* string[] args */ )
        {
            Test thisTest = new Test();
            thisTest.ThisMain();
        }

        void ThisMain()
        {
            DateTime noaaDate = new DateTime( 2021, 9, 1 );
            if ( noaaDate.Day == 1 ) noaaDate = noaaDate.AddDays( -1 );

            Console.WriteLine( $"noaaDate: {noaaDate}" );

            for ( int i = 1; i <= 12; i++ )
                Console.WriteLine( $"<option value='{i:00}' id='{i:00}' {( i == noaaDate.Month ? "selected" : "" )}>" +
                    $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i )}</option>" );

            Console.ReadKey();
        }
    }

}

