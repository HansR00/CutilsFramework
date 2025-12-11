/*
 * InetPHP - Part of CumulusUtils
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

namespace CumulusUtils
{
    public class InetPHP
    {
        // Based on the mail by Mark Crossley date: 17/02/2023 13:09

        readonly CuSupport Sup;

        public string PhpUrl;
        public string PhpSecret;
        public string PhpCompression;

        public HttpClient phpUploadHttpClient;

        public InetPHP( CuSupport s )
        {
            Sup = s;

            PhpUrl = Sup.GetCumulusIniValue( "FTP site", "PHP-URL", "" );
            PhpSecret = Crypto.DecryptString( Sup.GetCumulusIniValue( "FTP site", "PHP-Secret", "" ), CUtils.CryptoKey );
            PhpCompression = "none";

            // Constructor 
            // Clienthandler commented out, not needed but in just in case tge issue recurs
            // Prevent issues with OpenSSL so bypass the certificate for the CGI
            // https://stackoverflow.com/questions/52939211/the-ssl-connection-could-not-be-established
            /*
             * HttpClientHandler clientHandler = new HttpClientHandler
             * {
             *     ServerCertificateCustomValidationCallback = ( sender, cert, chain, sslPolicyErrors ) => { return true; }
             * };
             * 
             */

            phpUploadHttpClient = new HttpClient( /* clientHandler, true */ );

            return;
        }

        public async Task<bool> PhpInit()
        {
            bool PhpUseBrotli = Sup.GetCumulusIniValue( "FTP site", "PHP-UseBrotli", "0" ).Equals( "1", CUtils.Cmp );

            using ( var request = new HttpRequestMessage( HttpMethod.Get, PhpUrl ) )
            {
                try
                {
                    Sup.LogDebugMessage( $"Testing PHP upload compression on {PhpUrl}" );

                    request.Headers.Add( "Accept", "text/html" );
                    request.Headers.Add( "Accept-Encoding", "gzip, deflate" + ( PhpUseBrotli ? ", br" : "" ) );
                    var response = await phpUploadHttpClient.SendAsync( request );

                    response.EnsureSuccessStatusCode();
                    var encoding = response.Content.Headers.ContentEncoding;

                    PhpCompression = encoding.Count == 0 ? "none" : encoding.First();

                    if ( PhpCompression == "none" ) Sup.LogTraceInfoMessage( "PHP upload does not support compression" );
                    else Sup.LogTraceInfoMessage( $"PHP upload supports {PhpCompression} compression" );

                    return true;
                }
                catch ( Exception ex )
                {
                    Sup.LogTraceErrorMessage( $"PhpInit: Error - {ex.Message}" );

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

            Sup.LogTraceInfoMessage( $"PHP Upload: starting {localfile}" );

            if ( string.IsNullOrEmpty( localfile ) )
                Sup.LogTraceWarningMessage( $"InetPhp: The data string is empty, ignoring this upload" );
            else
            {
                data = File.ReadAllText( localfile );

                FileInfo fi = new FileInfo( localfile );
                ext = fi.Extension;

                if ( ext == ".json" && !CUtils.DoingUserAskedData ) incremental = false;
                else incremental = ext == ".json" && !( fi.Name.Contains( "ALL", CUtils.Cmp ) || fi.Name.Contains( "DAILY", CUtils.Cmp ) );

                Sup.LogTraceInfoMessage( $"Incremental = {incremental}; filename = {fi.Name}; ext = {ext}; HoursInGraph = {CUtils.HoursInGraph}" );
            }

            try
            {
                var encoding = new UTF8Encoding( false );

                using ( var request = new HttpRequestMessage( HttpMethod.Post, PhpUrl ) )
                {
                    var unixTs = CuSupport.DateTimeToUnixUTC( DateTime.Now ).ToString();
                    var signature = GetSHA256Hash( PhpSecret, unixTs + remotefile + data );

                    // disable expect 100 - PHP doesn't support it
                    request.Headers.ExpectContinue = false;
                    request.Headers.Add( "ACTION", incremental ? "append" : "replace" );
                    request.Headers.Add( "FILE", remotefile );

                    if ( incremental )
                    {
                        request.Headers.Add( "OLDEST", CuSupport.DateTimeToJSUTC( DateTime.Now.AddHours( -CUtils.HoursInGraph ) ).ToString() );
                    }

                    request.Headers.Add( "TS", unixTs );
                    request.Headers.Add( "SIGNATURE", signature );
                    request.Headers.Add( "BINARY", binary ? "1" : "0" );
                    request.Headers.Add( "UTF8", "1" );

                    //Sup.LogTraceInfoMessage( $"InetPhp: Header = {request.Headers}" );

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
                            else if ( PhpCompression == "br" )
                            {
                                using ( var zipped = new System.IO.Compression.BrotliStream( ms, System.IO.Compression.CompressionMode.Compress, true ) )
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
                    Sup.LogTraceInfoMessage( $"InetPhp: {remotefile}: Response code = {(int) response.StatusCode}: {response.StatusCode}" );

                    var responseBodyAsText = await response.Content.ReadAsStringAsync();
                    Sup.LogTraceInfoMessage( $"InetPhp: {remotefile}: Response text follows:\n{responseBodyAsText}" );

                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch ( Exception ex )
            {

                Sup.LogTraceInfoMessage( $"InetPhp: Error - {ex.Message}" );

                if ( ex.InnerException is not null )
                {
                    Sup.LogTraceInfoMessage( $"InetPhp: Base exception - {ex.InnerException.Message}" );
                }

                return false;

            }
        } // Upload


        public static string GetSHA256Hash( string key, string data )
        {
            byte[] hashValue;

            // Initialize the keyed hash object.
            using HMACSHA256 hmac = new HMACSHA256( Encoding.ASCII.GetBytes( key ) );

            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes( data );
            using ( MemoryStream stream = new MemoryStream( byteArray ) )
            {
                // Compute the hash of the input string.
                hashValue = hmac.ComputeHash( stream );
            }
            return BitConverter.ToString( hashValue ).Replace( "-", string.Empty ).ToLower();
        }
    } // Class InetPhp
}
