/*
 * InetPHP - Part of CumulusUtils
 * 
 * © Copyright 2019-2024 Hans Rottier <hans.rottier@gmail.com>
 *
 * The code of CumulusUtils is public domain and distributed under the  
 * Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * (Note: this is different license than for CumulusMX itself, it is basically is usage license)
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI .NET Framework 4.8
 *              Initial release: pwsFWI                 (version 1.0)
 *                               Website Generator      (version 3.0)
 *                               ChartsCompiler         (version 5.0)
 *                               Maintenance releases   (version 6.x) including CustomLogs
 *              Startdate : 16 november 2021 start of conversion to .NET 5, 6 and 7
 *              Startdate : 15 january 2024 start of conversion to .NET 8
 *              
 * Environment: Raspberry Pi 4B and up
 *              Raspberry Pi OS
 *              C# / Visual Studio / Windows for development
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
            PhpSecret = Sup.GetCumulusIniValue( "FTP site", "PHP-Secret", "" );
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
                    Sup.LogDebugMessage( $"Testing PHP upload compression on {PhpUrl}" );

                    request.Headers.Add( "Accept", "text/html" );
                    request.Headers.Add( "Accept-Encoding", "gzip, deflate" );
                    var response = await phpUploadHttpClient.SendAsync( request );
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
                        request.Headers.Add( "OLDEST", CuSupport.DateTimeToJS( DateTime.Now.AddHours( -CUtils.HoursInGraph ) ).ToString() );
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
                    Sup.LogTraceInfoMessage( $"InetPhp: {remotefile}: Response code = {(int) response.StatusCode}: {response.StatusCode}" );

                    var responseBodyAsText = await response.Content.ReadAsStringAsync();
                    Sup.LogTraceInfoMessage( $"InetPhp: {remotefile}: Response text follows:\n{responseBodyAsText}" );

                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch ( Exception ex )
            {

                Sup.LogTraceInfoMessage( $"InetPhp: Error - {ex.Message}" );

                if ( ex.InnerException != null )
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
    } // Class InetPhp
}
