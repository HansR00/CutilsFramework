/*
 * InetPHP - Part of Test for CumulusUtils
 * 
 * © Copyright 2019-2024 Hans Rottier <hans.rottier@gmail.com>
 *
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 *              
 * Environment: Raspberry Pi 4B and up
 *              Raspberry Pi OS
 *              C# / Visual Studio / Windows for development
 * 
 * Test for PHP upload where the path fails at some point : /httpdocs is accepted, /httpdocs/Reports is not accepted in the main CMX program.
 * It is currently unknown why this appens as this test and CUtils do not have an upload issue, it is only the Reports upload in CMX!
 * 
 */

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace Test
{
    public class InetPHP
    {
        // Based on the mail by Mark Crossley date: 17/02/2023 13:09

        public string PhpUrl;
        public string PhpSecret;
        public string PhpCompression;

        public HttpClient phpUploadHttpClient;

        public InetPHP()
        {
            PhpUrl = "https://meteo-wagenborgen.nl/upload.php";
            PhpSecret = "1c7ebb2f-77db-4673-89aa-3dae544774d0";
            PhpCompression = "none";

            // Constructor
            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ( sender, cert, chain, sslPolicyErrors ) => { return true; }
            };

            phpUploadHttpClient = new HttpClient( clientHandler, true );

            return;
        }

        public async Task<bool> PhpInit()
        {
            using ( var request = new HttpRequestMessage( HttpMethod.Get, PhpUrl ) )
            {
                try
                {
                    Console.WriteLine( $"Testing PHP upload compression on {PhpUrl}" );

                    request.Headers.Add( "Accept", "text/html" );
                    request.Headers.Add( "Accept-Encoding", "gzip, deflate" );
                    var response = await phpUploadHttpClient.SendAsync( request );
                    var encoding = response.Content.Headers.ContentEncoding;

                    PhpCompression = encoding.Count == 0 ? "none" : encoding.First();

                    if ( PhpCompression == "none" ) Console.WriteLine( "PHP upload does not support compression" );
                    else Console.WriteLine( $"PHP upload supports {PhpCompression} compression" );

                    return true;
                }
                catch ( Exception ex )
                {
                    Console.WriteLine( $"PhpInit: Error - {ex.Message}" );

                    return false;
                }
            }
        } // PhpInit (get the compression mode)

        public async Task<bool> UploadAsync( string localfile, string remotefile )
        {
            bool binary = false;
            bool incremental = false;

            string data = null;
            string ext;

            Console.WriteLine( $"PHP Upload: starting {localfile}" );

            if ( string.IsNullOrEmpty( localfile ) )
                Console.WriteLine( $"InetPhp: The data string is empty, ignoring this upload" );
            else
            {
                data = File.ReadAllText( localfile );

                FileInfo fi = new FileInfo( localfile );
                ext = fi.Extension;

                incremental = false;

                Console.WriteLine( $"Incremental = {incremental}; filename = {fi.Name}; ext = {ext}; HoursInGraph = -72" );
            }

            try
            {
                var encoding = new UTF8Encoding( false );

                using ( var request = new HttpRequestMessage( HttpMethod.Post, PhpUrl ) )
                {
                    var unixTs = DateTimeToUnixUTC( DateTime.Now ).ToString();
                    var signature = GetSHA256Hash( PhpSecret, unixTs + remotefile + data );

                    // disable expect 100 - PHP doesn't support it
                    request.Headers.ExpectContinue = false;
                    request.Headers.Add( "ACTION", incremental ? "append" : "replace" );
                    request.Headers.Add( "FILE", remotefile );

                    if ( incremental )
                    {
                        request.Headers.Add( "OLDEST", DateTimeToJS( DateTime.Now.AddHours( -72 ) ).ToString() );
                    }

                    request.Headers.Add( "TS", unixTs );
                    request.Headers.Add( "SIGNATURE", signature );
                    request.Headers.Add( "BINARY", binary ? "1" : "0" );
                    request.Headers.Add( "UTF8", "1" );

                    // Compress? if supported and payload exceeds 500 bytes
                    if ( data.Length < 500 || PhpCompression == "none" )
                    {
                        request.Content = new StringContent( data, encoding, "application/octet-stream" );
                    }
                    else
                    {
                        using ( MemoryStream ms = new MemoryStream() )
                        {
                            if ( PhpCompression == "gzip" )
                            {
                                using ( var zipped = new System.IO.Compression.GZipStream( ms, System.IO.Compression.CompressionMode.Compress, true ) )
                                {
                                    var byteData = encoding.GetBytes( data );
                                    zipped.Write( byteData, 0, byteData.Length );
                                }
                            }
                            else if ( PhpCompression == "deflate" )
                            {
                                using ( var zipped = new System.IO.Compression.DeflateStream( ms, System.IO.Compression.CompressionMode.Compress, true ) )
                                {
                                    var byteData = encoding.GetBytes( data );
                                    zipped.Write( byteData, 0, byteData.Length );
                                }
                            }

                            ms.Position = 0;
                            byte[] compressed = new byte[ ms.Length ];
                            ms.Read( compressed, 0, compressed.Length );

                            MemoryStream outStream = new MemoryStream( compressed );
                            StreamContent streamContent = new StreamContent( outStream );
                            streamContent.Headers.Add( "Content-Encoding", PhpCompression );
                            streamContent.Headers.ContentLength = outStream.Length;

                            request.Content = streamContent;
                        }
                    }

                    var response = await phpUploadHttpClient.SendAsync( request );
                    Console.WriteLine( $"InetPhp: {remotefile}: Response code = {(int) response.StatusCode}: {response.StatusCode}" );

                    var responseBodyAsText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine( $"InetPhp: {remotefile}: Response text follows:\n{responseBodyAsText}" );

                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch ( Exception ex )
            {

                Console.WriteLine( $"InetPhp: Error - {ex.Message}" );

                if ( ex.InnerException != null )
                {
                    Console.WriteLine( $"InetPhp: Base exception - {ex.InnerException.Message}" );
                }

                return false;

            }
        } // Upload

        public static string GetSHA256Hash( string key, string data )
        {
            byte[] hashValue;

            // Initialize the keyed hash object.
            using ( HMACSHA256 hmac = new HMACSHA256( key.ToAsciiBytes() ) )
            {
                // convert string to stream
                byte[] byteArray = Encoding.UTF8.GetBytes( data );

                using ( MemoryStream stream = new MemoryStream( byteArray ) )
                {
                    // Compute the hash of the input string.
                    hashValue = hmac.ComputeHash( stream );
                }

                return BitConverter.ToString( hashValue ).Replace( "-", string.Empty ).ToLower();
            }
        } // GetSHA256Hash

        public static long DateTimeToJS( DateTime timestamp ) => (long) ( timestamp - new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds * 1000;
        public static long DateTimeToUnix( DateTime timestamp ) => (long) ( timestamp - new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds;
        public static long DateTimeToUnixUTC( DateTime timestamp ) => (long) ( timestamp.ToUniversalTime() - new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds;
    } // Class InetPhp
}
