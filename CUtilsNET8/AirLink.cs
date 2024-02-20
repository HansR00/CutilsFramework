/*
 * DayRecords - Part of CumulusUtils
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
 */

/*
 * // How the AirLink sensor system works.
 * There are three components: 
 *   1) The historic data in the datafile AirLinkYYYYMMlog.txt
 *   2) The realtime data 
 *   3) The display of the actual data and the graphing of the data in addition to the archived data
 *   
 *ad 1) Apparently the datafile is not sent as a json. To create the graph, the data has to be sent to the module, the realtime data have to be added to the datastructure while the module is alive.
 *ad 2) The realtime data need to be sent to the module. Therefore:
 *     1) Cumulusutils must create the airlinkrealtime.txt and the entry in Cumulus.ini which processes and sends it to the site.
 *     2) On the site, the module - when active - needs to be hooked in the minute time to read the airlinkrealtime.txt and put the data in the respective fields
 *ad 3) The graphs need to be adjusted, realtime data added and redrawn
 *
 * Below I reproduce the links as found in the AQI code of CMX. Please note that for Canada there is an issue because CMX just takes the PM component of a combined formula.
 * I will give some additional links for Canada. Canada has confusing information about its AirQuality system.
 * Please check also this post on the forum: https://cumulus.hosiene.co.uk/viewtopic.php?t=18602
 * 
 * Links:
 *  1) US: https://www.airnow.gov/sites/default/files/2018-05/aqi-technical-assistance-document-may2016.pdf
 *  2) GB: https://assets.publishing.service.gov.uk/government/uploads/system/uploads/attachment_data/file/304633/COMEAP_review_of_the_uk_air_quality_index.pdf
 *  3) EU: http://www.airqualitynow.eu/about_indices_definition.php
 *  4) EU 2: https://www.airqualitynow.eu/download/CITEAIR-Comparing_Urban_Air_Quality_across_Borders.pdf
 *  5) CA: https://en.wikipedia.org/wiki/Air_Quality_Health_Index_(Canada)  (just the component of PM2.5 is used, no levels are given)
 *  6) AU: https://www.environment.nsw.gov.au/topics/air/understanding-air-quality-data/air-quality-index
 *  7) NL: https://www.luchtmeetnet.nl/informatie/luchtkwaliteit/luchtkwaliteitsindex-(lki)
 *  8) BE: https://www.irceline.be/en/air-quality/measurements/belaqi-air-quality-index/information?set_language=en
 *  
 *  The canadian link points to an equation which holds components to NO2, O3 and PM2.5 concentrations where CMX uses only the PM2.5 component for all PM values.
 *  This seems incorrect to me. Research gave some literature but not a very conclusive idea about Canadian AirLink. For now I will use the US AQIndex.
 *  NOTE: there are many online calculators to give an AQI for a concentration but none explains how the calculations are done.
 *  Additional Canadian links:
 *  1) https://www.ccme.ca/en/resources/air/aqms.html
 *  2) https://www.ccme.ca/en/resources/air/pm_ozone.html
 *  3) https://www.canada.ca/en/environment-climate-change/services/air-pollution/monitoring-networks-data/national-air-pollution-program.html
 *  
 *  Additional Australian links
 *  1) https://aqicn.org/faq/2014-09-06/australian-air-quality-comparison-with-the-us-epa-aqi-scale/
 *  
 *  Some research documents:
 *  1) https://www.researchgate.net/publication/329533162_Air_quality_health_indices_-_review
 *  2) Search for "Review of Air Quality Index and Air Quality Health Index"
 *  3) Search for "Ontario and the Canada-Wide Standards for particulate matter and ground-level ozone"
 *  4) https://www.tandfonline.com/doi/abs/10.3155/1047-3289.58.3.435 ("A New Multipollutant, No-Threshold Air Quality Health Index Based on Short-Term Associations Observed in Daily Time-Series Analyses")
 *  5) https://www.ccme.ca/en/resources/air/pm_ozone.html
 *  6) https://www.ccme.ca/en/resources/air/aqms.html
 *  7) https://www.canada.ca/en/environment-climate-change/services/air-pollution/monitoring-networks-data/national-air-pollution-program.html
 *  8) https://www.alberta.ca/air-quality-health-index--calculation.aspx
 *  9) https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4213258/
 *  10) http://www.airqualityontario.com/science/aqhi_description.php
 *  
 *  It seems as if Canada simply has no unifying AQI. Maybe because they have clean air and don't think it a big deal. But if air quality is objective
 *  they could measure their clean air. Couldn't they?
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentFTP.Helpers;
using ServiceStack.Text;

namespace CumulusUtils
{
    public class AirLink
    {
        readonly CuSupport Sup;

        enum SupportedCountries : int { US, UK, CA, EU, AU, NL, BE };
        enum CumulusCountries : int { US, UK, EUAQI, EUCAQI, CA, AU, NL, BE };

        readonly string[] Concentrations = { "2p5", "10" };
        readonly string[] Series = { "", "_1hr", "_3hr", "_24hr", "_nowcast" };

        readonly SupportedCountries CountrySelected;
        readonly CumulusCountries CumulusCountrySetting;

        readonly int NrOfClassesInCountry;
        readonly string[] Colours;
        readonly string[] Description;

        readonly int ReferenceNrOfClasses;
        readonly double[] ReferenceConcentrations2p5, ReferenceConcentrations10;

        // Defined in Cumulus.ini telling me which sensors are present
        // And what doe we want to see? Now, 1 hr, 3 hr, 24 hr and NowCast defined in cumulusutils.ini
        private readonly bool WantToSeeNow;
        private readonly bool WantToSeeNowCast;
        private readonly bool WantToSee1hr;
        private readonly bool WantToSee3hr;
        private readonly bool WantToSee24hr;
        private readonly bool WantToSeeWind;
        private readonly bool AirLinkIn;
        private readonly bool AirLinkOut;
        private readonly bool TwoSensors;
        private readonly bool StandAloneModule;
        readonly string Message;

        #region Constructor
        public AirLink( CuSupport s )
        {
            Sup = s;

            bool WrongNormativeCountry = false;

            Sup.LogDebugMessage( "AirLink Contructor: start" );

            // Check the country. Order is 1) found  in inifile 2) part of locale 3) Use the default if not present or not supported
            string tmp = Sup.GetUtilsIniValue( "AirLink", "CountrySelected", "" );
            if ( tmp.IsBlank() ) tmp = Sup.Country;

            try
            {
                Sup.LogTraceInfoMessage( $"AirLink Contructor: Parsing the country {tmp}" );
                CountrySelected = (SupportedCountries) Enum.Parse( typeof( SupportedCountries ), tmp, true );
            }
            catch ( Exception e ) when ( e is ArgumentException || e is ArgumentNullException )
            {
                Sup.LogTraceWarningMessage( $"AirLink Contructor: Exception parsing the country - {e.Message}" );
                Sup.LogTraceWarningMessage( $"AirLink Contructor: Country not found {tmp} - defaulting to EU" );
                CountrySelected = SupportedCountries.EU;
            }

            CumulusCountrySetting = (CumulusCountries) Convert.ToInt32( Sup.GetCumulusIniValue( "AirLink", "AQIformula", "" ) );

            switch ( CumulusCountrySetting )
            {
                case CumulusCountries.US:
                    if ( CountrySelected != SupportedCountries.US )
                        WrongNormativeCountry = true;
                    break;

                case CumulusCountries.UK:
                    if ( CountrySelected != SupportedCountries.UK )
                        WrongNormativeCountry = true;
                    break;

                case CumulusCountries.CA:
                    if ( CountrySelected != SupportedCountries.CA )
                        WrongNormativeCountry = true;
                    break;

                case CumulusCountries.EUAQI:
                case CumulusCountries.EUCAQI:
                    if ( CountrySelected != SupportedCountries.EU )
                        WrongNormativeCountry = true;
                    Sup.LogTraceWarningMessage( $"AirLink Contructor Warning: EUAQI or EUCAQI are both used as EUCAQI in CumulusUtils" );
                    break;

                case CumulusCountries.AU:
                    if ( CountrySelected != SupportedCountries.AU )
                        WrongNormativeCountry = true;
                    break;

                case CumulusCountries.NL:
                    if ( CountrySelected != SupportedCountries.NL )
                        WrongNormativeCountry = true;
                    break;

                case CumulusCountries.BE:
                    if ( CountrySelected != SupportedCountries.BE )
                        WrongNormativeCountry = true;
                    break;

                default:
                    WrongNormativeCountry = true;
                    break;
            }

            if ( WrongNormativeCountry )
            {
                Sup.LogTraceWarningMessage( $"AirLink Contructor: Found country {CountrySelected} against Cumulus Normatiove Country {CumulusCountrySetting}." );
                Sup.LogTraceWarningMessage( "Settings AQI Normative Country settings for calculation in both Cumulus and CumulusUtils must match." );
                Sup.LogTraceWarningMessage( $"Cumulusutils will continue with the existing CumulusUtils setting {CountrySelected}, values are from {CumulusCountrySetting}" );
                Message = "<p style='color:red'>Settings for AQI Normative Country for calculation in both Cumulus and CumulusUtils should match for useful results.</p>";
            }
            else
                Message = "";

            // Now fill the colours arrray for the different countries: colour research by Wim DKuil (Leuven Script) / Beteljuice / Mark Crossley
            // and fill the concentrations for the classes as defined in the Beteljuice script.
            //
            switch ( CountrySelected )
            {
                case SupportedCountries.US:
                    NrOfClassesInCountry = 6;
                    Colours = new string[ 6 ] { "#50F0E6", "#50CCAA", "#F0E641", "#FF5050", "#960032", "#7D2181" };
                    Description = new string[ 6 ] { $"{Sup.GetCUstringValue("AirQuality", "Good", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "UnhealthyIfSensitive", "Unhealthy for Sensitive Groups", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Unhealthy", "Unhealthy", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryUnhealthy", "Very Unhealthy", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Hazardous", "Hazardous", false)}" };
                    break;

                case SupportedCountries.UK:
                    NrOfClassesInCountry = 10;
                    Colours = new string[ 10 ] { "#9CFF9C", "#31FF00", "#31CF00", "#FFFF00", "#FFCF00", "#FF9A00", "#FF6464", "#FF0000", "#990000", "#CE30FF" };
                    Description = new string[ 10 ] { $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "VeryHigh", "Very High", false)}" };
                    break;

                case SupportedCountries.CA:  // Use the USA index for now. Canada is not clear about it all
                    NrOfClassesInCountry = 11;
                    Colours = new string[ 11 ] { "#00CCFF", "#0099CC", "#006699", "#FFFF00", "#FFCC00", "#FF9933", "#FF6666", "#FF0000", "#CC0000", "#990000", "#660000" };
                    Description = new string[ 11 ] { $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Moderate", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "VeryHigh", "Very High", false)}",
                                         $"{Sup.GetCUstringValue("AirQuality", "VeryHigh", "Very High", false)}" };
                    break;

                case SupportedCountries.EU:
                    NrOfClassesInCountry = 5;
                    Colours = new string[ 5 ] { "#79bc6a", "#bbcf4c", "#eec20b", "#f29305", "#960018" };
                    Description = new string[ 5 ] { $"{Sup.GetCUstringValue("AirQuality", "VeryLow", "Very Low", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Medium", "Medium", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryHigh", "Very High", false)}" };
                    break;

                case SupportedCountries.AU:
                    NrOfClassesInCountry = 6;
                    Colours = new string[ 6 ] { "#31ADD3", "#99B964", "#FFD236", "#EC783A", "#782D49", "#D04730" };
                    Description = new string[ 6 ] { $"{Sup.GetCUstringValue("AirQuality", "VeryGood", "Very Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Good", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Fair", "Fair", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Poor", "Poor", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryPoor", "Very Poor", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Hazardous", "Hazardous", false)}" };
                    break;

                case SupportedCountries.NL:
                    NrOfClassesInCountry = 11;
                    Colours = new string[ 11 ] { "#0065FF", "#63AEFF", "#94CCFF", "#FFFECE", "#FFFF95", "#FFFF00", "#FFCA00", "#FF9600", "#FF4900", "#FE0000", "#630094" };
                    Description = new string[ 11 ] {$"{Sup.GetCUstringValue("AirQuality", "Good", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Good", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Good", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Fair", "Fair", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Fair", "Fair", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Fair", "Fair", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Insufficient", "Insufficient", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Insufficient", "Insufficient", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Bad", "Bad", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Bad", "Bad", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryBad", "Very Bad", false)}" };
                    break;

                case SupportedCountries.BE:
                    NrOfClassesInCountry = 10;
                    Colours = new string[ 10 ] { "#0000FF", "#0099FF", "#009900", "#00FF00", "#FFFF00", "#FFBB00", "#FF6600", "#FF0000", "#990000", "#660000" };
                    Description = new string[ 10 ] { $"{Sup.GetCUstringValue("AirQuality", "Excellent", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryGood", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Good", "Good", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Reasonably good", "Fair", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Moderate", "Fair", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Insufficient", "Insufficient", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryInsufficient", "Very Insufficient", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Bad", "Bad", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryBad", "Very Bad", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Terrible", "Terrible", false)}" };

                    break;

                default:
                    Sup.LogTraceWarningMessage( "AirLink Contructor: Norm country is not supported - Not Allowed, using EU" );
                    CountrySelected = SupportedCountries.EU;
                    NrOfClassesInCountry = 5;
                    Colours = new string[ 5 ] { "#79bc6a", "#bbcf4c", "#eec20b", "#f29305", "#960018" };
                    Description = new string[ 5 ] { $"{Sup.GetCUstringValue("AirQuality", "VeryLow", "Very Low", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Low", "Low", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "Medium", "Medium", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "High", "High", false)}",
                                        $"{Sup.GetCUstringValue("AirQuality", "VeryHigh", "Very High", false)}" };
                    break;
            }

            // Set the values for the referencelines
            ReferenceNrOfClasses = 5;
            ReferenceConcentrations2p5 = new double[ 5 ] { 0, 15, 30, 55, 110 };
            ReferenceConcentrations10 = new double[ 5 ] { 0, 25, 50, 90, 180 };
            //ReferenceColours = ... not required because hardcoded - too complex to parameterize

            // What doe we want to see? Now, 1 hr, 3 hr, 24 hr and NowCast
            WantToSeeNow = Sup.GetUtilsIniValue( "AirLink", "WantToSeeNow", "true" ).Equals( "true", CUtils.Cmp );
            WantToSeeNowCast = Sup.GetUtilsIniValue( "AirLink", "WantToSeeNowCast", "false" ).Equals( "true", CUtils.Cmp );
            WantToSee1hr = Sup.GetUtilsIniValue( "AirLink", "WantToSee1hr", "false" ).Equals( "true", CUtils.Cmp );
            WantToSee3hr = Sup.GetUtilsIniValue( "AirLink", "WantToSee3hr", "false" ).Equals( "true", CUtils.Cmp );
            WantToSee24hr = Sup.GetUtilsIniValue( "AirLink", "WantToSee24hr", "false" ).Equals( "true", CUtils.Cmp );
            WantToSeeWind = Sup.GetUtilsIniValue( "AirLink", "WantToSeeWind", "false" ).Equals( "true", CUtils.Cmp );

            AirLinkIn = Sup.GetCumulusIniValue( "AirLink", "In-Enabled", "0" ).Equals( "1", CUtils.Cmp );
            AirLinkOut = Sup.GetCumulusIniValue( "AirLink", "Out-Enabled", "0" ).Equals( "1", CUtils.Cmp );

            TwoSensors = AirLinkIn & AirLinkOut;

            StandAloneModule = Sup.GetUtilsIniValue( "AirLink", "StandAloneModule", "false" ).Equals( "true", CUtils.Cmp );

            Sup.LogTraceInfoMessage( $"WantToSeeNow = {WantToSeeNow}" );
            Sup.LogTraceInfoMessage( $"WantToSeeNowCast = {WantToSeeNowCast}" );
            Sup.LogTraceInfoMessage( $"WantToSee1hr = {WantToSee1hr}" );
            Sup.LogTraceInfoMessage( $"WantToSee3hr = {WantToSee3hr}" );
            Sup.LogTraceInfoMessage( $"WantToSee24hr = {WantToSee24hr}" );
            Sup.LogTraceInfoMessage( $"WantToSeeWind = {WantToSeeWind}" );

            Sup.LogTraceInfoMessage( "AirLink Contructor: stop" );

            return;
        }

        #endregion

        #region DoAirLink
        public void DoAirLink()
        {
            Sup.LogDebugMessage( "DoAirLink - Start" );

            Sup.LogTraceInfoMessage( $"DoAirLink - AirLinkIn : {AirLinkIn} / AirLinkOut : {AirLinkOut}" );

            GenAirLinkModule();
        }

        #endregion

        #region GenAirLinkModule Tables and Graphs
        private void GenAirLinkModule()
        {
            Sup.LogDebugMessage( "DoAirLinkModule - Start" );

            StringBuilder of = new StringBuilder( 15000 );
            StringBuilder sb = new StringBuilder();

            // From here we do per RT per sensor. So In and Out
            int NrOfSensors = TwoSensors ? 2 : 1;

            #region AirLinkRealtime

            // I: First generate the RealTime AirLink file
            using ( StreamWriter rt = new StreamWriter( $"{Sup.PathUtils}{Sup.AirLinkRealtimeFilename}", false, Encoding.UTF8 ) )
            {
                sb.Clear();

                // Renew this file everytime?? or just when it does not exist. What if something changes?
                if ( AirLinkIn )
                {
                    Sup.LogTraceInfoMessage( $"DoAirLink - Writing the AirLink realtime file - Inside" );

                    sb.Append( "<#AirLinkTempIn rc=y> <#AirLinkHumIn rc=y> " +
                    "<#AirLinkPm1In rc=y> <#AirLinkPm2p5In rc=y> <#AirLinkPm2p5_1hrIn rc=y> <#AirLinkPm2p5_3hrIn rc=y> <#AirLinkPm2p5_24hrIn rc=y> <#AirLinkPm2p5_NowcastIn rc=y> " +
                    "<#AirLinkPm10In rc=y> <#AirLinkPm10_1hrIn rc=y> <#AirLinkPm10_3hrIn rc=y> <#AirLinkPm10_24hrIn rc=y> <#AirLinkPm10_NowcastIn rc=y> " +
                    "<#AirLinkAqiPm2p5In rc=y> <#AirLinkAqiPm2p5_1hrIn rc=y> <#AirLinkAqiPm2p5_3hrIn rc=y> <#AirLinkAqiPm2p5_24hrIn rc=y> <#AirLinkAqiPm2p5_NowcastIn rc=y> " +
                    "<#AirLinkAqiPm10In rc=y> <#AirLinkAqiPm10_1hrIn rc=y> <#AirLinkAqiPm10_3hrIn rc=y> <#AirLinkAqiPm10_24hrIn rc=y> <#AirLinkAqiPm10_NowcastIn rc=y> " +
                    "<#AirLinkPct_1hrIn rc=y> <#AirLinkPct_3hrIn rc=y> <#AirLinkPct_24hrIn rc=y> <#AirLinkPct_NowcastIn rc=y> " );
                }

                if ( AirLinkOut )
                {
                    Sup.LogTraceInfoMessage( $"DoAirLink - Writing the AirLink realtime file - Outside" );

                    // Here we need to start with a space to make sure it appends to the Inside part and can be read in the receiving javascript (the split)
                    sb.Append( "<#AirLinkTempOut rc=y> <#AirLinkHumOut rc=y> " +
                    "<#AirLinkPm1Out rc=y> <#AirLinkPm2p5Out rc=y> <#AirLinkPm2p5_1hrOut rc=y> <#AirLinkPm2p5_3hrOut rc=y> <#AirLinkPm2p5_24hrOut rc=y> <#AirLinkPm2p5_NowcastOut rc=y> " +
                    "<#AirLinkPm10Out rc=y> <#AirLinkPm10_1hrOut rc=y> <#AirLinkPm10_3hrOut rc=y> <#AirLinkPm10_24hrOut rc=y> <#AirLinkPm10_NowcastOut rc=y> " +
                    "<#AirLinkAqiPm2p5Out rc=y> <#AirLinkAqiPm2p5_1hrOut rc=y> <#AirLinkAqiPm2p5_3hrOut rc=y> <#AirLinkAqiPm2p5_24hrOut rc=y> <#AirLinkAqiPm2p5_NowcastOut rc=y> " +
                    "<#AirLinkAqiPm10Out rc=y> <#AirLinkAqiPm10_1hrOut rc=y> <#AirLinkAqiPm10_3hrOut rc=y> <#AirLinkAqiPm10_24hrOut rc=y> <#AirLinkAqiPm10_NowcastOut rc=y> " +
                    "<#AirLinkPct_1hrOut rc=y> <#AirLinkPct_3hrOut rc=y> <#AirLinkPct_24hrOut rc=y> <#AirLinkPct_NowcastOut rc=y>" );
                }

                rt.WriteLine( sb.ToString() );
            }

            #endregion

            // So, now assume this realtime file is correctly transferred with contents to the website and we must make sure the file is read and the contents is transferred
            // to the respective fields in the table where the user would like to see it together with the AQI which we calculate from the values in the realtime file.
            // The AQI (for the specific country!) determines the table: nr of columns, colors of the levels:
            //
            // -----------------------------------------------------------------------------------------------------------------------------------------------------------------
            //
            // II: And now write out the actual module

            #region Javascript

            // Generate the table on the basis of what is set in the inifile and setup in the constructor. 
            // We don't support live switching as we don't support that for anything.
            // Generate the start of the realtime script
            //

            if ( StandAloneModule )
            {
                of.AppendLine( "<head>" );
                of.AppendLine( " <meta charset=\"UTF-8\">" );
                of.AppendLine( " <meta name=\"description\" content=\"Cumulus standard Website, part of CumulusUtils by( c )Hans Rottier\" />" );
                of.AppendLine( " <meta name=\"keywords\" content=\"Cumulus, weather, data, weather station, CumulusUtils\" />" );
                of.AppendLine( " <meta name=\"robots\" content=\"index, noarchive, follow, noimageindex, noimageclick\" />" );
                of.AppendLine( " <link rel=\"shortcut icon\" href=\"favicon.ico\" type=\"image/x-icon\" />" );
                of.AppendLine( " <meta name=\"theme-color\" content=\"#ffffff\" />" );
                of.AppendLine( " <title>AirLink Standalone - CumulusUtils</title>" );
                of.AppendLine( " <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"> " );

                CUtils.DojQueryInclude = true;
                CUtils.DoLibraryIncludes = true;
                CUtils.DoWebsite = false;
            }


            of.AppendLine( $"{CuSupport.GenjQueryIncludestring()}" );

            if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
            {
                of.AppendLine( Sup.GenHighchartsIncludes().ToString() );
            }

            of.AppendLine( "<script>" );
            of.AppendLine( "console.log('Module AirLink...');" );
            of.AppendLine( "var AirLinkTimer;" );
            of.AppendLine( "$(function () {" );  // Get the whole thing going
            of.AppendLine( "  loadAirLinkTxt();" );
            of.AppendLine( "  if (AirLinkTimer == null) AirLinkTimer = setInterval(loadAirLinkTxt, 60 * 1000);" );
            of.AppendLine( "  if (urlParams.has('dropdown')) {" +
                "AirLink2Load = urlParams.get('dropdown');" +
                "switch (AirLink2Load) {" +
                "  case 'TableView':" +
                "    SetTableView();" +
                "    break;" +
                "  case 'GraphView0':" +
                "    SetGraphView0();" +
                "    break;" +
                "  case 'GraphView1':" +
                "    SetGraphView1();" +
                "    break;" +
                "  default:" +
                "    SetTableView();" +
                "    break;" +
                "  }" +
                "} else SetTableView();" );
            of.AppendLine( "});" );
            of.AppendLine( "" );
            of.AppendLine( "function loadAirLinkTxt() {" );
            of.AppendLine( "  $.ajax({" );
            of.AppendLine( $"    url: '{Sup.AirLinkRealtimeFilename}'," );
            of.AppendLine( "    timeout: 2000," );
            of.AppendLine( "    cache:false," );
            of.AppendLine( "    headers: { 'Access-Control-Allow-Origin': '*' }," );
            of.AppendLine( "    crossDomain: true" );
            of.AppendLine( "  })" +
                ".done(function (response, responseStatus) { DoAirLinkRT(response) })" +
                ".fail(function(jqXHR, textStatus, errorThrown){ console.log('loadAirLinkTxt: ' + textStatus + ' : ' + errorThrown) });" );
            of.AppendLine( "};" );

            of.AppendLine( "function DoAirLinkRT(input) {" );

            of.AppendLine( "  var AirLinkRT = input.split(' ');" );

            for ( int j = 0; j < NrOfSensors; j++ ) // NOTE: For the algorithm j==0 => IN and j==1 => Out; see relevance for the realtime value array
            {
                int Offset = j == 0 ? 0 : 27;
                string InOut = DetermineSensor( j, TwoSensors );

                of.AppendLine( $"  $('#ajxTemp{InOut}').html(AirLinkRT[0+{j * Offset}] + ' {Sup.StationTemp.Text()}');" );
                of.AppendLine( $"  $('#ajxHum{InOut}').html(AirLinkRT[1+{j * Offset}] + '%');" );

                if ( WantToSeeNow )
                {
                    of.AppendLine( $"  var nowPM2p5_{InOut}Val = AirLinkRT[3+{j * Offset}];" );
                    of.AppendLine( $"  var nowPM10_{InOut}Val = AirLinkRT[8+{j * Offset}];" );
                    of.AppendLine( $"  var nowPM2p5_{InOut}AQI = AirLinkRT[13+{j * Offset}];" );
                    of.AppendLine( $"  var nowPM2p5_{InOut}AQIint = NormaliseAQI(nowPM2p5_{InOut}AQI);" );
                    of.AppendLine( $"  var nowPM10_{InOut}AQI = AirLinkRT[18+{j * Offset}];" );
                    of.AppendLine( $"  var nowPM10_{InOut}AQIint = NormaliseAQI(nowPM10_{InOut}AQI);" );
                    of.AppendLine( $"  $('#nowPM2p5_{InOut}Val').html(nowPM2p5_{InOut}Val);" );
                    of.AppendLine( $"  $('#nowPM2p5_{InOut}AQI').html(nowPM2p5_{InOut}AQI);" );
                    of.AppendLine( $"  $('#nowPM10_{InOut}Val').html(nowPM10_{InOut}Val);" );
                    of.AppendLine( $"  $('#nowPM10_{InOut}AQI').html(nowPM10_{InOut}AQI);" );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == nowPM2p5_{InOut}AQIint ? $('#nowPM2p5Arrow{InOut}'+i).html('▲') : $('#nowPM2p5Arrow{InOut}'+i).html(' ') );  }} " );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == nowPM10_{InOut}AQIint ? $('#nowPM10Arrow{InOut}'+i).html('▲') : $('#nowPM10Arrow{InOut}'+i).html(' ') );  }} " );
                }

                if ( WantToSee1hr )
                {
                    of.AppendLine( $"  var PM2p5_{InOut}Val1h = AirLinkRT[4+{j * Offset}];" );
                    of.AppendLine( $"  var PM10_{InOut}Val1h = AirLinkRT[9+{j * Offset}];" );
                    of.AppendLine( $"  var PM2p5_{InOut}AQI1h = AirLinkRT[14+{j * Offset}];" );
                    of.AppendLine( $"  var PM2p5_{InOut}AQIint1h = NormaliseAQI(PM2p5_{InOut}AQI1h);" );  // parseInt(PM2p5_{InOut}AQI1h, 10) | 0;
                    of.AppendLine( $"  var PM10_{InOut}AQI1h = AirLinkRT[19+{j * Offset}];" );
                    of.AppendLine( $"  var PM10_{InOut}AQIint1h = NormaliseAQI(PM10_{InOut}AQI1h);" );   // parseInt(PM10_{InOut}AQI1h, 10) | 0;
                    of.AppendLine( $"  $('#PM2p5_{InOut}Val1h').html(PM2p5_{InOut}Val1h);" );
                    of.AppendLine( $"  $('#PM2p5_{InOut}AQI1h').html(PM2p5_{InOut}AQI1h);" );
                    of.AppendLine( $"  $('#PM10_{InOut}Val1h').html(PM10_{InOut}Val1h);" );
                    of.AppendLine( $"  $('#PM10_{InOut}AQI1h').html(PM10_{InOut}AQI1h);" );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == PM2p5_{InOut}AQIint1h ? $('#PM2p5Arrow{InOut}1h'+i).html('▲') : $('#PM2p5Arrow{InOut}1h'+i).html(' ') );  }} " );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == PM10_{InOut}AQIint1h ? $('#PM10Arrow{InOut}1h'+i).html('▲') : $('#PM10Arrow{InOut}1h'+i).html(' ') );  }} " );
                }

                if ( WantToSee3hr )
                {
                    of.AppendLine( $"  var PM2p5_{InOut}Val3h = AirLinkRT[5+{j * Offset}];" );
                    of.AppendLine( $"  var PM10_{InOut}Val3h = AirLinkRT[10+{j * Offset}];" );
                    of.AppendLine( $"  var PM2p5_{InOut}AQI3h = AirLinkRT[15+{j * Offset}];" );
                    of.AppendLine( $"  var PM2p5_{InOut}AQIint3h = NormaliseAQI(PM2p5_{InOut}AQI3h);" );  // parseInt(PM2p5_{InOut}AQI3h, 10) | 0;
                    of.AppendLine( $"  var PM10_{InOut}AQI3h = AirLinkRT[20+{j * Offset}];" );
                    of.AppendLine( $"  var PM10_{InOut}AQIint3h = NormaliseAQI(PM10_{InOut}AQI3h);" );  // parseInt(PM10_{InOut}AQI3h, 10) | 0;
                    of.AppendLine( $"  $('#PM2p5_{InOut}Val3h').html(PM2p5_{InOut}Val3h);" );
                    of.AppendLine( $"  $('#PM2p5_{InOut}AQI3h').html(PM2p5_{InOut}AQI3h);" );
                    of.AppendLine( $"  $('#PM10_{InOut}Val3h').html(PM10_{InOut}Val3h);" );
                    of.AppendLine( $"  $('#PM10_{InOut}AQI3h').html(PM10_{InOut}AQI3h);" );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == PM2p5_{InOut}AQIint3h ? $('#PM2p5Arrow{InOut}3h'+i).html('▲') : $('#PM2p5Arrow{InOut}3h'+i).html(' ') );  }} " );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == PM10_{InOut}AQIint3h ? $('#PM10Arrow{InOut}3h'+i).html('▲') : $('#PM10Arrow{InOut}3h'+i).html(' ') );  }} " );
                }

                if ( WantToSee24hr )
                {
                    of.AppendLine( $"  var PM2p5_{InOut}Val24h = AirLinkRT[6+{j * Offset}];" );
                    of.AppendLine( $"  var PM10_{InOut}Val24h = AirLinkRT[11+{j * Offset}];" );
                    of.AppendLine( $"  var PM2p5_{InOut}AQI24h = AirLinkRT[16+{j * Offset}];" );
                    of.AppendLine( $"  var PM2p5_{InOut}AQIint24h = NormaliseAQI(PM2p5_{InOut}AQI24h);" );  // parseInt(PM2p5_{InOut}AQI24h, 10) | 0;
                    of.AppendLine( $"  var PM10_{InOut}AQI24h = AirLinkRT[21+{j * Offset}];" );
                    of.AppendLine( $"  var PM10_{InOut}AQIint24h = NormaliseAQI(PM10_{InOut}AQI24h);" );  // parseInt(PM10_{InOut}AQI24h, 10) | 0;
                    of.AppendLine( $"  $('#PM2p5_{InOut}Val24h').html(PM2p5_{InOut}Val24h);" );
                    of.AppendLine( $"  $('#PM2p5_{InOut}AQI24h').html(PM2p5_{InOut}AQI24h);" );
                    of.AppendLine( $"  $('#PM10_{InOut}Val24h').html(PM10_{InOut}Val24h);" );
                    of.AppendLine( $"  $('#PM10_{InOut}AQI24h').html(PM10_{InOut}AQI24h);" );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == PM2p5_{InOut}AQIint24h ? $('#PM2p5Arrow{InOut}24h'+i).html('▲') : $('#PM2p5Arrow{InOut}24h'+i).html(' ') );  }} " );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == PM10_{InOut}AQIint24h ? $('#PM10Arrow{InOut}24h'+i).html('▲') : $('#PM10Arrow{InOut}24h'+i).html(' ') );  }} " );
                }

                if ( WantToSeeNowCast )
                {
                    of.AppendLine( $"  var nowcastPM2p5_{InOut}Val = AirLinkRT[7+{j * Offset}];" );
                    of.AppendLine( $"  var nowcastPM10_{InOut}Val = AirLinkRT[12+{j * Offset}];" );
                    of.AppendLine( $"  var nowcastPM2p5_{InOut}AQI = AirLinkRT[17+{j * Offset}];" );
                    of.AppendLine( $"  var nowcastPM2p5_{InOut}AQIint = NormaliseAQI(nowcastPM2p5_{InOut}AQI);" );  // parseInt(nowcastPM2p5_{InOut}AQI, 10) | 0;
                    of.AppendLine( $"  var nowcastPM10_{InOut}AQI = AirLinkRT[22+{j * Offset}];" );
                    of.AppendLine( $"  var nowcastPM10_{InOut}AQIint = NormaliseAQI(nowcastPM10_{InOut}AQI);" );  // parseInt(nowcastPM10_{InOut}AQI, 10) | 0;
                    of.AppendLine( $"  $('#nowcastPM2p5_{InOut}Val').html(nowcastPM2p5_{InOut}Val);" );
                    of.AppendLine( $"  $('#nowcastPM2p5_{InOut}AQI').html(nowcastPM2p5_{InOut}AQI);" );
                    of.AppendLine( $"  $('#nowcastPM10_{InOut}Val').html(nowcastPM10_{InOut}Val);" );
                    of.AppendLine( $"  $('#nowcastPM10_{InOut}AQI').html(nowcastPM10_{InOut}AQI);" );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == nowcastPM2p5_{InOut}AQIint ? $('#nowcastPM2p5Arrow{InOut}'+i).html('▲') : $('#nowcastPM2p5Arrow{InOut}'+i).html(' ') );  }} " );
                    of.AppendLine( $"  for (i = 0; i < {NrOfClassesInCountry}; i++) {{ (i+1 == nowcastPM10_{InOut}AQIint ? $('#nowcastPM10Arrow{InOut}'+i).html('▲') : $('#nowcastPM10Arrow{InOut}'+i).html(' ') );  }} " );
                }

                // "<#AirLinkPct_1hrIn> <#AirLinkPct_3hrIn> <#AirLinkPct_24hrIn> <#AirLink_NowcastIn> " +
                of.AppendLine( $"  $('#ajxPct_1hr{InOut}').html(AirLinkRT[23+{j * Offset}] + '%');" );
                of.AppendLine( $"  $('#ajxPct_3hr{InOut}').html(AirLinkRT[24+{j * Offset}] + '%');" );
                of.AppendLine( $"  $('#ajxPct_24hr{InOut}').html(AirLinkRT[25+{j * Offset}] + '%');" );
                of.AppendLine( $"  $('#ajxPct_Nowcast{InOut}').html(AirLinkRT[26+{j * Offset}] + '%');" );
            } // For any of the sensors (max 2)

            of.AppendLine( "};" ); // End DoAirLinkRT

            of.AppendLine( "function NormaliseAQI(rawAQI) {" );
            switch ( CumulusCountrySetting )
            {
                // https://aqicn.org/faq/2014-09-06/australian-air-quality-comparison-with-the-us-epa-aqi-scale/
                // Note that this normalisation may be equal, the calcs from conc to AQI are definitely not.
                case CumulusCountries.US:
                    of.AppendLine( "  if (rawAQI <= 50) return 1;" );
                    of.AppendLine( "  if (rawAQI <= 100) return 2;" );
                    of.AppendLine( "  if (rawAQI <= 150) return 3;" );
                    of.AppendLine( "  if (rawAQI <= 200) return 4;" );
                    of.AppendLine( "  if (rawAQI <= 300) return 5;" );
                    of.AppendLine( "  return 6;" );
                    break;

                case CumulusCountries.AU:
                    of.AppendLine( "  if (rawAQI <= 33) return 1;" );
                    of.AppendLine( "  if (rawAQI <= 66) return 2;" );
                    of.AppendLine( "  if (rawAQI <= 99) return 3;" );
                    of.AppendLine( "  if (rawAQI <= 149) return 4;" );
                    of.AppendLine( "  if (rawAQI <= 200) return 5;" );
                    of.AppendLine( "  return 6;" );
                    break;

                case CumulusCountries.EUCAQI:
                    of.AppendLine( "  if (rawAQI <= 25) return 1;" );
                    of.AppendLine( "  if (rawAQI <= 50) return 2;" );
                    of.AppendLine( "  if (rawAQI <= 75) return 3;" );
                    of.AppendLine( "  if (rawAQI <= 100) return 4;" );
                    of.AppendLine( "  return 5;" );
                    break;

                case CumulusCountries.CA:
                case CumulusCountries.NL:
                case CumulusCountries.BE:
                case CumulusCountries.UK:
                case CumulusCountries.EUAQI:
                    of.AppendLine( "  return parseInt(rawAQI, 10) | 0;" );
                    break;

                default:
                    // Can't happen
                    of.AppendLine( "  console.log('Normalise AQI: Internal error - can't happen situation');" );
                    break;
            }
            of.AppendLine( "};" );  // End normalise function

            // Prepare the functions for the buttons in the stringbuilder, they will be written out after 
            // the generation of the Highcharts functions
            //
            sb.Clear();
            sb.Append( "function SetTableView() {" );
            sb.Append( "  $('[id*=\"AIRQ\"]').hide();" );
            sb.Append( "  $('#AIRQTableView').show();" );
            sb.Append( "  " +
                "urlParams.delete('dropdown');" +
                "urlParams.set('dropdown', 'TableView');" +
                "history.pushState(null, null, window.location.origin + window.location.pathname + '?' + urlParams);" +
                "" );
            sb.Append( "};" );

            // Generate the graphing functions
            //

            // For the windbarbs we need the conversion to m/s. For this we need a special data conversion
            // which is done here in Javascript and not at compile time since we use the JSON files and 
            // the JSON conversion I think is more awkward and therefore performance loss. This seems nicer.
            if ( WantToSeeWind )
            {
                of.AppendLine( "function convertToMs(data){data.map(s => {" +
                    $"s[1] = s[1] * {Sup.StationWind.Convert( Sup.StationWind.Dim, WindDim.ms, 1 ).ToString( "F5", CUtils.Inv )} " +
                    "}); return data};" );

                of.AppendLine( "var convertedWindbarbData;" );  // Make this a module global i.s.o. within the ajax function for possible debugging
            }


            // Now generate the 
            for ( int j = 0; j < NrOfSensors; j++ ) // NOTE: For the algorithm j==0 => IN and j==1 => Out; see relevance for the realtime value array
            {
                string InOut = DetermineSensor( j, TwoSensors );

                sb.Append( $"function SetGraphView{j}() {{" );
                sb.Append( "  $('[id*=\"AIRQ\"]').hide();" );
                sb.Append( $"  $('#AIRQGraphView{j}').show();" );

                foreach ( string thisConc in Concentrations )
                {
                    StringBuilder tmpBuilder = new StringBuilder();

                    sb.Append( $"  doSensor{InOut}{thisConc}();" );

                    // The graphing functions
                    of.AppendLine( $"var doSensor{InOut}{thisConc} = function(){{" );
                    of.AppendLine( "  let ReferenceColours = ['#79bc6a', '#bbcf4c', '#eec20b', '#f29305', '#960018' ];" );

                    tmpBuilder.Clear();
                    tmpBuilder.Append( $"  let ReferenceConcentrations{thisConc} = [" );
                    if ( thisConc == "2p5" )
                        for ( int i = 0; i < ReferenceNrOfClasses; i++ )
                            tmpBuilder.Append( $"{ReferenceConcentrations2p5[ i ]}," );
                    else
                        for ( int i = 0; i < ReferenceNrOfClasses; i++ )
                            tmpBuilder.Append( $"{ReferenceConcentrations10[ i ]}," );
                    tmpBuilder.Remove( tmpBuilder.Length - 1, 1 );
                    of.AppendLine( tmpBuilder.ToString() + "];" );

                    of.AppendLine( $"  let t ={{chart: {{ renderTo: 'chartcontainer{InOut}{thisConc}',type: 'spline',alignTicks: true, zoomType: 'xy', pinchType: 'xy'}}," );
                    of.AppendLine( $"  title: {{ text: '{Sup.GetCUstringValue( "AirQuality", "AirQuality", "Air Quality", true )} " +
                                 $"{Sup.GetCUstringValue( "AirQuality", "Sensor", "Sensor", true )} {InOut} " +
                                 $"{Sup.GetCUstringValue( "AirQuality", "for", "for", true )} PM{thisConc}', margin: 35}}," );
                    of.AppendLine( "  credits: { enabled: true}," );
                    of.AppendLine( "  xAxis: [{ type: 'datetime', ordinal: false, dateTimeLabelFormats: { day: '%e %b',week: '%e %b %y',month: '%b %y',year: '%Y'} }," );
                    of.AppendLine( "          {linkedTo: 0, opposite: true, labels: {enabled: false}, gridLineWidth:0  }]," );  // required to get the barbs on top
                    of.AppendLine( "  yAxis:" );
                    of.AppendLine( "    [{" );
                    of.AppendLine( $"      title: {{ text: '{Sup.GetCUstringValue( "AirQuality", "AirQuality", "Air Quality", true )} (μg/m3)'}}," );
                    of.AppendLine( "      opposite: false, labels: { align: 'right',x: -5}," );
                    of.AppendLine( "    }," );
                    of.AppendLine( "    {" );
                    of.AppendLine( "      linkedTo: 0," );
                    of.AppendLine( "      gridLineWidth: 0," );
                    of.AppendLine( "      opposite: true," );
                    of.AppendLine( "      title: { text: null}," );
                    of.AppendLine( "      labels: { align: 'left',x: 5}," );

                    // I will use hardcode EU-CAQI values here for the pm2p5 and the pm10 graphs
                    tmpBuilder.Clear();
                    tmpBuilder.Append( " plotLines: [{" );

                    for ( int i = 1; i < ReferenceNrOfClasses; i++ )  // Skip the lowest concentration line which is on zero: not useful
                    {
                        tmpBuilder.AppendLine( $" value: ReferenceConcentrations{thisConc}[{i}]," );
                        tmpBuilder.AppendLine( $" color: ReferenceColours[{i}]," );
                        tmpBuilder.AppendLine( " zIndex: 0," );
                        tmpBuilder.AppendLine( $" label: {{ text:'{Sup.GetCUstringValue( "AirQuality", "RefLine", "Reference line", true )} '+ ReferenceConcentrations{thisConc}[{i}] + ' μg/m3', y:-10}}," );
                        tmpBuilder.AppendLine( $" width: {Sup.GetUtilsIniValue( "AirLink", "ReferenceLineThickness", "4" )}" );
                        tmpBuilder.Append( "},{" );
                    }

                    tmpBuilder.Remove( tmpBuilder.Length - 2, 2 );

                    of.AppendLine( tmpBuilder.ToString() + "]," );
                    of.AppendLine( "      tickInterval: 20" );
                    of.AppendLine( "    }]," );


                    of.AppendLine( "  legend: { enabled: true}," );
                    of.AppendLine( "  plotOptions: {series: {turboThreshold: 0}}," );
                    of.AppendLine( "  tooltip: { valueSuffix: ' μg/m3',valueDecimals: 1,xDateFormat: '%A, %b %e, %H:%M'}," );
                    of.AppendLine( "  series:[]," );
                    of.AppendLine( "  rangeSelector:" );
                    of.AppendLine( "  { floating: true, y: -50," );
                    of.AppendLine( "    buttons:[{ count: 6,type: 'hour',text: '6h'}," );
                    of.AppendLine( "             { count: 12,type: 'hour',text: '12h'}," );
                    of.AppendLine( "             { type: 'all',text: 'All'}]," );
                    of.AppendLine( "    inputEnabled: false" );
                    of.AppendLine( "  }" );
                    of.AppendLine( "};" );
                    of.AppendLine( "let chart = new Highcharts.stockChart(t);" );
                    of.AppendLine( "chart.showLoading();" );
                    of.AppendLine( "$.ajax({" );
                    of.AppendLine( $"  url: 'airlinkdata{InOut}{thisConc}.json', " ); // Note this is always the CU directory, never the CumulusRealTimeLocation
                    of.AppendLine( "  cache: false," );
                    of.AppendLine( "  dataType: 'json'" );
                    of.AppendLine( "  })" );
                    of.AppendLine( ".fail( function (xhr, textStatus, errorThrown) { console.log('airlinkdata[InOut][Conc].json ' + textStatus + ' : ' + errorThrown); })" );
                    of.AppendLine( ".done( function(resp){" );

                    tmpBuilder.Clear();
                    tmpBuilder.Append( "    let titles = {" );
                    foreach ( string thisSerie in Series )
                        tmpBuilder.Append( $"'{InOut}_pm{thisConc}{thisSerie}': '{InOut}_pm{thisConc}{thisSerie}'," );
                    if ( WantToSeeWind )
                        tmpBuilder.Append( $"'wind': '{Sup.GetCUstringValue( "AirQuality", "WindBarbs", "WindBarbs", true )}'," );
                    tmpBuilder.Remove( tmpBuilder.Length - 1, 1 );
                    of.AppendLine( tmpBuilder.ToString() + "};" );

                    tmpBuilder.Clear();
                    tmpBuilder.Append( "    let idxs = [" );
                    foreach ( string thisSerie in Series )
                        tmpBuilder.Append( $"'{InOut}_pm{thisConc}{thisSerie}'," );
                    if ( WantToSeeWind )
                        tmpBuilder.Append( $"'wind'," );
                    tmpBuilder.Remove( tmpBuilder.Length - 1, 1 );

                    of.Append( tmpBuilder.ToString() + "];" );
                    of.AppendLine( "    idxs.forEach(function(idx) {" );
                    of.AppendLine( "      if (idx in resp) {" );

                    if ( WantToSeeWind )
                    {
                        of.AppendLine( "        if (idx == 'wind') {" );
                        of.AppendLine( "           let convertedWindbarbData = convertToMs(resp[idx]);" );
                        of.AppendLine( $"          chart.addSeries({{name: titles[idx], xAxis: 1, color: 'black', type: 'windbarb', visible: true, " +
                            $"dataGrouping: {{enabled: true,units: [ ['hour', [{Sup.HighChartsWindBarbSpacing()}] ] ]}}, " );
                        of.AppendLine( "    tooltip: {pointFormatter() {return this.series.name + ': ' + " +
                            $"(this.value/{Sup.StationWind.Convert( Sup.StationWind.Dim, WindDim.ms, 1 ).ToString( "F5", CUtils.Inv )}).toFixed(1) + ' {Sup.StationWind.Text()}'}} }}," );
                        of.AppendLine( $"data: convertedWindbarbData }}, false);" );
                        of.AppendLine( "        }" );
                        of.AppendLine( "        else {" );
                    }

                    of.AppendLine( "          chart.addSeries({name: titles[idx], id: titles[idx], data: resp[idx]}, false);" );

                    if ( WantToSeeWind )
                        of.AppendLine( "        };" );

                    of.AppendLine( "    chart.hideLoading();" );
                    of.AppendLine( "    chart.redraw();" );
                    of.AppendLine( "      };" );  // End if (idx in resp)
                    of.AppendLine( "    });" ); // End of idxForeach
                    of.AppendLine( "  });" ); // End of .done / $.ajax handling
                    of.AppendLine( "};" ); // End of DoSensorXXYY function
                } // loop over concentrations

                sb.AppendLine( $"  " +
                    "urlParams.delete('dropdown');" +
                    $"urlParams.set('dropdown', 'GraphView{j}');" +
                    "history.pushState(null, null, window.location.origin + window.location.pathname + '?' + urlParams);" +
                    "" );

                sb.AppendLine( "};" );
            }

            of.AppendLine( sb.ToString() );

            of.AppendLine( "</script>" );

            #endregion

            #region Style

            of.AppendLine( "<style>" );
            of.AppendLine( "#report{" );
            of.AppendLine( "  font-family: arial;" );
            of.AppendLine( "  border-radius: 15px;" );
            of.AppendLine( "  border-spacing: 0;" );
            of.AppendLine( "  border: 1px solid #b0b0b0;" );
            of.AppendLine( "}" );

            for ( int j = 0; j < NrOfSensors; j++ )
            {
                string InOut = DetermineSensor( j, TwoSensors );

                foreach ( string thisConc in Concentrations )
                    of.AppendLine( $"#chartcontainer{InOut}{thisConc} " +
                            $"{{min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;}}" );

                foreach ( string thisConc in Concentrations )
                    // The optional parameters one and two accommodate the AirLink module which charts the In/Out (one) and pm2p5/pm10 (two) combinations
                    // All other calls to HighchartsAllowBackgroundImage will be without parameters.
                    of.AppendLine( Sup.HighchartsAllowBackgroundImage( InOut, thisConc ) );

            }

            of.AppendLine( ".buttonSlim {border-radius: 4px;}" );
            of.AppendLine( "</style>" );

            #endregion

            #region HTML

            // Create the page header titles, station  identification etc...
            if ( !Message.IsBlank() ) of.AppendLine( Message );

            of.AppendLine( "<div style='float:right;'>" );
            of.AppendLine( "<input type='button' class=buttonSlim id='TableViewBtn' value='TableView' onclick='SetTableView()'>" );

            for ( int j = 0; j < NrOfSensors; j++ )
            {
                string InOut = DetermineSensor( j, TwoSensors );
                of.AppendLine( $"<input type='button' class=buttonSlim value='{Sup.GetCUstringValue( "AirQuality", "GraphView", "Graph View", false )} {InOut}' onclick='SetGraphView{j}()'>" );
            }

            if ( !StandAloneModule )
            {
                of.AppendLine( $"<input type='submit' class=buttonSlim value='{Sup.GetCUstringValue( "AirQuality", "Help", "Help", false )}' class='nav-link' data-bs-toggle='modal' data-bs-target='#Help'>" );
            }
            of.AppendLine( "</div>" );

            of.AppendLine( $"<div><b>{Sup.GetCUstringValue( "AirQuality", "AirQuality", "Air Quality", false )} / {Sup.GetCUstringValue( "AirQuality", "NormativeCountry", "Normative country selected", true )}: {CountrySelected}</b>" +
                         $"&nbsp;-&nbsp;<b>{Sup.GetCumulusIniValue( "Station", "LocName", "" )}</b></div><br/>" );

            if ( /* CUtils.DoWebsite && */ !StandAloneModule )
            {
                Sup.LogDebugMessage( "DoAirLinkModule - Writing the Help Modal" );

                // The Help info
                of.AppendLine( "<div class='modal fade' id='Help' tabindex='-1' role='dialog' aria-hidden='true'>" );
                of.AppendLine( "<div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" );
                of.AppendLine( "<div class='modal-content'>" );
                of.AppendLine( "<div class='modal-header'>" );
                of.AppendLine( "<h5 class='modal-title'>Help Info on Air Quality reporting</h5>" );
                of.AppendLine( "<button type = 'button' class='close' data-bs-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" );
                of.AppendLine( "</div>" );
                of.AppendLine( "<div class='modal-body' style='text-align:left;'>" );
                of.AppendLine( @"In Air Quality (with either the cheap sensors or the government published data) it is important to know about the Air Quality Indices (AQI) and PM concentrations. " +
                  "Those are very different things. The AQI differ very much per country, per period (1 hr, 3 hr or 24 hr averages) and in scales (6 divisions or 10 divisions with subdivisions). " +
                  "To make it even more complex, some countries did combine the fine dust (PM) AQIs with indices for SO2 and NO2. That is ignored here. These graphs only deal with " +
                  "Particulate Matter sensors. <b>Please be aware that the scaling of the graphs is dynamic.</b><br/><br/>" +
                  "I made <a href='https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=18602&sid=c2ee8eb5efa767bdc24781e940817eda' target='_blank'>a post on the forum</a> in which I will add " +
                  "some links to the sites for specific countries. You will have to study yourself (if you want). <br/><br/>" +
                  "Please be aware that these charts only give the abolute measurements of the PM concentrations as given by the sensors used. The AQI  levels for the different countries have great variations " +
                  "and I will not go through the trouble to display those levels in the graphs for the normative country selected. <br/><br/>" +
                  "The graphs show the <a  href='https://www.researchgate.net/publication/269030465_Wwwairqualitynoweu_a_common_website_and_air_quality_indices_to_compare_cities_across_Europe' target='_blank'>indicative AQI levels of the EU-CAQI standards (researchgate paper)</a> " +
                  "(<a href='https://uk-air.defra.gov.uk/assets/documents/reports/cat12/0705231407_070516_CITEAIR-NH-Overview-v1.pdf' target='_blank'>UK explication</a>) for the 1 hr series.<br/><br/>" +
                  "These will show you reasonable short term danger levels although maybe not standardized in your country. 24 hr levels are considered irrelevant in the context of a personal station: you will be dead before this level reaches a level where help " +
                  "services are activated. If the 1 hr level shows an unhealthy level you better start running away (precautionary principle).<br><br/>" +
                  "Yes, big differences in different countries. Therefore: study the documents and the danger levels in the matrix I supplied." );
                of.AppendLine( "</a>" );
                of.AppendLine( "</div>" );
                of.AppendLine( "<div class='modal-footer'>" );
                of.AppendLine( $"<button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{Sup.GetCUstringValue( "Website", "Close", "Close", false )}</button>" );
                of.AppendLine( "</div>" );
                of.AppendLine( "</div>" );
                of.AppendLine( "</div>" );
                of.AppendLine( "</div>" );
            }

            // III
            // Now do the table generation
            //

            #region AIRQTableView

            of.AppendLine( "<div id='AIRQTableView'>" );
            for ( int j = 0; j < NrOfSensors; j++ ) // NOTE: For the algorithm j==0 => IN and j==1 => Out; see relevance for the realtime value array
            {
                string InOut = DetermineSensor( j, TwoSensors );

                of.AppendLine( "<div id=report>" );

                int thisNrOfClasses = NrOfClassesInCountry + 4;
                int ColumnWidth = 100 / thisNrOfClasses;

                of.AppendLine( "<table style='width:100%;margin:auto;text-align:center;'>" );
                of.AppendLine( $"<thead>" );
                of.AppendLine( $"<tr><td colspan='{NrOfClassesInCountry + 4}' style='padding:20px'>" +
                  $"<b><span>{Sup.GetCUstringValue( "AirQuality", "Sensor", "Sensor", false )} {InOut}: {Sup.GetCUstringValue( "AirQuality", "Temperature", "Temperature", false )}: </span><span id='ajxTemp{InOut}'></span> / " +
                  $"<span>{Sup.GetCUstringValue( "AirQuality", "Sensor", "Sensor", false )} {Sup.GetCUstringValue( "AirQuality", "Humidity", "Humidity", false )}: </span><span id='ajxHum{InOut}'></span></b></td></tr>" );

                of.AppendLine( "<tr style='border-bottom:solid;border-top:solid;'>" );
                of.AppendLine( $"<th style='width:{ColumnWidth}%'>{Sup.GetCUstringValue( "AirQuality", "Period", "Period", false )}</th>" );
                of.AppendLine( $"<th style='width:{ColumnWidth}%'>PM</th>" );
                of.AppendLine( $"<th style='width:{ColumnWidth}%'>raw AQI</th>" );
                of.AppendLine( $"<th style='border-right:solid;width:{ColumnWidth}%'>Conc.<br/>(μg/m3)</th>" );
                for ( int i = 0; i < NrOfClassesInCountry; i++ )
                    of.AppendLine( $"<th style='width:{ColumnWidth}%;background-color:{Colours[ i ]}'>{Description[ i ]}</th>" );
                of.AppendLine( "</tr>" );
                of.AppendLine( "</thead>" );

                if ( WantToSeeNow )
                {
                    of.AppendLine( "<tr>" );
                    of.AppendLine( $"<td>{Sup.GetCUstringValue( "AirQuality", "now", "now", false )}</td>" );
                    of.AppendLine( "<td>PM2.5</td>" );
                    of.AppendLine( $"<td id='nowPM2p5_{InOut}AQI'></td>" );
                    of.AppendLine( $"<td id='nowPM2p5_{InOut}Val' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='nowPM2p5Arrow{InOut}{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );

                    of.AppendLine( "<tr style='border-bottom:solid'>" );
                    of.AppendLine( $"<td>{Sup.GetCUstringValue( "AirQuality", "now", "now", false )}</td>" );
                    of.AppendLine( "<td>PM10</td>" );
                    of.AppendLine( $"<td id='nowPM10_{InOut}AQI'></td>" );
                    of.AppendLine( $"<td id='nowPM10_{InOut}Val' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='nowPM10Arrow{InOut}{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );
                }

                if ( WantToSee1hr )
                {
                    of.AppendLine( "<tr>" );
                    of.AppendLine( "<td>1 hr</td>" );
                    of.AppendLine( "<td>PM2.5</td>" );
                    of.AppendLine( $"<td id='PM2p5_{InOut}AQI1h'></td>" );
                    of.AppendLine( $"<td id='PM2p5_{InOut}Val1h' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='PM2p5Arrow{InOut}1h{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );

                    of.AppendLine( "<tr style='border-bottom:solid'>" );
                    of.AppendLine( "<td>1 hr</td>" );
                    of.AppendLine( "<td>PM10</td>" );
                    of.AppendLine( $"<td id='PM10_{InOut}AQI1h'></td>" );
                    of.AppendLine( $"<td id='PM10_{InOut}Val1h' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='PM10Arrow{InOut}1h{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );
                }

                if ( WantToSee3hr )
                {
                    of.AppendLine( "<tr>" );
                    of.AppendLine( "<td>3 hr</td>" );
                    of.AppendLine( "<td>PM2.5</td>" );
                    of.AppendLine( $"<td id='PM2p5_{InOut}AQI3h'></td>" );
                    of.AppendLine( $"<td id='PM2p5_{InOut}Val3h' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='PM2p5Arrow{InOut}3h{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );

                    of.AppendLine( "<tr style='border-bottom:solid'>" );
                    of.AppendLine( "<td>3 hr</td>" );
                    of.AppendLine( "<td>PM10</td>" );
                    of.AppendLine( $"<td id='PM10_{InOut}AQI3h'></td>" );
                    of.AppendLine( $"<td id='PM10_{InOut}Val3h' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='PM10Arrow{InOut}3h{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );
                }

                if ( WantToSee24hr )
                {
                    of.AppendLine( "<tr>" );
                    of.AppendLine( "<td>24 hr</td>" );
                    of.AppendLine( "<td>PM2.5</td>" );
                    of.AppendLine( $"<td id='PM2p5_{InOut}AQI24h'></td>" );
                    of.AppendLine( $"<td id='PM2p5_{InOut}Val24h' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='PM2p5Arrow{InOut}24h{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );

                    of.AppendLine( "<tr style='border-bottom:solid'>" );
                    of.AppendLine( "<td>24 hr</td>" );
                    of.AppendLine( "<td>PM10</td>" );
                    of.AppendLine( $"<td id='PM10_{InOut}AQI24h'></td>" );
                    of.AppendLine( $"<td id='PM10_{InOut}Val24h' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='PM10Arrow{InOut}24h{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );
                }

                if ( WantToSeeNowCast )
                {
                    of.AppendLine( "<tr>" );
                    of.AppendLine( "<td>nowcast</td>" );
                    of.AppendLine( "<td>PM2.5</td>" );
                    of.AppendLine( $"<td id='nowcastPM2p5_{InOut}AQI'></td>" );
                    of.AppendLine( $"<td id='nowcastPM2p5_{InOut}Val' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='nowcastPM2p5Arrow{InOut}{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );

                    of.AppendLine( "<tr style='border-bottom:solid'>" );
                    of.AppendLine( "<td>nowcast</td>" );
                    of.AppendLine( "<td>PM10</td>" );
                    of.AppendLine( $"<td id='nowcastPM10_{InOut}AQI'></td>" );
                    of.AppendLine( $"<td id='nowcastPM10_{InOut}Val' style='border-right:solid'></td>" );
                    for ( int i = 0; i < NrOfClassesInCountry; i++ )
                        of.AppendLine( $"<td id='nowcastPM10Arrow{InOut}{i}' style='width:{ColumnWidth}%;'></td>" );
                    of.AppendLine( "</tr>" );
                }

                of.AppendLine( "<tbody>" );
                of.AppendLine( "</table>" );

                of.AppendLine( $"<p>1 {Sup.GetCUstringValue( "AirQuality", "hour", "hour", false )} {Sup.GetCUstringValue( "AirQuality", "FillingGrade", "filling grade", false )}: <b><span id='ajxPct_1hr{InOut}'></span></b>;" +
                             $"&nbsp; 3 {Sup.GetCUstringValue( "AirQuality", "hour", "hour", false )} {Sup.GetCUstringValue( "AirQuality", "FillingGrade", "filling grade", false )}: <b><span id='ajxPct_3hr{InOut}'></span></b>;<br/> " +
                             $"24 {Sup.GetCUstringValue( "AirQuality", "hour", "hour", false )} {Sup.GetCUstringValue( "AirQuality", "FillingGrade", "filling grade", false )}: <b><span id='ajxPct_24hr{InOut}'></span></b>;" +
                             $"&nbsp; Nowcast {Sup.GetCUstringValue( "AirQuality", "FillingGrade", "filling grade", false )}: <b><span id='ajxPct_Nowcast{InOut}'></span></b>;</p>" );

                of.AppendLine( "</div><br/>" ); // from div report
            }

            if ( !CUtils.DoWebsite )
            {
                of.AppendLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
            }

            of.AppendLine( "</div>" );

            #endregion

            #region AIRQGraphView

            for ( int j = 0; j < NrOfSensors; j++ )
            {
                string InOut = DetermineSensor( j, TwoSensors );

                of.AppendLine( $"<div id='AIRQGraphView{j}'>" );
                of.AppendLine( "<div id=report><br/>" );

                foreach ( string thisConc in Concentrations )
                    of.AppendLine( $"<div id='chartcontainer{InOut}{thisConc}'></div>" );

                of.AppendLine( "<br/></div>" ); // from div report
                of.AppendLine( "</div>" );
            }

            #endregion

            #endregion // HTML

            // Here we are at the end of the generation, now time to write evereything to the output file

            if ( StandAloneModule )
                using ( StreamWriter thisFile = new StreamWriter( $"{Sup.PathUtils}{Sup.AirLinkStandaloneOutputFilename}", false, Encoding.UTF8 ) )
                {
                    thisFile.WriteLine( of );
                } // End Using the AirLink module
            else
                using ( StreamWriter thisFile = new StreamWriter( $"{Sup.PathUtils}{Sup.AirLinkOutputFilename}", false, Encoding.UTF8 ) )
                {
#if !RELEASE
                    thisFile.WriteLine( of );
#else
                    thisFile.WriteLine( CuSupport.StringRemoveWhiteSpace( of.ToString(), " " ) );
#endif
                } // End Using the AirLink module


            Sup.LogTraceInfoMessage( "DoAirLinkModule - End" );
        } // GenAirLinkModule

        #endregion

        #region GenAirLinkDataJson

        public async Task GenAirLinkDataJson()
        {
            // Purpose is to create the JSON for the Airlink data and offering the poissibility to do only that to accomodate the fact that
            // CMX does not (and probably will never) generate that JSON like it generates the temperature JSON for graphing.
            // CumulusUtils will only generate the AirLink JSON by issueing the command: "./utils/bin/cumulusutils.exe UserAskedData"
            // When we are generating the module, the JSON is automatically generated and uploaded (in the main loop) as well
            // So here we go: it has already been determined that an Airlink is present and that we need the data

            List<AirlinklogValue> thisList;
            string VariableName;
            PropertyInfo Field;

            Airlinklog All = new Airlinklog( Sup );
            thisList = All.ReadAirlinklog();

            int NrOfGraphs = TwoSensors ? 2 : 1;

            for ( int j = 0; j < NrOfGraphs; j++ )
            {
                string InOut = DetermineSensor( j, TwoSensors );

                StringBuilder sb = new StringBuilder();

                foreach ( string thisConc in Concentrations )  // Loop over PM2p5 and PM10  - bothe in their own JSON file
                {
                    sb.Clear();
                    sb.Append( '{' );

                    if ( thisList.Count != 0 )
                    {
                        if ( WantToSeeNow )
                        {
                            VariableName = $"{InOut}_pm{thisConc}{Series[ 0 ]}";
                            sb.Append( $"\"{VariableName}\":[" );

                            Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                            foreach ( AirlinklogValue value in thisList )
                            {
                                double d = (double) Field.GetValue( value );
                                sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", CUtils.Inv )}]," );
                            }

                            sb.Remove( sb.Length - 1, 1 );
                            sb.Append( $"]," );
                        } // WantToSeeNow

                        if ( WantToSee1hr )
                        {
                            VariableName = $"{InOut}_pm{thisConc}{Series[ 1 ]}";
                            sb.Append( $"\"{VariableName}\":[" );

                            Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                            foreach ( AirlinklogValue value in thisList )
                            {
                                double d = (double) Field.GetValue( value );
                                sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", CUtils.Inv )}]," );
                            }

                            sb.Remove( sb.Length - 1, 1 );
                            sb.Append( $"]," );
                        } // WantToSee1hr

                        if ( WantToSee3hr )
                        {
                            VariableName = $"{InOut}_pm{thisConc}{Series[ 2 ]}";
                            sb.Append( $"\"{VariableName}\":[" );

                            Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                            foreach ( AirlinklogValue value in thisList )
                            {
                                double d = (double) Field.GetValue( value );
                                sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", CUtils.Inv )}]," );
                            }

                            sb.Remove( sb.Length - 1, 1 );
                            sb.Append( $"]," );
                        } // WantToSee3hr

                        if ( WantToSee24hr )
                        {
                            VariableName = $"{InOut}_pm{thisConc}{Series[ 3 ]}";
                            sb.Append( $"\"{VariableName}\":[" );

                            Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                            foreach ( AirlinklogValue value in thisList )
                            {
                                double d = (double) Field.GetValue( value );
                                sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", CUtils.Inv )}]," );
                            }

                            sb.Remove( sb.Length - 1, 1 );
                            sb.Append( $"]," );
                        } // WantToSee24hr

                        if ( WantToSeeNowCast )
                        {
                            VariableName = $"{InOut}_pm{thisConc}{Series[ 4 ]}";
                            sb.Append( $"\"{VariableName}\":[" );

                            Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                            foreach ( AirlinklogValue value in thisList )
                            {
                                double d = (double) Field.GetValue( value );
                                sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", CUtils.Inv )}]," );
                            }

                            sb.Remove( sb.Length - 1, 1 );
                            sb.Append( $"]," );
                        } // WantToSeeNowCast

                        if ( WantToSeeWind )
                        {
                            string JSONstringWind, JSONstringWindDir, wspeedArray, wdirArray, thisWindSubstr, thisWindDirSubstr;
                            int startWind, startWindDir;

                            DateTime StartDateTime = DateTime.ParseExact( Sup.GetUtilsIniValue( "General", "LastUploadTime", "" ), "dd/MM/yy HH:mm", CUtils.Inv ).AddMinutes( 1 );

                            CmxIPC thisCmxIPC = new CmxIPC( CUtils.Sup, CUtils.Isup );

                            // Now, the wind JSONs should have the same startingtime as the AirLink data.

                            Sup.LogTraceInfoMessage( $"GenAirLinkJson - Doing Wind for {thisConc}." );

                            if ( CUtils.Isup.IsIncrementalAllowed() )
                            {
                                JSONstringWind = await thisCmxIPC.GetCMXGraphdataAsync( "winddata", StartDateTime );
                                JSONstringWindDir = await thisCmxIPC.GetCMXGraphdataAsync( "wdirdata", StartDateTime );
                            }
                            else
                            {
                                JSONstringWind = await thisCmxIPC.GetCMXGraphdataAsync( "winddata" );
                                JSONstringWindDir = await thisCmxIPC.GetCMXGraphdataAsync( "wdirdata" );
                            }

                            var ws = JsonObject.Parse( JSONstringWind );
                            wspeedArray = ws.Get<string>( "wspeed" );

                            var wd = JsonObject.Parse( JSONstringWindDir );
                            wdirArray = wd.Get<string>( "avgbearing" );

                            //Sup.LogTraceInfoMessage( $"StartDateTime = {StartDateTime}" );
                            //Sup.LogTraceInfoMessage( $"ws = {wspeedArray}" );
                            //Sup.LogTraceInfoMessage( $"wd = {wdirArray}" );

                            startWind = 1;        // Set at start of the first entry
                            startWindDir = 1;

                            sb.Append( $"\"wind\":[" );

                            int L1 = 0, L2 = 0, L3 = 0;

                            do
                            {
                                L1 = wspeedArray.IndexOf( ']', startWind ) - startWind;
                                if ( L1 <= 0 || L1 >= wspeedArray.Length ) break; // if not incremental we have to stop at some point 

                                L2 = wdirArray.IndexOf( ',', startWindDir );
                                L3 = wdirArray.IndexOf( ']', L2 );

                                thisWindSubstr = wspeedArray.Substring( startWind, L1 );
                                thisWindDirSubstr = wdirArray.Substring( L2, L3 - L2 + 1 );
                                startWind += thisWindSubstr.Length + 2;
                                startWindDir = L3 + 2;

                                sb.Append( $"{thisWindSubstr}{thisWindDirSubstr}," );
                            } while ( true );

                            sb.Remove( sb.Length - 1, 1 );
                            sb.Append( $"]," );
                        }

                        sb.Remove( sb.Length - 1, 1 );
                    } // WantToSeeWind

                    sb.Append( '}' );

                    using ( StreamWriter thisJSON = new StreamWriter( $"{Sup.PathUtils}{Sup.AirlinkJSONpart}{InOut}{thisConc}.json", false, Encoding.UTF8 ) )
                    {
                        thisJSON.WriteLine( sb.ToString() );
                    }
                } // for loop over 2p5 and 10 concentrations
            } // for loop over j for the number of graphs (In/Out)

            All.Dispose();

        } // GenAirLinkDataJason

        #endregion

        #region Divers
        private string DetermineSensor( int j, bool TwoSensors ) => ( j == 0 ) ? TwoSensors ? "In" : AirLinkIn ? "In" : "Out" : "Out";

        #endregion

    } // Class AirLink
} // Namespace CumulusUtils
