using System;
using System.Reflection;
using System.Threading.Tasks;

[assembly: AssemblyVersionAttribute( "1.0.0" )]

namespace Test
{
    class Test
    {
        public static async Task Main()
        {
            InetPHP clientPhp = new InetPHP( );

            Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Uploading = UploadTestforCloud86.txt" );

            // Probeer het voor de Reports directory
            Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Upload File values: localfile: UploadTestforCloud86.txt" );
            Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Upload File values: remotefile: Reports/UploadTestforCloud86.txt" );

            if ( !await clientPhp.UploadAsync( localfile: "UploadTestforCloud86.txt", remotefile: "Reports/UploadTestforCloud86.txt" ) )
                Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "PHP UploadFile: Failed" );
            else
                Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "PHP UploadFile: Success" );

            // Probeer het voor de /httpdocs directory
            Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Upload File values: localfile: UploadTestforCloud86.txt" );
            Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Upload File values: remotefile: UploadTestforCloud86.txt" );

            if ( !await clientPhp.UploadAsync( localfile: "UploadTestforCloud86.txt", remotefile: "UploadTestforCloud86.txt" ) )
                Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "PHP UploadFile: Failed" );
            else
                Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "PHP UploadFile: Success" );

            return;
        }
    }
}
