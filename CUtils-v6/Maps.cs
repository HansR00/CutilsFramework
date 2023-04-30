/*
 * Maps - Part of CumulusUtils
 *
 * © Copyright 2019-2023 Hans Rottier <hans.rottier@gmail.com>
 *
 * The code of CumulusUtils is public domain and distributed under the  
 * Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI .NET Framework 4.8
 *              Initial release: pwsFWI                 (version 1.0)
 *                               Website Generator      (version 3.0)
 *                               ChartsCompiler         (version 5.0)
 *                               Maintenance releases   (version 6.x)
 *              Startdate : 16 november 2021 start of conversion to .NET 5, 6 and 7
 *              
 * Environment: Raspberry Pi 3B+ and up
 *              Raspberry Pi OS  for testruns
 *              C# / Visual Studio / Windows for development
 * 
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CumulusUtils
{
    public class Maps : IDisposable
    {
        private readonly CuSupport Sup;

        public Maps( CuSupport s )
        {
            Sup = s;

            return;
        } // Constructor

        #region MapsOn

        public async Task<string> MapsOn()
        {
            Sup.LogDebugMessage( "MapsOn: Start" );

            string FileToSend = $"MapsOn-{RandomGenerator.RandomString( 10, true )}.xml";

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{FileToSend}", false, Encoding.UTF8 ) )
            {
                string Name = Sup.GetCumulusIniValue( "Station", "LocName", "" );
                string Description = Sup.GetCumulusIniValue( "Station", "LocDesc", "" );
                string Website = Sup.GetUtilsIniValue( "Maps", "Website", "" );
                string Latitude = Sup.GetCumulusIniValue( "Station", "Latitude", "" );
                string Longitude = Sup.GetCumulusIniValue( "Station", "Longitude", "" );

                if ( !Website.StartsWith( "http:", CUtils.Cmp ) &&
                    !Website.StartsWith( "https:", CUtils.Cmp ) &&
                    !string.IsNullOrEmpty( Website ) )
                {
                    Website = "http://" + Website;
                }

                Sup.LogTraceInfoMessage( $" MapsOn: Adding Station: {Name}" );

                if ( !string.IsNullOrEmpty( Name ) && /* !string.IsNullOrEmpty( Website ) && */ !string.IsNullOrEmpty( Latitude ) && !string.IsNullOrEmpty( Longitude ) )
                {
                    try
                    {
                        XElement root = new XElement( "Station",
                                          new XElement( "Name", Name ),
                                          new XElement( "Description", Description ),
                                          new XElement( "Website", Website ),
                                          new XElement( "Latitude", Latitude ),
                                          new XElement( "Longitude", Longitude ),
                                          new XElement( "Date", DateTime.UtcNow.ToString( "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture ) )
                                        );

                        // And save the file and write back to the central storage
                        root.Save( of );
                    }
                    catch ( Exception e )
                    {
                        Sup.LogTraceErrorMessage( $"MapsOn: XElement Exception: {e.Message}" );
                        Sup.LogTraceErrorMessage( $"MapsOn failed: continuing" );
                    }
                }
                else
                {
                    Sup.LogTraceInfoMessage( "Maps->MapsOn: Not enough info for Entry!!" );
                    Sup.LogTraceInfoMessage( $"Maps->MapsOn: Name: {Name}" );
                    Sup.LogTraceInfoMessage( $"Maps->MapsOn: Description:{Description}" );
                    Sup.LogTraceInfoMessage( $"Maps->MapsOn: Website: {Website}" );
                    Sup.LogTraceInfoMessage( $"Maps->MapsOn: Latitude: {Latitude}" );
                    Sup.LogTraceInfoMessage( $"Maps->MapsOn: Longitude: {Longitude}" );
                    Sup.LogTraceInfoMessage( $"Maps->MapsOn: Date (UTC): {DateTime.UtcNow.ToString( "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture )}" );

                    // An exit is made here. This is especially disturbing when operating with the website generator
                    // I do this to oblige the user to actually fill in correct data for his website (which he needs for use of CumulusUtils
                    // I do not physically check the site because it may not yet be online, but that could be a next step.

                    Sup.LogDebugMessage( "Maps->MapsOn: Name, Website, Latitude and Longitude are compulsory so exit here!!" );
                    Sup.LogDebugMessage( "See forum Post 'For New Users' (https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=18226)." );
                    Environment.Exit( 0 );
                }
            }

            // Check DoneToday and if last time was yesterday send it again
            //
            bool DoMapsOn;
            string retval;
            DateTime DoneToday;

            string tmp = Sup.GetUtilsIniValue( "Maps", "DoneToday", $"{DateTime.Now.AddDays( -1 ):s}" );

            try
            {
                // This converts the last date string to a DateTime, value is in DoneToday, function returns true
                if ( DateTime.TryParse( tmp, out DoneToday ) )
                {
                    Sup.LogTraceInfoMessage( $"MapsOn: Before testing DoneToday after parsing: {DoneToday} " );
                    DoMapsOn = !Sup.DateIsToday( DoneToday );
                }
                else DoMapsOn = true;
            }
            catch
            {
                DoneToday = DateTime.Now;
                DoMapsOn = true;
            }

            if ( DoMapsOn )
            {
                Sup.LogTraceInfoMessage( $"MapsOn: Must send signature: {DoneToday:s} / Setting DoneToday to now." );
                Sup.SetUtilsIniValue( "Maps", "DoneToday", $"{DateTime.Now:s}" );

                string thisContent = $"filename#{FileToSend}&";
                thisContent += "filecontent#" + File.ReadAllText( Sup.PathUtils + FileToSend, Encoding.UTF8 );
                retval = await CUtils.Isup.PostUrlDataAsync( new Uri( "https://meteo-wagenborgen.nl/cgi-bin/receive.pl" ), thisContent );
                Sup.LogTraceInfoMessage( $"MapsOn : Success" );
            }
            else retval = $"MapsOn: Must NOT send signature, has been done already : {DoneToday:s}";

            if ( File.Exists( $"{Sup.PathUtils}{FileToSend}" ) ) File.Delete( $"{Sup.PathUtils}{FileToSend}" );

            return retval;
        }

        #endregion

        #region MapsOff
        /*
                public void MapsOff()
                {
                    // Note MapsOff is actually no longer being used. If everything works fine, let's remove it and  only rely on the timeout
                    Sup.LogDebugMessage( "MapsOff: Start" );

                    string FileToSend = $"MapsOff-{RandomGenerator.RandomString( 10, true )}.txt";

                    using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{FileToSend}", false, Encoding.UTF8 ) )
                    {
                        string Name = Sup.GetCumulusIniValue( "Station", "LocName", "" );

                        Sup.LogTraceInfoMessage( $" MapsOff: Adding Station: {Name}" );
                        of.WriteLine( Name );
                    }

                    // This function is not used anymore so could be deleted.
                    // If we find another use then this is where the upload must be.

                    if ( File.Exists( $"{Sup.PathUtils}{FileToSend}" ) )
                        File.Delete( $"{Sup.PathUtils}{FileToSend}" );
                }
        */
        #endregion

        #region CreateMap

        const string dbName = "stationswithutils.xml";          // MUST exist (current directory) and be prepared with <Stations></Stations> root element
        const string CuMapTimeformat = "dd-MM-yyyy HH:mm";
        const string CuMapTimeformat_old1 = "dd-MM-yyyy";

        public void CreateMap()
        {
            // We get here because of the command CreateMap and the existence of the file "paMuCetaerCyaM" which is "MayCreateCuMap" backwards
            // Double precaution so nobody accidently will start creating a map because that can't work. So all conditions are for the 
            // Map owner (me in first instance) himself

            Sup.LogDebugMessage( $"CreateMap: Start" );

            int fileCount;
            string[] localFiles;
            XElement root;

            //
            // 1* Read the current database into the XElement root from stationswithutils.xml which only exists locally
            //    and download the contents of the remote maps directory to the utils maps directory
            //
            #region No 1
            Sup.LogTraceInfoMessage( $"CreateMap: Start Phase 1" );

            root = XElement.Load( dbName );
            Sup.LogTraceInfoMessage( $"CreateMap: {dbName} loaded" );

            CUtils.Isup.DownloadSignatureFiles();

            #endregion

            //
            // 2* Read the MapsOff files present to remove those names from the database.
            //    Also remove all entries with a refresh date older than 7 days
            //
            #region No 2
            Sup.LogTraceInfoMessage( $"CreateMap: Start Phase 2" );

            localFiles = Directory.GetFiles( "utils/maps", "MapsOff*.txt" );
            fileCount = 0;

            foreach ( string thisFile in localFiles )
            {
                string thisName;

                using ( StreamReader sr = new StreamReader( thisFile, Encoding.UTF8 ) )
                {
                    thisName = sr.ReadLine();
                }

                // Immediately delete that file. No retry when done
                File.Delete( thisFile );
                fileCount++;

                try
                {
                    int i = 0;

                    List<XElement> result = root.Descendants( "Station" ).Where( x => x.Element( "Name" ).Value.Equals( thisName ) ).ToList();

                    if ( result.Any() )
                    {
                        foreach ( XElement station in result )
                        {
                            // Remove all possible instances of this station
                            Sup.LogTraceInfoMessage( $"CreateMap: Deleting station {thisName} nr {++i}" );
                            station.Remove();
                        }
                    }
                    else
                    {
                        // Station not found so nothing to do!
                        Sup.LogTraceWarningMessage( $" MapsOff: Station {thisName} not found in {dbName}! No removal" );
                    }
                }
                catch ( Exception e ) when ( e is XmlException )
                {
                    Sup.LogTraceWarningMessage( $"Maps->MapsOff: Error in {thisName}" );
                    Sup.LogTraceWarningMessage( $"Maps->MapsOff: Xml Exception {e.Message}" );
                }
                catch ( Exception e )
                {
                    Sup.LogTraceWarningMessage( $"Maps->MapsOff: General Error in {thisName}" );
                    Sup.LogTraceWarningMessage( $"Maps->MapsOff: General Exception {e.Message}" );
                }
            }

            Sup.LogDebugMessage( $"CreateMap: {fileCount} MapsOff files handled for Map." );

            // Check if the station in the database is passed its date

            double AcceptedTimespan = 7;

            XElement[] StationArray;
            StationArray = root.Elements( "Station" ).ToArray();
            fileCount = 0;

            for ( int i = StationArray.Count() - 1; i >= 0; i-- )
            {
                // foreach (XElement station in root.Elements() )
                string[] dateFormats = { CuMapTimeformat, CuMapTimeformat_old1 };

                string strDate;
                DateTime lastSeen;

                strDate = StationArray[ i ].Element( "Date" ).Value;

                try
                {
                    lastSeen = DateTime.ParseExact( strDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None );
                    Sup.LogTraceVerboseMessage( $"GenUtilsMap: ParseExact : Succesful parse Date lastSeen: {strDate} / {lastSeen}" );
                }
                catch ( Exception e ) when ( e is FormatException || e is ArgumentNullException )
                {
                    Sup.LogTraceErrorMessage( $"GenUtilsMap: Cannot parse date: {strDate}" );
                    lastSeen = DateTime.Now;
                }

                if ( ( DateTime.Now - lastSeen ).TotalDays > AcceptedTimespan )
                {
                    //Remove it from the list
                    Sup.LogTraceInfoMessage( $" GenUtilsMap: Station {StationArray[ i ].Element( "Name" ).Value} removed from list, not seen for {AcceptedTimespan} days )" );
                    StationArray[ i ].Remove();
                    fileCount++;
                }
            }

            Sup.LogDebugMessage( $"CreateMap: {fileCount} Stations removed of Map on basis  of timeout." );


            #endregion

            //
            // 3* Read the MapsOn files present to refresh or add entries. If entries exist already just update the DateTime
            //    If the entry (name) does not exist, add the entry
            //
            #region No 3
            Sup.LogTraceInfoMessage( $"CreateMap: Start Phase 3" );

            localFiles = Directory.GetFiles( "utils/maps", "MapsOn*.xml" );

            foreach ( string thisFile in localFiles )
            {
                string thisName;
                XElement tmp;

                Sup.LogTraceInfoMessage( $"CreateMap Phase 3: reading {thisFile}" );

                try
                {
                    XElement thisStation = XElement.Load( thisFile );
                    thisName = thisStation.Element( "Name" ).Value;

                    Sup.LogTraceInfoMessage( $"CreateMap Phase 3: using {thisStation}" );

                    // Remove an existing entry (only one, if more than the old one will disappear eventually by timing out
                    tmp = root.Descendants( "Station" ).Where( x => x.Element( "Name" ).Value.Equals( thisName ) ).FirstOrDefault();
                    tmp?.Remove();

                    root.Add( thisStation );
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"GenUtilsMap: Exception: {e.Message}" );
                    Sup.LogDebugMessage( $"GenUtilsMap: Continuing from error in file {thisFile}." );
                }

                File.Delete( thisFile );
            } // Foreach loop over all signature files

            Sup.LogDebugMessage( $"CreateMap: {localFiles.Length} MapsOn files handled for Map." );

            #endregion

            //
            // 4* Write away the database locally in stationswithutils.xml
            //
            #region No 4
            Sup.LogTraceInfoMessage( $"CreateMap: Start Phase 4" );

            root.Save( dbName );

            #endregion

            //
            // 5* Create the map from the updated database (still in memory)
            //
            #region No 5
            Sup.LogTraceInfoMessage( $"CreateMap: Start Phase 5" );

            // Finally, all data updated and saved, we can create the map. The Map.txt file is written to the utils directory and simply uploaded to 
            // the website where all everybopdy can download it and incorporate it in their own website.

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.MapsOutputFilename}", false, Encoding.UTF8 ) )
            {
                string Name, Description, Website, Date;
                float Latitude, Longitude;

                Sup.LogTraceInfoMessage( $"CreateMap: Creating the CumulusUtils Map" );

                // jQuery is included when the Map is downloaded in MapsOn. That is the only place where it is known if it
                // is required to include it or not (is it a module or withing the generated website)
                //of.WriteLine($"{Sup.GenjQueryIncludestring()}");
                // Skip this: jQuery is not required for the map module

                of.WriteLine( "<style>" );
                of.WriteLine( ".cuMapCircle {cursor: grab;}" );
                of.WriteLine( "#CumulusUtils {height: 750px; width: 100%;}" );
                of.WriteLine( "</style>" );
                of.WriteLine( Sup.GenLeafletIncludes().ToString() );

                of.WriteLine( "<script>" );

                of.WriteLine( "$(function(){ CreateThisMap() });" );  // Residu from previous versions, might remove the call to the bare code
                of.WriteLine( "function CreateThisMap() {" );
                of.WriteLine( "  var CumulusStations = L.map('CumulusUtils').setView([0, 0], 2);" );

                of.WriteLine( "  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', " +
                  "{attribution: '&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors'}).addTo(CumulusStations);" );

                // Make sure we got the latest version
                try
                {
                    root = XElement.Load( dbName );
                    fileCount = 0;

                    foreach ( XElement thisStation in root.Elements( "Station" ) )
                    {
                        fileCount++;

                        Name = thisStation.Element( "Name" ).Value;
                        Description = thisStation.Element( "Description" ).Value;
                        Latitude = Convert.ToSingle( thisStation.Element( "Latitude" ).Value.Replace( ',', '.' ), CultureInfo.InvariantCulture );
                        Longitude = Convert.ToSingle( thisStation.Element( "Longitude" ).Value.Replace( ',', '.' ), CultureInfo.InvariantCulture );
                        Website = thisStation.Element( "Website" )?.Value ?? "";
                        Date = thisStation.Element( "Date" )?.Value ?? "";

                        Sup.LogTraceInfoMessage( $"CreateMap: Writing Station {Name}" );

                        of.WriteLine( $"  var marker = L.marker([{Latitude.ToString( CultureInfo.InvariantCulture )}, {Longitude.ToString( CultureInfo.InvariantCulture )}]).addTo(CumulusStations);" );

                        of.WriteLine( $"  marker.bindPopup(\"<b>{Name}</b><br/>{Description}<br/>" +
                                      $"Lat: {Latitude} / Lon: {Longitude}<br/>" +
                                      $"{( Website != "" ? "<a href='" + Website + "' target='_blank'>Website</a><br/>" : "Website: No link supplied<br/>" )}" +
                                      //$"<a href='{Website}' target='_blank'>Website</a><br/>" +
                                      $"Last alive (UTC): {Date}\");" );

                        of.WriteLine( $"  var circle = L.circle([{Latitude.ToString( "F4", CultureInfo.InvariantCulture )}, {Longitude.ToString( "F4", CultureInfo.InvariantCulture )}], {{" );
                        of.WriteLine( "     color: 'lightgrey', weight:2," );
                        of.WriteLine( "     fillColor: 'whitesmoke'," );
                        of.WriteLine( "     fillOpacity: 0.3," );
                        of.WriteLine( "     className: 'cuMapCircle'," );
                        of.WriteLine( "     radius: 25000}).addTo(CumulusStations);" );
                    }// Foreach station in Stations
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"GenUtilsMap: Exception: {e.Message}" );
                    Sup.LogDebugMessage( $"GenUtilsMap: Continuing from error, Map has been generated, none or partial Stations on Map!" );
                }

                of.WriteLine( "}" );
                of.WriteLine( "</script>" );
                of.WriteLine( "<div id=\"CumulusUtils\"></div>" );
                of.WriteLine( $"<br/><div style ='margin-left:auto; margin-right:auto; text-align:center; font-size: 12px;'>" +
                              $"{CuSupport.FormattedVersion()} - {CuSupport.Copyright()} </div>" );

                Sup.LogDebugMessage( $"CreateMap: {fileCount} Stations on this Map." );
            }

            #endregion

            return;
        }

        #endregion

        #region IDisposable CuSupport
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                }

                disposedValue = true;
            }
        }

        ~Maps()
        {
            Dispose( false );
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }
        #endregion
    }
}