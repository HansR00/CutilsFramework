/*
 * Maps - Part of CumulusUtils
 *
 * © Copyright 2019 - 2020 Hans Rottier <hans.rottier@gmail.com>
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
 *              Initial release: Website Generator (3.0) 
 *              Branched to 4.0.0 on 27 july 2020 to accomodate CMX version 3.7.0
 *              
 * Environment: Raspberry 3B+
 *              Raspbian / Linux 
 *              C# / Visual Studio
 *              
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using FluentFTP;

namespace CumulusUtils
{
  internal class Maps : IDisposable
  {
    private readonly CuSupport Sup;

    private const string HTTPserverMaps = "https://meteo-wagenborgen.nl/";
    private const string URLserverMaps = "ftp.meteo-wagenborgen.nl";
    private const string filenameRemote = "stationswithutils.xml";
    private const string filenameLocal = "stationswithutils.xml";
    private readonly string lockfileName;
    private readonly string decryptun;
    private readonly string decryptpw;

    private readonly FtpClient MapFluentFTP;
    private readonly bool ValidMapFtp = true;     // Signaled in constructor, if set we skip GetLock and return false not creating a map

    public Maps(CuSupport s)
    {
      Sup = s;
      lockfileName = "cuMapLockfile";
      //lockfileName = "cuTestLockfile";

      // Keep the next line for future reference.
      // string encryptedWord = StringCipher.Encrypt("", "http://meteo-wagenborgen.nl/");
      // Reference: https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
      // And: https://stackoverflow.com/questions/19124633/c-sharp-ftp-upload-and-download
      //

      decryptun = StringCipher.Decrypt("xXXUoD+nmOTkGkx/y0u/+EyHUYrt6SHwsuYlYThpKZzkU1H3l/7tYWrcfK1OWiizWWXJvFJBRbRh68LhDOoDiy0ZrrePon1LP/RBTd7wGw/IWYLd/1gt6TYDbmOMzOzD", "http://meteo-wagenborgen.nl/");
      decryptpw = StringCipher.Decrypt("QH9gXIgJkVeofWUgGCtpcOdNrRZyWbqquVR05aoSiXKXTK1Lov7bhDM0U45d34yo8CHJSi8LKaKhP0rz0dwmGU16v4qTewFOuHiiZxvqVrR4RibISnUYoz3MBgBa53Mw", "http://meteo-wagenborgen.nl/");

      // Setup the FTP handling through FluentFTP for meteo-wagenborgen.nl (always FTPS and Passive)
      //

      FtpTrace.LogPassword = false;
      FtpTrace.LogUserName = false;
      FtpTrace.LogIP = false;

      try
      {
        MapFluentFTP = new FtpClient(URLserverMaps,
                                     decryptun,
                                     decryptpw)
        {
          EncryptionMode = FtpEncryptionMode.None,
          SslProtocols = SslProtocols.None,
          DataConnectionType = FtpDataConnectionType.EPSV,
          Encoding = Encoding.UTF8,

          SocketKeepAlive = true,
          UploadDataType = FtpDataType.Binary
        };

        MapFluentFTP.Connect();

        MapFluentFTP.TimeOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Hours;
        Sup.LogTraceErrorMessage($" MapFluentFTP.TimeOffset = {MapFluentFTP.TimeOffset}");

        Sup.LogDebugMessage($" Maps: FTP activated.");
      }
      catch (Exception e) when (e is FtpAuthenticationException || e is FtpCommandException || e is FtpSecurityNotAvailableException)
      {
        Sup.LogTraceErrorMessage($" Maps Inet: Exception on FTPS connecting to {URLserverMaps}: {e.Message}");
        Sup.LogTraceErrorMessage($" Maps Inet: Failed FTPS connecting to {URLserverMaps}. Lockfilesystem failing");
        ValidMapFtp = false;
      }
      catch (Exception e)
      {
        Sup.LogTraceErrorMessage($" Maps Inet: Unknown Exception on FTPS connecting to {URLserverMaps}: {e.Message}");
        Sup.LogTraceErrorMessage($" Maps Inet: Failed FTP connecting to {URLserverMaps}. Files will not be transferred");
        ValidMapFtp = false;
        return;
        throw; // satisfy compiler
      }

      return;
    }


    // Relevant are the following links to the map:
    // https://leafletjs.com/
    // https://openlayers.org/
    // https://www.openstreetmap.org/copyright
    // https://wiki.openstreetmap.org/wiki/Software
    // https://wiki.openstreetmap.org/wiki/Tiles
    // https://wiki.openstreetmap.org/wiki/Servers/tile
    // https://wiki.openstreetmap.org/wiki/Tile_servers
    // 
    // For those who run Maps on a too high frequency and won't listen: block out via .htaccess
    // https://htaccessbook.com/block-ip-address/
    // 
    public void GenUtilsMap()
    {
      float Latitude, Longitude;
      string Name, Description, Website;
      string Date;

      Sup.LogDebugMessage(message: "GenUtilsMap: Start");

      bool MapDirty = false;

      using (StreamWriter of = new StreamWriter($"{Sup.PathUtils}{Sup.MapsOutputFilename}", false, Encoding.UTF8))
      {
        if (!CMXutils.DoWebsite)
        {
          of.WriteLine("<link rel=\"stylesheet\" href=\"https://unpkg.com/leaflet@1.5.1/dist/leaflet.css\"" +
                       " integrity = \"sha512-xwE/Az9zrjBIphAcBb3F6JVqxf46+CDLwfLMHloNu6KEQCAWi6HcDUbeOfBIptF7tcCzusKFjFw2yuvEpDL9wQ==\" " +
                       " crossorigin = \"\" />");
          of.WriteLine("<script src=\"https://unpkg.com/leaflet@1.5.1/dist/leaflet.js\"" +
                       " integrity=\"sha512-GffPMF3RvMeYyc1LWMHtK8EbPv0iNZ8/oTtHPx9/cc2ILxQ+u905qIwdpULaqDkyBKgOaB57QTMg7ztg8Jm2Og==\" " +
                       " crossorigin=\"\"></script>");
        }

        of.WriteLine("<style>");
        of.WriteLine("#CumulusUtils {height: 750px; width: 100%;}");
        of.WriteLine("</style>");
        of.WriteLine("<div id=\"CumulusUtils\"></div>");
        of.WriteLine("<script>");
        of.WriteLine("var CumulusStations = L.map('CumulusUtils').setView([0, 0], 2);");

        of.WriteLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', " +
          "{attribution: '&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors'}).addTo(CumulusStations);");

        // Make sure we got the latest version
        if (DownloadXml(filenameRemote, filenameLocal))
        {
          try
          {
            XElement root = XElement.Load(filenameLocal);

            // foreach (XElement Station in root.Elements()) // loop over the stations in stationswithutils
            // Because of remove if out of date, we cannot use the foreach construction but have to loop backwards
            for (int i = root.Elements().Count() - 1; i >= 0; i--)
            {
              const int AcceptedTimespan = 7;
              DateTime thisDate;

              XElement Station = root.Elements().ElementAt(i);

              Name = Station.Element("Name").Value;
              Description = Station.Element("Description").Value;
              Latitude = Convert.ToSingle(Station.Element("Latitude").Value.Replace(',', '.'), CultureInfo.InvariantCulture);
              Longitude = Convert.ToSingle(Station.Element("Longitude").Value.Replace(',', '.'), CultureInfo.InvariantCulture);
              Website = Station.Element("Website")?.Value ?? "";
              Date = Station.Element("Date")?.Value ?? "";

              // Here we place the cleanup action: if the date older than a <timespan> then delete
              // If any field missing: delete!
              // thisDate = DateTime.Parse(Date, "dd-MM-yyyy", CultureInfo.InvariantCulture);

              string[] dateFormats = { "dd-MM-yyyy HH:mm", "dd-MM-yyyy" };

              try
              {
                thisDate = DateTime.ParseExact(Date, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                Sup.LogTraceInfoMessage($"GenUtilsMap: ParseExact : Succesful parse Date: {Date} / {thisDate}");
              }
              catch (Exception e) when (e is FormatException || e is ArgumentNullException)
              {
                Sup.LogTraceErrorMessage($"GenUtilsMap: Cannot parse date: {Date}");
                thisDate = DateTime.Now;
              }

              if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Website) || (DateTime.Now - thisDate.Date).TotalDays > AcceptedTimespan)
              {
                //Remove it from the list
                MapDirty = true;
                Station.Remove();
                Sup.LogTraceInfoMessage(message: $" GenUtilsMap: Station {Name} removed from list ({Description} / {Website} / {Latitude:F4} / {Longitude:F4} / not seen for {(DateTime.Now - thisDate.Date).TotalDays} days )");
              }
              else
              {
                // Place the station on the map
                //
                if (!Website.StartsWith("http:", StringComparison.OrdinalIgnoreCase) &&
                    !Website.StartsWith("https:", StringComparison.OrdinalIgnoreCase) &&
                    !String.IsNullOrEmpty(Website))
                {
                  Website = "http://" + Website;
                }

                // Now output all info for this station
                //
                of.WriteLine($"var marker = L.marker([{Latitude.ToString(CultureInfo.InvariantCulture)}, {Longitude.ToString(CultureInfo.InvariantCulture)}]).addTo(CumulusStations);");

                of.WriteLine($"marker.bindPopup(\"<b>{Name}</b><br/>{Description}<br/>" +
                             $"Lat: {Latitude} / Lon: {Longitude}<br/>" +
                             $"<a href=\'{Website}\' target=\'_blank\'>Website</a><br/>" +
                             $"Last alive: {Date}\");");

                of.WriteLine($"var circle = L.circle([{Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {Longitude.ToString("F4", CultureInfo.InvariantCulture)}], {{");
                of.WriteLine("    color: 'lightgrey',");
                of.WriteLine("    fillColor: 'whitesmoke',");
                of.WriteLine("    fillOpacity: 0.5,");
                of.WriteLine("    radius: 25000}).addTo(CumulusStations);");
              } // If the station is valid and should not be cleaned
            }// Foreach station in Stations

            if (MapDirty && ValidMapFtp)
            {
              root.Save(filenameLocal, SaveOptions.None);
              UploadXml(filenameRemote, filenameLocal);

              Sup.LogTraceInfoMessage(message: " GenUtilsMap: MapDirty - stations removed, file uploaded");
            }
            else if (!ValidMapFtp)
            {
              Sup.LogTraceInfoMessage(message: $" GenUtilsMap: MapDirty - Could not upload {filenameLocal}");
            }
          }
          catch (Exception e)
          {
            Sup.LogTraceErrorMessage(message: $" GenUtilsMap: Exception: {e.Message}");
            Sup.LogTraceErrorMessage(message: $" GenUtilsMap: Continuing from error, Map has been generated, none or partial Stations on Map!");
          }

          of.WriteLine("</script>");

          if (!CMXutils.DoWebsite)
          {
            of.WriteLine($"<br/><div style ='margin-left:auto; margin-right:auto; text-align:center; font-size: 11px;'>" +
                        $"{CuSupport.Version()} - {CuSupport.Copyright()} </div>");
          }
        }
        else
        {
          Sup.LogTraceErrorMessage(message: $" GenUtilsMap: XML has not been downloaded, Map has been generated, none or partial Stations on Map!");
        }
      }

      return;
    }

    public void MapsOn()
    {
      Sup.LogDebugMessage(message: "MapsOn: Start");

      if (!CMXutils.Thrifty || CMXutils.RunStarted.DayOfYear % CMXutils.ThriftyMapsPeriod == 0)
      {
        CMXutils.ThriftyMapsDirty = true;
      }
      else return;

      string thisName = Sup.GetCumulusIniValue("Station", "LocName", "");
      string thisLatitude = Sup.GetCumulusIniValue("Station", "Latitude", "");

      if (DownloadXml(filenameRemote, filenameLocal))
      {
        try
        {
          XElement root = XElement.Load(filenameLocal);
          List<XElement> result = root.Descendants("Station").Where(x => x.Element(name: "Name").Value.Equals(thisName)).ToList();

          if (result.Count >= 1)
          {
            int i = 0;

            Sup.LogTraceInfoMessage(message: $" MapsOn: removing Station: {thisName}");

            foreach (XElement station in result)
            {
              // Remove all possible instances of this station
              Sup.LogTraceInfoMessage(message: $"MapsOn: Deleting station {thisName} nr {++i}");
              station.Remove();
            }
          }
          else
          {
            Sup.LogTraceInfoMessage(message: $" MapsOn: Station {thisName} not found in XML!");
          }

          Sup.LogTraceInfoMessage(message: $" MapsOn: Adding Station: {thisName}");

          string Name = Sup.GetCumulusIniValue("Station", "LocName", "");
          string Description = Sup.GetCumulusIniValue("Station", "LocDesc", "");
          string Website = Sup.GetUtilsIniValue("Maps", "Website", "");
          string Latitude = Sup.GetCumulusIniValue("Station", "Latitude", "");
          string Longitude = Sup.GetCumulusIniValue("Station", "Longitude", "");

          if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Website) && !string.IsNullOrEmpty(Latitude) && !string.IsNullOrEmpty(Longitude))
          {
            root.Add(
                    new XElement("Station",
                        new XElement("Name", Name),
                        new XElement("Description", Description),
                        new XElement("Website", Website),
                        new XElement("Latitude", Latitude),
                        new XElement("Longitude", Longitude),
                        new XElement("Date", DateTime.Now.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
                    )
                  );

            // And save the file and write back to the central storage
            root.Save(filenameLocal, SaveOptions.None);
            UploadXml(filenameRemote, filenameLocal);

            Sup.LogTraceInfoMessage(message: "Maps->MapsOn: Finished - station added/refreshed, file uploaded");
          }
          else
          {
            Sup.LogTraceInfoMessage(message: "Maps->MapsOn: Not enough info for Entry!!");
            Sup.LogTraceInfoMessage(message: $"Maps->MapsOn: Name: {Name}");
            Sup.LogTraceInfoMessage(message: $"Maps->MapsOn: Description:{Description}");
            Sup.LogTraceInfoMessage(message: $"Maps->MapsOn: Website: {Website}");
            Sup.LogTraceInfoMessage(message: $"Maps->MapsOn: Latitude: {Latitude}");
            Sup.LogTraceInfoMessage(message: $"Maps->MapsOn: Longitude: {Longitude}");

            // An exit is made here. This is especially disturbing when operating with the website generator
            // I do this to oblige the user to actually fill in correct data for his website (which he needs for use of CumulusUtils
            // I do not physically check the site because it may not yet be online, but that could be a next step.

            Sup.LogTraceErrorMessage(message: "Maps->MapsOn: Name, Website, Latitude and Longitude are compulsory so exit here!!");
            Environment.Exit(0);
          }
        }
        catch (Exception e) when (e is XmlException)
        {
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOn: Error in {filenameLocal}");
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOn: Xml Exception {e.Message}");
        }
        catch (Exception e)
        {
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOn: General Error in {filenameLocal}");
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOn: General Exception {e.Message}");
        }
      }
      else
      {
        Sup.LogTraceWarningMessage(message: "Maps->MapsOn: Could not download XML!!");
      }
    }

    public void MapsOff()
    {
      string thisName = Sup.GetCumulusIniValue("Station", "LocName", "");
      string thisLatitude = Sup.GetCumulusIniValue("Station", "Latitude", "");

      Sup.LogDebugMessage(message: "MapsOff: Start");

      if (DownloadXml(filenameRemote, filenameLocal))
      {
        try 
        {
          XElement root = XElement.Load(filenameLocal);
          List<XElement> result = root.Descendants("Station").Where(x => x.Element("Name").Value.Equals(thisName)).ToList();

          if (result.Count >= 1)
          {
            int i = 0;

            Sup.LogTraceInfoMessage(message: $" MapsOff: removing Station: {thisName}");

            foreach (XElement station in result)
            {
              // Remove all possible instances of this station
              Sup.LogTraceInfoMessage(message: $"MapsOff: Deleting station {thisName} nr {++i}");
              station.Remove();
            }

            // Save the file and write back to the central storage
            root.Save(filenameLocal, SaveOptions.None);
            UploadXml(filenameRemote, filenameLocal);

            Sup.LogTraceInfoMessage(message: $" MapsOff: Finished - station {thisName} removed, file uploaded");
          }
          else
          {
            // Station not found so nothing to do!
            Sup.LogTraceWarningMessage(message: $" MapsOff: Station {thisName} not found in XML!");
          }
        }
        catch (Exception e) when (e is XmlException)
        {
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOff: Error in {filenameLocal}");
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOff: Xml Exception {e.Message}");
        }
        catch (Exception e)
        {
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOff: General Error in {filenameLocal}");
          Sup.LogTraceWarningMessage(message: $"Maps->MapsOff: General Exception {e.Message}");
        }
      }
      else
      {
        // File not downloaded
        Sup.LogTraceWarningMessage(message: $" MapsOff: XML not downloaded nothing done!");
      }
    }

    private bool DownloadXml(string remotefile, string localfile)
    {
      string requestname = HTTPserverMaps + remotefile;

      Sup.LogDebugMessage(message: $" DownloadXml: {requestname}");

      try
      {
        MapFluentFTP.DownloadFile(localfile, "/public_html/" + remotefile, FtpLocalExists.Overwrite, FtpVerify.None);
      }
      catch (Exception e) // Take on any exception!! // when (e is FtpException || e is TimeoutException)
      {
        Sup.LogTraceErrorMessage(message: $" DownloadXml: DownloadFile Exception: {e.Message}");
        return false;
      }

      return true;
    }

    private void UploadXml(string remotefile, string localfile)
    {
      Sup.LogDebugMessage(message: $" UploadXml: {localfile} to {remotefile}");

      try
      {
        MapFluentFTP.UploadFile(localfile, "/public_html/" + remotefile, FtpRemoteExists.Overwrite, false, FtpVerify.None);
      }
      catch (Exception e) // Take on any exception!! // when (e is FtpException || e is TimeoutException)
      {
        // Just register the exception and fall through, returning and as such not registering the modifications.
        // Better next time
        Sup.LogTraceErrorMessage(message: $" UploadXml: TimeoutException: {e.Message}");
      }

      return;
    }

    public bool GetFTPLock()
    {
      Sup.LogDebugMessage(message: $"Maps GetFTPLock : Checking for a lockfile");

      if (!ValidMapFtp) return false;

      if (FTPLockfileExists() > 0)
      {
        const int NrOfMilliseconds = 500; // for the retry wait
        const int NrOfRetries = 16;       // for 500 ms wait time this  makes 8 seconds to release the lock
        int count = 0;

        do
        {
          if (++count > NrOfRetries) return false;
          Thread.Sleep(NrOfMilliseconds);
        } while (FTPLockfileExists() > 0);

        Sup.LogTraceInfoMessage(message: $"Maps GetFTPLock : Found no lockfile, {count} times retried.");
      }

      Sup.LogTraceInfoMessage(message: $"Maps GetFTPLock : No lockfile so create one");

      // Now, create a lockfile, we have freedom as there is none
      try
      {
        int filecount;

        string requestname = "/public_html/" + lockfileName + RandomGenerator.RandomString(10, true);
        using (Stream lf = MapFluentFTP.OpenWrite(requestname, FtpDataType.Binary, false)) { lf.Dispose(); }
        FtpReply reply = MapFluentFTP.GetReply();  // Required by FluentFTP, not by me.

        // Check for the lockfiles existence: there can be only one, my own!!
        // If there are two, then by accident a second  process created a lockfile at the same time. 
        // Rare but not impossible.
        filecount = FTPLockfileExists();
        if ( filecount == 1)
        {
          Sup.LogTraceInfoMessage(message: $"Maps GetFTPLock : Filecount = {filecount} / Lockfile create success!! {requestname}");
          Sup.LogTraceInfoMessage(message: $"Maps GetFTPLock : Done");

          return true;
        }
        else
        {
          Sup.LogTraceWarningMessage(message: $"More than one lockfile ({filecount}) after creation so delete and fail lockfile creation.");

          MapFluentFTP.DeleteFile(requestname);
          return false;
        }
      }
      catch (Exception e) when (e is FtpException || e is TimeoutException)
      {
        // Just register the exception and fall through, returning and as such not registering the modifications.
        // Better next time
        Sup.LogTraceErrorMessage(message: $" Maps GetFTPLock Exception : {e.Message}");
        return false;
      }
    }

    public bool ReleaseFTPLock()
    {
      Sup.LogDebugMessage(message: $"Maps ReleaseFTPLock : start");

      FtpListItem[] filelist;

      try
      {
        filelist = MapFluentFTP.GetListing("/public_html/" + lockfileName + "*", FtpListOption.ForceList);

        foreach (FtpListItem file in filelist)
        {
          MapFluentFTP.DeleteFile(file.FullName);
          Sup.LogTraceInfoMessage(message: $"Maps Lockfile {file.FullName} released.");
        }
      }
      catch (Exception e) when (e is FtpException || e is TimeoutException)
      {
        // Just register the exception and fall through, returning and as such not registering the modifications.
        // Better next time
        Sup.LogTraceErrorMessage(message: $" Maps ReleaseFTPLock : TimeoutException: {e.Message}");
        return false;
      }

      return true;
    }

    private int FTPLockfileExists()
    {
      const int LockfileExpiration = 3;  // Minutes
      FtpListItem[] filelist;

      try
      {
        filelist = MapFluentFTP.GetListing("/public_html/" + lockfileName + "*", FtpListOption.ForceList);
      }
      catch (Exception e) when (e is FtpException || e is TimeoutException)
      {
        Sup.LogTraceErrorMessage(message: $" Maps FTPLockfileExists Exception 1: {e.Message}");
        return 0;  // No file found
      }

      int filecount = filelist.Length;
      Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists: LockfilesFound = {filecount}");

      if (filelist.Length > 0) // is there any lockfile present?
      {
        foreach (FtpListItem file in filelist)
        {
          try
          {
            TimeSpan ts;

            //Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists: Checking lockfile age of {file.FullName}");
            //Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists: Checking lockfile age - UtcNow = {DateTime.UtcNow}");
            //Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists: Checking lockfile age - File UTC Modified Time = {MapFluentFTP.GetModifiedTime(file.FullName, FtpDate.UTC)}");

            ts = DateTime.UtcNow - MapFluentFTP.GetModifiedTime(file.FullName, FtpDate.UTC);

            // Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists file age = {Math.Abs(ts.TotalMinutes)} minutes");

            if (Math.Abs(ts.TotalMinutes) > LockfileExpiration)   // Orphaned?
            {
              Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists: {file.FullName} can be removed because older than {LockfileExpiration} minutes");
              MapFluentFTP.DeleteFile(file.FullName);
              filecount--;
            }
          }
          catch (Exception e) when (e is FtpException || e is TimeoutException)
          {
            Sup.LogTraceErrorMessage(message: $" Maps FTPLockfileExists Exception 2: {e.Message}");
            return 1;  // Error so act as if a lockfile has been found
          }
        }
      }

      Sup.LogTraceInfoMessage(message: $" Maps FTPLockfileExists: There is {filecount} lockfile");
      return filecount;
    }

    #region IDisposable CuSupport
    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          MapFluentFTP.Dispose();
        }

        disposedValue = true;
      }
    }

    ~Maps()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}