﻿/*
 * InetSupport - Part of CumulusUtils
 *
 * © Copyright 2019 - 2021 Hans Rottier <hans.rottier@gmail.com>
 *
 * When the code is made public domain the licence will be changed to the GNU 
 * General Public License as published by the Free Software Foundation;
 * Until then, the code of CumulusUtils is not public domain and only the executable is 
 * distributed under the  Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * As a consequence, this code should not be in your posession unless with explicit permission by Hans Rottier
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI
 *              Initial release: pwsFWI             (version 1.0)
 *                               Website Generator  (version 3.0)
 *                               ChartsCompiler     (version 5.0)
 *              
 * Environment: Raspberry 3B+
 *              Raspbian / Linux 
 *              C# / Visual Studio
 *              
 * Inet FTP issue wrt :
 *   https://stackoverflow.com/questions/61068219/authentication-failed-because-the-remote-party-has-closed-the-transport-stream
 *   https://stackoverflow.com/questions/65636482/can-connect-to-ftp-using-filezilla-or-winscp-but-not-with-ftpwebrequest-or-flue
 *   https://stackoverflow.com/questions/1371964/free-ftp-library
 *   https://github.com/dotnet/runtime/issues/22977
 *   https://github.com/dotnet/runtime/issues/27916
 *   https://github.com/PhilippC/keepass2android/issues/1617
 *   https://github.com/robinrodricks/FluentFTP/issues/347
 *   
 *   https://www.venafi.com/blog/tls-session-resumption  // description!
 */

