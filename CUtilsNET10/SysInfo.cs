/*
 * SysInfo - Part of CumulusUtils
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CumulusUtils
{
    public class SysInfo : IDisposable
    {
        enum LinuxDialects : int { Alma, Alpine, Arch, CentOS, Debian, Fedora, Mint, openSUSE, Raspbian, RHEL, Rocky, Stream, SUSE, Ubuntu, WSL };

        private readonly CuSupport Sup;
        private readonly InetSupport Isup;
        private readonly CmxIPC thisIPC;

        private List<string> returnValues;

        private InfoFromCMX thisInfo;

        #region Constructor
        public SysInfo( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;

            thisIPC = new CmxIPC( Sup, Isup );
        }

        #endregion

        #region GenerateSystemStatusAsync

        public async Task GenerateSystemStatusAsync()
        {
            int StationType;

            Sup.LogTraceInfoMessage( "SystemStatus : starting" );

            thisInfo = await thisIPC.GetCMXInfoAsync();
            Sup.LogDebugMessage( $"CumulusMX Version: {thisInfo.version} build {thisInfo.build}" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.SysInfoOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                StringBuilder DeviceInfo = new StringBuilder();
                string tmp;

                StationType = Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "Type", "" ), CUtils.Inv );
                Sup.LogTraceInfoMessage( $" SystemStatus: Found device {StationType} {CuSupport.StationInUse( StationType )}" );

                of.WriteLine( $"<div style='margin:auto; text-align:left; width:{Sup.GetUtilsIniValue( "SysInfo", "ReportWidth", "700" )}px'><pre>" );

                // Just for recognition of where we are : start of Station Info
                of.WriteLine( $"Cumulus version: {thisInfo.version} (build: {thisInfo.build})" );
                of.WriteLine( $"Cumulus uptime: {thisInfo.ProgramUpTime}" );
                of.WriteLine( $"Weather station: {CuSupport.StationInUse( StationType )}" );

                of.WriteLine( "" );

                switch ( StationType )
                {
                    case 0:
                    case 1: // Vantage Pro or Pro2

                        DeviceInfo.AppendLine( "Total number of data packets received: <#DavisTotalPacketsReceived>" );
                        DeviceInfo.AppendLine( "Number of missed data packets: <#DavisTotalPacketsMissed>" );
                        DeviceInfo.AppendLine( "Number of times the console resynchronised with the transmitter: <#DavisNumberOfResynchs>" );
                        DeviceInfo.AppendLine( "Longest streak of consecutive packets received: <#DavisMaxInARow>" );
                        DeviceInfo.AppendLine( "Number of packets received with CRC errors: <#DavisNumCRCerrors>" );
                        DeviceInfo.AppendLine( "The console firmware version: <#DavisFirmwareVersion>" );
                        DeviceInfo.AppendLine( "The console battery condition in volts: <#battery> V" );
                        DeviceInfo.AppendLine( "The transmitter battery condition: <#txbattery>" );
                        DeviceInfo.AppendLine( "" );

                        break;

                    case 11: // WLL

                        string TxUsed = Sup.GetUtilsIniValue( "SysInfo", "Tx", "" );    // Comma separated string
                        TxUsed = CuSupport.StringRemoveWhiteSpace( TxUsed, "" );        // Replace any space with nothing (empty string)
                        string[] TxUsedArray = TxUsed.Split( GlobConst.CommaSeparator );

                        if ( TxUsedArray.Length < 1 )
                            DeviceInfo.AppendLine( "A value for the Transmitter used is needed - please set parameter Tx" );
                        else
                        {
                            DeviceInfo.AppendLine( "The WLL firmware version: <#DavisFirmwareVersion>" );
                            DeviceInfo.AppendLine( "The WLL battery condition in volts: <#battery> V" );
                            DeviceInfo.AppendLine( "WLL WifiRssi: <#DavisTxRssi tx=0>" );
                            DeviceInfo.AppendLine( "" );

                            foreach ( string Tx in TxUsedArray )
                            {
                                DeviceInfo.AppendLine( $"WLL Stats for channel {Tx}:" );
                                DeviceInfo.AppendLine( $"  WLL DavisReceptionPercent: <#DavisReceptionPercent tx={Tx}>" );
                                DeviceInfo.AppendLine( $"  WLL TxRssi: <#DavisTxRssi  tx={Tx}>" );
                                DeviceInfo.AppendLine( $"  Number of missed data packets: <#DavisTotalPacketsMissed tx={Tx}>" );
                                DeviceInfo.AppendLine( $"  Number of times the console resynchronised with the transmitter: <#DavisNumberOfResynchs tx={Tx}>" );
                                DeviceInfo.AppendLine( $"  Longest streak of consecutive packets received: <#DavisMaxInARow tx={Tx}>" );
                                DeviceInfo.AppendLine( $"  Number of packets received with CRC errors: <#DavisNumCRCerrors tx={Tx}>" );
                                DeviceInfo.AppendLine( $"  The transmitter battery condition: <#txbattery channel={Tx}>" );
                                DeviceInfo.AppendLine( "" );
                            }
                        }

                        break;

                    case 12: //Ecowitt API
                        DeviceInfo.AppendLine( "The Ecowitt Firmware Version: <#EcowittFirmwareVersion>" );
                        DeviceInfo.AppendLine( "The Ecowitt Reception Stats: <#EcowittReception>" );
                        DeviceInfo.AppendLine( "The Ecowitt Free Memory: <#StationFreeMemory>" );
                        DeviceInfo.AppendLine( $"Extra Station Info: {Sup.GetUtilsIniValue( "SysInfo", "ExtraStationInfo", "" )}" );
                        DeviceInfo.AppendLine( "" );
                        break;

                    case 14: //Ecowitt HTTP
                        DeviceInfo.AppendLine( "The Ecowitt Firmware Version: <#EcowittFirmwareVersion>" );
                        DeviceInfo.AppendLine( "The Ecowitt Reception Stats: <#EcowittReception>" );
                        DeviceInfo.AppendLine( "The Ecowitt Free Memory: <#StationFreeMemory>" );
                        DeviceInfo.AppendLine( "The Ecowitt GW Uptime: <#StationRuntime>" );
                        DeviceInfo.AppendLine( $"Extra Station Info: {Sup.GetUtilsIniValue( "SysInfo", "ExtraStationInfo", "" )}" );
                        DeviceInfo.AppendLine( "" );
                        break;

                    //case 20:
                    //break;

                    default:
                        //DeviceInfo.AppendLine( "Device type: <#stationtype>" );
                        DeviceInfo.AppendLine( $"Extra Station Info: {Sup.GetUtilsIniValue( "SysInfo", "ExtraStationInfo", "" )}" );
                        DeviceInfo.AppendLine( "" );
                        break;
                } // End Switch for device stats if any

                tmp = await thisIPC.ReplaceWebtagsPostAsync( DeviceInfo.ToString() );
                of.WriteLine( tmp );

                // Now do the OS dependent stuff
                //
                if ( Environment.OSVersion.Platform.Equals( PlatformID.Unix ) )
                {
                    // Maybe MacOS should be implemented as a Linux Dialect...  wait for somebody to really use MacOS
                    if ( IsRunningOnMac() ) { Sup.LogDebugMessage( "SystemStatus : DoingMacOS" ); DoingMacOS( of ); }
                    else
                    {
                        Sup.LogDebugMessage( "SystemStatus : DoingUnix" );
                        DoingUnix( of );
                    }
                }
                else if ( Environment.OSVersion.Platform.Equals( PlatformID.Win32NT ) )
                {
                    Sup.LogDebugMessage( "SystemStatus : DoingWindows" );
                    DoingWindows( of );
                }
                else
                {
                    //other OSs not implemented yet
                    Sup.LogDebugMessage( $" SystemStatus : System value = {Environment.OSVersion.Platform}" );
                    Sup.LogDebugMessage( $" Please notify and ask for implementation. Now exiting." );
                    of.WriteLine( "Not implemented. Please ask for implementation." );
                    return;
                }

                of.WriteLine( "</pre></div>" );
                of.WriteLine( $"<div style ='width:100%; margin-left: auto; margin-right: auto; text-align: center; font-size: 12px;'>" +
                                      $"{CuSupport.FormattedVersion()} - {CuSupport.Copyright()} </div>" );
            } // End using of

            return;
        }

        #endregion

        #region Doing Windows
        private void DoingWindows( StreamWriter of )
        {
            Sup.LogTraceInfoMessage( "SystemStatus : DoingWindows Start" );

            string[] StringLinesToSkip = Sup.GetUtilsIniValue( "SysInfo", "SystemInfoLinesToSkip", "" ).Split( GlobConst.CommaSeparator );
            int[] LinesToSkip = new int[ StringLinesToSkip.Length ];

            for ( int i = 0; i < LinesToSkip.Length; i++ )
                try
                {
                    LinesToSkip[ i ] = Convert.ToInt32( StringLinesToSkip[ i ], CUtils.Inv );
                }
                catch
                {
                    // Do Nothing, ignore the line and take the next
                }

            of.WriteLine( "Windows" );
            of.WriteLine( "" );

            try
            {
                int skip = 0;

                StartProcess( "systeminfo", "" );

                for ( int i = 0; i < returnValues.Count; i++ )
                {
                    string line = returnValues[ i ];

                    if ( line.Contains( "pagefile.sys" ) ) break;               // Stop when pagfile.sys is found

                    if ( skip < LinesToSkip.Length && LinesToSkip[ skip ] == i )  // Skip the lines the user wants to skip
                    {
                        skip++;
                        continue;
                    }

                    of.WriteLine( $"{line}" );                                  // Write the line to the SysInfo output
                }
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"SystemStatus : DoingWindows Exception {e.Message}" );

                of.WriteLine( "Device: Unknown - stopping here." );
            }

            return;
        }

        #endregion

        #region Doing Unix
        private void DoingUnix( StreamWriter of )
        {
            Sup.LogTraceInfoMessage( "SystemStatus : DoingUnix Start" );

            LinuxDialects thisDialect = GetLinuxDialect();

            of.WriteLine( "Linux/Unix" );
            of.WriteLine( "" );

            try
            {
                switch ( thisDialect )
                {
                    case LinuxDialects.openSUSE:
                        StartProcess( "uptime", "" );
                        if ( !string.IsNullOrEmpty( returnValues[ 0 ] ) ) of.WriteLine( $"System uptime: {returnValues[ 0 ].Substring( 13 )}" );
                        break;
                    default:
                        StartProcess( "uptime", "-p" );
                        if ( !string.IsNullOrEmpty( returnValues[ 0 ] ) ) of.WriteLine( $"System uptime: {returnValues[ 0 ].Substring( 3 )}" );
                        break;
                }
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"System uptime: Unknown exception: {e.Message}" );
            }

            try
            {
                switch ( thisDialect )
                {
                    case LinuxDialects.openSUSE:
                        StartProcess( "/usr/sbin/lshw", "-quiet -class system" );
                        break;
                    default:
                        StartProcess( "lshw", "-quiet -class system" );
                        break;
                }

                if ( !string.IsNullOrEmpty( returnValues[ 2 ] ) ) of.WriteLine( $"System: {returnValues[ 2 ].Remove( 0, "    product: ".Length )}" );
                if ( !string.IsNullOrEmpty( returnValues[ 1 ] ) ) of.WriteLine( $"Processor: {returnValues[ 1 ].Remove( 0, "    description: ".Length )}" );
                of.WriteLine( $"Nr of processors: {thisInfo.CpuCount}" );
                of.WriteLine( $"Processor Temperature: {thisInfo.CPUTemp} °C" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"System/Processor: Unknown (exception) - {e.Message}" );
                Sup.LogDebugMessage( "Please install lshw (apt-get install lshw)" );
            }

            // Linux 4.19.58-v7+ armv7l
            try
            {
                StartProcess( "uname", "-s -r -m" );
                of.WriteLine( $"OS: {returnValues[ 0 ]}" );

                StartProcess( "lsb_release", "-a" );
                foreach ( string line in returnValues )
                    if ( line.StartsWith( "Description", CUtils.Cmp ) ) of.WriteLine( CuSupport.StringRemoveWhiteSpace( line, " " ) );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"OS: Unknown exception - {e.Message}" );
            }

            try
            {
                StartProcess( "dotnet", "--list-runtimes" );

                foreach ( string line in returnValues ) of.WriteLine( $"{line}" );
                of.WriteLine( "" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"dotnet: Unknown, most likely it is not installed - {e.Message}" );
                throw;
            }

            try
            {
                StartProcess( "free", "-m" );
                of.WriteLine( "Memory info:" );
                foreach ( string line in returnValues ) of.WriteLine( line );
                of.WriteLine( "" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Memory: Unknown error - {e.Message}" );
            }

            try
            {
                StartProcess( "df", "-T -text4 -tvfat -h" );
                foreach ( string line in returnValues ) of.WriteLine( line );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Disk info: Unknown error - {e.Message}" );
            }

            return;
        }

        #endregion

        #region Doing MacOS
        private void DoingMacOS( StreamWriter of )
        {
            Sup.LogTraceInfoMessage( "SystemStatus : DoingMacOS Start" );

            of.WriteLine( "MacOS" );
            of.WriteLine( "" );

            try
            {
                StartProcess( "uptime", "" );

                if ( !string.IsNullOrEmpty( returnValues[ 0 ] ) )
                {
                    of.WriteLine( $"System uptime: {returnValues[ 0 ].Substring( 3 )}" );
                }
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"System uptime: Unknown exception: {e.Message}" );
            }

            try
            {
                StartProcess( "lshw", "-quiet -class system" );

                if ( !string.IsNullOrEmpty( returnValues[ 2 ] ) )
                    of.WriteLine( $"System: {returnValues[ 2 ].Remove( 0, "    product: ".Length )}" );
                if ( !string.IsNullOrEmpty( returnValues[ 1 ] ) )
                    of.WriteLine( $"Processor: {returnValues[ 1 ].Remove( 0, "    description: ".Length )}" );
                of.WriteLine( $"Nr of processors: {thisInfo.CpuCount}" );
                of.WriteLine( $"Processor Temperature: {thisInfo.CPUTemp} °C" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"System/Processor: Unknown (exception) - {e.Message}" );
                Sup.LogDebugMessage( "Please install lshw (apt-get install lshw)" );
            }

            // Linux 4.19.58-v7+ armv7l
            try
            {
                StartProcess( "uname", "-s -r -m" );
                of.WriteLine( $"OS: {returnValues[ 0 ]}" );

                StartProcess( "lsb_release", "-a" );
                foreach ( string line in returnValues )
                    if ( line.StartsWith( "Description", CUtils.Cmp ) )
                        of.WriteLine( CuSupport.StringRemoveWhiteSpace( line, " " ) );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"OS: Unknown exception - {e.Message}" );
            }

            try
            {
                StartProcess( "mono", "-V" );
                of.WriteLine( $"{returnValues[ 0 ]}" );
                of.WriteLine( "" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Mono: Unknown, most likely it is not installed - {e.Message}" );
            }

            try
            {
                StartProcess( "free", "-m" );
                of.WriteLine( "Memory info:" );
                foreach ( string line in returnValues ) of.WriteLine( line );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Memory: Unknown error - {e.Message}" );
            }

            of.WriteLine( "" );

            try
            {
                StartProcess( "df", "-T -text4 -tvfat -h" );
                foreach ( string line in returnValues ) of.WriteLine( line );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Disk info: Unknown error - {e.Message}" );
            }

            return;
        }

        #endregion

        #region StartProcess
        private void StartProcess( string command, string parameters )
        {
            Sup.LogTraceInfoMessage( "StartProcess " + command + " " + parameters );

            returnValues = new List<string>(); // let the other ones be handle by the GC, not good practice, but OK.

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = parameters,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // suppress any output on the commandline
                    CreateNoWindow = true
                }
            };

            process.Start();

            while ( !process.StandardOutput.EndOfStream )
            {
                string? line = process.StandardOutput.ReadLine();
                if ( line != null )
                {
                    returnValues.Add( line );
                    Sup.LogTraceInfoMessage( "StartProcess " + command + ": output=" + line );
                }
            }

            process.WaitForExit();
            process.Dispose();
        }

        #endregion

        #region supporting functions
        private LinuxDialects GetLinuxDialect()
        {
            // NOTE: this array of strings must correspond 1to1 with the enum
            string[] LinuxDialectsStr = new string[] { "Alma", "Alpine", "Arch", "CentOS", "Debian", "Fedora", "Mint", "openSUSE", "Raspbian", "RHEL", "Rocky", "Stream", "SUSE", "Ubuntu", "WSL" };

            LinuxDialects thisDialect = LinuxDialects.Raspbian;

            try
            {
                // Output of 'cat /etc/os-release':
                //  PRETTY_NAME="Raspbian GNU/Linux 11 (bullseye)"
                //  NAME = "Raspbian GNU/Linux"
                //  VERSION_ID = "11"
                //  VERSION = "11 (bullseye)"
                //  VERSION_CODENAME = bullseye
                //  ID = raspbian
                //  ID_LIKE = debian
                //  HOME_URL = "http://www.raspbian.org/"
                //  SUPPORT_URL = "http://www.raspbian.org/RaspbianForums"
                //  BUG_REPORT_URL = "http://www.raspbian.org/RaspbianBugs"

                StartProcess( "cat", "/etc/os-release" );

                if ( !string.IsNullOrEmpty( returnValues[ 0 ] ) )
                {
                    foreach ( string value in returnValues )
                    {
                        if ( value.Contains( "NAME=" ) )
                        {
                            Sup.LogTraceInfoMessage( $"Found Linux NAME: {value}" );

                            foreach ( string value2 in LinuxDialectsStr )
                            {
                                if ( value.Contains( value2 ) ) // Check for all possibilities
                                {
                                    thisDialect = (LinuxDialects) Enum.Parse( typeof( LinuxDialects ), value2, true ); // and make it an enum
                                    Sup.LogTraceInfoMessage( $"Using Linux dialect: {thisDialect}" );

                                    break; // get out of this loop
                                } // if
                            } // foreach

                            break; // and get out of the outer loop
                        } // if
                    } // foreach
                }
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"{e.Message}" );
                Sup.LogTraceErrorMessage( $"Can't determine Linux dialect, using Raspbian" );
                thisDialect = LinuxDialects.Raspbian;
            }

            return thisDialect;
        }

        [DllImport( "libc" )]
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "<Pending>" )]
        static extern int uname( IntPtr buf );

        static bool IsRunningOnMac()
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal( 8192 );
                // This is a hacktastic way of getting sysname from uname ()
                if ( uname( buf ) == 0 )
                {
                    string os = Marshal.PtrToStringAnsi( buf );
                    if ( os == "Darwin" )
                        return true;
                }
            }
            catch
            {
            }
            finally
            {
                if ( buf != IntPtr.Zero )
                    Marshal.FreeHGlobal( buf );
            }
            return false;
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
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~SysInfo()
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
    }
}