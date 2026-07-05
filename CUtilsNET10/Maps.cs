/*
 * Maps - Part of CumulusUtils
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
            Sup.LogMessage( "MapsOn: Starting", TraceLevel.Info );

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
                    Website = "https://" + Website;
                }

                Sup.LogMessage( $" MapsOn: Adding Station: {Name}", TraceLevel.Info );

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
                                          new XElement( "Date", DateTime.UtcNow.ToString( "dd-MM-yyyy HH:mm", CUtils.Inv ) ),
                                          new XElement( "CUversion", CuSupport.UnformattedVersion() )
                                        );

                        // And save the file and write back to the central storage
                        root.Save( of );
                    }
                    catch ( Exception e )
                    {
                        Sup.LogMessage( $"MapsOn: XElement Exception: {e.Message}", TraceLevel.Error );
                        Sup.LogMessage( $"MapsOn failed: continuing", TraceLevel.Error );
                    }
                }
                else
                {
                    Sup.LogMessage( "Maps->MapsOn: Not enough info for Entry!!", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Name: {Name}", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Description:{Description}", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Website: {Website}", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Latitude: {Latitude}", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Longitude: {Longitude}", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Date (UTC): {DateTime.UtcNow.ToString( "dd-MM-yyyy HH:mm", CUtils.Inv )}", TraceLevel.Info );
                    Sup.LogMessage( $"Maps->MapsOn: Version: {CuSupport.UnformattedVersion()}", TraceLevel.Info );

                    // An exit is made here. This is especially disturbing when operating with the website generator
                    // I do this to oblige the user to actually fill in correct data for his website (which he needs for use of CumulusUtils
                    // I do not physically check the site because it may not yet be online, but that could be a next step.

                    Sup.LogMessage( "Maps->MapsOn: Name, Website, Latitude and Longitude are compulsory so exit here!!", TraceLevel.Info );
                    Sup.LogMessage( "See forum Post 'For New Users' (https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=18226).", TraceLevel.Info );
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
                    Sup.LogMessage( $"MapsOn: Before testing DoneToday after parsing: {DoneToday} ", TraceLevel.Info );
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
                Sup.LogMessage( $"MapsOn: Must send signature: {DoneToday:s} / Setting DoneToday to now.", TraceLevel.Info );
                Sup.SetUtilsIniValue( "Maps", "DoneToday", $"{DateTime.Now:s}" );

                string thisContent = $"filename#{FileToSend}&";
                thisContent += "filecontent#" + File.ReadAllText( Sup.PathUtils + FileToSend, Encoding.UTF8 );
                retval = await CUtils.Isup.PostUrlDataAsync( new Uri( "https://meteo-wagenborgen.nl/cgi-bin/receive.pl" ), thisContent );
                Sup.LogMessage( $"MapsOn : Success", TraceLevel.Info );
            }
            else retval = $"MapsOn: Must NOT send signature, has been done already : {DoneToday:s}";

            if ( File.Exists( $"{Sup.PathUtils}{FileToSend}" ) ) File.Delete( $"{Sup.PathUtils}{FileToSend}" );

            return retval;
        }

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

            Sup.LogDebugMessage( $"CreateMap: Starting" );

            int fileCount;
            string[] localFiles;
            XElement root;

            //
            // 1* Read the current database into the XElement root from stationswithutils.xml which only exists locally
            //    and download the contents of the remote maps directory to the utils maps directory
            //
            #region No 1
            Sup.LogMessage( $"CreateMap: Starting Phase 1", TraceLevel.Info );

            root = XElement.Load( dbName );
            Sup.LogMessage( $"CreateMap: {dbName} loaded", TraceLevel.Info );

            CUtils.Isup.DownloadSignatureFiles();

            #endregion

            //
            // 2* Read the MapsOff files present to remove those names from the database.
            //    Also remove all entries with a refresh date older than 7 days
            //
            #region No 2
            Sup.LogMessage( $"CreateMap: Starting Phase 2", TraceLevel.Info );

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
                            Sup.LogMessage( $"CreateMap: Deleting station {thisName} nr {++i}", TraceLevel.Info );
                            station.Remove();
                        }
                    }
                    else
                    {
                        // Station not found so nothing to do!
                        Sup.LogMessage( $" MapsOff: Station {thisName} not found in {dbName}! No removal", TraceLevel.Warning );
                    }
                }
                catch ( Exception e ) when ( e is XmlException )
                {
                    Sup.LogMessage( $"Maps->MapsOff: Error in {thisName}", TraceLevel.Warning );
                    Sup.LogMessage( $"Maps->MapsOff: Xml Exception {e.Message}", TraceLevel.Warning );
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"Maps->MapsOff: General Error in {thisName}", TraceLevel.Warning );
                    Sup.LogMessage( $"Maps->MapsOff: General Exception {e.Message}", TraceLevel.Warning );
                }
            }

            Sup.LogMessage( $"CreateMap: {fileCount} MapsOff files handled for Map.", TraceLevel.Info );

            // Check if the station in the database is passed its date

            double AcceptedTimespan = 7;

            XElement[] StationArray;
            StationArray = root.Elements( "Station" ).ToArray();
            fileCount = 0;

            for ( int i = StationArray.Length - 1; i >= 0; i-- )
            {
                // foreach (XElement station in root.Elements() )
                string[] dateFormats = { CuMapTimeformat, CuMapTimeformat_old1 };

                string strDate;
                DateTime lastSeen;

                strDate = StationArray[ i ].Element( "Date" ).Value;

                try
                {
                    lastSeen = DateTime.ParseExact( strDate, dateFormats, CUtils.Inv, DateTimeStyles.None );
                    Sup.LogMessage( $"GenUtilsMap: ParseExact : Succesful parse Date lastSeen: {strDate} / {lastSeen}", TraceLevel.Verbose );
                }
                catch ( Exception e ) when ( e is FormatException || e is ArgumentNullException )
                {
                    Sup.LogMessage( $"GenUtilsMap: Cannot parse date: {strDate}", TraceLevel.Warning );
                    lastSeen = DateTime.Now;
                }

                if ( ( DateTime.Now - lastSeen ).TotalDays > AcceptedTimespan )
                {
                    //Remove it from the list
                    Sup.LogMessage( $" GenUtilsMap: Station {StationArray[ i ].Element( "Name" ).Value} removed from list, not seen for {AcceptedTimespan} days )", TraceLevel.Info );
                    StationArray[ i ].Remove();
                    fileCount++;
                }
            }

            Sup.LogMessage( $"CreateMap: {fileCount} Stations removed of Map on basis  of timeout.", TraceLevel.Info );


            #endregion

            //
            // 3* Read the MapsOn files present to refresh or add entries. If entries exist already just update the DateTime
            //    If the entry (name) does not exist, add the entry
            //
            #region No 3
            Sup.LogMessage( $"CreateMap: Starting Phase 3", TraceLevel.Info );

            localFiles = Directory.GetFiles( "utils/maps", "MapsOn*.xml" );

            foreach ( string thisFile in localFiles )
            {
                string thisName;
                XElement tmp;

                Sup.LogMessage( $"CreateMap Phase 3: reading {thisFile}", TraceLevel.Info );

                try
                {
                    XElement thisStation = XElement.Load( thisFile );
                    thisName = thisStation.Element( "Name" ).Value;

                    Sup.LogMessage( $"CreateMap Phase 3: using {thisStation}", TraceLevel.Info );

                    // Remove an existing entry (only one, if more than the old one will disappear eventually by timing out
                    tmp = root.Descendants( "Station" ).Where( x => x.Element( "Name" ).Value.Equals( thisName ) ).FirstOrDefault();
                    tmp?.Remove();

                    root.Add( thisStation );
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"GenUtilsMap: Exception: {e.Message}", TraceLevel.Warning );
                    Sup.LogMessage( $"GenUtilsMap: Continuing from error in file {thisFile}.", TraceLevel.Info );
                }

                File.Delete( thisFile );
            } // Foreach loop over all signature files

            Sup.LogMessage( $"CreateMap: {localFiles.Length} MapsOn files handled for Map.", TraceLevel.Info );

            #endregion

            //
            // 4* Write away the database locally in stationswithutils.xml
            //
            #region No 4
            Sup.LogMessage( $"CreateMap: Starting Phase 4", TraceLevel.Info );

            root.Save( dbName );

            #endregion

            //
            // 5* Create the map from the updated database (still in memory)
            //
            #region No 5
            Sup.LogMessage( $"CreateMap: Starting Phase 5", TraceLevel.Info );

            // Finally, all data updated and saved, we can create the map. The Map.txt file is written to the utils directory and simply uploaded to 
            // the website where all everybopdy can download it and incorporate it in their own website.

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.MapsOutputFilename}", false, Encoding.UTF8 ) )
            {
                string Name, Description, Website, Date, CUversion;
                float Latitude, Longitude;

                Sup.LogMessage( $"CreateMap: Creating the CumulusUtils Map", TraceLevel.Info );

                // jQuery is included when the Map is downloaded in MapsOn. That is the only place where it is known if it
                // is required to include it or not (is it a module or withing the generated website)
                //of.WriteLine($"{Sup.GenjQueryIncludestring()}");
                // Skip this: jQuery is not required for the map module

                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                of.WriteLine( "<style>" );
                of.WriteLine( ".cuMapCircle {cursor: grab;}" );
                of.WriteLine( "#CumulusUtils {height: 750px; width: 100%;}" );
                of.WriteLine( "</style>" );
                of.WriteLine( CuSupport.GenLeafletIncludes().ToString() );

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
                        Latitude = Convert.ToSingle( thisStation.Element( "Latitude" ).Value.Replace( ',', '.' ), CUtils.Inv );
                        Longitude = Convert.ToSingle( thisStation.Element( "Longitude" ).Value.Replace( ',', '.' ), CUtils.Inv );
                        Website = thisStation.Element( "Website" )?.Value ?? "";
                        Date = thisStation.Element( "Date" )?.Value ?? "-";
                        CUversion = thisStation.Element( "CUversion" )?.Value ?? "-";

                        Sup.LogMessage( $"CreateMap: Writing Station {Name}", TraceLevel.Info );

                        of.WriteLine( $"  var marker = L.marker([{Latitude.ToString( CUtils.Inv )}, {Longitude.ToString( CUtils.Inv )}]).addTo(CumulusStations);" );

                        of.WriteLine( $"  marker.bindPopup(\"<b>{Name}</b><br/>{Description}<br/>" +
                                      $"Lat: {Latitude} / Lon: {Longitude}<br/>" +
                                      $"{( Website != "" ? "<a href='" + Website + "' target='_blank'>Website</a><br/>" : "Website: No link supplied<br/>" )}" +
                                      //$"<a href='{Website}' target='_blank'>Website</a><br/>" +
                                      $"Last alive (UTC): {Date} / CUversion: {CUversion}\");" );

                        of.WriteLine( $"  var circle = L.circle([{Latitude.ToString( "F4", CUtils.Inv )}, {Longitude.ToString( "F4", CUtils.Inv )}], {{" );
                        of.WriteLine( "     color: 'lightgrey', weight:2," );
                        of.WriteLine( "     fillColor: 'whitesmoke'," );
                        of.WriteLine( "     fillOpacity: 0.3," );
                        of.WriteLine( "     className: 'cuMapCircle'," );
                        of.WriteLine( "     radius: 25000}).addTo(CumulusStations);" );
                    }// Foreach station in Stations
                }
                catch ( Exception e )
                {
                    Sup.LogMessage( $"GenUtilsMap: Exception: {e.Message}", TraceLevel.Warning );
                    Sup.LogMessage( $"GenUtilsMap: Continuing from error, Map has been generated, none or partial Stations on Map!", TraceLevel.Info );
                }

                of.WriteLine( "}" );
                of.WriteLine( "</script>" );
                of.WriteLine( "<div id=\"CumulusUtils\"></div>" );
                of.WriteLine( $"<br/><div style ='margin-left:auto; margin-right:auto; text-align:center; font-size: 12px;'>" +
                              $"{CuSupport.FormattedVersion()} - {CuSupport.Copyright()} </div>" );

                Sup.LogMessage( $"CreateMap: {fileCount} Stations on this Map.", TraceLevel.Info );
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