using System;
using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace CumulusUtils
{
    enum FtpProtocols { FTP, FTPS, SFTP }  // Defined 1,2,3 inn CumulusMX and as such stored in the Cumulus.ini!!

    public class InetSupport : IDisposable
    {
        readonly CuSupport Sup;

        readonly string username;
        readonly string password;
        readonly string hostname;
        readonly int port;
        readonly bool PassiveFTP;                       // param: ActiveFTP not ticked

        readonly FtpClient clientFluentFTP;             // The actual client to be used, FluentFTP version;
        readonly FtpProtocols ProtocolUsed;             // param: Sslftp set to 0,1 or 2

        readonly string SshftpAuthentication;
        readonly string SshftpPskFile;

        SftpClient clientRenci;
        bool FTPvalid;                         // Indication whether a connection could be made and filetransfer is possible.

        public InetSupport( CuSupport s )
        {
            Sup = s;

            Sup.LogDebugMessage( "InetSupport: Constructor start" );

            // For version 4.7.0 the FTP coding / File Upload will be rewritten and requires a bit more info in this class
            username = Sup.GetCumulusIniValue( "FTP site", "Username", "" );
            password = Sup.GetCumulusIniValue( "FTP site", "Password", "" );
            hostname = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            port = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "Port", "21" ) );

            // The next parameter require the default because Cumulus 1 does not know Sslftp parameter so an empty default fails the convert
            ProtocolUsed = (FtpProtocols) Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "Sslftp", "0" ) );
            PassiveFTP = Sup.GetCumulusIniValue( "FTP site", "ActiveFTP", "" ).Equals( "0" );

            SshftpAuthentication = Sup.GetCumulusIniValue( "FTP site", "SshFtpAuthentication", "password" ); // valid options: password, psk, password_psk
            SshftpPskFile = Sup.GetCumulusIniValue( "FTP site", "SshFtpPskFile", "" );

            FTPvalid = true;

            //
            // Now do the initialisation thing for the protocol selected.
            //

            FtpTrace.LogPassword = false;
            FtpTrace.LogUserName = false;
            FtpTrace.LogIP = false;

            if ( ProtocolUsed == FtpProtocols.FTP )
            {
                try
                {
                    clientFluentFTP = new FtpClient( hostname,
                                                    port,
                                                    username,
                                                    password )
                    {
                        EncryptionMode = FtpEncryptionMode.None,
                        SslProtocols = SslProtocols.None,
                        DataConnectionType = PassiveFTP ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.PORT,
                        Encoding = Encoding.UTF8,

                        SocketKeepAlive = true,
                        UploadDataType = FtpDataType.Binary
                    };

                    clientFluentFTP.Connect();
                    Sup.LogTraceInfoMessage( "InetSupport: FTP Setup (After connect):" );
                    Sup.LogDebugMessage( "InetSupport: Plain Old FTP activated." );
                    Sup.LogTraceInfoMessage( $"InetSupport: FTP Server: {clientFluentFTP.ServerType} on {clientFluentFTP.ServerOS}" );
                }
                catch ( Exception e ) when ( e is FtpAuthenticationException || e is FtpCommandException || e is FtpSecurityNotAvailableException )
                {
                    Sup.LogTraceErrorMessage( $"InetSupport: Exception on FTP connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"InetSupport: Failed FTP connecting to {hostname}. Files will not be transferred" );
                    FTPvalid = false;
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"InetSupport: Unknown Exception on FTP connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"InetSupport: Failed FTP connecting to {hostname}. Files will not be transferred" );
                    FTPvalid = false;
                    // throw;  Simply continue and leave all files without uploading Is OK
                }
            }
            else if ( ProtocolUsed == FtpProtocols.FTPS )
            {
                try
                {
                    clientFluentFTP = new FtpClient( hostname,
                                                    port,
                                                    username,
                                                    password )
                    {
                        EncryptionMode = FtpEncryptionMode.Explicit,
                        DataConnectionEncryption = true,
                        SslProtocols = SslProtocols.None, //SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12,
                        DataConnectionType = PassiveFTP ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.PORT,
                        Encoding = Encoding.UTF8,

                        SocketKeepAlive = true,
                        ValidateAnyCertificate = true,
                        UploadDataType = FtpDataType.Binary
                    };

                    clientFluentFTP.Connect();
                    Sup.LogTraceInfoMessage( " InetSupport: FTPS Setup (After connect):" );
                    Sup.LogDebugMessage( " InetSupport: FTPS activated." );
                    Sup.LogTraceInfoMessage( $" InetSupport: FTPS Server: {clientFluentFTP.ServerType} on {clientFluentFTP.ServerOS}" );
                }
                catch ( Exception e ) when ( e is FtpAuthenticationException || e is FtpCommandException || e is FtpSecurityNotAvailableException )
                {
                    Sup.LogTraceErrorMessage( $"InetSupport: Exception on FTPS connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"InetSupport: Failed FTPS connecting to {hostname}. Files will not be transferred" );
                    FTPvalid = false;
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"InetSupport: Unknown Exception on FTPS connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"InetSupport: Failed FTPS connecting to {hostname}. Files will not be transferred" );
                    FTPvalid = false;
                    // throw;  Simply continue and leave all files without uploading Is OK
                }
            }
            else if ( ProtocolUsed == FtpProtocols.SFTP )
            {
                try
                {
                    ConnectionInfo connectionInfo;
                    PrivateKeyFile pskFile;

                    if ( SshftpAuthentication == "password" )
                    {
                        connectionInfo = new ConnectionInfo( hostname, port, username, new PasswordAuthenticationMethod( username, password ) );
                        Sup.LogTraceInfoMessage( $"InetSupport SFTP: Connecting using password authentication" );
                    }
                    else if ( SshftpAuthentication == "psk" )
                    {
                        pskFile = new PrivateKeyFile( SshftpPskFile );
                        connectionInfo = new ConnectionInfo( hostname, port, username, new PrivateKeyAuthenticationMethod( username, pskFile ) );
                        Sup.LogTraceInfoMessage( $"InetSupport SFTP: Connecting using PSK authentication" );
                    }
                    else if ( SshftpAuthentication == "password_psk" )
                    {
                        pskFile = new PrivateKeyFile( SshftpPskFile );
                        connectionInfo = new ConnectionInfo( hostname, port, username, new PasswordAuthenticationMethod( username, password ), new PrivateKeyAuthenticationMethod( username, pskFile ) );
                        Sup.LogTraceInfoMessage( $"InetSupport SFTP: Connecting using password or PSK authentication" );
                    }
                    else
                    {
                        Sup.LogTraceInfoMessage( $"InetSupport SFTP: Invalid SshftpAuthentication specified [{SshftpAuthentication}]" );
                        FTPvalid = false;
                        return;
                    }

                    clientRenci = new SftpClient( connectionInfo );
                    clientRenci.ConnectionInfo.Timeout = TimeSpan.FromSeconds( 300 );

                    clientRenci.Connect();
                    clientRenci.OperationTimeout = TimeSpan.FromSeconds( 15 );
                    if ( !clientRenci.IsConnected )
                    {
                        FTPvalid = false;
                        Sup.LogTraceInfoMessage( $"Upload SFTP: Connection error." );
                    }


                    Sup.LogDebugMessage( $" InetSupport: SFTP activated" );
                }
                catch ( Exception ex ) when ( ex is SshException )
                {
                    Sup.LogTraceErrorMessage( $"InetSupport: Error connecting SFTP - {ex.Message}" );
                    Sup.LogTraceErrorMessage( $"InetSupport: Failed SFTP connecting to {hostname}. Files will not be transferred" );
                    FTPvalid = false;
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"InetSupport: Unknown Exception on SFTP connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"InetSupport: Failed SFTP connecting to {hostname}. Files will not be transferred" );
                    FTPvalid = false;
                }
            }
            else
            {
                Sup.LogTraceErrorMessage( $"InetSupport: Protocol not implemented {ProtocolUsed}. Files will not be transferred" );
                FTPvalid = false;
            }

            return;
        }

        public bool UploadFile( string remotefile, string localfile )
        {
            // On Async FTP: https://social.msdn.microsoft.com/Forums/vstudio/en-US/994fa6e8-e345-4d10-97e6-e540bec0cb76/what-is-asynchronous-ftp?forum=csharpgeneral
            //     Read the first answer, I understand async FTP does not really have large effects and is only really important with UI interaction.
            //     So I leave this as is (and also all FTPs in the project)
            bool Upload;

            // Immediately return if something was wrong at contructor time
            if ( !FTPvalid )
                return false;

            string URL = "";
            string Dir = "";

            Sup.LogDebugMessage( $"UploadFile: Start {localfile} => {remotefile}" );

            if ( string.IsNullOrEmpty( remotefile ) || string.IsNullOrEmpty( localfile ) ) { Sup.LogTraceErrorMessage( $"UploadFile: Nothing uploaded either in or outfile are empty." ); return false; }           // No reason to upload if there is  no file or destination

            Upload = Sup.GetUtilsIniValue( "FTP site", "DoUploadFTP", "false" ).ToLower() == "true";
            if ( !Upload ) { Sup.LogTraceInfoMessage( $"UploadFile: DoUploadFTP configured false => No Upload." ); return false; }      // No reason to do the whole procedure if we don't have to upload
            if ( !FTPvalid ) { Sup.LogTraceErrorMessage( $"UploadFile: Nothing uploaded because of connection error." ); return false; }  // No reason to do the whole procedure if we can't upload

            string CumulusURL = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            string CumulusDir = Sup.GetCumulusIniValue( "FTP site", "Directory", "" );

            string CumulusUtilsDir = Sup.GetUtilsIniValue( "FTP site", "UploadDir", "" );

            // Check for URL in CumulusIni
            if ( string.IsNullOrEmpty( CumulusURL ) )
                Upload = false;                                                               // Kind of paranoia check but well,you never know :(
            else
            {
                URL = CumulusURL;

                if ( string.IsNullOrEmpty( CumulusUtilsDir ) )
                    Dir = CumulusDir;
                else
                    Dir = CumulusUtilsDir;
            }

            if ( Upload )
            {
                Sup.LogTraceInfoMessage( $"Upload File values: DoUploadFTP: {Upload}" );
                Sup.LogTraceInfoMessage( $"Upload File values: URL: {CumulusURL}" );
                Sup.LogTraceInfoMessage( $"Upload File values: Dir: {CumulusDir}" );
                Sup.LogTraceInfoMessage( $"Upload File values: UtilsDir: {CumulusUtilsDir}" );
                Sup.LogTraceInfoMessage( $"Upload File values: remotefile: {remotefile}" );

                // Get the object used to communicate with the server.
                string requestname = Dir + "/" + remotefile;

                Sup.LogTraceInfoMessage( $"UploadFile: uploading {requestname}" );

                if ( ProtocolUsed == FtpProtocols.FTP || ProtocolUsed == FtpProtocols.FTPS )
                {
                    try
                    {
                        clientFluentFTP.UploadFile( localfile, requestname, FtpRemoteExists.Overwrite, false, FtpVerify.Throw );
                    }
                    catch ( Exception e ) when ( e is TimeoutException )
                    {
                        Sup.LogTraceErrorMessage( $"UploadFile ERROR: Timeout Exception: {e.Message}" );
                        if ( e.InnerException != null )
                            Sup.LogTraceErrorMessage( $"UploadFile ERROR: Inner Exception: {e.InnerException}" );
                        return false;
                    }
                    catch ( Exception e ) when ( e is FtpAuthenticationException || e is FtpCommandException || e is FtpSecurityNotAvailableException )
                    {
                        Sup.LogTraceErrorMessage( $"UploadFile ERROR: Exception: {e.Message}" );
                        if ( e.InnerException != null )
                            Sup.LogTraceErrorMessage( $"UploadFile ERROR: Inner Exception: {e.InnerException}" );
                        return false;
                    }
                    catch ( Exception e )
                    {
                        Sup.LogTraceErrorMessage( $"UploadFile ERROR: General Exception: {e.Message}" );
                        if ( e.InnerException != null )
                            Sup.LogTraceErrorMessage( $"UploadFile ERROR: Inner Exception: {e.InnerException}" );
                        return false;
                    }
                }
                else if ( ProtocolUsed == FtpProtocols.SFTP )
                {
                    using ( Stream istream = new FileStream( localfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                    {
                        try
                        {
                            if ( clientRenci.IsConnected )
                            {
                                clientRenci.UploadFile( istream, requestname, true );
                            }
                            else
                            {
                                ConnectionInfo connectionInfo;
                                PrivateKeyFile pskFile;

                                if ( SshftpAuthentication == "password" )
                                {
                                    connectionInfo = new ConnectionInfo( hostname, port, username, new PasswordAuthenticationMethod( username, password ) );
                                    Sup.LogTraceInfoMessage( $"InetSupport SFTP: Connecting using password authentication" );
                                }
                                else if ( SshftpAuthentication == "psk" )
                                {
                                    pskFile = new PrivateKeyFile( SshftpPskFile );
                                    connectionInfo = new ConnectionInfo( hostname, port, username, new PrivateKeyAuthenticationMethod( username, pskFile ) );
                                    Sup.LogTraceInfoMessage( $"InetSupport SFTP: Connecting using PSK authentication" );
                                }
                                else if ( SshftpAuthentication == "password_psk" )
                                {
                                    pskFile = new PrivateKeyFile( SshftpPskFile );
                                    connectionInfo = new ConnectionInfo( hostname, port, username, new PasswordAuthenticationMethod( username, password ), new PrivateKeyAuthenticationMethod( username, pskFile ) );
                                    Sup.LogTraceInfoMessage( $"InetSupport SFTP: Connecting using password or PSK authentication" );
                                }
                                else
                                {
                                    Sup.LogTraceInfoMessage( $"InetSupport SFTP: Invalid SshftpAuthentication specified [{SshftpAuthentication}]" );
                                    FTPvalid = false;
                                    return false;
                                }

                                clientRenci = new SftpClient( connectionInfo );
                                clientRenci.ConnectionInfo.Timeout = TimeSpan.FromSeconds( 900 );

                                clientRenci.Connect();
                                clientRenci.OperationTimeout = TimeSpan.FromSeconds( 15 );
                                if ( !clientRenci.IsConnected )
                                {
                                    Sup.LogTraceInfoMessage( $"Upload SFTP: Connection error." );
                                    FTPvalid = false;
                                    return false;
                                }

                                Sup.LogTraceInfoMessage( $" InetSupport SFTP: Realtime SFTP connected" );

                                // And finally upload
                                clientRenci.UploadFile( istream, requestname, true );
                            }
                        }
                        catch ( Exception e ) when ( e is SshException )
                        {
                            Sup.LogTraceErrorMessage( $"Upload SFTP: Error uploading {localfile} to {remotefile} : {e.Message}" );
                            if ( e.InnerException != null )
                                Sup.LogTraceErrorMessage( $"UploadFile SFTP ERROR: Inner Exception: {e.InnerException}" );
                            return false;
                        }
                        catch ( Exception e )
                        {
                            Sup.LogTraceErrorMessage( $"Upload SFTP: ERROR General Exception: {e.Message}" );
                            if ( e.InnerException != null )
                                Sup.LogTraceErrorMessage( $"UploadFile SFTP ERROR: Inner Exception: {e.InnerException}" );
                            return false;
                        }
                    }
                } // if else on basis of protocol

                Sup.LogTraceInfoMessage( $"UploadFile: Done" );
            }
            else // Upload = false
            {
                Sup.LogTraceInfoMessage( $"UploadFile Upload=false -> No file(s) uploaded." );
                return false;
            }

            return true;
        } // EndOf UploadFile


        // NOTE: The PostClient and the GetClient are solely used for the IPC with CMX
        // So the baseaddress is allways the localhost and set here from the CmxIPC module
        //

        public async Task<string> GetUrlDataAsync( Uri thisURL )
        {
            string retval;

            Sup.LogDebugMessage( $"GetUrlData Start: URL - {thisURL} " );

            // Note: I use 'using' because it is easier and it gets only called for UserReports, MAps and yourweather.co.uk so 
            //       there is no risk - I don't see a risk - of socket exhaustion
            //
            using ( HttpClient GetClient = new HttpClient() )
            {
                try
                {
                    Sup.LogTraceInfoMessage( $"GetUrlData Calling GetAsync" );
                    retval = await GetClient.GetStringAsync( thisURL );
                }
                catch ( Exception e ) when ( e is HttpRequestException )
                {
                    Sup.LogTraceErrorMessage( $"GetUrlData : Exception - {e.Message}" );
                    if ( e.InnerException != null )
                        Sup.LogTraceErrorMessage( $"GetUrlData: Inner Exception: {e.InnerException}" );
                    retval = "";
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"GetUrlData : General exception - {e.Message}" );
                    retval = "";
                }
            }

            return retval;
        } // EndOf GetUrlData


        public async Task<string> PostUrlDataAsync( Uri thisURL, string data )
        {
            string retval;

            Sup.LogDebugMessage( $" PostUrlData Start " );

            // Note: I use 'using' because it is easier and it gets only called for UserReports so 
            //       there is no risk - I don't see a risk - of socket exhaustion
            // Prevent issues with OpenSSL so bypass the certificate for the CGI
            // https://stackoverflow.com/questions/52939211/the-ssl-connection-could-not-be-established

            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ( sender, cert, chain, sslPolicyErrors ) => { return true; }
            };

            using ( HttpClient PostClient = new HttpClient( clientHandler, true ) )
            {
                Sup.LogTraceInfoMessage( $"PostUrlData Calling PostAsync" );

                try
                {
                    using ( StringContent requestData = new StringContent( data, Encoding.UTF8 ) )
                    {
                        using ( HttpResponseMessage response = await PostClient.PostAsync( thisURL, requestData ) )
                        {
                            if ( response.IsSuccessStatusCode )
                            {
                                retval = await response.Content.ReadAsStringAsync();
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"PostUrlData : Error: {response.StatusCode} - {response.ReasonPhrase}" );
                                retval = "";
                            }
                        } // End using response -> dispose
                    } // End using requestData -> dispose
                }
                catch ( Exception e ) when ( e is HttpRequestException )
                {
                    Sup.LogTraceErrorMessage( $"PostUrlData : Exception - {e.Message}" );
                    if ( e.InnerException != null )
                        Sup.LogTraceErrorMessage( $"PostUrlData: Inner Exception: {e.InnerException}" );
                    retval = "";
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"PostUrlData : General exception - {e.Message}" );
                    retval = "";
                }
            }

            return retval;
        }

        #region IDisposable

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                    if ( clientFluentFTP != null )
                        clientFluentFTP.Dispose();
                    if ( clientRenci != null )
                        clientRenci.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~InetSupport()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( false );
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( true );
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable
    } // EndOf Class
} // EndOf NameSpace
