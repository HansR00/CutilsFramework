/*
 * Website - Part of CumulusUtils
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

//
// Remember minification: 
//      1) c# minify string google search
//      2) https://nugetmusthaves.com/Tag/minify
//
// Javascript security / privacy :
//      1) https://web.dev/articles/csp?utm_source=devtools#summary
//      2) https://developers.google.com/privacy-sandbox/3pcd#report-issues
//      3) https://csp.withgoogle.com/docs/index.html
//      4) https://www.iothreat.com/blog/csp-script-src-unsafe-eval
//      5) https://developers.google.com/privacy-sandbox/3pcd/prepare/prepare-for-phaseout
//
// Javascript URL handling :
//      1) https://developer.mozilla.org/en-US/docs/Web/API/URL/origin
//      2) https://www.geeksforgeeks.org/how-to-modify-url-without-reloading-the-page-using-javascript/
//      3) https://stackoverflow.com/questions/3338642/updating-address-bar-with-new-url-without-hash-or-reloading-the-page
//      4) https://www.30secondsofcode.org/js/s/modify-url-without-reload/
//      5) https://html.spec.whatwg.org/multipage/nav-history-apis.html#the-history-interface
//      6) https://developer.mozilla.org/en-US/docs/Web/API/History
//      7) https://developer.mozilla.org/en-US/docs/Web/API/Window/pageshow_event
//

namespace CumulusUtils
{
    // RealtimeFields is used in cumulusutils.js library to index realtime.txt
    public enum RealtimeFields
    {
        date, timehhmmss, temp, hum, dew, wspeed, wlatest, bearing, rrate, rfall, press, currentwdir, beaufortnumber, windunit, tempunitnodeg, pressunit, rainunit,
        windrun, presstrendval, rmonth, ryear, rfallY, intemp, inhum, wchill, temptrend, tempTH, TtempTH, tempTL, TtempTL, windTM, TwindTM, wgustTM, TwgustTM, pressTH,
        TpressTH, pressTL, TpressTL, version, build, wgust, heatindex, humidex, UV, ET, SolarRad, avgbearing, rhour, forecastnumber, isdaylight, SensorContactLost,
        wdir, cloudbasevalue, cloudbaseunit, apptemp, SunshineHours, CurrentSolarMax, IsSunny, feelslike
    };

    // DashboardPanels is used in GenerateWebsite at specific location and the dedicated function GeneratePanelCode(thisPanel)
    public enum DashboardPanels
    {
        TemperatureText, PressureText, RainText, Clocks, WindGauge1, WindDirGauge1, WindRoseGauge1, WindText, SolarDisc, LunarDisc, HumidityText, SolarText,
        TemperatureGauge, OtherTempsGauge, PressureGauge, HumidityGauge, WindGauge2, WindDirGauge2, WindRoseGauge2, CloudBaseGauge, RainGauge, RainSpeedGauge, SolarGauge, UVGauge, Empty
    };

    class Website
    {
        private readonly string[] Package = new string[] {"index.html", "cumulusutils.js","cumuluscharts.txt","CUgauges.js","HighchartsDefaults.js","HighchartsLanguage.js",
                                     "suncalc.js","CUtween.min.js", "CUsteelseries.min.js","CURGraph.rose.js","CURGraph.common.core.js","CUlanguage.js",
                                     "CUgauges-ss.css"};

        private readonly string[] PanelsConfiguration;
        private readonly string StatisticsType;

        private readonly CuSupport Sup;
        private readonly InetSupport Isup;

        private readonly float Latitude;
        private readonly float Longitude;
        private readonly int Altitude;
        private readonly bool AltitudeInFeet;
        private readonly bool ShowSolar;
        private readonly bool ShowUV;
        private readonly bool DoStatistics;
        private readonly bool PermitGoogleOptOut;
        private readonly bool PwsfwiButtonInHeader;
        private InfoFromCMX thisCMXInfo;
        private CmxIPC thisIPC;
        private bool NewVersionAvailable;

        #region Initialisation and controlfunction
        public Website( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;

            Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );
            Longitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Longitude", "" ), CUtils.Inv );
            Altitude = Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "Altitude", "" ), CUtils.Inv );
            AltitudeInFeet = Sup.GetCumulusIniValue( "Station", "AltitudeInFeet", "" ).Equals( "1" );

            // ShowSolar and HasSolar are the same for now, but it may be different as HasSolar determines whether there is a sensor, ShowSolar is to show it on screen....
            // Difficult. May change into one variable (which must be global then: also used in Graphs
            ShowSolar = CUtils.HasSolar;
            ShowUV = Sup.GetUtilsIniValue( "Website", "ShowUV", "true" ).Equals( "true", CUtils.Cmp );

            StatisticsType = Sup.GetUtilsIniValue( "Website", "StatisticsType", "" );
            DoStatistics = !string.IsNullOrEmpty( Sup.GetUtilsIniValue( "Website", "StatisticsType", "" ) );
            PermitGoogleOptOut = Sup.GetUtilsIniValue( "Website", "PermitGoogleOptout", "false" ).Equals( "true", CUtils.Cmp );
            PwsfwiButtonInHeader = Sup.GetUtilsIniValue( "Website", "PwsfwiButtonInHeader", "true" ).Equals( "true", CUtils.Cmp );

            PanelsConfiguration = new string[ 24 ]
            {
                Sup.GetUtilsIniValue("Website", "Panel-1", "TemperatureText"),
                Sup.GetUtilsIniValue("Website", "Panel-2", "PressureText"),
                Sup.GetUtilsIniValue("Website", "Panel-3", "RainText"),
                Sup.GetUtilsIniValue("Website", "Panel-4", "Clocks"),
                Sup.GetUtilsIniValue("Website", "Panel-5", "WindGauge1"),
                Sup.GetUtilsIniValue("Website", "Panel-6", "WindDirGauge1"),
                Sup.GetUtilsIniValue("Website", "Panel-7", "WindRoseGauge1"),
                Sup.GetUtilsIniValue("Website", "Panel-8", "WindText"),
                Sup.GetUtilsIniValue("Website", "Panel-9", "SolarDisc"),
                Sup.GetUtilsIniValue("Website", "Panel-10", "LunarDisc"),
                Sup.GetUtilsIniValue("Website", "Panel-11", "HumidityText"),
                Sup.GetUtilsIniValue("Website", "Panel-12", "SolarText"),

                Sup.GetUtilsIniValue("Website", "Panel-13", "TemperatureGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-14", "OtherTempsGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-15", "PressureGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-16", "HumidityGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-17", "WindGauge2"),
                Sup.GetUtilsIniValue("Website", "Panel-18", "WindDirGauge2"),
                Sup.GetUtilsIniValue("Website", "Panel-19", "WindRoseGauge2"),
                Sup.GetUtilsIniValue("Website", "Panel-20", "CloudBaseGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-21", "RainGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-22", "RainSpeedGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-23", "SolarGauge"),
                Sup.GetUtilsIniValue("Website", "Panel-24", "UVGauge")
            };
        }

        public async Task GenerateWebsite()
        {
            Sup.LogDebugMessage( $"Generating Website: Starting" );

            Sup.LogDebugMessage( $"Generating Website: Generating index.html" );
            await GenerateIndexFileAsync();

            // From version 6.5.0 cumuluscharts.txt is generated by the compiler so, no need to have the default. 
            // But it is still generated to have the fallback in case of errors in the CutilsCharts.def which  leads to non-generation

            if ( !CUtils.Thrifty )
            {
                Sup.LogDebugMessage( $"Generating Website: Generating cumulusutils.js" );

                CUlib fncs = new CUlib( Sup );
                fncs.Generate();

                Sup.LogDebugMessage( $"Generating Website: Generating cumuluscharts.txt (only emergency fall back for the Compiler when syntax errors)" );
                await GenerateCumulusChartsTxt();
            }
            else
                Sup.LogDebugMessage( $"Generating Website: Only index.html done because Thrifty active" );

            return;
        }

        #endregion

        #region index.html
        private async Task GenerateIndexFileAsync()
        {
            StringBuilder indexFile = new StringBuilder();

            // USed in the Footer generation
            thisIPC = new CmxIPC( Sup, Isup );
            thisCMXInfo = await thisIPC.GetCMXInfoAsync();
            NewVersionAvailable = thisCMXInfo.NewBuildAvailable?.Equals( "1" ) ?? false;

            indexFile.Append( "<!DOCTYPE HTML>" +
              $"<html lang='{Sup.Locale}'><head>" +
              "<meta charset=\"UTF-8\"><meta name=\"description\" content=\"Cumulus standard Website, part of CumulusUtils by (c)Hans Rottier\"/>" +
              "<meta name=\"keywords\" content=\"Cumulus, weather, data, weather station, CumulusUtils\" />" +
              "<meta name=\"robots\" content=\"index, noarchive, follow, noimageindex, noimageclick\" />" +
              "<link rel=\"shortcut icon\" href=\"favicon.ico\" type=\"image/x-icon\" />" +
              "<meta name=\"theme-color\" content=\"#ffffff\" />" +
             $"<title>{Sup.GetCumulusIniValue( "Station", "LocName", "" )} - CumulusUtils</title>" +
              "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" );

#if !RELEASE
            ////
            //// https://stackoverflow.com/questions/25753708/how-to-view-javascript-console-on-android-and-ios
            ////
            //indexFile.Append( "<script src=\"https://cdn.jsdelivr.net/npm/eruda\"></script>" +
            //    "<script>eruda.init();</script>" );
#endif

            indexFile.Append( DoStatistics ? GenerateStatisticsCode( StatisticsType, false ) : "" );

            // Setting of DST moved to Index so it is always done even under thrifty
            TimeZoneInfo TZ;
            int DST = 0;

            TZ = TimeZoneInfo.Local;
            if ( TZ.SupportsDaylightSavingTime )
                DST = TZ.GetUtcOffset( DateTime.Today.AddHours( 12 ) ).Hours - TZ.BaseUtcOffset.Hours;

            indexFile.Append(
                    "<script>" +
                     $"var DST = {DST};" +
                    "</script>" );

            // Stub for non [deferred?] library includes
            // Currently all are at the bottom
            indexFile.Append(
              "<script defer src='https://ajax.googleapis.com/ajax/libs/jquery/3.6.0/jquery.min.js'></script>" +

              "<script defer src='https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/js/bootstrap.bundle.min.js' integrity='sha384-A3rJD856KowSb7dwlZdYEkO39Gagi7vIsF0jrRAoQmDKKtQBHUuLZ9AsSv4jD4Xa' crossorigin='anonymous'></script>" +
              "<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css' integrity='sha384-gH2yIJqKdNHPEq0n4Mqa/HGKIhSkIHeL5AyhkYV8i59U5AR6csBvApHHNl/vI1Bx' crossorigin='anonymous'>" +
              "<script src='https://unpkg.com/leaflet@1.5.1/dist/leaflet.js' integrity='sha512-GffPMF3RvMeYyc1LWMHtK8EbPv0iNZ8/oTtHPx9/cc2ILxQ+u905qIwdpULaqDkyBKgOaB57QTMg7ztg8Jm2Og==' crossorigin=''></script>" +
              "<link rel='stylesheet' href='https://unpkg.com/leaflet@1.5.1/dist/leaflet.css' integrity='sha512-xwE/Az9zrjBIphAcBb3F6JVqxf46+CDLwfLMHloNu6KEQCAWi6HcDUbeOfBIptF7tcCzusKFjFw2yuvEpDL9wQ==' crossorigin='' />" +
              "" );

            indexFile.Append(
                "<style>" +
                $".navbar {{background-color: {Sup.GetUtilsIniValue( "Website", "ColorMenuBackground", "Lightgrey" )}; color:{Sup.GetUtilsIniValue( "Website", "ColorMenuText", "Black" )};}}" +
                $".navbar a {{color:{Sup.GetUtilsIniValue( "Website", "ColorMenuText", "Black" )}; }}" +

                // Made according to:
                // https://www.geeksforgeeks.org/how-to-change-hamburger-toggler-color-in-bootstrap/
                $".custom-toggler.navbar-toggler {{ border-color: {Sup.GetUtilsIniValue( "Website", "ColorToggler", "Black" )}; }}" +
                $".custom-toggler .navbar-toggler-icon {{background-image: url(\"data:image/svg+xml;charset=utf8,%3Csvg viewBox='0 0 32 32' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath " +
                    $"stroke='{Sup.GetUtilsIniValue( "Website", "ColorToggler", "Black" )}' stroke-width='2' stroke-linecap='round' stroke-miterlimit='10' d='M4 8h24M4 16h24M4 24h24'/%3E%3C/svg%3E\");}}" +

                $".navbar span {{color:{Sup.GetUtilsIniValue( "Website", "ColorMenuText", "Black" )}; cursor: pointer; }}" +
                $".navbar-nav > li > .dropdown-menu {{background-color:{Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuBackground", "Lightgrey" )}; cursor: pointer;  color: {Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuText", "Black" )};}}" +
                $".navbar-nav > li > .dropdown-menu a:hover {{background-color:{Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverBackground", "Silver" )}; color: {Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverText", "Black" )};}}" +
                $".navbar-nav > li > a:hover {{background-color:{Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuBackground", "Lightgrey" )}; color: {Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverText", "Black" )};}}" +
                $".navbar-nav > li > .dropdown-menu li:hover {{background-color:{Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverBackground", "Silver" )}; cursor: pointer;  color: {Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverText", "Black" )};}}" +
                $".navbar-nav > li > li:hover {{background-color:{Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverBackground", "Lightgrey" )}; cursor:pointer; color: {Sup.GetUtilsIniValue( "Website", "ColorDropdownMenuHoverText", "Black" )};}}" +
                $".CUTitle {{text-align:center;background-color: {Sup.GetUtilsIniValue( "Website", "ColorTitleBar", "#E87510" )};color:{Sup.GetUtilsIniValue( "Website", "ColorTitleText", "White" )}; " +
                    $"background-image: url('{Sup.GetUtilsIniValue( "Website", "ColorTitleBackGroundImage", "" )}');}} " +
                $".CUCellTitle {{text-align: center;background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )};}} " +
                $".CUCellBody {{color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellText", "Black" )}; background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellBackground", "White" )}; text-align: center;min-height: 220px;padding: 0 !important;}} " +
                $".CUReport {{color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewText", "Black" )};background-color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewBackground", "White" )}; min-height: 600px;text-align: center;padding: 10px; }} " +
                $".CUReport td {{color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewTableData", "Black" )}; }} " +
                $".CUReport pre {{color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewText", "Black" )};background-color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewBackground", "White" )};}} " +
                $".CUReport p {{color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewText", "Black" )};padding:0px !important;}} " +
                $".CUReport p a {{ color: {Sup.GetUtilsIniValue( "Website", "ColorFooterLink", "#E87510" )} ; text-decoration: underline; }} " +
                $".CUReport p a:hover {{color: {Sup.GetUtilsIniValue( "Website", "ColorFooterLinkHover", "OrangeRed" )};}} " +
                $".CUFooter a {{ color: {Sup.GetUtilsIniValue( "Website", "ColorFooterLink", "#E87510" )} ; text-decoration: underline; }} " +
                $".CUFooter a:hover {{color: {Sup.GetUtilsIniValue( "Website", "ColorFooterLinkHover", "OrangeRed" )};}} " +
                $".CUFooter {{background-color: {Sup.GetUtilsIniValue( "Website", "ColorFooterBackground", "lightgrey" )}; color: {Sup.GetUtilsIniValue( "Website", "ColorFooterText", "Black" )}; min-height: 50px;}} " +
                 ".CUTable {text-align: center;margin: auto;} " +
                $".CUTable a {{ color: {Sup.GetUtilsIniValue( "Website", "ColorFooterLink", "#E87510" )} ; text-decoration: underline; }} " +
                $".CUTable a:hover {{color: {Sup.GetUtilsIniValue( "Website", "ColorFooterLinkHover", "OrangeRed" )};}} " +
                $".keytext {{color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewText", "Black" )};}} " +
                $".fwi_key {{color: {Sup.GetUtilsIniValue( "Website", "ColorReportviewText", "Black" )};}}" +
                "</style>" +
                "</head>" );

            // The body begins here
            indexFile.Append(
                $"<body style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorBodyBackground", "white" )};'>" + //whitesmoke
                "<div class='container-fluid border' style='padding: 5px'>" );

            // Start the header
            indexFile.Append(
                "<div class='col-sm-12 CUTitle'>" +
                "<table style='table-layout:fixed; width:100%; margin:auto' class='CUTable'><tr>" +
                $"<td style='width:20%;text-align:left'>" );

            if ( PwsfwiButtonInHeader )
            {
                indexFile.Append(
                    $"  <span onclick=\"LoadUtilsReport('pwsFWI.txt', false);\">{Sup.GetUtilsIniValue( "pwsFWI", "CurrentPwsFWI", "" )}</span><br/>" );
            }

            indexFile.Append(
                $"  {Sup.GetUtilsIniValue( "Website", "HeaderLeftText", "" )}</td>" +
                "<td style='width:60%;text-align:center'>" +
                $"  <h2 style = 'padding:10px' >{Sup.GetCumulusIniValue( "Station", "LocName", "" )} {Sup.GetUtilsIniValue( "Website", "SiteTitleAddition", "" )}</h2 > " +
                $"  <h5 style='padding:2px'>" +
                $"      {Sup.GetCUstringValue( "Website", "Latitude", "Latitude", false )}: {Latitude:F4}  " +
                $"      {Sup.GetCUstringValue( "Website", "Longitude", "Longitude", false )}: {Longitude:F4} " +
                $"      {Sup.GetCUstringValue( "Website", "Altitude", "Altitude", false )}: {Altitude} {( AltitudeInFeet ? "ft" : "m" )}" +
                $"  </h5>" +
                "</td>" +
                $"<td style='width:20%;text-align:right'>" +
                $"  {Sup.GetUtilsIniValue( "Website", "HeaderRightText", "" )}" +
                $"</td>" +
                "</tr></table>" +
                "</div >" );

            // Start writing the menu
            //
            indexFile.Append( await GenerateMenu() );

            // Do the modal forms for the About's
            //
            indexFile.Append(
              "<div class='modal fade' id='CUabout' tabindex='-1' role='dialog' aria-hidden='true'>" +
              "  <div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" +
              "  <div class='modal-content'><div class='modal-header'>" +
              "  <h5 class='modal-title'>About CumulusUtils</h5>" +
              "  <button type='button' class='close' data-bs-dismiss='modal' aria-label='Close'>" +
              "  <span aria-hidden='true'>&times;</span></button>" +
              "  </div>" +
              "  <div id='CUserContent' class='modal-body'>" +
              "  </div>" +
              "    <div class='modal-body'>" +
              @"This site is made with CumulusUtils, a generator tool for HTML presentation of weather data, retrieved from a weather station powered by <a href='https://cumulus.hosiene.co.uk/index.php'>Cumulus(MX)</a> (all versions). CumulusUtils is built and &copy; by Hans Rottier. It can be freely used and distributed under the Creative Commons 4.0 license (see Licenses). You can find it <a href='https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=17998' target='_blank'>here</a>.<br/> <br/>
It has a wiki of its own: <a href='https://cumuluswiki.org/a/Category:CumulusUtils'>go here</a>.<br/> <br/>
With a background in Forestry, chemistry and ICT, I started developing a Top10 list and - above all - a Fire Weather Index. This quickly evolved into a standard website generator and it became CumulusUtils. All versions can be used for the single modules as well. CumulusUtils is for cooperation with CumulusMX.<br/><br/>
CumulusUtils stands on the shoulders of the following:<br/><br/>
<ul>
<li><a href='https://cumulus.hosiene.co.uk/index.php'>CumulusMX</a> by Steve Loft (retired). Now maintained by Mark Crossley who also did the Gauges (based on the Steelseries). Beteljuice†, user on the Cumulus forum, who, often unsollicited, provided advice and ideas. He made a large range of PHP/javascript tools to enhance the basic sites.</li>
<li>Ken True of <a href='http://saratoga-weather.org/station.php'>Saratoga Weather</a> (Read the About of that site!), Murry Conarroe of <a href='http://weather.wildwoodnaturist.com/'>Wildwood Weather</a> had something to do with it.</li>
<li>Many sites I visited showed an enormous diversity and originality and therefore generated ideas. Look here for <a href='https://meteo-wagenborgen.nl/wp/2019/09/21/sites-which-carry-the-fire-weather-index-pwsfwi/'>a list of sites (the first 15)</a>, carrying CumulusUtils (or part of it), which inspired and contributed to the development and testing of the tool. Notably <a href='http://weather.inverellit.com/index.htm'><em>Phil’s Backyard</em></a>, which quickly became my first test site in the fire summer of 2020 in NSW Autralia. Thanks Phil! Also PaulMy's 'Komoka' played an important role closely followed by <a href='http://kocher.es/index.php'>Kocher.es</a> and <a href='http://meteo.laurentmey.fr/php/index.php'>Meyenheim</a> which showed the way for the statistics. And last but not least <a href='https://weather.wilmslowastro.com/'>Mark Crossley's site</a>.</li>
<li>Finally, FWIcalc by Graeme Kates. <em><a href='http://www.arthurspass.com/fwicalc'>FWIcalc</a></em>, has been developed (from 2002 to 2014) by Graeme Kates in New Zealand, on the basis of the <a href='https://meteo-wagenborgen.nl/wp/2019/08/11/fire-weather-the-canadian-fwi/'>FWI (Fire Weather Index)</a>, developed in Canada and also used in New-Zealand and France. Some other indices like the <a href='https://meteo-wagenborgen.nl/wp/2019/07/10/fire-weather-the-angstrom-index-and-the-fmi-index/'> Angstrom</a> and the <a href='https://meteo-wagenborgen.nl/wp/2019/07/08/the-chandler-burning-index/'>CBI (Chandler Burning Index)</a> are calculated by FWIcalc as well. Although pwsFWI has theoretically nothing to do with FWIcalc, it definitly is linked to it from a genealogists point of view.</li>
</ul>" +
              "  </div>" +
              "  <div class='modal-footer'>" +
              $"    <button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{Sup.GetCUstringValue( "Website", "Close", "Close", false )}</button>" +
              "  </div></div></div>" +
              "</div>" +
              "<div class='modal fade' id='CUserAbout' tabindex='-1' role='dialog' aria-hidden='true'>" +
              "  <div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" +
              "  <div class='modal-content'>" +
              "  <div class='modal-header'>" +
              "    <h5 class='modal-title'>About this site</h5>" +
              "    <button type='button' class='close' data-bs-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" +
              "  </div>" +
              "  <div id='CUserAboutTxt' class='modal-body'>Here comes the content of file CUserAbout.txt</div>" +
              "  <div class='modal-footer'>" +
              $"    <button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{Sup.GetCUstringValue( "Website", "Close", "Close", false )}</button>" +
              "  </div></div></div>" +
              "</div>" +
              "<div class='modal fade' id='CUlicense' tabindex='-1' role='dialog' aria-hidden='true'>" +
              "  <div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" +
              "  <div class='modal-content'>" +
              "  <div class='modal-header'>" +
              "    <h5 class='modal-title'>Licences</h5>" +
              "    <button type='button' class='close' data-bs-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" +
              "  </div>" +
              "    <div class='modal-body'>" +
              @"<a rel='license' href='http://creativecommons.org/licenses/by-nc-nd/4.0/'><img alt='Creative Commons Licence' style='border-width:0' src='https://i.creativecommons.org/l/by-nc-nd/4.0/80x15.png' /></a><br/>This work is licensed under a <a rel='license' href='http://creativecommons.org/licenses/by-nc-nd/4.0/'>Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License</a>.
The tool is constructed in C# and javascript by &copy; Hans Rottier, on the basis of the Bootstrap toolkit. Licence may change in future. 
<br/><br/>CumulusUtils is built in C# with the use of <a href='https://visualstudio.microsoft.com/vs/'>Microsoft Visual Studio</a> under the community Licence. The runtime system makes use of HTML and javascript (tools by MicroSoft, Mozilla and Google). The resulting HTML should pass <a href='https://validator.w3.org/'>the W3.org Markup Validation</a>. Please notify if any issue.
<br/><br/>Use is made of the following software libraries: <br/><br/>
<ul>
<li><a href='https://github.com/cumulusmx/CumulusMX'>Cumulus</a> - License: GNU GPL V 3.0;</li>
<li><a href='https://github.com/mcrossley/SteelSeries-Weather-Gauges'>Gauges by Mark Crossley (modified)</a> - License: GNU GPL V 2.0;<br/>
<li><a href='https://github.com/mcrossley/SteelSeries-Weather-Gauges'>Steelseries by Gerrit Grunwald & Mark Crossley</a> - License: GNU GPL V 2.0;<br/>
<li><a href='https://www.rgraph.net/'>RGraph</a> - License: MIT;<br/>
<li><a href='https://www.highcharts.com/'>HighCharts</a> - License: The personal non-commercial Highcharts <a href='https://shop.highcharts.com/contact/personal'>License</a>; </li>
<li><a href='https://github.com/jquery/jquery'>jQuery</a> - License: MIT;</li>
<li><a href='https://github.com/d3/d3'>d3</a> - License: BSD 3-Clause 'New' or 'Revised' License;</li>
<li><a href='https://github.com/mourner'>suncalc</a> - License: BSD 2-Clause 'Simplified' License.</li>
<li><a href='https://github.com/Leaflet/Leaflet'>Leaflet</a> - License: BSD 2-Clause 'Simplified' License;</li>
<li><a href='https://www.yourweather.co.uk'>Yourweather.co.uk</a> - License: <a href='https://creativecommons.org/licenses/by-sa/4.0/deed.en'>Attribution-ShareAlike 4.0 International</a> License (used for predictions);</li>
<li><a href='https://getbootstrap.com/'>Bootstrap</a> - License: <a href='https://github.com/twbs/bootstrap/blob/v4.4.1/LICENSE'>MIT</a>;</li>
</ul>

Please don't forget to get your own Personal <a href='https://shop.highcharts.com/contact/personal'>License</a> with Highcharts. <br>
Charting may change in future because of licensing issues.<br><br>
If I forgot anybody or anything or made the wrong interpretation or reference, please let me know and I will correct. You can contact me at the <a href='https://cumulus.hosiene.co.uk/viewforum.php?f=44'>Cumulus Support Forum</a>, user: HansR" +
              "  </div>" +
              "  <div class='modal-footer'>" +
              $"    <button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{Sup.GetCUstringValue( "Website", "Close", "Close", false )}</button>" +
              "  </div></div></div>" +
              "</div>" +
              "<div id='popup'></div>" );

            // Start the dashboard bootstrap Cells
            //
            indexFile.Append(
              "<div class='row' style='margin:auto'>" +
              "  <div class='col-xl-5 scrollable' id='NormalDashboard'>" +
              "<section id='Dashboard'>" +
              "    <div class='row'>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(1);\">" +
              // Default: TemperatureText
              $"       {GeneratePanelCode( PanelsConfiguration[ 0 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(2);\">" +
              // Default: PressureText
              $"       {GeneratePanelCode( PanelsConfiguration[ 1 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(3);\">" +
              // Default: RainText
              $"       {GeneratePanelCode( PanelsConfiguration[ 2 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(4);\">" +
              // Default: Clocks
              $"       {GeneratePanelCode( PanelsConfiguration[ 3 ] )}" +
              "      </div>" +
              "    </div>" +
              "    <div class='row' id='WindRow1'>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(5);\">" +
              // Default: WindGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 4 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(6);\">" +
              // Default: WindDirGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 5 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(7);\">" +
              // Default: WindRoseGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 6 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(8);\">" +
              // Default: WindText
              $"       {GeneratePanelCode( PanelsConfiguration[ 7 ] )}" +
              "      </div>" +
              "    </div>" +
              "    <div class='row'>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(9);\">" +
              // Default: SolarDisc
              $"       {GeneratePanelCode( PanelsConfiguration[ 8 ] )}" +
              "      </div >" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(10);\">" +
              // Default: LunarDisc
              $"       {GeneratePanelCode( PanelsConfiguration[ 9 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(11);\">" +
              // Default: HumidityText
              $"       {GeneratePanelCode( PanelsConfiguration[ 10 ] )}" +
              "      </div>" );

            if ( ShowSolar || ShowUV )
            {
                indexFile.Append( "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(12);\">" +
                // Default: SolarText
                $"       {GeneratePanelCode( PanelsConfiguration[ 11 ] )}" +
                "      </div>" );
            }

            indexFile.Append( "    </div>" +
              "  </section>" +
              "  <section id='Gauges'>" +
              "  <div class='row'>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(13);\">" +
              // Default: TemperatureGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 12 ] )}" +
              "    </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(14);\">" +
              // Default: OtherTempsGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 13 ] )}" +
              "    </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(15);\">" +
              // Default: PressureGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 14 ] )}" +
              "    </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(16);\">" +
              // Default: HumidityGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 15 ] )}" +
              "    </div>" +
              "  </div>" +
              "  <div class='row' id='WindRow2'>" +
              //  This space is filled by the dashboard switch through calls to .prependTo()
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(17);\">" +
              $"       {GeneratePanelCode( PanelsConfiguration[ 16 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(18);\">" +
              $"       {GeneratePanelCode( PanelsConfiguration[ 17 ] )}" +
              "      </div>" +
              "      <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(19);\">" +
              $"       {GeneratePanelCode( PanelsConfiguration[ 18 ] )}" +
              "      </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(20);\">" +
              //TemperatureText, PressureText, RainText, Clocks, WindGauge1, WindDirGauge1, WindRoseGauge1, WindText, SolarDisc, LunarDisc, HumidityText, SolarText,
              //TemperatureGauge, OtherTempsGauge, PressureGauge, HumidityGauge, WindGauge2, WindDirGauge2, WindRoseGauge2, CloudBaseGauge, RainGauge, RainSpeedGauge, SolarGauge, UVGauge
              // Default: CloudBaseGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 19 ] )}" +
              "    </div>" +
              "  </div>" +
              "  <div class='row'>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(21);\">" +
              // Default: RainGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 20 ] )}" +
              "    </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(22);\">" +
              // Default: RainSpeedGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 21 ] )}" +
              "    </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(23);\">" );


            if ( ShowSolar || ShowUV )
            {
                // Default: SolarGauge
                indexFile.Append( $"       {GeneratePanelCode( PanelsConfiguration[ 22 ] )}" +
              "    </div>" +
              "    <div class='col border rounded-lg CUCellBody' onclick=\"ClickGauge(24);\">" +
              // Default: UVGauge
              $"       {GeneratePanelCode( PanelsConfiguration[ 23 ] )}" +
              "    </div>" );
            }

            indexFile.Append( "  </div>" +
              "</section>" +
              "<section id='ExtraAndCustom'>" +
              //"  <div class='col border rounded-lg CUCellBody' style='overflow - y: scroll;'></div>" +
              "</section>" +
              "  </div>" + // id='NormalDashboard' 
              "  <div class='col-xl-7  scrollable'>" +
              "    <div id='CUReportView' class='scrollable CUReport'></div>" +
              "  </div>" +
              "</div>" +
              "<div class='col-sm-12 CUFooter'>" +
              "  <table style='width:100%; margin:auto'><tr>" +
              "    <td style='text-align: left; font-size: smaller'>Powered by <a href='https://cumulus.hosiene.co.uk/index.php'>Cumulus[MX]</a>&nbsp;" +
              $"         <span id=programVersion>&nbsp;{thisCMXInfo.Version}&nbsp;(build:&nbsp;{thisCMXInfo.Build})</span>" +
              $"         &nbsp;{( NewVersionAvailable ? "[New version available:&nbsp;build: " + thisCMXInfo.NewBuildNumber + "]" : "" )}<br/>" +
               "      Gauges by Mark Crossley. See further under <a data-bs-toggle='modal' href='#CUabout'>About</a> / <a data-bs-toggle='modal' href='#CUlicense'>Licenses</a>.</td>" +
              $"   <td style='text-align: right; font-size: smaller'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</td>" +
              "</tr></table>" +
              "</div>" +
              "</div>" );

            indexFile.Append( Sup.GenHighchartsIncludes() );

            indexFile.Append(
                "<script defer src='https://cdnjs.cloudflare.com/ajax/libs/d3/5.15.1/d3.min.js'></script>" +
                "" );

            indexFile.Append( "<!--STEELSERIES-->" +
                "<link href='css/CUgauges-ss.css' rel='stylesheet'/>" +

                "<script defer src='lib/suncalc.js'></script>" +

                "<script defer src='lib/CUtween.min.js'></script>" +
                "<script defer  src='lib/CUsteelseries.min.js'></script>" +

                "<script defer  src='lib/CURGraph.common.core.js'></script>" +
                "<script defer  src='lib/CURGraph.rose.js'></script>" +
                "<script defer  src='lib/CUlanguage.js'></script>" +

                "<!--- Other utility includes of my own making -->" +
                "<script defer src='lib/HighchartsDefaults.js'></script>" +
                "<script defer src='lib/HighchartsLanguage.js'></script>" +
                "<script defer src='lib/cumulusutils.js'></script>" +
                "" );

            indexFile.Append(
            "</body>" +
              "</html>"
              );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}index.html", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

#if !RELEASE
                of.WriteLine( indexFile );
#else
                of.WriteLine( CuSupport.StringRemoveWhiteSpace( indexFile.ToString(), " " ) );
#endif
            }

            return;
        }

        #endregion

        #region cumuluscharts.txt
        private async Task GenerateCumulusChartsTxt()
        {
            List<OutputDef> theseCharts;

            if ( File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                // Note:"This might be combined with the CompileOnly command handling because it is exactly the same!!
                // For the time being it remains here.
                Sup.LogDebugMessage( $"CutilsCharts.def exists so: Parsing User Defined {Sup.PathUtils}{Sup.CutilsChartsDef}" );

                ChartsCompiler c = new ChartsCompiler( Sup );
                theseCharts = c.ParseChartDefinitions();

                // In case of error no new file is generated and the old one remains in place
                // null means no Defs file present or errors in parsing
                if ( theseCharts != null )
                {
                    int i = 0;

                    Sup.LogDebugMessage( $"{Sup.PathUtils}{Sup.CutilsChartsDef} exists and its content is valid so: Generating User Defined cumuluscharts.txt" );
                    foreach ( OutputDef thisDef in theseCharts )
                    {
                        // Generate
                        c.GenerateUserDefinedCharts( thisDef.TheseCharts, thisDef.Filename, i++ );

                        // and Upload
                        Sup.LogTraceInfoMessage( $"Uploading = {thisDef.Filename}" );
                        _ = await Isup.UploadFileAsync( $"{thisDef.Filename}", $"{Sup.PathUtils}{thisDef.Filename}" );
                    }
                }
                else
                {
                    Sup.LogDebugMessage( $"Errors in Charts definition. See logfile, please correct and run again." );
                    Sup.LogTraceErrorMessage( $"No new cumuluscharts.txt is generated and the old one remains in place!" );
                }
            } // End generating the cumuluscharts.txt as a result of CutilsCharts.def file - a totally new concept of generating the charts
            else // Default charts
            {
                Sup.LogDebugMessage( $"{Sup.PathUtils}{Sup.CutilsChartsDef} does not exist so: Generating standard cumuluscharts.txt" );

                StringBuilder CumulusCharts = new StringBuilder();

                // We are still using the default ones set by Steve for Cumulus, it contains the Grid theme for Highcharts JS @author Torstein Honsi
                // That theme is included as HighchartsDefaults.js
                // For v 4.0.0 we start using the parameters of Cumulus as those are now set by the user in Cumulus: what does he want to see for:
                //   All different temperatures + the inside temp/humidity in the following paramas:
                //      TempVisible=1
                //      InTempVisible = 1
                //      HIVisible = 1
                //      DPVisible = 1
                //      WCVisible = 1
                //      AppTempVisible = 1
                //      FeelsLikeVisible = 1
                //      HumidexVisible = 1
                //      InHumVisible = 1
                //      OutHumVisible = 1

                // So we need to get the parameters and store in booleans for later use where:
                //      Temperature is always visible.
                //      if one of the inside temps is invisible, CumulusUtils displays neither
                //      if Solar exists, then UV is assumed to be present as well
                // 
                // ShowInsideMeasurements is driving the inside values, even if in Cumulus they are set to display
                //
                bool ShowInsideMeasurements = Sup.GetUtilsIniValue( "Website", "ShowInsideMeasurements", "true" ).Equals( "true", CUtils.Cmp );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}cumuluscharts.txt", false, Encoding.UTF8 ) )
                {
                    CumulusCharts.Append( "<script>" +
                      "$('#graph0').change(function() {handleChange()});" +
                      "function handleChange(){" +
                      "var w1 = document.getElementById('graph0').value;" + // = CurrentChart;
                      "if (w1 == 'Temp') { doTemp(); }" +
                      "if (w1 == 'DailyTemp') { doDailyTemp(); }" +
                      "if (w1 == 'Pression') { doPress(); }" +
                      "if (w1 == 'WindSpeed') { doWind(); }" +
                      "if (w1 == 'WindDir') { doWindDir(); }" +
                      "if (w1 == 'Rain') { doRain(); }" +
                      "if (w1 == 'DailyRain') { doDailyRain(); }" +
                      "if (w1 == 'Humidity') { doHum(); }" +
                      "if (w1 == 'Solar') { doSolar(); }" +
                      "if (w1 == 'SunHours') { doSunHours(); }" +
                      "}" +
                      "</script><div>" +
                      $"<p style='text-align:center;'><select id='graph0'>" +
                      $"<option value='Temp' selected>{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", false )}</option>" +
                      $"<option value='DailyTemp'>{Sup.GetCUstringValue( "Website", "DailyTemperature", "Daily Temperature", false )}</option>" +
                      $"<option value='Pression'>{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", false )}</option>" +
                      $"<option value='WindSpeed'>{Sup.GetCUstringValue( "Website", "WindSpeed", "Wind Speed", false )}</option>" +
                      $"<option value='WindDir'>{Sup.GetCUstringValue( "Website", "WindDirection", "Wind Direction", false )}</option>" +
                      $"<option value='Rain'>{Sup.GetCUstringValue( "Website", "Rainfall", "Rainfall", false )}</option>" +
                      $"<option value='DailyRain'>{Sup.GetCUstringValue( "Website", "Daily Rainfall", "Daily Rainfall", false )}</option>" +
                      $"<option value='Humidity'>{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", false )}</option>" );

                    if ( ShowSolar )
                    {
                        CumulusCharts.Append( $"<option value='Solar'>{Sup.GetCUstringValue( "Website", "Solar", "Solar", false )}</option>" +
                          $"<option value='SunHours'>{Sup.GetCUstringValue( "Website", "SunHours", "Sun Hours", false )}</option>" );
                    }

                    CumulusCharts.Append( "</select>" +
                      $"</p></div><div id='chartcontainer' " +
                        $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'></div>" +
                      "<script>" +
                     $"var chart, config;" +

                    //"$(function() {InitCumulusCharts = InitCumulusCharts0; InitCumulusCharts();});" +
                    "$(function() {InitCumulusCharts()});" +
                      "function InitCumulusCharts()" +
                      "{" +
                      "  $.ajax({" +
                     $"    url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}graphconfig.json', " +
                      "    dataType: 'json', " +
                      "    success: function(result) { config = result; handleChange() }" +
                      "  });" +
                      "}" +
                      "" +
                      //  Temperature
                      "var doTemp=function()" +
                      "{" +
                      "  var n=config.temp.units==='C'?0:32," +
                      "  t={chart:{renderTo:'chartcontainer',type:'line',alignTicks:false}," +
                      "" +
                     $"title:{{text:'{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", true )}'}}," +
                      "credits:{enabled:true}," +
                      "xAxis:{" +
                      "  type:'datetime'," +
                      "  ordinal:false," +
                      "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                      "}," +
                      "yAxis:[" +
                      "{" +
                     $"  title:{{text:'{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", true )} (\u00b0'+config.temp.units+')'}}," +
                      "  opposite:false," +
                      "  labels:{align:'right',x:-5}" +
                      "}," +
                      "{" +
                      "  linkedTo:0," +
                      "  gridLineWidth:0," +
                      "  opposite:true," +
                      "  title:{text:null}," +
                      "  labels:{align:'left',x:5}" +
                      "} ], " +
                      "legend:{enabled:true}," +
                      "tooltip:{valueSuffix:'°'+config.temp.units,valueDecimals:config.temp.decimals,xDateFormat:'%A, %b %e, %H:%M'}," +
                      $"series:[]," +
                      "rangeSelector:{buttons:[{count:6,type:\"hour\",text:\"6h\"},{count:12,type:\"hour\",text:\"12h\"},{type:\"all\",text:\"All\"}],inputEnabled:false}};" +
                      "chart=new Highcharts.stockChart(t);chart.showLoading();" +
                      "" +
                      "$.ajax({" +
                     $"  url:\"{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}tempdata.json\"," +
                      "  cache:false," +
                      "  dataType:\"json\"," +
                      "  success:function(resp){ " +
                      "    var titles = {" +
                      $"     'temp'     : '{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", true )}'," +
                      $"     'dew'      : '{Sup.GetCUstringValue( "Website", "Dewpoint", "Dew Point", true )}'," +
                      $"     'apptemp'  : '{Sup.GetCUstringValue( "Website", "Apparent", "Apparent", true )}'," +
                      $"     'feelslike': '{Sup.GetCUstringValue( "Website", "FeelsLike", "Feels Like", true )}'," +
                      $"     'wchill'   : '{Sup.GetCUstringValue( "Website", "WindChill", "Wind Chill", true )}'," +
                      $"     'heatindex': '{Sup.GetCUstringValue( "Website", "HeatIndex", "Heat Index", true )}'," +
                      $"     'humidex'  : '{Sup.GetCUstringValue( "Website", "Humidex", "Humidex", true )}'," +
                      $"     'intemp'   : '{Sup.GetCUstringValue( "Website", "Inside", "Inside", true )}'" +
                      "    };" +
                      "    var lineColors = {" +
                      $"     'temp'     : '{Sup.GetUtilsIniValue( "Website", "HomeGraphTempColor", "#058DC7" )}'," +
                      $"     'dew'      : '{Sup.GetUtilsIniValue( "Website", "HomeGraphDewPointColor", "#50B432" )}'," +
                      $"     'apptemp'  : '{Sup.GetUtilsIniValue( "Website", "HomeGraphApparentTempColor", "#ED561B" )}'," +
                      $"     'feelslike': '{Sup.GetUtilsIniValue( "Website", "HomeGraphFeelsLikeColor", "#DDDF00" )}'," +
                      $"     'wchill'   : '{Sup.GetUtilsIniValue( "Website", "HomeGraphWindChillColor", "#24CBE5" )}'," +
                      $"     'heatindex': '{Sup.GetUtilsIniValue( "Website", "HomeGraphHeatIndexColor", "#64E572" )}'," +
                      $"     'humidex'  : '{Sup.GetUtilsIniValue( "Website", "HomeGraphHumidexColor", "#FF9655" )}'," +
                      $"     'intemp'   : '{Sup.GetUtilsIniValue( "Website", "HomeGraphInsideTempColor", "#6AF9C4" )}'" +
                      "    };" +
                      $"    var idxs = ['temp', 'dew', 'apptemp', 'feelslike', 'wchill', 'heatindex', 'humidex'{( ShowInsideMeasurements ? ", 'intemp'" : "" )}];" +
                      "    var cnt = 0;" +
                      "    idxs.forEach(function(idx) {" +
                      "      if (idx in resp) {" +
                      "        chart.addSeries({" +
                      "           name: titles[idx]," +
                      "           color: lineColors[idx]," +
                      "           data: resp[idx]" +
                      "        }, false);" +
                      "        if (idx === 'temp'){" +
                      "          chart.series[cnt].options.zIndex = 99;" +
                      "        }" +
                      "        cnt++;" +
                      "      }" +
                      "    });" +
                      "    chart.hideLoading();" +
                      "    chart.redraw();" +
                      "  }}" +
                      ")}," +
                      "" +
                      "doPress=function()" +
                      "{" +
                      "   var n={chart:{renderTo:'chartcontainer',type:'line',alignTicks:false}," +
                      "" +
                     $"title:{{text:'{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", true )}'}}," +
                      "credits:{enabled:true}," +
                      "xAxis:" +
                      "  {" +
                      "    type: 'datetime'," +
                      "    ordinal:false," +
                      "    dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                      "  }," +
                      "yAxis:[" +
                      "  {" +
                     $"    title:{{text:'{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", true )} ('+config.press.units+')'}}," +
                      "    opposite:false," +
                      "    labels:{align:'right',x:-5}" +
                      "  }," +
                      "  {" +
                      "    linkedTo:0," +
                      "    gridLineWidth:0," +
                      "    opposite:true," +
                      "    title:{text:null}," +
                      "    labels:{align:'left',x:5}" +
                      "  }" +
                      "]," +
                      "legend:{enabled:true}," +
                      "tooltip:{valueSuffix:config.press.units,valueDecimals:config.press.decimals,xDateFormat:'%A, %b %e, %H:%M'}," +
                      "series:" +
                      "[" +
                      "  {" +
                     $"    name:'{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", true )}'," +
                     $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphPressureColor", "#058DC7" )}'" +
                      "  }" +
                      "]," +
                      "rangeSelector:{ buttons:[{count:6,type:'hour',text:'6h'},{count:12,type:'hour',text:'12h'},{type:'all',text:'All'}],inputEnabled:false}" +
                      "};" +
                      "" +
                      "chart=new Highcharts.stockChart(n);chart.showLoading();" +
                      "" +
                      "$.ajax({" +
                     $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}pressdata.json'," +
                      "         dataType:'json'," +
                      "         cache:false," +
                      "         success:function(n){chart.hideLoading();chart.series[0].setData(n.press)}" +
                      "  })" +
                      "}," +
                      "" +
                      "compassP=function(n){return['N','NE','E','SE','S','SW','W','NW'][Math.floor((n+22.5)/45)%8]}," +
                      "" +
                      "doWindDir=function()" +
                      "{  var n={chart:{renderTo:'chartcontainer',type:'scatter',alignTicks:false}," +
                      "" +
                     $"title:{{text:'{Sup.GetCUstringValue( "Website", "WindDirection", "Wind Direction", true )}'}}," +
                      "credits:{enabled:false}," +
                    $"{( Graphx.UseHighchartsBoostModule ? "boost: {useGPUTranslations: true, usePreAllocated: true}," : "" )}" +
                     $"" +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:[" +
                     "  {" +
                    $"    title:{{text:'{Sup.GetCUstringValue( "Website", "Bearing", "Bearing", true )}'}}," +
                     "    opposite:false," +
                     "    min:0,max:360," +
                     "    tickInterval:45," +
                     "    labels:{align:'right',x:-5,formatter:function(){return compassP(this.value)}}" +
                     "  }," +
                     "  {" +
                     "    linkedTo:0," +
                     "    gridLineWidth:0," +
                     "    opposite:true," +
                     "    title:{text:null}," +
                     "    min:0,max:360," +
                     "    tickInterval:45," +
                     "    labels:{align:'left',x:5,formatter:function(){return compassP(this.value)}}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "plotOptions:{" +
                     "  scatter:{" +
                    $"    {( Graphx.UseHighchartsBoostModule ? "boostThreshold: 200," : "" )}" +
                     "  }" +
                     "}," +
                      "tooltip:{enabled:false}," +
                      "series:[" +
                     "  {" +
                    $"    name: '{Sup.GetCUstringValue( "Website", "Bearing", "Bearing", true )}', " +
                     "    type: 'scatter'," +
                    $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphBearingColor", "#058DC7" )}'" +
                     "  }," +
                     "  {" +
                    $"    name: '{Sup.GetCUstringValue( "Website", "AvBearing", "Avg Bearing", true )}'," +
                     "    type: 'scatter'," +
                    $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphAverageBearingColor", "#ED561B" )}'" +
                     "  }" +
                     "]," +
                     "rangeSelector:{buttons:[{count:6,type:'hour',text:'6h'},{count:12,type:'hour',text:'12h'},{type:'all',text:'All'}],inputEnabled:false}};" +
                     "chart=new Highcharts.stockChart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}wdirdata.json'," +
                     "  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.bearing);chart.series[1].setData(n.avgbearing)}" +
                     "  })" +
                     "}," +
                     "" +
                     "doWind=function()" +
                     "{" +
                     "  var n={chart:{renderTo:\"chartcontainer\",type:\"line\",alignTicks:false}," +
                     "" +
                    $"title:{{text:'{Sup.GetCUstringValue( "Website", "WindSpeed", "Wind Speed", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                    $"yAxis:" +
                     "[" +
                     "  {" +
                    $"    title:{{text:'{Sup.GetCUstringValue( "Website", "WindSpeed", "Wind Speed", true )} ('+config.wind.units+')'}}," +
                     "    opposite:false," +
                     "    min:0," +
                     "    labels:{align:'right',x:-5}" +
                     "  }," +
                     "  {" +
                     "    linkedTo:0," +
                     "    gridLineWidth:0," +
                     "    opposite:true," +
                     "    min:0," +
                     "    title:{text:null}," +
                     "    labels:{align:'left',x:5}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{valueSuffix:config.wind.units,valueDecimals:config.wind.decimals,xDateFormat:'%A, %b %e, %H:%M'}," +
                     "series:" +
                     "[" +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "WindSpeed", "Wind Speed", true )}'," +
                    $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphWindSpeedColor", "#058DC7" )}'" +
                     "  }," +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "WindGust", "Wind Gust", true )}'," +
                    $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphWindGustColor", "#ED561B" )}'" +
                     "  }" +
                     "]," +
                     "rangeSelector:{buttons:[{count:6,type:'hour',text:'6h'},{count:12,type:'hour',text:'12h'},{type:'all',text:'All'}],inputEnabled:false}};" +
                     "chart=new Highcharts.stockChart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}winddata.json'," +
                    $"  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.wspeed);chart.series[1].setData(n.wgust)}" +
                     "  })" +
                     "}," +
                     "" +
                     "doRain=function()" +
                     "{" +
                     "  var n={chart:{renderTo:'chartcontainer',type:'line',alignTicks:true}," +
                     "" +
                    $"title:{{text:'{Sup.GetCUstringValue( "Website", "Rainfall", "Rainfall", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:" +
                     "[" +
                     "  {" +
                     $"   title:{{text:'{Sup.GetCUstringValue( "Website", "RainfallRate", "Rainfall Rate", true )} ('+config.rain.units+'{Sup.PerHour})'}}," +
                     "    min:0," +
                     "    opposite:false," +
                     "    labels:{align:'right',x:-5}" +
                     "  }," +
                     "  {" +
                     "    opposite:true," +
                    $"    title:{{text:'{Sup.GetCUstringValue( "Website", "Rainfall", "Rainfall", true )} ('+config.rain.units+')'}}," +
                     "    min:0," +
                     "    labels:{align:'left',x:5}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{valueDecimals:config.rain.decimals,xDateFormat:'%A, %b %e, %H:%M'}," + // shared:true,crosshairs:true,
                     "series:" +
                     "[" +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "Rainfall", "Rainfall", true )}'," +
                     "    type:'area'," +
                    $"    {GetAreaGradientColors( Sup.GetUtilsIniValue( "Website", "HomeGraphRainfallColor1", "#50B432" ), Sup.GetUtilsIniValue( "Website", "HomeGraphRainfallColor2", "" ), Sup.GetUtilsIniValue( "Website", "HomeGraphRainfallColor3", "" ) )}" +
                     "    yAxis:1," +
                     "    tooltip:{valueSuffix:config.rain.units}" +
                     "  }," +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "RainfallRate", "Rainfall Rate", true )}'," +
                     "    type:'line'," +
                    $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphRainRateColor", "#058DC7" )}'," +
                     "    yAxis:0," +
                    $"    tooltip:{{valueSuffix:config.rain.units+'{Sup.PerHour}'}}" +
                     "  }" +
                     "]," +
                     "rangeSelector:{buttons:[{count:6,type:'hour',text:'6h'},{count:12,type:'hour',text:'12h'},{type:'all',text:'All'}],inputEnabled:false}};" +
                     "chart=new Highcharts.stockChart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}raindata.json'," +
                     "  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.rfall);chart.series[1].setData(n.rrate)}" +
                     "  })" +
                     "}," +
                     "" +
                     "doHum=function()" +
                     "{" +
                     "  var n={chart:{renderTo:'chartcontainer',type:'line',alignTicks:false}," +
                    $"title:{{text:'{Sup.GetCUstringValue( "Website", "RelativeHumidity", "Relative Humidity", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:" +
                     "[" +
                     "  {" +
                    $"    title:{{text:'{Sup.GetCUstringValue( "Website", "RelativeHumidity", "Relative Humidity", true )} (%)'}}," +
                     "    opposite:false," +
                     "    min:0,max:100," +
                     "    labels:{align:\"right\",x:-5}" +
                     "  }," +
                     "  {" +
                     "    linkedTo:0," +
                     "    gridLineWidth:0," +
                     "    opposite:true," +
                     "    min:0,max:100," +
                     "    title:{text:null}," +
                     "    labels:{align:\"left\",x:5}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{valueSuffix:'%',valueDecimals:config.hum.decimals,xDateFormat:'%A, %b %e, %H:%M'}," + // shared:true, crosshairs: true,
                     "series:[]," +
                     "rangeSelector:{buttons:[{count:6,type:'hour',text:'6h'},{count:12,type:'hour',text:'12h'},{type:'all',text:'All'}],inputEnabled:false}};" +
                     "chart=new Highcharts.stockChart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}humdata.json'," +
                    $"  dataType:'json'," +
                     "  cache:false," +
                     "  success: function (resp) {" +
                     "    var titles = {" +
                    $"      'hum'  : '{Sup.GetCUstringValue( "Website", "OutdoorHumidity", "Outdoor Humidity", true )}'," +
                    $"      'inhum': '{Sup.GetCUstringValue( "Website", "IndoorHumidity", "Indoor Humidity", true )}'" +
                     "    };" +
                     "    var lineColors = {" +
                    $"     'hum'    : '{Sup.GetUtilsIniValue( "Website", "HomeGraphHumidityColor", "#058DC7" )}'," +
                    $"     'inhum'  : '{Sup.GetUtilsIniValue( "Website", "HomeGraphInsideHumidityColor", "#50B432" )}'" +
                     "    };" +
                    $"    var idxs = ['hum'{( ShowInsideMeasurements ? ", 'inhum'" : "" )}];" +
                     "    var cnt = 0;" +
                     "    idxs.forEach(function(idx) {" +
                     "      if (idx in resp) {" +
                     "        chart.addSeries({" +
                     "          name: titles[idx]," +
                     "          color: lineColors[idx]," +
                     "          data: resp[idx]" +
                     "        }, false);" +
                     "        cnt++;" +
                     "      }" +
                     "    });" +
                     "    chart.hideLoading();" +
                     "    chart.redraw();" +
                     "  }" +
                     "})" +
                     "}," +
                     "" +
                     "doSolar=function()" +
                     "{" +
                     "  var n={chart:{renderTo:'chartcontainer',type:'line',alignTicks:true}," +
                     "" +
                     $"title:{{text:'{Sup.GetCUstringValue( "Website", "Solar", "Solar", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:" +
                     "[" +
                     "  {" +
                     $"    title:{{text:'{Sup.GetCUstringValue( "Website", "SolarRadiation", "Solar Radiation", true )} (W/m²)'}}," +
                     "     min:0," +
                     "     opposite:false," +
                     "     labels:{align:\"right\",x:-5}" +
                     "  }," +
                     "  {" +
                     "    opposite:true," +
                    $"    title:{{text:'{Sup.GetCUstringValue( "Website", "UVindex", "UV index", true )}'}}," +
                     "    min:0," +
                     "    labels:{align:\"left\",x:5}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{xDateFormat:'%A, %b %e, %H:%M'}," +
                     "series:" +
                     "[" +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "TheoreticalMax", "Theoretical Max", true )}'," +
                     "    type:'area'," +
                    $"    {GetAreaGradientColors( Sup.GetUtilsIniValue( "Website", "HomeGraphSolarTheoreticalMaxColor1", "#DDDF00" ), Sup.GetUtilsIniValue( "Website", "HomeGraphSolarTheoreticalMaxColor2", "" ), Sup.GetUtilsIniValue( "Website", "HomeGraphSolarTheoreticalMaxColor3", "" ) )}" +
                     "    yAxis:0," +
                     "    valueDecimals:0," +
                     "    tooltip:{valueSuffix:'W/m²'}" +
                     "  }," +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "SolarRadiation", "Solar Radiation", true )}'," +
                     "    type:'area'," +
                    $"    {GetAreaGradientColors( Sup.GetUtilsIniValue( "Website", "HomeGraphSolarRadiationColor1", "#FF9655" ), Sup.GetUtilsIniValue( "Website", "HomeGraphSolarRadiationColor2", "" ), Sup.GetUtilsIniValue( "Website", "HomeGraphSolarRadiationColor3", "" ) )} " +
                     "    yAxis:0," +
                     "    valueDecimals:0," +
                     "    tooltip:{valueSuffix:'W/m²'} " +
                     "  }," +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "UVindex", "UV index", true )}'," +
                     "    type:'line'," +
                    $"    color: '{Sup.GetUtilsIniValue( "Website", "HomeGraphUVindexColor", "#058DC7" )}'," +
                     "    yAxis:1," +
                     "    valueDecimals:config.uv.decimals," +
                     "  }" +
                     "]," +
                     "rangeSelector:{buttons:[{count:6,type:\"hour\",text:\"6h\"},{count:12,type:\"hour\",text:\"12h\"},{type:\"all\",text:\"All\"}],inputEnabled:false}};" +
                     "chart=new Highcharts.stockChart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}solardata.json'," +
                     "  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.CurrentSolarMax);chart.series[1].setData(n.SolarRad);chart.series[2].setData(n.UV)}" +
                     "  })" +
                     "}," +
                     "" +
                     "doSunHours=function()" +
                     "{" +
                     "  var n={chart:{renderTo:'chartcontainer',type:'column',alignTicks:false}," +
                     "" +
                     $"title:{{text:'{Sup.GetCUstringValue( "Website", "SunHours", "Sun Hours", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:" +
                     "[" +
                     "  {" +
                     $"    title:{{text:'{Sup.GetCUstringValue( "Website", "SunshineHours", "Sunshine Hours", true )}'}}," +
                     "     min:0," +
                     "     opposite:false," +
                     "     labels:{align:\"right\",x:-12}" +
                     "  }," +
                     "  {" +
                     "    linkedTo:0," +
                     "    gridLineWidth:0," +
                     "    opposite:true," +
                     "    title:{text:null}," +
                     "    labels:{align:'left',x:12}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{xDateFormat:'%A, %b %e'}," +
                     "series:" +
                     "[" +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "SunHours", "Sun Hours", true )}'," +
                     "    type:'column'," +
                    $"    color:'{Sup.GetUtilsIniValue( "Website", "HomeGraphSunHoursColor", "gold" )}'," +
                     "    yAxis:0," +
                     "    valueDecimals:1," +
                     "    tooltip:{valueSuffix:' Hrs'}" +
                     "  }" +
                     "]};" +
                     "" +
                     "chart=new Highcharts.chart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}sunhours.json'," +
                    $"  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.sunhours)} " +
                     "  })" +
                     "}," +
                     "" +
                     "doDailyRain=function()" +
                     "{" +
                     "  var n={chart:{renderTo:'chartcontainer',type:'column',alignTicks:false}," +
                     "" +
                     $"title:{{text:'{Sup.GetCUstringValue( "Website", "Daily Rainfall", "Daily Rainfall", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:" +
                     "[" +
                     "  {" +
                    $"    title:{{text:\"{Sup.GetCUstringValue( "Website", "Daily Rainfall", "Daily Rainfall", true )}\"}}," +
                     "    min:0," +
                     "    opposite:false," +
                     "    labels:{align:'right',x:-12}" +
                     "  }," +
                     "  {" +
                     "    linkedTo:0," +
                     "    gridLineWidth:0," +
                     "    opposite:true," +
                     "    title:{text:null}," +
                     "    labels:{align:\"left\",x:12}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{xDateFormat:'%A, %b %e'}," +
                     "series:" +
                     "[" +
                     "  {" +
                    $"    name:'{Sup.GetCUstringValue( "Website", "Daily Rainfall", "Daily Rainfall", true )}'," +
                     "    type:'column'," +
                    $"    color:'{Sup.GetUtilsIniValue( "Website", "HomeGraphDailyRainColor", "#058DC7" )}'," +
                    "    yAxis:0," +
                     "    valueDecimals:config.rain.decimals," +
                     "    tooltip:{valueSuffix:config.rain.units}" +
                     "  }" +
                     "]};" +
                     "" +
                     "chart=new Highcharts.chart(n);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}dailyrain.json'," +
                    $"  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.dailyrain)}" +
                     "  })" +
                     "}," +
                     "" +
                     "doDailyTemp=function()" +
                     "{" +
                     "  var n=config.temp.units===\"C\"?0:32,t={chart:{renderTo:\"chartcontainer\",type:\"spline\",alignTicks:false}," +
                    $"title:{{text:'{Sup.GetCUstringValue( "Website", "DailyTemperature", "Daily Temperature", true )}'}}," +
                     "credits:{enabled:false}," +
                     "xAxis:" +
                     "{" +
                     "  type:'datetime'," +
                     "  ordinal:false," +
                     "  dateTimeLabelFormats:{day:'%e %b',week:'%e %b %y',month:'%b %y',year:'%Y'}" +
                     "}," +
                     "yAxis:" +
                     "[" +
                     "  {" +
                    $"    title:{{text:'{Sup.GetCUstringValue( "Website", "DailyTemperature", "Daily Temperature", true )} (\u00b0'+config.temp.units+')'}}," +
                     "    opposite:false," +
                     "    labels:{align:'right',x:-5} " +
                     "  }," +
                     "  {" +
                     "    linkedTo:0," +
                     "    gridLineWidth:0," +
                     "    opposite:true," +
                     "    title:{text:null}," +
                     "    labels:{align:'left',x:5}" +
                     "  }" +
                     "]," +
                     "legend:{enabled:true}," +
                     "tooltip:{valueSuffix:'°'+config.temp.units,valueDecimals:config.temp.decimals,xDateFormat:'%A, %b %e'}," +
                     "rangeSelector:{enabled:false}," +
                     "series:" +
                     "[" +
                     $"  {{name:\"{Sup.GetCUstringValue( "Website", "AvgTemp", "Avg Temp", true )}\",color:'{Sup.GetUtilsIniValue( "Website", "HomeGraphDailyTempAverageColor", "#50B432" )}'}}," +
                     $"  {{name:\"{Sup.GetCUstringValue( "Website", "MinTemp", "Min Temp", true )}\",color:'{Sup.GetUtilsIniValue( "Website", "HomeGraphDailyTempMinColor", "#058DC7" )}'}}," +
                     $"  {{name:\"{Sup.GetCUstringValue( "Website", "MaxTemp", "Max Temp", true )}\",color:'{Sup.GetUtilsIniValue( "Website", "HomeGraphDailyTempMaxColor", "#ED561B" )}'}} " +
                     "]};" +
                     "" +
                     "chart=new Highcharts.stockChart(t);chart.showLoading();" +
                     "" +
                     "$.ajax({" +
                    $"  url:'{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}dailytemp.json'," +
                     "  dataType:'json'," +
                     "  cache:false," +
                     "  success:function(n){chart.hideLoading();chart.series[0].setData(n.avgtemp);chart.series[1].setData(n.mintemp);chart.series[2].setData(n.maxtemp)}" +
                     "  })" +
                     "};" +
                     "</script>" );

#if !RELEASE
                    of.WriteLine( CumulusCharts );
#else
                    of.WriteLine( CuSupport.StringRemoveWhiteSpace( CumulusCharts.ToString(), " " ) );
#endif

                    return;
                } // Generate using the old CumulusMX charts. This will be the fall back until the CutilsCharts.def are correct
            } // If then else of uning the CutilsCharts.def or just generate the old defaults
        }

        #endregion

        #region Package Upload
        public async Task<bool> CheckPackageAndCopy()
        {
            bool MustUpload = false;

            foreach ( string file in Package )
            {
                string filename = Sup.PathUtils + file;

                if ( File.Exists( filename ) )
                {
                    string FTPfilename = "";

                    if ( Path.GetExtension( filename ).Equals( ".html" ) )
                    {
                        // copy to cumulus dir 
                        FTPfilename = file;
                        MustUpload = true;
                    }

                    if ( Path.GetExtension( filename ).Equals( ".txt" ) )
                    {
                        FTPfilename = file;
                    }

                    if ( Path.GetExtension( filename ).Equals( ".js" ) )
                    {
                        FTPfilename = "lib/" + file;
                    }

                    if ( Path.GetExtension( filename ).Equals( ".css" ) )
                    {
                        FTPfilename = "css/" + file;
                    }

                    // Copy file

                    if ( string.IsNullOrEmpty( FTPfilename ) )
                    {
                        Sup.LogTraceWarningMessage( $"CheckPackageAndCopy: File (IsNullOrEmpty) can't be copied. Cancelling operation." );
                        Sup.LogTraceWarningMessage( "CheckPackageAndCopy: Website not created/updated. Website may not be [fully] operational" );
                        Sup.LogTraceWarningMessage( "CheckPackageAndCopy: NOTE: This has no influence on the operation of Cumulus itself." );

                        return false;
                    }
                    else
                    {
                        if ( !CUtils.Thrifty || ( CUtils.Thrifty && MustUpload ) )
                        {
                            if ( await Isup.UploadFileAsync( FTPfilename, filename ) )
                                Sup.LogTraceInfoMessage( $"CheckPackageAndCopy: Uploaded {filename} to {FTPfilename}" );
                            else
                            {
                                Sup.LogTraceErrorMessage( $"CheckPackageAndCopy: Upload of {filename} to {FTPfilename} failed." );
                                return false;
                            }
                        }
                        else
                        {
                            Sup.LogTraceInfoMessage( $"CheckPackageAndCopy: {filename} not uploaded because of Thrifty condition." );
                        }

                        MustUpload = false;
                    }
                }
                else // File does  not exist
                {
                    if ( filename.Equals( "index.html" ) || filename.Equals( "cumulusutils.js" ) )
                    {
                        Sup.LogTraceErrorMessage( $"CheckPackageAndCopy: File {filename} is missing. Cancelling operation." );
                        Sup.LogTraceErrorMessage( "CheckPackageAndCopy: Website not created/updated. Website may not be [fully] operational" );
                        Sup.LogTraceInfoMessage( "CheckPackageAndCopy: NOTE: This has no influence on the operation of Cumulus itself." );

                        return false;
                    }
                    else
                    {
                        Sup.LogTraceInfoMessage( $"CheckPackageAndCopy: File {filename} is missing." );
                        Sup.LogTraceInfoMessage( "CheckPackageAndCopy: Website may not be [fully] operational but file may still exist from previous installation." );
                    }
                }
            }

            return true;
        }

        #endregion

        #region Panelcode

        private string GeneratePanelCode( string thisPanel )
        {
            string thisPanelCode = "";
            DashboardPanels localPanel;

            //Sup.LogDebugMessage( $"GeneratePanelCode: generating {thisPanel}" );

            try
            {
                localPanel = (DashboardPanels) Enum.Parse( typeof( DashboardPanels ), thisPanel );
            }
            catch ( Exception e ) when ( e is ArgumentException || e is ArgumentNullException )
            {
                Sup.LogTraceErrorMessage( $"GeneratePanelCode: Exception parsing the thisPanel {thisPanel} - {e.Message}" );
                Sup.LogTraceErrorMessage( $"GeneratePanelCode: Leaving the Panel empty!" );
                localPanel = DashboardPanels.Empty;
            }

            switch ( localPanel )
            {
                case DashboardPanels.TemperatureText:
                    thisPanelCode =
                 $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", false )}</h4>" +
                  "        <span style='font-size:200%' id='ajxCurTemp'></span>" +
                  "        <p>" +
                  "          <span id='ajxTempChange'></span><span id='ajxTempChangeIndicator'></span><br/><br/>" +
                  "          <span id='ajxTempMax'></span><br/>" +
                  "          <span id='ajxTimeTempMax'></span><br/><br/>" +
                  "          <span id='ajxTempMin'></span><br/>" +
                  "          <span id='ajxTimeTempMin'></span>" +
                  "        </p>";
                    break;

                case DashboardPanels.PressureText:
                    thisPanelCode =
                 $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", false )}</h4>" +
                  "        <span style='font-size:200%' id='ajxCurPression'></span>" +
                  "        <p>" +
                  "          <span id='ajxBarChange'></span>" +
                  "          <span id='ajxBarChangeIndicator'></span><br/><br/>" +
                  "          <span id='ajxBarMax'></span ><br/>" +
                  "          <span id='ajxTimeBarMax'></span><br/><br/>" +
                  "          <span id='ajxBarMin'></span><br/>" +
                  "          <span id='ajxTimeBarMin'></span>" +
                  "        </p>";
                    break;

                case DashboardPanels.RainText:
                    thisPanelCode =
                 $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Rain", "Rain", false )}</h4><span style='font-size:200%' id='ajxRainToday'></span>" +
                  "        <p>" +
                  "          <span id='ajxRainRateNow'></span><br/><br/>" +
                  "          <!-- span id='ajxTodayRainRateHigh'></span> -->" +
                  "          <span id='ajxRainYesterday'></span><br/>" +
                  "          <span id='ajxRainMonth'></span><br/>" +
                  "          <span id='ajxRainYear'></span >" +
                  "        </p>";
                    break;

                case DashboardPanels.Clocks:
                    thisPanelCode =
                 $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Clock", "Clock", false )}</h4>" +
                  "        <span id='ajxDateUpdate'></span ><br/>" +
                  "        <span id='ajxTimeUpdate'></span><br/>" +
                  "        <span id='CUClocktimetz'></span>" +
                  "        <span id='CUClocktimeBrowser'></span>" +
                  "        <span id='CUClocktimeutc'></span>";
                    break;

                case DashboardPanels.WindGauge1:
                    thisPanelCode =
                    "    <section id='WindGauge1'>" +
                   $"        {WindGaugeContent()}" +
                    "    </section>";
                    break;

                case DashboardPanels.WindDirGauge1:
                    thisPanelCode =
                    "    <section id='WindDirGauge1'>" +
                   $"        {WindDirContent()}" +
                    "    </section>";
                    break;

                case DashboardPanels.WindRoseGauge1:
                    thisPanelCode =
                    "    <section id='WindRoseGauge1'>" +
                   $"        {WindRoseContent()}" +
                    "    </section>";
                    break;

                case DashboardPanels.WindText:
                    thisPanelCode =
                 $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Wind", "Wind", false )}</h4>" +
                 $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Current", "Current", false )}: </span><span style='font-size: 140%' id='ajxCurWind'></span><br/>" +
                 $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Average", "Average", false )}: </span><span style='font-size: 140%' id='ajxAverageWind'></span><br/>" +
                 $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Beaufort", "Beaufort", false )}: </span><span style='font-size: 140%' id='ajxCurWindBf'></span><br/><br/>" +
                  "        <p>" +
                  "          <span id='ajxHighAverage'></span><br/>" +
                  "          <span id='ajxTimeHighAverage'></span ><br/>" +
                  "          <span id='ajxHighGust'></span><br/>" +
                  "          <span id='ajxTimeHighGust'></span >" +
                  "        </p>";
                    break;

                case DashboardPanels.SolarDisc:
                    thisPanelCode =
                  $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Sun", "Sun", false )}</h4>" +
                  "        <div id='d3SunDisc'></div>" +
                  "        <p>" +
                  $"          <span>{Sup.GetCUstringValue( "Website", "Sunrise", "Sunrise", false )}: @&nbsp;</span><span id='CUsunrise'></span><br/>" +
                  $"          <span>{Sup.GetCUstringValue( "Website", "Sunset", "Sunset", false )}: @&nbsp;</span><span id='CUsunset'></span><br/>" +
                  $"          <span>{Sup.GetCUstringValue( "Website", "DayLength", "Day length", false )}: @&nbsp;</span><span id='CUdaylength'></span>" +
                  "        </p>";
                    break;

                case DashboardPanels.LunarDisc:
                    thisPanelCode =
                  $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Moon", "Moon", false )}</h4>" +
                  "        <div id='d3MoonDisc'></div>" +
                  "        <br/>" +
                  "        <p>" +
                  $"          <span>{Sup.GetCUstringValue( "Website", "Moonrise", "Moonrise", false )}: @&nbsp;</span><span id='CUmoonrise'></span><br/>" +
                  $"          <span>{Sup.GetCUstringValue( "Website", "Moonset", "Moonset", false )}: @&nbsp;</span><span id='CUmoonset'></span >" +
                  "        </p>";
                    break;

                case DashboardPanels.HumidityText:
                    thisPanelCode =
                  $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", false )}</h4>" +
                  $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", false )}:</span><br/>" +
                  "        <span style='font-size: 200%' id='ajxCurHumidity'></span><br/>" +
                  $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Dewpoint", "Dew Point", false )}:</span><br/>" +
                  "        <span style='font-size: 200%' id='ajxDewpoint'></span><br/>";
                    break;

                case DashboardPanels.SolarText:
                    thisPanelCode = $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Solar", "Solar", false )}</h4>";

                    if ( ShowSolar )
                    {
                        thisPanelCode +=
                        $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "SolarRadiation", "Solar Radiation", false )}:</span><br/>" +
                         "        <span style='font-size: 200%' id='ajxCurSolar'></span><br/>" +
                        $"        <span>{Sup.GetCUstringValue( "Website", "MaxValue", "Max value", false )}: </span><span id='ajxCurSolarMax'></span><br/>" +
                        $"        <span>{Sup.GetCUstringValue( "Website", "SolarHours", "Sunshine today", false )}: </span><span id='ajxSolarHours'></span><br/>";
                    }

                    if ( ShowUV )
                    {
                        thisPanelCode +=
                       $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "UVindex", "UV index", false )}: </span>" +
                        "        <span style='font-size: 200%' id='ajxCurUVindex'></span><br/>";
                    }


                    break;

                case DashboardPanels.TemperatureGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", false )}</h4>" +
                  "      <canvas id='canvas_temp' class='gaugeSizeSml' ></canvas><br/>" +
                  $"      <input id='rad_temp1' type='radio' name='rad_temp' value='out' checked onclick='gauges.doTemp(this);'><label id='lab_temp1' for='rad_temp1'>{Sup.GetCUstringValue( "Website", "Outside", "Outside", false )}</label>" +
                  $"      <input id='rad_temp2' type='radio' name='rad_temp' value='in' onclick='gauges.doTemp(this);'><label id='lab_temp2' for='rad_temp2'>{Sup.GetCUstringValue( "Website", "Inside", "Inside", false )}</label>";
                    break;

                case DashboardPanels.OtherTempsGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "OtherTemps", "Other Temps", false )}</h4>" +
                  "      <canvas id='canvas_dew' class='gaugeSizeSml' ></canvas><br/>" +
                  $"      <input id='rad_dew1' type='radio' name='rad_dew' value='dew' onclick='gauges.doDew(this);'>" +
                  $"        <label for='rad_dew1'>{Sup.GetCUstringValue( "Website", "Dewpoint", "Dewpoint", false )}</label>" +
                  $"      <input id='rad_dew2' type='radio' name='rad_dew' value='app' onclick='gauges.doDew(this);'>" +
                  $"        <label for='rad_dew2'>{Sup.GetCUstringValue( "Website", "Apparent", "Apparent", false )}</label>" +
                  $"      <input id='rad_dew3' type='radio' name='rad_dew' value='feel' checked onclick='gauges.doDew(this);'>" +
                  $"        <label for='rad_dew3'>{Sup.GetCUstringValue( "Website", "FeelsLike", "FeelsLike", false )}</label>" +
                  $"      <br/>" +
                  $"      <input id='rad_dew4' type='radio' name='rad_dew' value='wnd' onclick='gauges.doDew(this);'>" +
                  $"        <label for='rad_dew4'>{Sup.GetCUstringValue( "Website", "WindChill", "Wind Chill", false )}</label>" +
                  $"      <input id='rad_dew5' type='radio' name='rad_dew' value='hea' onclick='gauges.doDew(this);'>" +
                  $"        <label for='rad_dew5'>{Sup.GetCUstringValue( "Website", "HeatIndex", "Heat Index", false )}</label>" +
                  $"      <input id='rad_dew6' type='radio' name='rad_dew' value='hum' onclick='gauges.doDew(this);'>" +
                  $"        <label for='rad_dew6'>{Sup.GetCUstringValue( "Website", "HumIndex", "Hum Index", false )}</label>";
                    break;

                case DashboardPanels.PressureGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", false )}</h4>" +
                  "      <canvas id='canvas_baro' class='gaugeSizeSml'></canvas><br/>";
                    break;

                case DashboardPanels.HumidityGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", false )}</h4>" +
                  "      <canvas id='canvas_hum' class='gaugeSizeSml'></canvas><br/>" +
                  $"      <input id='rad_hum1' type='radio' name='rad_hum' value='out' checked onclick='gauges.doHum(this);'><label id='lab_hum1' for='rad_hum1'>{Sup.GetCUstringValue( "Website", "Outside", "Outside", false )}</label>" +
                  $"      <input id='rad_hum2' type='radio' name='rad_hum' value='in' onclick='gauges.doHum(this);'><label id='lab_hum2' for='rad_hum2'>{Sup.GetCUstringValue( "Website", "Inside", "Inside", false )}</label>";
                    break;

                case DashboardPanels.WindGauge2:
                    thisPanelCode =
                    "    <section id='WindGauge2'>" +
                    "    </section>";
                    break;
                case DashboardPanels.WindDirGauge2:
                    thisPanelCode =
                  "    <section id='WindDirGauge2'>" +
                  "    </section>";
                    break;
                case DashboardPanels.WindRoseGauge2:
                    thisPanelCode =
                    "    <section id='WindRoseGauge2'>" +
                    "    </section>";
                    break;
                case DashboardPanels.CloudBaseGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "CloudBase", "Cloud Base", false )}</h4>" +
                  "      <canvas id='canvas_cloud' class='gaugeSizeSml'></canvas><br/>";
                    break;

                case DashboardPanels.RainGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Rain", "Rain", false )}</h4>" +
                  "      <canvas id='canvas_rain' class='gaugeSizeSml' ></canvas><br/>";
                    break;

                case DashboardPanels.RainSpeedGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Rainrate", "Rain Rate", false )}</h4>" +
                  "      <canvas id='canvas_rrate' class='gaugeSizeSml'></canvas><br/>";
                    break;

                case DashboardPanels.SolarGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "SolarRadiation", "Solar Radiation", false )}</h4>" +
                  "      <canvas id='canvas_solar' class='gaugeSizeSml'></canvas><br/>";
                    break;

                case DashboardPanels.UVGauge:
                    thisPanelCode =
                  $"      <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "UVindex", "UV index", false )}</h4>" +
                  "      <canvas id='canvas_uv' class='gaugeSizeSml'></canvas><br/>";
                    break;

                case DashboardPanels.Empty:
                    thisPanelCode = "<h4 class='CUCellTitle'></h4>";
                    break;

                default:
                    Sup.LogTraceErrorMessage( $"GeneratePanelCode: Illegal default Panel Code - Exiting CumulusUtils." );
                    Environment.Exit( -1 );
                    break;
            }

            string WindGaugeContent()
            {
                return $"<div id='WindContent'> <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Wind", "Wind", false )}</h4>" +
                      "<canvas id='canvas_wind' class='gaugeSizeSml'></canvas></div>";
            }

            string WindDirContent()
            {
                return $"<div id='WindDirContent'><h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Direction", "Direction", false )}</h4>" +
                      "<canvas id='canvas_dir' class='gaugeSizeSml'></canvas></div>";
            }

            string WindRoseContent()
            {
                // The div around the Canvas with position:relative is required to have the odometer respond to orientation changes correctly
                return $"<div id='WindRoseContent'><h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "WindRose", "Wind Rose", false )}</h4>" +
                      "<div style='position: relative'><canvas id='canvas_rose' class='gaugeSizeSml'></canvas></div></div>";
            }

            return thisPanelCode;
        }

        #endregion

        #region GenerateMenu

        enum ItemTypes : int { None, External, Internal, Image, Report, Separator };

        private async Task<string> GenerateMenu()
        {
            StringBuilder tmpMenu = new StringBuilder();
            List<string> AllMenuFiles = new List<string>();

            if ( File.Exists( $"{Sup.PathUtils}{Sup.CutilsMenuDef}" ) )
            {
                Sup.LogDebugMessage( $"Website Menu generator: Menu definition found, using the user defined menu" );

                // Preparation

                string[] MenuContents = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsMenuDef}", Encoding.UTF8 );
                string tmp = "";

                foreach ( string thisline in MenuContents )
                    if ( string.IsNullOrEmpty( thisline ) || thisline[ 0 ] == ';' ) continue;
                    else tmp += thisline + ' ';

                // End of preparation

                char[] charSeparators = new char[] { ' ', ';' };
                string[] Keywords;
                string thisKeyword;

                Keywords = tmp.Split( charSeparators, StringSplitOptions.RemoveEmptyEntries );  // .Where( s => !string.IsNullOrWhiteSpace( s ) )

                WriteMenuStart( tmpMenu );

                for ( int i = 0; i < Keywords.Length; i++ )   // string thisKeyword in Keywords 
                {
                    thisKeyword = Keywords[ i ];

                    Sup.LogTraceInfoMessage( $"Website Menu generator: Generating menu {thisKeyword} on main level" );

                    switch ( thisKeyword )
                    {
                        case "Home":
                            WriteMenuHome( tmpMenu );
                            break;
                        case "About":
                            WriteAboutMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Reports":
                            WriteReportsMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Graphs":
                            WriteGraphsMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Records":
                            WriteRecordsMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Extra":
                            // Prevent the menu to be generated if it is not required.
                            if ( CUtils.HasAirLink || CUtils.HasExtraSensors || CUtils.HasCustomLogs )
                            {
                                WriteExtraMenu( tmpMenu );
                                AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                                tmpMenu.Append( "</ul></li>" );
                            }
                            break;
                        case "Misc":
                            WriteMiscellaneousMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Print":
                            // https://stackoverflow.com/questions/12997123/print-specific-part-of-webpage => looks best
                            // https://jsfiddle.net/jdavidzapatab/6sctvg2z/

                            WritePrintMenu( tmpMenu );
                            break;
                        case "ToggleDashboard":
                            WriteToggleMenu( tmpMenu );
                            break;
                        default:
                            tmpMenu.Append( "      <li class='nav-item dropdown'>" +
                                "        <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownAbout' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                                $"          {thisKeyword}" +
                                "        </a>" );
                            tmpMenu.Append( "        <ul class='dropdown-menu' aria-labelledby='navbarDropdown'>" );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, false, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                    }
                }

                WriteMenuEnd( tmpMenu );

                //Write out all menufiles in the list
                foreach ( string thisFile in AllMenuFiles )
                    await Isup.UploadFileAsync( $"{thisFile}", $"{Sup.PathUtils}{thisFile}" );

                Sup.LogTraceInfoMessage( $"Website Menu generator: Menu generation finished." );
            }
            else
            {
                Sup.LogTraceWarningMessage( $"Website Menu generator: No Menu definition found, using standard menu" );

                // No user definition found so generate the default standard menu
                //
                WriteMenuStart( tmpMenu );
                WriteMenuHome( tmpMenu );
                WriteAboutMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );
                WriteReportsMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );
                WriteGraphsMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );
                WriteRecordsMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );

                // Prevent the menu to be generated if it is not required.
                if ( CUtils.HasAirLink || CUtils.HasExtraSensors || CUtils.HasCustomLogs )
                {
                    WriteExtraMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );
                }

                WriteMiscellaneousMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );
                WriteToggleMenu( tmpMenu );
                WritePrintMenu( tmpMenu );
                WriteMenuEnd( tmpMenu );
            }

            List<string> WriteUserItems( string[] K, bool UseDivider, StringBuilder s, ref int i )
            {
                bool ItemNameIsURL = false;

                int ItemNumber = 0;
                int ItemNameLength = 20;

                string Destination = "";
                string ItemName, ItemNameURL, baseName, MenuFile;
                List<string> thisMenuFileList = new List<string>();

                ItemTypes thisType = ItemTypes.None;

                baseName = K[ i++ ]; // Get the Menu name and advance to the first item (if any)

                if ( i >= K.Length ) return thisMenuFileList;
                if ( K[ i ].Equals( "item", CUtils.Cmp ) && UseDivider ) s.AppendLine( "<div class='dropdown-divider'></div>" );

                while ( K[ i ].Equals( "item", CUtils.Cmp ) )
                {
                    MenuFile = $"{baseName}{ItemNumber++:00}.txt";
                    i++;

                    ItemName = K[ i++ ];

                    if ( ItemName.Equals( "separator", CUtils.Cmp ) )
                    {
                        thisType = ItemTypes.Separator;
                        Destination = "";
                    }
                    else
                    {
                        try
                        {
                            while ( !Enum.IsDefined( typeof( ItemTypes ), K[ i ] ) ) ItemName += ' ' + K[ i++ ];  // Get all words before the Itemtype

                            if ( ItemName.StartsWith( "../", CUtils.Cmp ) || ItemName.StartsWith( "./", CUtils.Cmp ) ||
                                 ItemName.StartsWith( "http", CUtils.Cmp ) || ItemName.StartsWith( "https", CUtils.Cmp ) ) ItemNameIsURL = true;

                            thisType = (ItemTypes) Enum.Parse( typeof( ItemTypes ), K[ i++ ], true );
                            Destination = K[ i++ ];
                        }
                        catch ( Exception )
                        {
                            Sup.LogTraceErrorMessage( $"Website Menu generator: Error generating {ItemName}" );
                        }
                    }

                    if ( K[ i++ ].Equals( "enditem", CUtils.Cmp ) )
                    {
                        string WidthStyleString = "";

                        // Write out what we have found
                        Sup.LogTraceInfoMessage( $"Website Menu generator: Generating {ItemName}" );

                        if ( ItemNameIsURL ) { ItemNameURL = $"<img src={ItemName}>"; ItemNameIsURL = false; }
                        else
                        {
                            ItemNameURL = ItemName;

                            if ( ItemName.Length > ItemNameLength )
                            {
                                WidthStyleString = $"style='width:{ItemName.Length / 2}rem'";
                                ItemNameLength = ItemName.Length;
                            }
                        }

                        switch ( thisType )
                        {
                            case ItemTypes.External:
                                s.AppendLine( $"<li {WidthStyleString}><a class='nav-link' href=\"{Destination}\" target='_blank'>{ItemNameURL}</a></li>" );
                                break;

                            case ItemTypes.Internal:
                                using ( StreamWriter sw = new StreamWriter( $"{Sup.PathUtils}{MenuFile}" ) )
                                    sw.WriteLine( $"<iframe src='{Destination}' frameborder='0' style='border: 0; width:100%; height: 75vh;'></iframe>" );

                                Sup.LogTraceInfoMessage( $"Website Menu generator: Created Iframe file {MenuFile}" );

                                //Isup.UploadFile( $"{MenuFile}", $"{Sup.PathUtils}{MenuFile}" );
                                thisMenuFileList.Add( MenuFile );

                                s.AppendLine( $"<li class='nav-link' onclick=\"LoadUtilsReport('{MenuFile}');\" {WidthStyleString}>{ItemNameURL}</li>" );
                                break;

                            case ItemTypes.Report:
                                s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('{Destination}');\" {WidthStyleString}>{ItemNameURL}</li>" );
                                break;

                            case ItemTypes.Image:
                                using ( StreamWriter sw = new StreamWriter( $"{Sup.PathUtils}{MenuFile}" ) )
                                    sw.WriteLine( $"<image src='{Destination}' style='width:100%; height:100%; border:0;'>" );

                                Sup.LogTraceInfoMessage( $"Website Menu generator: Created Image-link file {MenuFile}" );
                                //Isup.UploadFile( $"{MenuFile}", $"{Sup.PathUtils}{MenuFile}" );
                                thisMenuFileList.Add( MenuFile );

                                s.AppendLine( $"<li class='nav-link' onclick=\"LoadUtilsReport('{MenuFile}');\" {WidthStyleString}>{ItemNameURL}</li>" );
                                break;

                            case ItemTypes.Separator:
                                s.Append( "<div class='dropdown-divider'></div>" );
                                break;

                            default:
                                Sup.LogTraceInfoMessage( $"Website Menu generator: Illegal UserItem, can't generate..." );
                                break;
                        }
                    }
                    else
                    {
                        Sup.LogTraceErrorMessage( $"Website Menu generator: Error generating {ItemName} - EndItem not found" );
                    }
                }

                i--;        // Correct for the position (we had to preview the next keyword
                return thisMenuFileList; // return if no more items found
            }

            void WriteMenuStart( StringBuilder s )
            {
                s.Append(
                    "<nav class='navbar navbar-default navbar-expand-lg '>" + // navbar-light bg-light
                    "  <div class='container-fluid'>" +
                    "  <div class='collapse navbar-collapse' id='navbarSupportedContent'>" +
                    "    <ul class='navbar-nav'>" );

                return;
            }

            void WriteMenuHome( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item'>" +
                    $"          <span class='nav-link' onclick=\"LoadUtilsReport('cumuluscharts.txt', true);\">{Sup.GetCUstringValue( "Website", "Home", "Home", false )}</span>" +
                    "      </li>" );

                return;
            }

            void WriteAboutMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item dropdown'>" +
                    "        <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownAbout' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                    $"          {Sup.GetCUstringValue( "Website", "About", "About", false )}" +
                    "        </a>" +
                    "        <ul class='dropdown-menu' aria-labelledby='navbarDropdownAbout'>" +
                    $"          <li class='nav-link' data-bs-toggle='modal' data-bs-target='#CUserAbout'>{Sup.GetCUstringValue( "Website", "ThisSite", "This Site", false )}</li>" +
                    "          <li class='nav-link' data-bs-toggle='modal' data-bs-target='#CUabout'>CumulusUtils</li>" +
                    "          <li class='nav-link' data-bs-toggle='modal' data-bs-target='#CUlicense'>License</li>" +
                    "          <li><a class='nav-link' href=\"https://cumuluswiki.org/a/Category:CumulusUtils\" target=\"_blank\">CumulusUtils Wiki</a></li>" );

                return;
            }

            void WriteReportsMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item dropdown'>" +
                    "        <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownReports' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                    $"          {Sup.GetCUstringValue( "Website", "Reports", "Reports", false )}" +
                    "        </a>" +
                    "        <ul class='dropdown-menu' aria-labelledby='navbarDropdownReports'>" +
                    "          <li class='nav-link' onclick=\"LoadUtilsReport('pwsFWI.txt', false);\">pwsFWI</li>" +
                    $"          <li class='nav-link' onclick=\"LoadUtilsReport('Yadr.txt', false);\">{Sup.GetCUstringValue( "Website", "Yadr", "Yadr", false )}</li>" +
                    $"          <li class='nav-link' onclick=\"LoadUtilsReport('noaa.txt', false);\">{Sup.GetCUstringValue( "Website", "NOAA", "NOAA", false )}</li>" );

                if (CUtils.HasDiaryMenu)
                    s.Append( $"          <li class='nav-link' onclick=\"LoadUtilsReport('diary.txt', false);\">{Sup.GetCUstringValue( "Website", "Diary", "Diary", false )}</li>" );

                return;
            }

            void WriteGraphsMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item dropdown'>" +
                    "        <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownGraphs' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                    $"          {Sup.GetCUstringValue( "Website", "Graphs", "Graphs", false )}" +
                    "       </a>" +
                    "        <ul class='dropdown-menu' aria-labelledby='navbarDropdownGraphs'>" );

                if ( CUtils.HasRainGraphMenu )
                    s.Append( $"          <li class='nav-link' onclick=\"LoadUtilsReport('graphsrain.txt', false);\">{Sup.GetCUstringValue( "Website", "RainGraphs", "Rain Graphs", false )}</li>" );

                if ( CUtils.HasTempGraphMenu )
                    s.Append( $"          <li class='nav-link' onclick=\"LoadUtilsReport('graphstemp.txt', false);\">{Sup.GetCUstringValue( "Website", "TempGraphs", "Temp Graphs", false )}</li>" );

                if ( CUtils.HasWindGraphMenu )
                    s.Append( $"          <li class='nav-link' onclick=\"LoadUtilsReport('graphswind.txt', false);\">{Sup.GetCUstringValue( "Website", "WindGraphs", "Wind Graphs", false )}</li>" );

                if ( CUtils.HasSolarGraphMenu )
                    s.Append( $"          <li class='nav-link' onclick=\"LoadUtilsReport('graphssolar.txt', false);\">{Sup.GetCUstringValue( "Website", "SolarGraphs", "Solar Graphs", false )}</li>" );

                if ( CUtils.HasMiscGraphMenu )
                    s.Append( $"          <li class='nav-link' onclick=\"LoadUtilsReport('graphsmisc.txt', false);\">{Sup.GetCUstringValue( "Website", "MiscGraphs", "Misc Graphs", false )}</li>" );

                return;
            }

            void WriteRecordsMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item dropdown'>" +
                    "         <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownRecords' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                    $"          {Sup.GetCUstringValue( "Website", "Records", "Records", false )}" +
                    "         </a>" +
                    "          <ul class='dropdown-menu' aria-labelledby='navbarDropdownRecords'>" +
                    $"           <li class='nav-link' onclick=\"LoadUtilsReport('records.txt', false);\">{Sup.GetCUstringValue( "Website", "Records", "Records", false )}</li>" +
                    $"           <li class='nav-link' onclick=\"LoadUtilsReport('top10Table.txt', false);\">{Sup.GetCUstringValue( "Website", "Top10Records", "Top 10 Records", false )}</li>" +
                    $"           <li class='nav-link' onclick=\"LoadUtilsReport('dayrecords.txt', false);\">{Sup.GetCUstringValue( "Website", "DayRecords", "Day Records", false )}</li>" );

                return;
            }

            void WriteExtraMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item dropdown'>" +
                    "       <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownExtra' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                    $"         {Sup.GetCUstringValue( "Website", "Extra", "Extra", false )}" +
                    "       </a>" +
                    "        <ul class='dropdown-menu' aria-labelledby='navbarDropdownExtra'>" );

                if ( CUtils.HasAirLink )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('airlink.txt', false);\">{Sup.GetCUstringValue( "Website", "AirLink", "AirLink", false )}</li>" );

                if ( CUtils.HasExtraSensors )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('extrasensors.txt', false);\">{Sup.GetCUstringValue( "Website", "ExtraSensors", "Extra Sensors", false )}</li>" );

                if ( CUtils.HasCustomLogs )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('customlogs.txt', false);\">{Sup.GetCUstringValue( "Website", "CustomLogs", "Custom Logs", false )}</li>" );

                if ( CUtils.ParticipatesSensorCommunity )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('sensorcommunity.txt', false);\">{Sup.GetCUstringValue( "Website", "SC map", "SC map", false )}</li>" );

                return;
            }

            void WriteMiscellaneousMenu( StringBuilder s )
            {
                s.Append(
                "      <li class='nav-item dropdown'>" +
                "        <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownMisc' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                $"          {Sup.GetCUstringValue( "Website", "Misc", "Misc.", false )}" +
                "        </a>" +
                "        <ul class='dropdown-menu' aria-labelledby='navbarDropdownMisc'>" +
                  $"          <li class='nav-link' onclick=\"LoadUtilsReport('forecast.txt', false);\">{Sup.GetCUstringValue( "Website", "Forecast", "Forecast", false )}</li>" +
                  $"          <li class='nav-link' onclick=\"LoadUtilsReport('systeminfoTable.txt', false);\">{Sup.GetCUstringValue( "Website", "SystemInfo", "System Info", false )}</li>" +
                  $"          <li class='nav-link' onclick=\"LoadUtilsReport('maps.txt', false);\">{Sup.GetCUstringValue( "Website", "UserMap", "User Map", false )}</li>" );

                if ( CUtils.HasStationMapMenu )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('stationmap.txt', false);\">{Sup.GetCUstringValue( "Website", "StationMap", "StationMap", false )}</li>" );

                if ( CUtils.HasMeteoCamMenu )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('meteocam.txt', false);\">{Sup.GetCUstringValue( "Website", "MeteoCam", "MeteoCam", false )}</li>" );

                return;
            }

            void WritePrintMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item'>" +
                    $"        <span class='nav-link' onclick=\"PrintScreen('CUReportView');\">{Sup.GetCUstringValue( "Website", "Print", "Print", false )}</span>" +
                    "      </li>" );
            }

            void WriteToggleMenu( StringBuilder s )
            {
                s.Append(
                    "      <li class='nav-item'>" +
                    $"        <span class='nav-link' onclick='ToggleDashboard()'>{Sup.GetCUstringValue( "Website", "ToggleDashboard", "Toggle Dashboard", false )}</span>" +
                    "      </li>" );

                return;
            }

            void WriteMenuEnd( StringBuilder s )
            {
                s.Append(
                    "    </ul>" +
                    "    <ul id='CUsermenu' class='navbar-nav'></ul>" +
                    "  </div>" + // id='navbarSupportedContent'
                    "  <button class='navbar-toggler navbar-toggler-right custom-toggler ms-auto' type='button' data-bs-toggle='collapse' data-bs-target='#navbarSupportedContent' aria-controls='navbarSupportedContent' aria-expanded='false' aria-label='Toggle navigation'>" +
                    "    <span class='navbar-toggler-icon'></span>" +
                    "  </button>" );

                if ( !PwsfwiButtonInHeader )
                    s.Append(
                        $"  <span onclick=\"LoadUtilsReport('pwsFWI.txt', false);\">{Sup.GetUtilsIniValue( "pwsFWI", "CurrentPwsFWI", "" )}</span><br/>" );

                s.Append(
                    $"  <canvas id='canvas_led' width=30 height=30 style='float:left;'></canvas><span class='navbar-text'>{Sup.GetCUstringValue( "Website", "StationStatus", "Station Status", false )}</span>" +
                    "  </div>" + // Containerfluid, required for bootstrap 5.2 ??
                    "</nav>" );

                return;
            }

            return tmpMenu.ToString();
        }


        #endregion

        #region GenerateStatisticsCode

        private string GenerateStatisticsCode( string StatisticsType, bool Event )
        {
            StringBuilder Buf = new StringBuilder();

            Sup.LogTraceInfoMessage( $"GenerateStatisticsCode: StatisticsType is '{StatisticsType}'; Event is '{Event}'" );

            if ( StatisticsType.Equals( "Google" ) )
            {
                if ( Event )
                {
                    // Now Event is the parameter, it might be usefull to have a string giving the variable ("ReportName") ???
                    Buf.Append( $" gtag('event', 'LoadReport', {{'event_category' : ReportName.replace(/\\.[^/.]+$/, '') }});" );
                }
                else // generate the generic code 
                {
                    // -----------------------------------------------------------------------------------
                    // https://developers.google.com/analytics/devguides/collection/gajs/#disable
                    // https://webgilde.com/en/analytics-opt-out/
                    // -----------------------------------------------------------------------------------
                    //
                    Buf.Append( "<script async src=\"https://www.googletagmanager.com/gtag/js?id=" + $"{Sup.GetUtilsIniValue( "Website", "GoogleStatsId", "" )}" + "\"></script>" );
                    Buf.Append( "<script>" +
                                        "  window.dataLayer = window.dataLayer || [];" +
                                        "  function gtag() {dataLayer.push(arguments);}" +
                                        "  gtag('js', new Date());" +
                                        $"  gtag('config', '{Sup.GetUtilsIniValue( "Website", "GoogleStatsId", "" )}');" +
                                        "</script>" );

                    if ( PermitGoogleOptOut )
                    {
                        Buf.Append( "<script>" +
                                            $"  var gaProperty = '{Sup.GetUtilsIniValue( "Website", "GoogleStatsId", "" )}';" +
                                            "  var disableStr = 'ga-disable-' + gaProperty;" +
                                            "  if (document.cookie.indexOf(disableStr + '=true') > -1){" +
                                            "    console.log('Analytics Opt-Out found and set');" +
                                            "    window[disableStr] = true;" +
                                            "  }" +
                                            "  else {" +
                                            "    console.log('Analytics Opt-Out Fail');" +
                                            "  }" );
                        Buf.Append( "function gaOptout(){" +
                                            "  document.cookie = disableStr + '=true; " +
                                            "  expires=Thu, 31 Dec 2099 23:59:59 UTC; " +
                                            "  path=/';" +
                                            "  window[disableStr] = true;" +
                                            "  console.log('Analytics Opt-Out Set');" +
                                            "}" +
                                            "</script>" );
                    }
                }
            }
            else if ( StatisticsType.Equals( "Matomo" ) )
            {
                if ( Event )
                {
                    Sup.LogTraceWarningMessage( $"GenerateStatisticsCode: No Matomo events implemented yet" );
                }
                else
                {
                    Buf.Append( "<!-- Matomo -->" +
                        "<script>" +
                        "  var _paq = window._paq = window._paq || [];" +
                        "/* tracker methods like \"setCustomDimension\" should be called before \"trackPageView\" */" +
                        "_paq.push(['trackPageView']);" +
                        "_paq.push(['enableLinkTracking']);" +
                        "(function() {" +
                        $"    var u='{Sup.GetUtilsIniValue( "Website", "MatomoTrackerUrl", "" )}';" +
                        $"    _paq.push(['setTrackerUrl', u+'matomo.php']);" +
                        $"    _paq.push(['setSiteId', '{Sup.GetUtilsIniValue( "Website", "MatomoSiteId", "" )}']);" +
                        "    var d=document, g=d.createElement('script'), s=d.getElementsByTagName('script')[0];" +
                        "    g.async=true; g.src=u+'matomo.js'; s.parentNode.insertBefore(g,s);" +
                        "  })();" +
                        "</script>" +
                        "<!-- End Matomo Code -->" );
                }
            }
            else
            {
                Sup.LogTraceErrorMessage( $"GenerateStatisticsCode: StatisticsType '{StatisticsType}' is unknown, nothing generated" );
            }

            return Buf.ToString();
        }

        #endregion

        #region Other Functions
        // Note that these colors contain transparency in the first two HEX digits. If not it is NOT OK
        private string GetAreaGradientColors( string Color1, string Color2, string Color3 )
        {
            bool ValidColor1 = false;
            bool ValidColor2 = false;
            bool ValidColor3 = false;
            StringBuilder result = new StringBuilder();

            if ( !String.IsNullOrEmpty( Color1 ) )
                ValidColor1 = true;
            if ( !String.IsNullOrEmpty( Color2 ) )
                ValidColor2 = true;
            if ( !String.IsNullOrEmpty( Color3 ) )
                ValidColor3 = true;

            if ( ValidColor1 )
            {
                // There  is only the first color, return what we need
                Sup.LogTraceVerboseMessage( "GetAreaGradientColors : Generating the color code..." );
                result.Append( $"color: '{Color1}'," );

                if ( ValidColor2 && ValidColor3 )
                {
                    // Add the gradient to the result
                    Sup.LogTraceVerboseMessage( "GetAreaGradientColors : Generating the code for just 3 colors so a full gradient" );
                    result.Append( "fillColor: {linearGradient:{x1: 0,y1: 1,x2: 0,y2: 0}," +
                      $"stops:[[0,'{Color2}'],[1,'{Color3}']]}}," );
                }
            }
            else
            {
                Sup.LogTraceVerboseMessage( $"GetAreaGradientColors : No valid colours supplied: 1: {Color1}, 2: {Color2}, 3: {Color3}" );
                result.Append( "" );
            }

            return result.ToString();
        }

        #endregion

        #region Useful NOTUSED (cookie function)

        // From Mark Crossley in Select a Graph of CumulusMX 3.9.7 (b3105) and up
        //
        //  var prefs = {
        //    data: {
        //        series: ['0','0','0','0','0','0'],
        //        colours: ['','','','','','']
        //    },
        //    load: function()
        //    {
        //      var cookie = document.cookie.split(';');
        //      cookie.forEach(function(val) {
        //        if (val.trim().startsWith('selecta='))
        //        {
        //          var dat = unescape(val).split('=');
        //          data = JSON.parse(dat[1]);
        //        }
        //      });
        //      return data;
        //    },

        //    save: function(settings)
        //    {
        //      this.data = settings;
        //      var d = new Date();
        //      d.setTime(d.getTime() + (365 * 24 * 60 * 60 * 1000));
        //      document.cookie = 'selecta=' + escape(JSON.stringify(this.data)) + ';expires=' + d.toUTCString();
        //    }
        //  };

        #endregion
    }
}
