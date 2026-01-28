/*
 * Website - Part of CumulusUtils
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CumulusUtils
{
    // DashboardPanels is used in GenerateWebsite at specific location and the dedicated function GeneratePanelCode(thisPanel)
    public enum DashboardPanels
    {
        TemperatureText, PressureText, RainText, Clocks, WindGauge1, WindDirGauge1, WindRoseGauge1, WindText, SolarDisc, LunarDisc, HumidityText, SolarText,
        TemperatureGauge, OtherTempsGauge, PressureGauge, HumidityGauge, WindGauge2, WindDirGauge2, WindRoseGauge2, CloudBaseGauge, RainGauge, RainSpeedGauge, SolarGauge, UVGauge, Empty
    };

    class Website
    {
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
              "<link rel=\"shortcut icon\" href=\"favicon.ico\" type=\"image/x-icon\" />" +
             $"<title>{Sup.GetCumulusIniValue( "Station", "LocName", "" )} - CumulusUtils</title>" +
              "<meta name=\"theme-color\" content=\"#ffffff\" />" +
              "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
              "<meta charset=\"UTF-8\">" );

            // Possibility of a user header meta section
            //
            if ( File.Exists( $"{Sup.PathUtils}{Sup.CutilsHeadDef}" ) )
            {
                string allText = File.ReadAllText( $"{Sup.PathUtils}{Sup.CutilsHeadDef}" );

                Sup.LogTraceInfoMessage( $"{allText}/>" );
                indexFile.Append( allText );
            }
            else
            {
                Sup.LogDebugMessage( $"CutilsHead.def does not exist, Using default <Meta> strings." );

                indexFile.Append(
                  "<meta name=\"description\" content=\"CumulusMX Website, part of CumulusUtils\"/>" +
                  "<meta name=\"keywords\" content=\"CumulusMX, weather, data, private weather station, CumulusUtils\" />" +
                  "<meta name=\"robots\" content=\"index, noarchive, follow, noimageindex, noimageclick\" />" );
            }

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
                $"<body style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorBodyBackground", "white" )}; height:100vh; width:100vw>" + //whitesmoke
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
              @"This site is made with CumulusUtils, a generator tool for HTML presentation of weather data, retrieved from a weather station powered by <a href='https://cumulus.hosiene.co.uk/index.php'>CumulusMX</a>. CumulusUtils is originally built by Hans Rottier. It can be freely used and distributed under the GNU GPL license (see Licenses). You can find it <a href='https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=17998' target='_blank'>here</a>.<br/> <br/>
It has a wiki of its own: <a href='https://cumuluswiki.org/a/Category:CumulusUtils'>go here</a>.<br/> <br/>
All versions can be used as a website generator but for the single modules as well. CumulusUtils is for cooperation with CumulusMX.<br/><br/>
CumulusUtils stands on the shoulders of the following (but is unrelated to):<br/><br/>
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
              @"This work is licensed under the <a rel='license' href='https://www.gnu.org/licenses/gpl-3.0.en.html'>GNU GENERAL PUBLIC LICENSE</a> as per September 2025 withdrawing previous license statements. The tool is constructed in C# and javascript, on the basis of the Bootstrap toolkit.
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
<br><br>
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
              "</div>" );


            indexFile.Append( "<div class='col-sm-12 CUFooter'>" +
              "  <table style='width:100%; margin:auto'><tr>" +
              "    <td style='width: 35%;text-align: left; font-size: smaller'>" +
              $"       {CuSupport.FormattedVersion()}<br/>" +
               "       Powered by <a href='https://cumulus.hosiene.co.uk/index.php'>Cumulus[MX]</a>&nbsp;" +
              $"           <span id=programVersion>&nbsp;{thisCMXInfo.version}&nbsp;(build:&nbsp;{thisCMXInfo.build})</span>" +
              $"             &nbsp;{( NewVersionAvailable ? "[New version available:&nbsp;build: " + thisCMXInfo.NewBuildNumber + "]" : "" )}<br/>" +
               "         See further under <a data-bs-toggle='modal' href='#CUabout'>About</a> / <a data-bs-toggle='modal' href='#CUlicense'>Licenses</a>.</td>" +
              $"   <td style='width:30%;text-align: center; font-size: smaller'>{Sup.GetUtilsIniValue( "Website", "FooterCenterText", "" )}</td>" +
              $"   <td style='width:35%;text-align: right; font-size: smaller'>" +
              $"   <img src='{Base64LogoImage()}' width='auto' height='70vh' alt='CumulusUtils Logo'></td>" +
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
                if ( theseCharts is not null )
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
            } // End generating the cumuluscharts.txt as a result of CutilsCharts.def file
            else
            {
                Sup.LogDebugMessage( $"{Sup.PathUtils}{Sup.CutilsChartsDef} does not exist. No fall back exists from version 8 and up!" );
                Sup.LogDebugMessage( $"{Sup.PathUtils}{Sup.CutilsChartsDef} Please create CutilsCharts.def" );
                Sup.LogTraceErrorMessage( $"{Sup.PathUtils}{Sup.CutilsChartsDef} Exiting here" );

                Environment.Exit( 0 );
            }
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
                 $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Rain", "Rain", false )}</h4>" +
                 $"        <span style='font-size:200%' id='ajxRainToday'></span>" +
                  "        <p>" +
                  "          <span id='ajxRainRateNow'></span><br/><br/>" +
                  "          <!-- span id='ajxTodayRainRateHigh'></span> -->" +
                  "          <span id='ajxRainYesterday'></span><br/>" +
                  "          <span id='ajxRainWeek'></span><br/>" +
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
                  //$"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", false )}:</span><br/>" +
                  "        <span style='font-size: 200%' id='ajxCurHumidity'></span><br/><br/><br/>" +
                  $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "Dewpoint", "Dew Point", false )}:</span>" +
                  "        <span style='font-size: 140%' id='ajxDewpoint'></span><br/>";
                    break;

                case DashboardPanels.SolarText:
                    thisPanelCode = $"        <h4 class='CUCellTitle'>{Sup.GetCUstringValue( "Website", "Solar", "Solar", false )}</h4>";

                    if ( ShowSolar )
                    {
                        thisPanelCode +=
                        $"        <span style='font-size: 140%'>{Sup.GetCUstringValue( "Website", "SolarRadiation", "Radiation", false )}:</span><br/>" +
                         "        <span style='font-size: 200%' id='ajxCurSolar'></span><br/>" +
                        $"        <span>{Sup.GetCUstringValue( "Website", "MaxValue", "Max value", false )}: </span><span id='ajxCurSolarMax'></span><br/>" +
                        $"        <span>{Sup.GetCUstringValue( "Website", "SolarHours", "Sunshine hrs today", false )}: </span><span id='ajxSolarHours'></span><br/><br/>";
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

        enum ItemTypes : int { None, External, Internal, Image, Report, Separator }
        enum CompulsoryItems : int { Home, ToggleDashboard, Reports, Graphs, Records, Extra, Misc, About }

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

                bool[] CompulsoryItemsPresent = new bool[ Enum.GetNames( typeof( CompulsoryItems ) ).Length ];
                char[] charSeparators = new char[] { ' ', '\t', ';' };
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
                            CompulsoryItemsPresent[ (int) CompulsoryItems.Home ] = true;
                            WriteMenuHome( tmpMenu );
                            break;
                        case "About":
                            CompulsoryItemsPresent[ (int) CompulsoryItems.About ] = true;
                            WriteAboutMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Reports":
                            CompulsoryItemsPresent[ (int) CompulsoryItems.Reports ] = true;
                            WriteReportsMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Graphs":
                            CompulsoryItemsPresent[ (int) CompulsoryItems.Graphs ] = true;
                            WriteGraphsMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Records":
                            CompulsoryItemsPresent[ (int) CompulsoryItems.Records ] = true;
                            WriteRecordsMenu( tmpMenu );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                        case "Extra":
                            // Prevent the menu to be generated if it is not required.
                            CompulsoryItemsPresent[ (int) CompulsoryItems.Extra ] = true;
                            if ( CUtils.HasAirLink || CUtils.HasExtraSensors || CUtils.HasCustomLogs )
                            {
                                WriteExtraMenu( tmpMenu );
                                AllMenuFiles.AddRange( WriteUserItems( Keywords, true, tmpMenu, ref i ) );
                                tmpMenu.Append( "</ul></li>" );
                            }
                            break;
                        case "Misc":
                            CompulsoryItemsPresent[ (int) CompulsoryItems.Misc ] = true;
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
                            CompulsoryItemsPresent[ (int) CompulsoryItems.ToggleDashboard ] = true;
                            WriteToggleMenu( tmpMenu );
                            break;
                        default:
                            tmpMenu.Append( "      <li class='nav-item dropdown'>" +
                                "        <a class='nav-link dropdown-toggle' href='#' id='navbarDropdownAbout' role='button' data-bs-toggle='dropdown' aria-haspopup='true' aria-expanded='false'>" +
                                $"          {thisKeyword.Replace( '_', ' ' )}" +
                                "        </a>" );
                            tmpMenu.Append( "        <ul class='dropdown-menu' aria-labelledby='navbarDropdown'>" );
                            AllMenuFiles.AddRange( WriteUserItems( Keywords, false, tmpMenu, ref i ) );
                            tmpMenu.Append( "</ul></li>" );
                            break;
                    }
                } // Loop over all Keywords

                WriteMenuEnd( tmpMenu );

                if ( !Keywords[ Keywords.Length - 1 ].Equals( "About", CUtils.Cmp ) || !Keywords[ 0 ].Equals( "Home", CUtils.Cmp ) )
                {
                    Sup.LogTraceErrorMessage( $"Website Menu generator: 'Home' not first or 'About' not last Top Menu item." );
                    Sup.LogTraceErrorMessage( $"Website Menu generator: Using default menu." );

                    WriteDefaultMenu();
                }
                else // Did  we have all compulsory items?
                {
                    foreach ( CompulsoryItems thisItem in Enum.GetValues( typeof( CompulsoryItems ) ) )
                    {
                        if ( CompulsoryItemsPresent[ (int) thisItem ] != true )
                        {
                            Sup.LogTraceErrorMessage( $"Website Menu generator: Missing compulsory item {thisItem}." );
                            Sup.LogTraceErrorMessage( $"Website Menu generator: Missing compulsory items: Using default menu." );

                            WriteDefaultMenu();

                            // no need to continue
                            break;
                        }
                    }
                }

                //Write out all menufiles in the list
                foreach ( string thisFile in AllMenuFiles )
                    await Isup.UploadFileAsync( $"{thisFile}", $"{Sup.PathUtils}{thisFile}" );

            }
            else
            {
                WriteDefaultMenu();
            }

            Sup.LogTraceInfoMessage( $"Website Menu generator: Menu generation finished." );

            return tmpMenu.ToString();

            //
            // Below are the functions local to GenerateMenu()

            void WriteDefaultMenu()
            {
                Sup.LogTraceWarningMessage( $"Website Menu generator: using default menu" );

                tmpMenu.Clear();

                // No user definition found so generate the default standard menu
                //
                WriteMenuStart( tmpMenu );
                WriteMenuHome( tmpMenu );
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
                WriteAboutMenu( tmpMenu ); tmpMenu.Append( "</ul></li>" );
                WriteMenuEnd( tmpMenu );

                return;
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
                                thisMenuFileList.Add( MenuFile );

                                s.AppendLine( $"<li class='nav-link' onclick=\"LoadUtilsReport('{MenuFile}');\" {WidthStyleString}>{ItemNameURL}</li>" );
                                break;

                            case ItemTypes.Separator:
                                s.Append( "<div class='dropdown-divider'></div>" );
                                break;

                            default:
                                Sup.LogTraceErrorMessage( $"Website Menu generator: Illegal UserItem, can't generate..." );
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

                if ( CUtils.HasDiaryMenu )
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
                $"          <li class='nav-link' onclick=\"LoadUtilsReport('forecast.txt', false);\">{Sup.GetCUstringValue( "Website", "Forecast", "Forecast", false )}</li>" );

                if ( CUtils.HasSystemInfoMenu )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('systeminfoTable.txt', false);\">{Sup.GetCUstringValue( "Website", "SystemInfo", "System Info", false )}</li>" );

                s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('maps.txt', false);\">{Sup.GetCUstringValue( "Website", "UserMap", "User Map", false )}</li>" );

                if ( CUtils.HasStationMapMenu )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('stationmap.txt', false);loadRealtimeTxt();\">{Sup.GetCUstringValue( "Website", "StationMap", "StationMap", false )}</li>" );

                if ( CUtils.HasMeteoCamMenu )
                    s.Append( $"<li class='nav-link' onclick=\"LoadUtilsReport('meteocam.txt', false);loadRealtimeTxt();\">{Sup.GetCUstringValue( "Website", "MeteoCam", "MeteoCam", false )}</li>" );

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
        } // End GenerateMenu()


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

        #region Base64LogoImage
        private static string Base64LogoImage() => "data:image/png;base64,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/GMzLdsAAEJBaVRYdFhNTDpjb20uYWRvYmUueG1wAAAAAAA8P3hwYWNrZXQgYmVnaW49Iu+7vyIgaWQ9Ilc1TTBNcENlaGlIenJlU3pOVGN6a2M5ZCI/Pgo8eDp4bXBtZXRhIHhtbG5zOng9ImFkb2JlOm5zOm1ldGEvIiB4OnhtcHRrPSJBZG9iZSBYTVAgQ29yZSA1LjYtYzE0OCA3OS4xNjM4NTgsIDIwMTkvMDMvMDYtMDM6MTg6MzYgICAgICAgICI+CiAgIDxyZGY6UkRGIHhtbG5zOnJkZj0iaHR0cDovL3d3dy53My5vcmcvMTk5OS8wMi8yMi1yZGYtc3ludGF4LW5zIyI+CiAgICAgIDxyZGY6RGVzY3JpcHRpb24gcmRmOmFib3V0PSIiCiAgICAgICAgICAgIHhtbG5zOnhtcD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wLyIKICAgICAgICAgICAgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIgogICAgICAgICAgICB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIgogICAgICAgICAgICB4bWxuczpzdEV2dD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlRXZlbnQjIgogICAgICAgICAgICB4bWxuczpzdFJlZj0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlUmVmIyIKICAgICAgICAgICAgeG1sbnM6dGlmZj0iaHR0cDovL25zLmFkb2JlLmNvbS90aWZmLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOmV4aWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20vZXhpZi8xLjAvIj4KICAgICAgICAgPHhtcDpDcmVhdG9yVG9vbD5BZG9iZSBQaG90b3Nob3AgRWxlbWVudHMgMTguMCAoV2luZG93cyk8L3htcDpDcmVhdG9yVG9vbD4KICAgICAgICAgPHhtcDpDcmVhdGVEYXRlPjIwMjYtMDEtMjFUMTc6MDM6MjdaPC94bXA6Q3JlYXRlRGF0ZT4KICAgICAgICAgPHhtcDpNb2RpZnlEYXRlPjIwMjYtMDEtMjRUMTA6NTA6MTVaPC94bXA6TW9kaWZ5RGF0ZT4KICAgICAgICAgPHhtcDpNZXRhZGF0YURhdGU+MjAyNi0wMS0yNFQxMDo1MDoxNVo8L3htcDpNZXRhZGF0YURhdGU+CiAgICAgICAgIDxkYzpmb3JtYXQ+aW1hZ2UvcG5nPC9kYzpmb3JtYXQ+CiAgICAgICAgIDxwaG90b3Nob3A6Q29sb3JNb2RlPjM8L3Bob3Rvc2hvcDpDb2xvck1vZGU+CiAgICAgICAgIDxwaG90b3Nob3A6SUNDUHJvZmlsZT5zUkdCIElFQzYxOTY2LTIuMTwvcGhvdG9zaG9wOklDQ1Byb2ZpbGU+CiAgICAgICAgIDx4bXBNTTpJbnN0YW5jZUlEPnhtcC5paWQ6MmNmZmVjMTktYTQ1OS1lNDRjLTg2YTMtZTMwN2ZiNzBjMDE3PC94bXBNTTpJbnN0YW5jZUlEPgogICAgICAgICA8eG1wTU06RG9jdW1lbnRJRD5hZG9iZTpkb2NpZDpwaG90b3Nob3A6NmZjNTc4ZGItZjkxMi0xMWYwLThkZjAtYmQ3YjRiNDAwZDU0PC94bXBNTTpEb2N1bWVudElEPgogICAgICAgICA8eG1wTU06T3JpZ2luYWxEb2N1bWVudElEPnhtcC5kaWQ6ODczNTIzYjctZTU5Zi0zZDQzLTgxOGYtNTdlNjM4N2ZjNWVlPC94bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ+CiAgICAgICAgIDx4bXBNTTpIaXN0b3J5PgogICAgICAgICAgICA8cmRmOlNlcT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+Y3JlYXRlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6aW5zdGFuY2VJRD54bXAuaWlkOjg3MzUyM2I3LWU1OWYtM2Q0My04MThmLTU3ZTYzODdmYzVlZTwvc3RFdnQ6aW5zdGFuY2VJRD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OndoZW4+MjAyNi0wMS0yMVQxNzowMzoyN1o8L3N0RXZ0OndoZW4+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDpzb2Z0d2FyZUFnZW50PkFkb2JlIFBob3Rvc2hvcCBFbGVtZW50cyAxOC4wIChXaW5kb3dzKTwvc3RFdnQ6c29mdHdhcmVBZ2VudD4KICAgICAgICAgICAgICAgPC9yZGY6bGk+CiAgICAgICAgICAgICAgIDxyZGY6bGkgcmRmOnBhcnNlVHlwZT0iUmVzb3VyY2UiPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6YWN0aW9uPmNvbnZlcnRlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6cGFyYW1ldGVycz5mcm9tIGltYWdlL3BuZyB0byBhcHBsaWNhdGlvbi92bmQuYWRvYmUucGhvdG9zaG9wPC9zdEV2dDpwYXJhbWV0ZXJzPgogICAgICAgICAgICAgICA8L3JkZjpsaT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+c2F2ZWQ8L3N0RXZ0OmFjdGlvbj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0Omluc3RhbmNlSUQ+eG1wLmlpZDo4YTI1NTEyZC1mZjk5LTA4NDctYjM2ZC1lOTQ1YTY2YmQwNWI8L3N0RXZ0Omluc3RhbmNlSUQ+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDp3aGVuPjIwMjYtMDEtMjFUMTc6MDg6NTBaPC9zdEV2dDp3aGVuPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6c29mdHdhcmVBZ2VudD5BZG9iZSBQaG90b3Nob3AgRWxlbWVudHMgMTguMCAoV2luZG93cyk8L3N0RXZ0OnNvZnR3YXJlQWdlbnQ+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDpjaGFuZ2VkPi88L3N0RXZ0OmNoYW5nZWQ+CiAgICAgICAgICAgICAgIDwvcmRmOmxpPgogICAgICAgICAgICAgICA8cmRmOmxpIHJkZjpwYXJzZVR5cGU9IlJlc291cmNlIj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OmFjdGlvbj5zYXZlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6aW5zdGFuY2VJRD54bXAuaWlkOjkzMjQwMzhiLTBlNGYtYTg0Ni04ZjFjLTZhODY1MDA5M2UzMzwvc3RFdnQ6aW5zdGFuY2VJRD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OndoZW4+MjAyNi0wMS0yNFQxMDo1MDoxNVo8L3N0RXZ0OndoZW4+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDpzb2Z0d2FyZUFnZW50PkFkb2JlIFBob3Rvc2hvcCBFbGVtZW50cyAxOC4wIChXaW5kb3dzKTwvc3RFdnQ6c29mdHdhcmVBZ2VudD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OmNoYW5nZWQ+Lzwvc3RFdnQ6Y2hhbmdlZD4KICAgICAgICAgICAgICAgPC9yZGY6bGk+CiAgICAgICAgICAgICAgIDxyZGY6bGkgcmRmOnBhcnNlVHlwZT0iUmVzb3VyY2UiPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6YWN0aW9uPmNvbnZlcnRlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6cGFyYW1ldGVycz5mcm9tIGFwcGxpY2F0aW9uL3ZuZC5hZG9iZS5waG90b3Nob3AgdG8gaW1hZ2UvcG5nPC9zdEV2dDpwYXJhbWV0ZXJzPgogICAgICAgICAgICAgICA8L3JkZjpsaT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+ZGVyaXZlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6cGFyYW1ldGVycz5jb252ZXJ0ZWQgZnJvbSBhcHBsaWNhdGlvbi92bmQuYWRvYmUucGhvdG9zaG9wIHRvIGltYWdlL3BuZzwvc3RFdnQ6cGFyYW1ldGVycz4KICAgICAgICAgICAgICAgPC9yZGY6bGk+CiAgICAgICAgICAgICAgIDxyZGY6bGkgcmRmOnBhcnNlVHlwZT0iUmVzb3VyY2UiPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6YWN0aW9uPnNhdmVkPC9zdEV2dDphY3Rpb24+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDppbnN0YW5jZUlEPnhtcC5paWQ6MmNmZmVjMTktYTQ1OS1lNDRjLTg2YTMtZTMwN2ZiNzBjMDE3PC9zdEV2dDppbnN0YW5jZUlEPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6d2hlbj4yMDI2LTAxLTI0VDEwOjUwOjE1Wjwvc3RFdnQ6d2hlbj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OnNvZnR3YXJlQWdlbnQ+QWRvYmUgUGhvdG9zaG9wIEVsZW1lbnRzIDE4LjAgKFdpbmRvd3MpPC9zdEV2dDpzb2Z0d2FyZUFnZW50PgogICAgICAgICAgICAgICAgICA8c3RFdnQ6Y2hhbmdlZD4vPC9zdEV2dDpjaGFuZ2VkPgogICAgICAgICAgICAgICA8L3JkZjpsaT4KICAgICAgICAgICAgPC9yZGY6U2VxPgogICAgICAgICA8L3htcE1NOkhpc3Rvcnk+CiAgICAgICAgIDx4bXBNTTpEZXJpdmVkRnJvbSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgIDxzdFJlZjppbnN0YW5jZUlEPnhtcC5paWQ6OTMyNDAzOGItMGU0Zi1hODQ2LThmMWMtNmE4NjUwMDkzZTMzPC9zdFJlZjppbnN0YW5jZUlEPgogICAgICAgICAgICA8c3RSZWY6ZG9jdW1lbnRJRD5hZG9iZTpkb2NpZDpwaG90b3Nob3A6YmMxOTc5NTgtZjkwNi0xMWYwLThkZjAtYmQ3YjRiNDAwZDU0PC9zdFJlZjpkb2N1bWVudElEPgogICAgICAgICAgICA8c3RSZWY6b3JpZ2luYWxEb2N1bWVudElEPnhtcC5kaWQ6ODczNTIzYjctZTU5Zi0zZDQzLTgxOGYtNTdlNjM4N2ZjNWVlPC9zdFJlZjpvcmlnaW5hbERvY3VtZW50SUQ+CiAgICAgICAgIDwveG1wTU06RGVyaXZlZEZyb20+CiAgICAgICAgIDx0aWZmOk9yaWVudGF0aW9uPjE8L3RpZmY6T3JpZW50YXRpb24+CiAgICAgICAgIDx0aWZmOlhSZXNvbHV0aW9uPjcyMDAwMC8xMDAwMDwvdGlmZjpYUmVzb2x1dGlvbj4KICAgICAgICAgPHRpZmY6WVJlc29sdXRpb24+NzIwMDAwLzEwMDAwPC90aWZmOllSZXNvbHV0aW9uPgogICAgICAgICA8dGlmZjpSZXNvbHV0aW9uVW5pdD4yPC90aWZmOlJlc29sdXRpb25Vbml0PgogICAgICAgICA8ZXhpZjpDb2xvclNwYWNlPjE8L2V4aWY6Q29sb3JTcGFjZT4KICAgICAgICAgPGV4aWY6UGl4ZWxYRGltZW5zaW9uPjMzNTwvZXhpZjpQaXhlbFhEaW1lbnNpb24+CiAgICAgICAgIDxleGlmOlBpeGVsWURpbWVuc2lvbj45MDwvZXhpZjpQaXhlbFlEaW1lbnNpb24+CiAgICAgIDwvcmRmOkRlc2NyaXB0aW9uPgogICA8L3JkZjpSREY+CjwveDp4bXBtZXRhPgogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgIAo8P3hwYWNrZXQgZW5kPSJ3Ij8+P4lt1wAAACBjSFJNAAB6JQAAgIMAAPn/AACA6QAAdTAAAOpgAAA6mAAAF2+SX8VGAABjjElEQVR42ux9d5wcxZn2U1UdJu7szs6sNimsNLPKWUISGCRAgAPGPgw4gOXDEZ8DNs532D58jhd8Tjidz3cOcGcwtjEYBzBJtjFZ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";

        #endregion

        #region Useful NOT USED (GetAreaGradientColors)
        // Note that these colors contain transparency in the first two HEX digits. If not it is NOT OK
        // Not Used
        //private string GetAreaGradientColors( string Color1, string Color2, string Color3 )
        //{
        //    bool ValidColor1 = false;
        //    bool ValidColor2 = false;
        //    bool ValidColor3 = false;
        //    StringBuilder result = new StringBuilder();

        //    if ( !String.IsNullOrEmpty( Color1 ) )
        //        ValidColor1 = true;
        //    if ( !String.IsNullOrEmpty( Color2 ) )
        //        ValidColor2 = true;
        //    if ( !String.IsNullOrEmpty( Color3 ) )
        //        ValidColor3 = true;

        //    if ( ValidColor1 )
        //    {
        //        // There  is only the first color, return what we need
        //        Sup.LogTraceVerboseMessage( "GetAreaGradientColors : Generating the color code..." );
        //        result.Append( $"color: '{Color1}'," );

        //        if ( ValidColor2 && ValidColor3 )
        //        {
        //            // Add the gradient to the result
        //            Sup.LogTraceVerboseMessage( "GetAreaGradientColors : Generating the code for just 3 colors so a full gradient" );
        //            result.Append( "fillColor: {linearGradient:{x1: 0,y1: 1,x2: 0,y2: 0}," +
        //              $"stops:[[0,'{Color2}'],[1,'{Color3}']]}}," );
        //        }
        //    }
        //    else
        //    {
        //        Sup.LogTraceVerboseMessage( $"GetAreaGradientColors : No valid colours supplied: 1: {Color1}, 2: {Color2}, 3: {Color3}" );
        //        result.Append( "" );
        //    }

        //    return result.ToString();
        //}

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
