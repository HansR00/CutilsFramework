using System;
using System.Drawing;

namespace Test
{
    class Test
    {
        public static void Main()
        {
            //int count = 0;
            //DateTime epochDate = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );

            //long firstDate = javascriptValues[ 0 ];

            //foreach ( long thisDate in javascriptValues )
            //{

            //    if ( thisDate < firstDate ) Console.WriteLine( $"Thisdate is not sorted in list: {count}:{epochDate.AddMilliseconds( thisDate )}" );
            //    count++;
            //}

            //Console.WriteLine( $"Done - press any key..." );
            //Console.ReadKey();

            //int[] Frequencies = { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60 };

            //foreach(int a in Frequencies)
            //{
            //    int b = 10;
            //    Console.WriteLine( $"{b} % {a} = {b % a}" );
            //}

            using ( System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage( new Bitmap( 1, 1 ) ) )
            {
                SizeF size = graphics.MeasureString( "Hello World", new Font( "Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel ) );
                Console.WriteLine( $"Hello World measures {size}" );
            }

            Console.ReadLine();
            return;
        }
    }
}
