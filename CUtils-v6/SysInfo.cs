/*
 * SysInfo - Part of CumulusUtils
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
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CumulusUtils
{
    internal class SysInfo : IDisposable
    {
        private readonly CuSupport Sup;
        private readonly InetSupport Isup;
        private readonly CmxIPC thisIPC;

        private List<string> returnValues;

        private InfoFromCMX thisInfo;

        public SysInfo( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;

            thisIPC = new CmxIPC( Sup, Isup );
        }

        public async Task GenerateSystemStatusAsync()
        {
            int DeviceType;

            Sup.LogDebugMessage( "SystemStatus : starting" );

            thisInfo = await thisIPC.GetCMXInfoAsync();
            Sup.LogDebugMessage( $"CumulusMX Version: {thisInfo.Version} build {thisInfo.Build}" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.SysInfoOutputFilename}", false, Encoding.UTF8 ) )
            {
                StringBuilder DeviceInfo = new StringBuilder();
                string tmp;

                DeviceType = Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "Type", "1" ), CultureInfo.InvariantCulture );
                //DeviceType = 11;
                Sup.LogTraceInfoMessage( $" SystemStatus: Found device {DeviceType} {CuSupport.StationInUse( DeviceType )}" );

                of.WriteLine( $"<div style='margin:auto; text-align:left; width:{Sup.GetUtilsIniValue( "SysInfo", "ReportWidth", "700" )}px'><pre>" );

                // Just for recognition of where we are : start of Station Info
                of.WriteLine( $"Cumulus version: {thisInfo.Version} (build: {thisInfo.Build})" );
                of.WriteLine( $"Cumulus uptime: {thisInfo.ProgramUpTime}" );
                of.WriteLine( $"Weather station: {CuSupport.StationInUse( DeviceType )}" );
                of.WriteLine( "" );

                switch ( DeviceType )
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

                        //using( StreamWriter rt = new StreamWriter( $"TagsToReplace.txt", false, Encoding.UTF8 ) )
                        //{
                        //    rt.WriteLine( thisIPC.CmxBaseURL + "\n" );
                        //    rt.WriteLine( DeviceInfo.ToString() );
                        //    rt.WriteLine("---------------");
                        tmp = await thisIPC.ReplaceWebtagsPostAsync( DeviceInfo.ToString() );
                        //    rt.WriteLine( tmp );
                        //}

                        of.WriteLine( tmp );
                        break;

                    case 11: // WLL

                        string TxUsed = Sup.GetUtilsIniValue( "SysInfo", "Tx", "" );    // Comma separated string
                        TxUsed = CuSupport.StringRemoveWhiteSpace( TxUsed, "" );        // Replace any space with nothing (empty string)
                        string[] TxUsedArray = TxUsed.Split( ',' );

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

                        //using ( StreamWriter rt = new StreamWriter( $"TagsToReplace.txt", false, Encoding.UTF8 ) )
                        //{
                        //    rt.WriteLine( thisIPC.CmxBaseURL + "\n");
                        //    rt.WriteLine( DeviceInfo.ToString() );
                        //    rt.WriteLine( "---------------" );
                        tmp = await thisIPC.ReplaceWebtagsPostAsync( DeviceInfo.ToString() );
                        //    rt.WriteLine( tmp );
                        //}

                        of.WriteLine( tmp );
                        break;

                    default:
                        of.WriteLine( $"Extra Station Info: {Sup.GetUtilsIniValue( "SysInfo", "ExtraStationInfo", "" )}" );
                        of.WriteLine( "" );
                        break;
                } // End Switch for device stats if any

                // Now do the OS dependent stuff
                //
                if ( Environment.OSVersion.Platform.Equals( PlatformID.Unix ) )
                {
                    Sup.LogDebugMessage( "SystemStatus : DoingUnix" );
                    DoingUnix( of );
                }
                else if ( Environment.OSVersion.Platform.Equals( PlatformID.MacOSX ) )
                {
                    Sup.LogDebugMessage( "SystemStatus : DoingMacOS" );
                    Sup.LogDebugMessage( "Please, if you want the info generated by cumulusutils," );
                    Sup.LogDebugMessage( "send me the commands and the corresponding outputs." );

                    Console.WriteLine( DateTime.Now.ToString( CultureInfo.CurrentCulture ) + " Please, if you want the info generated by cumulusutils," );
                    Console.WriteLine( DateTime.Now.ToString( CultureInfo.CurrentCulture ) + " send me the commands and the corresponding outputs." );
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
                    Sup.LogDebugMessage( $" Please notify programmer and ask for implementation. Now exiting." );
                    of.WriteLine( "Not implemented. Please ask for implementation." );
                    return;
                }

                of.WriteLine( "</pre></div>" );
                of.WriteLine( $"<div style ='width:100%; margin-left: auto; margin-right: auto; text-align: center; font-size: 12px;'>" +
                                      $"{CuSupport.FormattedVersion()} - {CuSupport.Copyright()} </div>" );
            } // End using of

            return;
        }

        private void DoingWindows( StreamWriter of )
        {
            Sup.LogTraceInfoMessage( "SystemStatus : DoingWindows Start" );

            string[] StringLinesToSkip = Sup.GetUtilsIniValue( "SysInfo", "SystemInfoLinesToSkip", "" ).Split( ',' );
            int[] LinesToSkip = new int[ StringLinesToSkip.Length ];

            for ( int i = 0; i < LinesToSkip.Length; i++ )
                if ( !string.IsNullOrEmpty( StringLinesToSkip[ i ] ) )
                    LinesToSkip[ i ] = Convert.ToInt32( StringLinesToSkip[ i ], CultureInfo.InvariantCulture ) - 1;

            of.WriteLine( "Windows" );
            of.WriteLine( "" );

            try
            {
                int count = 0;

                StartProcess( "systeminfo", "" );
                for ( int i = 0; i < returnValues.Count; i++ )
                {
                    string line = returnValues[ i ];
                    if ( String.IsNullOrEmpty( line ) )
                        continue;
                    if ( count < LinesToSkip.Length && i == LinesToSkip[ count ] ) { count++; continue; }
                    if ( line.Contains( "pagefile.sys" ) )
                        break;
                    of.WriteLine( $"{line}" );

                }
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"SystemStatus : DoingWindows Exception {e.Message}" );

                of.WriteLine( "Device: Unknown - stopping here." );
                of.WriteLine( "Device: Unknown - Please inform programmer." );
                throw;
            }

            return;
        }

        private void DoingUnix( StreamWriter of )
        {
            Sup.LogTraceInfoMessage( "SystemStatus : DoingUnix Start" );

            of.WriteLine( "Linux/Unix" );
            of.WriteLine( "" );

            try
            {
                StartProcess( "uptime", "-p" );

                if ( !String.IsNullOrEmpty( returnValues[ 0 ] ) )
                {
                    // Just for recognition of where we are : start of Station Info
                    of.WriteLine( $"System uptime: {returnValues[ 0 ].Substring( 3 )}" );
                    of.WriteLine( "" );
                }
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"System uptime: Unknown exception: {e.Message}" );
                throw;
            }

            try
            {
                StartProcess( "lshw", "-quiet -class system" );

                if ( !string.IsNullOrEmpty( returnValues[ 2 ] ) )
                    of.WriteLine( $"System: {returnValues[ 2 ].Remove( 0, "    product: ".Length )}" );
                if ( !string.IsNullOrEmpty( returnValues[ 1 ] ) )
                    of.WriteLine( $"Processor: {returnValues[ 1 ].Remove( 0, "    description: ".Length )}" );
                of.WriteLine( $"Nr of processors: {thisInfo.CpuCount}" );
                of.WriteLine( $"Processor Temperature: {thisInfo.CpuTemp} °C" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"System/Processor: Unknown (exception) - {e.Message}" );
                Sup.LogDebugMessage( "Please install lshw (apt-get install lshw)" );
                throw;
            }

            // Linux 4.19.58-v7+ armv7l
            try
            {
                StartProcess( "uname", "-s -r -m" );
                of.WriteLine( $"OS: {returnValues[ 0 ]}" );

                StartProcess( "lsb_release", "-a" );
                foreach ( string line in returnValues )
                    if ( line.StartsWith( "Description", StringComparison.OrdinalIgnoreCase ) )
                        of.WriteLine( CuSupport.StringRemoveWhiteSpace( line, " " ) );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"OS: Unknown exception - {e.Message}" );
                throw;
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
                throw;
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
                throw;
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
                throw;
            }

            return;
        }

        // When we are on linux, check if mono >= 6
        // Return true when version MONO is 6 or higher
        public bool CheckMonoVersion()
        {
            try
            {
                int tmp;

                StartProcess( "mono", "-V" );
                tmp = Convert.ToInt32( returnValues[ 0 ].Substring( 26, 1 ) ); // Skip "Mono JIT compiler version " and get the major version nr

                Sup.LogDebugMessage( $"CheckMonoVersion: detected MONO major version: {tmp} ({returnValues[ 0 ]})" );

                if ( tmp >= 6 )
                    return true;
                else
                    return false;
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Can't test MONO major version - {e.Message}" );
                return false;
            }
        } // CheckMonoVersion



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
                returnValues.Add( process.StandardOutput.ReadLine() );
                Sup.LogTraceInfoMessage( "StartProcess " + command + ": output=" + returnValues.Last() );
            }

            process.WaitForExit();
            process.Dispose();
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