/*
 * InetSupport - Part of CumulusUtils
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Helpers;
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

        bool FTPvalid;                         // Indication whether a connection could be made and filetransfer is possible.

        public bool IsIncrementalAllowed() => ProtocolUsed == FtpProtocols.PHP;

        #region Initialiser

        public InetSupport( CuSupport s )
        {
            Sup = s;

            Sup.LogDebugMessage( "InetSupport: Constructor start" );

            username = Sup.GetCumulusIniValue( "FTP site", "Username", "" );
            password = Sup.GetCumulusIniValue( "FTP site", "Password", "" );
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

                    // From CMX, take care of ECDSA ciphers not implemented in mono
                    try
                    {
#pragma warning disable CS0642 // Possible mistaken empty statement
                        using ( ECDsaCng ecdsa = new ECDsaCng() ) ;
#pragma warning restore CS0642 // Possible mistaken empty statement
                    }
                    catch ( NotImplementedException )
                    {
                        Sup.LogTraceInfoMessage( $"Upload SFTP: ECDSA Cipher not implemented." );
                        var algsToRemove = connectionInfo.HostKeyAlgorithms.Keys.Where( algName => algName.StartsWith( "ecdsa" ) ).ToArray();
                        foreach ( var algName in algsToRemove )
                            connectionInfo.HostKeyAlgorithms.Remove( algName );
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
            else if ( ProtocolUsed == FtpProtocols.PHP )
            {
                clientPhp = new InetPHP( Sup );
                FTPvalid = false; // Init needs to be done. Because of async needs to be done in Upload first time
            }
            else
            {
                Sup.LogTraceErrorMessage( $"InetSupport: Protocol not implemented {ProtocolUsed}. Files will not be transferred" );
                FTPvalid = false;
            }

            return;
        }

        #endregion

        #region UploadFile

        public async Task<bool> UploadFileAsync( string remotefile, string localfile )
        {
            // On Async FTP: https://social.msdn.microsoft.com/Forums/vstudio/en-US/994fa6e8-e345-4d10-97e6-e540bec0cb76/what-is-asynchronous-ftp?forum=csharpgeneral
            //     Read the first answer, I understand async FTP does not really have large effects and is only really important with UI interaction.
            //     So I leave this as is (and also all FTPs in the project)

            // Immediately return if something was wrong at contructor time
            if ( !FTPvalid && ProtocolUsed == FtpProtocols.PHP )
            {
                FTPvalid = await clientPhp.PhpInit();
            }

            if ( !FTPvalid )
            {
                Sup.LogTraceErrorMessage( $"UploadFile: Nothing uploaded because of connection error." );
                return false;
            }

            string URL = "";
            string Dir = "";

            //Sup.LogDebugMessage( $"UploadFile: Start {localfile} => {remotefile}" );

            // No reason to upload if there is  no file or destination
            if ( string.IsNullOrEmpty( remotefile ) || string.IsNullOrEmpty( localfile ) ) { Sup.LogTraceErrorMessage( $"UploadFile: Nothing uploaded either in or outfile are empty." ); return false; }

            bool Upload = Sup.GetUtilsIniValue( "FTP site", "DoUploadFTP", "false" ).ToLower() == "true";
            if ( !Upload ) { Sup.LogTraceInfoMessage( $"UploadFile: DoUploadFTP configured false => No Upload." ); return false; }      // No reason to do the whole procedure if we don't have to upload

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

                    Sup.LogTraceInfoMessage( $"Upload File values: URL: {CumulusURL}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: CMX Dir: {CumulusDir}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: UtilsDir: {CumulusUtilsDir}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: remotefile: {remotefile}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: requestname: {Dir}/{remotefile}" );

                    try
                    {
                        await clientFluentFTP.UploadFileAsync( localfile, requestname, FtpRemoteExists.Overwrite, false, FtpVerify.Throw );
                    }
                    catch ( Exception e )
                    {
                        Sup.LogTraceErrorMessage( $"UploadFile ERROR: General Exception: {e.Message}" );
                        if ( e.InnerException != null ) Sup.LogTraceErrorMessage( $"UploadFile ERROR: Inner Exception: {e.InnerException}" );
                        return false;
                    }

                    Sup.LogTraceInfoMessage( $"FTP/FTPS UploadFile: Done" );

                }
                else if ( ProtocolUsed == FtpProtocols.SFTP )
                {
                    string requestname = Dir + "/" + remotefile;

                    Sup.LogTraceInfoMessage( $"Upload File values: URL: {CumulusURL}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: CMX Dir: {CumulusDir}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: UtilsDir: {CumulusUtilsDir}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: remotefile: {remotefile}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: requestname: {Dir}/{remotefile}" );

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

                                // From CMX / Mark Crossley, take care of ECDSA ciphers not implemented in mono
                                try
                                {
#pragma warning disable CS0642 // Possible mistaken empty statement
                                    using ( var ecdsa = new System.Security.Cryptography.ECDsaCng() ) ;
#pragma warning restore CS0642 // Possible mistaken empty statement
                                }
                                catch ( NotImplementedException )
                                {
                                    Sup.LogTraceInfoMessage( $"Upload SFTP: ECDSA Cipher not implemented." );
                                    var algsToRemove = connectionInfo.HostKeyAlgorithms.Keys.Where( algName => algName.StartsWith( "ecdsa" ) ).ToArray();
                                    foreach ( var algName in algsToRemove )
                                        connectionInfo.HostKeyAlgorithms.Remove( algName );
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
                        catch ( Exception e )
                        {
                            Sup.LogTraceErrorMessage( $"Upload SFTP: ERROR General Exception: {e.Message}" );
                            if ( e.InnerException != null ) Sup.LogTraceErrorMessage( $"UploadFile SFTP ERROR: Inner Exception: {e.InnerException}" );
                            return false;
                        }
                    }

                    Sup.LogTraceInfoMessage( $"SFTP UploadFile: Done" );

                } // if else on basis of protocol
                else if ( ProtocolUsed == FtpProtocols.PHP )
                {
                    // We can't use the CMX Upload directory definition as that is used for the signature files FTP
                    string requestname;

                    if ( !string.IsNullOrEmpty( CumulusUtilsDir ) )
                        requestname = $"{CumulusUtilsDir}/{remotefile}";
                    else
                        requestname = remotefile;

                    Sup.LogTraceInfoMessage( $"Upload File values: localfile: {localfile}" );
                    Sup.LogTraceInfoMessage( $"Upload File values: remotefile: {requestname}" );

                    if ( !await clientPhp.UploadAsync( localfile: localfile, remotefile: requestname ) )
                    {
                        // The send apparently failed so return false
                        Sup.LogTraceInfoMessage( $"PHP UploadFile: Failed" );
                        return false;
                    }

                    Sup.LogTraceInfoMessage( $"PHP UploadFile: Success" );
                }
            }
            else // Upload == false
            {
                Sup.LogTraceInfoMessage( $"UploadFile Upload=false -> No file(s) uploaded." );
                return false;
            }

            return true;
        } // EndOf UploadFile

        #endregion

        #region DownloadSignatureFiles

        public void DownloadSignatureFiles()
        {
            Sup.LogDebugMessage( $"DownloadSignatureFiles: Start" );

            string localDir = "utils/maps";

            string CumulusURL = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            string CumulusDir = Sup.GetCumulusIniValue( "FTP site", "Directory", "" );
            CumulusDir += "/maps";

            Sup.LogTraceInfoMessage( $"DownloadSignatureFiles: URL: {CumulusURL}" );
            Sup.LogTraceInfoMessage( $"DownloadSignatureFiles: Dir: {CumulusDir}" );

            string username = Sup.GetCumulusIniValue( "FTP site", "Username", "" );
            string password = Sup.GetCumulusIniValue( "FTP site", "Password", "" );
            string hostname = Sup.GetCumulusIniValue( "FTP site", "Host", "" );
            int port = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "Port", "21" ) );
            bool PassiveFTP = Sup.GetCumulusIniValue( "FTP site", "ActiveFTP", "" ).Equals( "0" );

            // Choose whatever I want as this may deviate from the general setup and depends on provider of the map hoster
            FtpProtocols ProtocolUsed = (FtpProtocols) Convert.ToInt32( "0" );
            FtpClient localFluentFTP = null;

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
                    localFluentFTP = new FtpClient( hostname,
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

                    localFluentFTP.Connect();
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"DownloadSignatureFiles: Exception on FTP connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"DownloadSignatureFiles: Failed FTP connecting to {hostname}. Files will not be transferred" );
                    return;
                }
            }
            else if ( ProtocolUsed == FtpProtocols.FTPS )
            {
                try
                {
                    localFluentFTP = new FtpClient( hostname,
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

                    localFluentFTP.Connect();
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"DownloadSignatureFiles: Exception on FTPS connecting to {hostname}: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"DownloadSignatureFiles: Failed FTPS connecting to {hostname}. Files will not be transferred" );
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

                Sup.LogTraceInfoMessage( $"DownloadSignatureFiles: {remoteFiles.Count} Signature files successfully Downloaded to {localDir}" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"DownloadSignatureFiles ERROR: General Exception: {e.Message}" );
                if ( e.InnerException != null ) Sup.LogTraceErrorMessage( $"DownloadSignatureFiles ERROR: Inner Exception: {e.InnerException}" );
                return;
            }

            localFluentFTP?.Dispose();

            Sup.LogTraceInfoMessage( $"DownloadSignatureFiles: Done" );

            return;
        } // EndOf DownloadSignatureFiles

        #endregion

        #region GET/POST

        public async Task<string> GetUrlDataAsync( Uri thisURL )
        {
            Sup.LogTraceInfoMessage( $"GetUrlData Start: URL - {thisURL} " );

            // Note: I use 'using' because it is easier and it gets only called for UserReports, MAps and yourweather.co.uk so 
            //       there is no risk - I don't see a risk - of socket exhaustion
            //
            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ( sender, cert, chain, sslPolicyErrors ) => { return true; }
            };

            using ( HttpClient GetClient = new HttpClient( clientHandler, true ) )
            {
                try
                {
                    return await GetClient.GetStringAsync( thisURL );
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"GetUrlData : Exception - {e.Message}" );
                    if ( e.InnerException != null )
                        Sup.LogTraceErrorMessage( $"GetUrlData: Inner Exception: {e.InnerException}" );
                    return "";
                }
            }
        } // EndOf GetUrlData


        public async Task<string> PostUrlDataAsync( Uri thisURL, string data )
        {
            string retval;

            Sup.LogTraceInfoMessage( $" PostUrlData Start: {thisURL} " );

            // Note: I use 'using' because it is easier and it gets only called for UserReports so 
            //       there is no risk - I don't see a risk - of socket exhaustion
            // Prevent issues with OpenSSL so bypass the certificate for the CGI
            // https://stackoverflow.com/questions/52939211/the-ssl-connection-could-not-be-established

            // This does no longer seem necessary:
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
                                Sup.LogTraceInfoMessage( $"PostUrlData success response : {response.StatusCode} - {response.ReasonPhrase}" );
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"PostUrlData : Error: {response.StatusCode} - {response.ReasonPhrase}" );
                                retval = "";
                            }
                        } // End using response -> dispose
                    } // End using requestData -> dispose
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"PostUrlData : Exception - {e.Message}" );
                    if ( e.InnerException != null )
                        Sup.LogTraceErrorMessage( $"PostUrlData: Inner Exception: {e.InnerException}" );
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
