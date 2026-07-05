/*
 * InetSupport - Part of CumulusUtils
 * 
 * Contains the code for all protocols which makes it a bit messy.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace CumulusUtils
{
    enum FtpProtocols { FTP, FTPS, SFTP, PHP }  // Defined 1,2,3 inn CumulusMX and as such stored in the Cumulus.ini!!
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

        readonly InetPHP clientPhp;
        readonly SemaphoreSlim uploadSemaphore;
        readonly int delayMilliSeconds;
        readonly int MaxUploadThreads;

        bool FTPvalid;                         // Indication whether a connection could be made and filetransfer is possible.

        public bool IsIncrementalAllowed() => ProtocolUsed == FtpProtocols.PHP;

        #region Initialiser

        public InetSupport( CuSupport s )
        {
            Sup = s;

            Sup.LogDebugMessage( "InetSupport: Constructor starting" );

            username = Crypto.DecryptString( Sup.GetCumulusIniValue( "FTP site", "Username", "" ), CUtils.CryptoKey );
            password = Crypto.DecryptString( Sup.GetCumulusIniValue( "FTP site", "Password", "" ), CUtils.CryptoKey );
            hostname = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            port = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "Port", "21" ) );

            ProtocolUsed = (FtpProtocols) Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "Sslftp", "0" ) );
            PassiveFTP = Sup.GetCumulusIniValue( "FTP site", "ActiveFTP", "" ).Equals( "0" );

            SshftpAuthentication = Sup.GetCumulusIniValue( "FTP site", "SshFtpAuthentication", "password" ); // valid options: password, psk, password_psk
            SshftpPskFile = Sup.GetCumulusIniValue( "FTP site", "SshFtpPskFile", "" );

            FTPvalid = true;

            //
            // Now do the initialisation thing for the protocol selected.
            //

            if ( ProtocolUsed == FtpProtocols.FTP )
            {
                try
                {
                    clientFluentFTP = new FtpClient
                    {
                        Host = hostname,
                        Port = port,
                        Credentials = new NetworkCredential( username, password ),
                    };

                    clientFluentFTP.Config.EncryptionMode = FtpEncryptionMode.None;
                    clientFluentFTP.Config.SslProtocols = SslProtocols.None;
                    clientFluentFTP.Config.DataConnectionType = PassiveFTP ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.PORT;
                    clientFluentFTP.Encoding = Encoding.UTF8;

                    clientFluentFTP.Config.SocketKeepAlive = true;
                    clientFluentFTP.Config.UploadDataType = FtpDataType.Binary;

                    clientFluentFTP.Connect();

                    Sup.LogMessage( "InetSupport: FTP Setup (After connect):", TraceLevel.Info );
                    Sup.LogMessage( "InetSupport: Plain Old FTP activated.", TraceLevel.Info );
                    Sup.LogMessage( $"InetSupport: FTP Server: {clientFluentFTP.ServerType} on {clientFluentFTP.ServerOS}", TraceLevel.Info );
                }
                catch ( Exception e ) when ( e is FtpAuthenticationException || e is FtpCommandException || e is FtpSecurityNotAvailableException )
                {
                    Sup.LogMessage( $"InetSupport: Exception on FTP connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"InetSupport: Failed FTP connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    FTPvalid = false;
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"InetSupport: Unknown Exception on FTP connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"InetSupport: Failed FTP connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    FTPvalid = false;
                }
            }
            else if ( ProtocolUsed == FtpProtocols.FTPS )
            {
                try
                {
                    clientFluentFTP = new FtpClient
                    {
                        Host = hostname,
                        Port = port,
                        Credentials = new NetworkCredential( username, password )
                    };

                    clientFluentFTP.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                    clientFluentFTP.Config.SslProtocols = SslProtocols.None;
                    clientFluentFTP.Config.DataConnectionType = PassiveFTP ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.PORT;
                    clientFluentFTP.Encoding = Encoding.UTF8;

                    clientFluentFTP.Config.SocketKeepAlive = true;
                    clientFluentFTP.Config.ValidateAnyCertificate = true;
                    clientFluentFTP.Config.UploadDataType = FtpDataType.Binary;

                    clientFluentFTP.Connect();

                    Sup.LogMessage( " InetSupport: FTPS Setup (After connect):", TraceLevel.Info );
                    Sup.LogMessage( " InetSupport: FTPS activated.", TraceLevel.Info );
                    Sup.LogMessage( $" InetSupport: FTPS Server: {clientFluentFTP.ServerType} on {clientFluentFTP.ServerOS}", TraceLevel.Info );
                }
                catch ( Exception e ) when ( e is FtpAuthenticationException || e is FtpCommandException || e is FtpSecurityNotAvailableException )
                {
                    Sup.LogMessage( $"InetSupport: Exception on FTPS connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"InetSupport: Failed FTPS connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    FTPvalid = false;
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"InetSupport: Unknown Exception on FTPS connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"InetSupport: Failed FTPS connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    FTPvalid = false;
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
                        Sup.LogMessage( $"InetSupport SFTP: Connecting using password authentication", TraceLevel.Info );
                    }
                    else if ( SshftpAuthentication == "psk" )
                    {
                        pskFile = new PrivateKeyFile( SshftpPskFile );
                        connectionInfo = new ConnectionInfo( hostname, port, username, new PrivateKeyAuthenticationMethod( username, pskFile ) );
                        Sup.LogMessage( $"InetSupport SFTP: Connecting using PSK authentication", TraceLevel.Info );
                    }
                    else if ( SshftpAuthentication == "password_psk" )
                    {
                        pskFile = new PrivateKeyFile( SshftpPskFile );
                        connectionInfo = new ConnectionInfo( hostname, port, username, new PasswordAuthenticationMethod( username, password ), new PrivateKeyAuthenticationMethod( username, pskFile ) );
                        Sup.LogMessage( $"InetSupport SFTP: Connecting using password or PSK authentication", TraceLevel.Info );
                    }
                    else
                    {
                        Sup.LogMessage( $"InetSupport SFTP: Invalid SshftpAuthentication specified [{SshftpAuthentication}]", TraceLevel.Error );
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
                        Sup.LogMessage( $"Upload SFTP: Connection error.", TraceLevel.Error );
                    }


                    Sup.LogMessage( $" InetSupport: SFTP activated", TraceLevel.Info );
                }
                catch ( Exception ex ) when ( ex is SshException )
                {
                    Sup.LogMessage( $"InetSupport: Error connecting SFTP - {ex.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"InetSupport: Failed SFTP connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    FTPvalid = false;
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"InetSupport: Unknown Exception on SFTP connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"InetSupport: Failed SFTP connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    FTPvalid = false;
                }
            }
            else if ( ProtocolUsed == FtpProtocols.PHP )
            {
                string tmp;

                clientPhp = new InetPHP( Sup );
                FTPvalid = false; // Init needs to be done. Because of async needs to be done in Upload first time

                // Use CMX default nr of threads else take the specific configured CUtils value
                //
                tmp = Sup.GetUtilsIniValue( "FTP site", "MaxConcurrentUploads", "" );

                if ( string.IsNullOrEmpty( tmp ) ) MaxUploadThreads = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "MaxConcurrentUploads", "" ) );
                else MaxUploadThreads = Convert.ToInt32( tmp );

                delayMilliSeconds = Convert.ToInt32( Sup.GetUtilsIniValue( "FTP site", "delayMilliSeconds", "0" ) );

                Sup.LogMessage( $"Upload PHP: MaxUploadThreads = {MaxUploadThreads} / delayMilliSeconds = {delayMilliSeconds}", TraceLevel.Info );

                uploadSemaphore = new SemaphoreSlim( MaxUploadThreads, MaxUploadThreads );
            }
            else
            {
                Sup.LogMessage( $"InetSupport: Protocol not implemented {ProtocolUsed}. Files will not be transferred", TraceLevel.Error );
                FTPvalid = false;
            }

            return;
        }

        #endregion

        #region UploadFile

        public async Task<bool> UploadFileAsync( string remotefile, string localfile )
        {
            if ( !FTPvalid && ProtocolUsed == FtpProtocols.PHP )
            {
                FTPvalid = await clientPhp.PhpInit();
            }

            if ( !FTPvalid )
            {
                Sup.LogMessage( $"UploadFile: Nothing uploaded because of connection error.", TraceLevel.Error );
                return false;
            }

            string URL = "";
            string Dir = "";

            Sup.LogMessage( $"UploadFile: Starting {localfile} => {remotefile}", TraceLevel.Info );

            // No reason to upload if there is  no file or destination
            if ( string.IsNullOrEmpty( remotefile ) || string.IsNullOrEmpty( localfile ) ) { Sup.LogMessage( $"UploadFile: Nothing uploaded either in or outfile are empty.", TraceLevel.Error ); return false; }
            if ( !File.Exists( localfile ) ) { Sup.LogMessage( $"UploadFile: Local file {localfile} does not exist", TraceLevel.Error ); return false; }

            bool Upload = Sup.GetUtilsIniValue( "FTP site", "DoUploadFTP", "false" ).Equals( "true", CUtils.Cmp );
            if ( !Upload ) { Sup.LogMessage( $"UploadFile: DoUploadFTP configured false => No Upload.", TraceLevel.Error ); return false; }      // No reason to do the whole procedure if we don't have to upload

            string CumulusURL;
            string CumulusDir = Sup.GetCumulusIniValue( "FTP site", "Directory", "" );
            string CumulusUtilsDir = Sup.GetUtilsIniValue( "FTP site", "UploadDir", "" );

            CumulusURL = ProtocolUsed == FtpProtocols.PHP ? Sup.GetCumulusIniValue( "FTP site", "PHP-URL", "" ) : Sup.GetCumulusIniValue( "FTP site", "Host", "" );

            if ( string.IsNullOrEmpty( CumulusURL ) ) Upload = false; // Kind of paranoia check but well,you never know :|
            else
            {
                URL = CumulusURL;

                if ( string.IsNullOrEmpty( CumulusUtilsDir ) ) Dir = CumulusDir;
                else Dir = CumulusUtilsDir;
            }

            if ( Upload )
            {
                if ( ProtocolUsed == FtpProtocols.FTP || ProtocolUsed == FtpProtocols.FTPS )
                {
                    string requestname = Dir + "/" + remotefile;

                    Sup.LogMessage( $"Upload File values: URL: {CumulusURL}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: CMX Dir: {CumulusDir}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: UtilsDir: {CumulusUtilsDir}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: remotefile: {remotefile}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: requestname: {Dir}/{remotefile}", TraceLevel.Info );

                    try
                    {
                        FtpStatus flag;

                        flag = clientFluentFTP.UploadFile( localfile, requestname, FtpRemoteExists.Overwrite, false, FtpVerify.Throw );

                        if ( flag != FtpStatus.Success )
                        {
                            Sup.LogMessage( $"UploadFile Failed: status = {flag} (0 = failed)", TraceLevel.Error );
                        }
                    }
                    catch ( Exception e )
                    {
                        Sup.LogMessage( $"UploadFile ERROR: General Exception: {e.Message}", TraceLevel.Error );
                        if ( e.InnerException is not null ) Sup.LogMessage( $"UploadFile ERROR: Inner Exception: {e.InnerException}", TraceLevel.Error );
                        return false;
                    }

                    Sup.LogMessage( $"FTP/FTPS UploadFile: Done", TraceLevel.Info );

                }
                else if ( ProtocolUsed == FtpProtocols.SFTP )
                {
                    string requestname = Dir + "/" + remotefile;

                    Sup.LogMessage( $"Upload File values: URL: {CumulusURL}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: CMX Dir: {CumulusDir}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: UtilsDir: {CumulusUtilsDir}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: remotefile: {remotefile}", TraceLevel.Info );
                    Sup.LogMessage( $"Upload File values: requestname: {Dir}/{remotefile}", TraceLevel.Info );

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
                                    Sup.LogMessage( $"InetSupport SFTP: Connecting using password authentication", TraceLevel.Info );
                                }
                                else if ( SshftpAuthentication == "psk" )
                                {
                                    pskFile = new PrivateKeyFile( SshftpPskFile );
                                    connectionInfo = new ConnectionInfo( hostname, port, username, new PrivateKeyAuthenticationMethod( username, pskFile ) );
                                    Sup.LogMessage( $"InetSupport SFTP: Connecting using PSK authentication", TraceLevel.Info );
                                }
                                else if ( SshftpAuthentication == "password_psk" )
                                {
                                    pskFile = new PrivateKeyFile( SshftpPskFile );
                                    connectionInfo = new ConnectionInfo( hostname, port, username, new PasswordAuthenticationMethod( username, password ), new PrivateKeyAuthenticationMethod( username, pskFile ) );
                                    Sup.LogMessage( $"InetSupport SFTP: Connecting using password or PSK authentication", TraceLevel.Info );
                                }
                                else
                                {
                                    Sup.LogMessage( $"InetSupport SFTP: Invalid SshftpAuthentication specified [{SshftpAuthentication}]", TraceLevel.Error );
                                    FTPvalid = false;
                                    return false;
                                }

                                clientRenci = new SftpClient( connectionInfo );
                                clientRenci.ConnectionInfo.Timeout = TimeSpan.FromSeconds( 900 );

                                clientRenci.Connect();
                                clientRenci.OperationTimeout = TimeSpan.FromSeconds( 15 );

                                if ( !clientRenci.IsConnected )
                                {
                                    Sup.LogMessage( $"Upload SFTP: Connection error.", TraceLevel.Error );
                                    FTPvalid = false;
                                    return false;
                                }

                                Sup.LogMessage( $" InetSupport SFTP: Realtime SFTP connected", TraceLevel.Info );

                                // And finally upload
                                clientRenci.UploadFile( istream, requestname, true );
                            }
                        }
                        catch ( Exception e )
                        {
                            Sup.LogMessage( $"Upload SFTP: ERROR General Exception: {e.Message}", TraceLevel.Error );
                            if ( e.InnerException is not null ) Sup.LogMessage( $"UploadFile SFTP ERROR: Inner Exception: {e.InnerException}", TraceLevel.Error );
                            return false;
                        }
                    }

                    Sup.LogMessage( $"SFTP UploadFile: Done", TraceLevel.Info );

                }
                else if ( ProtocolUsed == FtpProtocols.PHP )
                {
                    string requestname = !string.IsNullOrEmpty( CumulusUtilsDir ) ? $"{CumulusUtilsDir}/{remotefile}" : remotefile;

                    // There are two methods to adjust to the system:
                    //   1) the number of concurrent uploads
                    //   2) the delay between retries and after successful upload to give the server some time to breathe
                    // The semaphore will take care of the concurrent uploads and the delay will be applied after each upload (successful or not) 
                    //

                    await uploadSemaphore.WaitAsync();

                    try
                    {
                        int retryCount = 0;

                        while ( !await clientPhp.UploadAsync( localfile: localfile, remotefile: requestname ) )
                        {
                            if ( ++retryCount > 1 ) return false; // Avoid infinite loop in case of a problem

                            Sup.LogMessage( $"PHP UploadFile: Failed for {localfile}, Delaying 1 second...", TraceLevel.Info );

                            await Task.Delay( 1000 ); // Fix this to 1 second to give the server some time to breathe and to avoid flooding the server with requestsin case of a problem.
                                                      // The delay after success will be applied as well but that one if configurable to avoid issues with err 429
                        }

                        Sup.LogMessage( $"PHP UploadFile: Success for {localfile}, pausing {delayMilliSeconds} millisecond...\n", TraceLevel.Info );
                        await Task.Delay( delayMilliSeconds );

                        return true;
                    }
                    finally
                    {
                        uploadSemaphore.Release();
                    }
                }
            }
            else // Upload == false
            {
                Sup.LogMessage( $"UploadFile Upload=false -> No file(s) uploaded.", TraceLevel.Info );
                return false;
            }

            return true;
        } // EndOf UploadFile

        #endregion

        #region DownloadSignatureFiles

        public void DownloadSignatureFiles()
        {
            Sup.LogDebugMessage( $"DownloadSignatureFiles: Starting" );

            string localDir = "utils/maps";

            string CumulusURL = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            string CumulusDir = Sup.GetCumulusIniValue( "FTP site", "Directory", "" );
            CumulusDir += "/maps";

            Sup.LogMessage( $"DownloadSignatureFiles: URL: {CumulusURL}", TraceLevel.Info );
            Sup.LogMessage( $"DownloadSignatureFiles: Dir: {CumulusDir}", TraceLevel.Info );

            string username = Crypto.DecryptString( Sup.GetCumulusIniValue( "FTP site", "Username", "" ), CUtils.CryptoKey );
            string password = Crypto.DecryptString( Sup.GetCumulusIniValue( "FTP site", "Password", "" ), CUtils.CryptoKey );
            string hostname = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            int port = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "Port", "21" ) );
            bool PassiveFTP = Sup.GetCumulusIniValue( "FTP site", "ActiveFTP", "" ).Equals( "0" );

            // Choose whatever I want as this may deviate from the general setup and depends on provider of the map hoster
            FtpProtocols ProtocolUsed = (FtpProtocols) Convert.ToInt32( "0" );
            FtpClient localFluentFTP = null;

            //
            // Now do the initialisation thing for the protocol selected.
            //

            if ( ProtocolUsed == FtpProtocols.FTP )
            {
                try
                {
                    localFluentFTP = new FtpClient
                    {
                        Host = hostname,
                        Port = port,
                        Credentials = new NetworkCredential( username, password ),
                    };

                    localFluentFTP.Config.EncryptionMode = FtpEncryptionMode.None;
                    localFluentFTP.Config.SslProtocols = SslProtocols.None;
                    localFluentFTP.Config.DataConnectionType = PassiveFTP ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.PORT;
                    localFluentFTP.Encoding = Encoding.UTF8;

                    localFluentFTP.Config.SocketKeepAlive = true;
                    localFluentFTP.Config.UploadDataType = FtpDataType.Binary;

                    localFluentFTP.Connect();
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"DownloadSignatureFiles: Exception on FTP connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"DownloadSignatureFiles: Failed FTP connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    return;
                }
            }
            else if ( ProtocolUsed == FtpProtocols.FTPS )
            {
                try
                {
                    localFluentFTP = new FtpClient
                    {
                        Host = hostname,
                        Port = port,
                        Credentials = new NetworkCredential( username, password )
                    };

                    localFluentFTP.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                    localFluentFTP.Config.SslProtocols = SslProtocols.None;
                    localFluentFTP.Config.DataConnectionType = PassiveFTP ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.PORT;
                    localFluentFTP.Encoding = Encoding.UTF8;

                    localFluentFTP.Config.SocketKeepAlive = true;
                    localFluentFTP.Config.ValidateAnyCertificate = true;
                    localFluentFTP.Config.UploadDataType = FtpDataType.Binary;

                    localFluentFTP.Connect();
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"DownloadSignatureFiles: Exception on FTPS connecting to {hostname}: {e.Message}", TraceLevel.Error );
                    Sup.LogMessage( $"DownloadSignatureFiles: Failed FTPS connecting to {hostname}. Files will not be transferred", TraceLevel.Error );
                    return;
                }
            }

            // 
            List<FtpResult> remoteFiles;

            try
            {
                remoteFiles = localFluentFTP.DownloadDirectory( localDir, CumulusDir );
                localFluentFTP.DeleteDirectory( CumulusDir, FtpListOption.AllFiles );
                localFluentFTP.CreateDirectory( CumulusDir, true );

                Sup.LogMessage( $"DownloadSignatureFiles: {remoteFiles.Count} Signature files successfully Downloaded to {localDir}", TraceLevel.Info );
            }
            catch ( Exception e )
            {
                Sup.LogMessage( $"DownloadSignatureFiles ERROR: General Exception: {e.Message}", TraceLevel.Error );
                if ( e.InnerException is not null ) Sup.LogMessage( $"DownloadSignatureFiles ERROR: Inner Exception: {e.InnerException}", TraceLevel.Error );
                return;
            }

            localFluentFTP?.Dispose();

            Sup.LogMessage( $"DownloadSignatureFiles: Done", TraceLevel.Info );

            return;
        } // EndOf DownloadSignatureFiles

        #endregion

        #region GET/POST

        public async Task<string> GetUrlDataAsync( Uri thisURL )
        {
            Sup.LogMessage( $"GetUrlData Start: URL - {thisURL} ", TraceLevel.Info );

            // Note: I use 'using' because it is easier and it gets only called for UserReports, MAps and yourweather.co.uk so 
            //       there is no risk - I don't see a risk - of socket exhaustion
            //
            using ( HttpClient GetClient = new HttpClient() )
            {
                try
                {
                    return await GetClient.GetStringAsync( thisURL );
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"GetUrlData : Exception - {e.Message}", TraceLevel.Error );
                    if ( e.InnerException is not null )
                        Sup.LogMessage( $"GetUrlData: Inner Exception: {e.InnerException}", TraceLevel.Error );
                    return "";
                }
            }
        } // EndOf GetUrlData


        public async Task<string> PostUrlDataAsync( Uri thisURL, string data )
        {
            string retval;

            Sup.LogMessage( $" PostUrlData Start: {thisURL} ", TraceLevel.Info );

            // Note: I use 'using' because it is easier and it gets only called for UserReports so 
            //       there is no risk - I don't see a risk - of socket exhaustion

            using ( HttpClient PostClient = new HttpClient() )
            {
                Sup.LogMessage( $"PostUrlData Calling PostAsync", TraceLevel.Info );

                try
                {
                    using ( StringContent requestData = new StringContent( data, Encoding.UTF8 ) )
                    {
                        using ( HttpResponseMessage response = await PostClient.PostAsync( thisURL, requestData ) )
                        {
                            if ( response.IsSuccessStatusCode )
                            {
                                retval = await response.Content.ReadAsStringAsync();
                                Sup.LogMessage( $"PostUrlData success response : {response.StatusCode} - {response.ReasonPhrase}", TraceLevel.Info );
                            }
                            else
                            {
                                Sup.LogMessage( $"PostUrlData : Error: {response.StatusCode} - {response.ReasonPhrase}", TraceLevel.Error );
                                retval = "";
                            }
                        } // End using response -> dispose
                    } // End using requestData -> dispose
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"PostUrlData : Exception - {e.Message}", TraceLevel.Error );
                    if ( e.InnerException is not null )
                        Sup.LogMessage( $"PostUrlData: Inner Exception: {e.InnerException}", TraceLevel.Error );
                    retval = "";
                }
            }

            return retval;
        }

        #endregion

        #region IDisposable

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                    clientFluentFTP?.Dispose();
                    clientRenci?.Dispose();
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
