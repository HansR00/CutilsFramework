/*
 * Forecast - Part of CumulusUtils
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
using System.Xml.Linq;

namespace CumulusUtils
{
    public struct StructPrediction
    {
        public string Icon { get; set; }
        public float MinTemp { get; set; }
        public float MaxTemp { get; set; }
        public int WindIcon { get; set; }
        public string WindText { get; set; }
        public int WeatherIcon { get; set; }
        public string WeatherText { get; set; }
        public string Day { get; set; }
    }

    public class WeatherForecasts
    {
        private readonly CuSupport Sup;
        private readonly InetSupport Isup;

        readonly StructPrediction[] PredictionList = new StructPrediction[ 7 ];

        public WeatherForecasts( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;
        }

        public async Task GenerateForecasts()
        {
            string ForecastSystem;

            // Make sure the parameters exist in one run, otherwise the user has to run, define and run again
            ForecastSystem = Sup.GetUtilsIniValue( "Forecasts", "ForecastSystem", "Yourweather" );

            if ( ForecastSystem.Equals( "Yourweather", CUtils.Cmp ) )
            {
                string YourWeatherPredictionURL = Sup.GetUtilsIniValue( "Forecasts", "SevenDayPredictionURL", "" );

                // IF no PRediction URL set, then no prediction possible
                if ( string.IsNullOrEmpty( YourWeatherPredictionURL ) )
                {
                    Sup.LogTraceErrorMessage( "Prediction : No URL in Prediction ini section." );
                    Sup.LogTraceErrorMessage( "Prediction : Impossible to continue, exiting procedure." );
                    return;
                }

                string XMLresult = await Isup.GetUrlDataAsync( new Uri( YourWeatherPredictionURL ) );

                if ( CreateSevenDayPrediction( XMLresult ) )
                {
                    Sup.LogTraceInfoMessage( "Prediction : Read the predicition XML, now generating the  table." );

                    // OK, Generate the HTML table straightforward from the structs
                    using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ForecastOutputFilename}", false, Encoding.UTF8 ) )
                    {
                        of.WriteLine( "<style>" );
                        of.WriteLine( "#report{font-family: arial;text-align: center;border-radius: 15px;border-spacing: 0;border: 1px solid #b0b0b0;}" );
                        of.WriteLine( "#report .CUtable{border-radius: 15px; text-align: center; border-collapse: collapse; border-spacing: 0; width: 100%; max-width: 1000px; margin: auto;}" );
                        of.WriteLine( "#report th{text-align: left; border: 0px solid #b0b0b0; background-color: #d0d0d0; padding: 0px;}" );
                        of.WriteLine( "#report td{text-align: left; border: 0px solid #b0b0b0; background-color: #f0f0f0; padding: 0px;}" );
                        of.WriteLine( "</style>" );

                        of.WriteLine( "<div id='report'>" );

                        //string Title = Sup.GetUtilsIniValue( "Forecasts", "SevenDayPredictionURL", "" );
                        string Title = Sup.GetCUstringValue( "Forecasts", "Title", "Seven Day Prediction", false );
                        of.WriteLine( $"<h2>{Title}</h2>" );

                        of.WriteLine( "<table class=CUtable><tbody>" );
                        of.WriteLine( $"<tr style='border-bottom: 1px'><th>{Sup.GetCUstringValue( "Forecasts", "Day", "Day", false )}</th>" +
                          $"<th></th><th></th><th>{Sup.GetCUstringValue( "Forecasts", "Forecast", "Forecast", false )}</th>" +
                          $"<th></th><th>{Sup.GetCUstringValue( "Forecasts", "Wind", "Wind", false )}</th></tr>" );

                        for ( int i = 0; i < 7; i++ )
                        {
                            StructPrediction tmp = PredictionList[ i ];

                            of.WriteLine( $"<tr><td>{tmp.Day}</td>" +
                              $"<td text-align:center;><img src='CUicons/weather/{tmp.WeatherIcon}.png' alt='{tmp.WeatherIcon}.png'/></td>" +
                              $"<td style='text-align: center; font-size: 80%;padding: 0px 10px'><div style='color: red'>{tmp.MaxTemp}</div><div style='color: blue'>{tmp.MinTemp}</div></td>" +
                              $"<td>{tmp.WeatherText}</td>" +
                              $"<td><img src='CUicons/wind/{tmp.WindIcon}.png' alt='{tmp.WindIcon}.png'/></td>" +
                              $"<td>{tmp.WindText}</td></tr>" );
                        }
                        of.WriteLine( "</tbody></table>" );
                        of.WriteLine( "<p style='font-size: 80%;padding-top: 20px !important'>" +
                            $"{Sup.GetCUstringValue( "Forecasts", "Footer1", "Forecast API by", false )} " +
                            $"<a href='https://www.yourweather.co.uk/'>yourweather.co.uk</a>" +
                            $" {Sup.GetCUstringValue( "Forecasts", "Footer2", "on the basis of", false )} " +
                            $"<a href='https://www.ecmwf.int/'>ECMWF</a>.</div>" );
                    }
                }

                return;

            }
            else if ( ForecastSystem.Equals( "Norway", CUtils.Cmp ) )
            {
                // https://developer.yr.no/
                //
                string NorwayPredictionURL = Sup.GetUtilsIniValue( "Forecasts", "NorwegianURL", "" );

                // IF no PRediction URL set, then no prediction possible
                if ( string.IsNullOrEmpty( NorwayPredictionURL ) )
                {
                    Sup.LogTraceErrorMessage( "Prediction : No URL in Prediction ini section." );
                    Sup.LogTraceErrorMessage( "Prediction : Impossible to continue, exiting procedure." );
                    return;
                }

                // OK, Generate the HTML table straightforward from the structs
                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ForecastOutputFilename}", false, Encoding.UTF8 ) )
                {
                    // moved width='1000' height='650' to style
                    of.WriteLine( $"<iframe src='{NorwayPredictionURL}' frameborder='0' style='margin: 10px 0 10px 0; width:1000px; height:700px' scrolling='no'></iframe>" );
                }
                return;
            }
            else if ( ForecastSystem.Equals( "WXSIM", CUtils.Cmp ) )
            {
                string WxsimPredictionURL = Sup.GetUtilsIniValue( "Forecasts", "WXsimURL", "" );

                // IF no PRediction URL set, then no prediction possible
                if ( string.IsNullOrEmpty( WxsimPredictionURL ) )
                {
                    Sup.LogTraceErrorMessage( "Forecasts : No URL in Prediction ini section." );
                    Sup.LogTraceErrorMessage( "Prediction : Impossible to continue, exiting procedure." );
                    return;
                }

                // OK, Generate the HTML table straightforward from the structs
                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ForecastOutputFilename}", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<iframe src='{WxsimPredictionURL}' frameborder='0' style='border: 0;width:100%; height: 75vh;'></iframe>" );
                }
                return;
            }
            else if ( ForecastSystem.Equals( "SPOTWX", CUtils.Cmp ) )
            {
                string SpotwxPredictionURL;

                // Convert the string to float and then print as F4. This to prevent problems with the string representation in the ini
                // of the Lat / Lon : sometimes they are long with repetetive digits, sometime just 4 digits.
                //
                float Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );
                float Longitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Longitude", "" ), CUtils.Inv );

                string TzOffset = TimeZoneInfo.Local.GetUtcOffset( DateTime.Now ).ToString( "hh", CUtils.Inv );

                SpotwxPredictionURL = $"https://spotwx.com/products/grib_index.php?model=gem_glb_25km&" +
                  $"lat={Latitude.ToString( "F4", CUtils.Inv )}&" +
                  $"lon={Longitude.ToString( "F4", CUtils.Inv )}&" +
                  $"tz={TzOffset}";

                // OK, Generate the HTML table straightforward from the structs
                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ForecastOutputFilename}", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<iframe src='{SpotwxPredictionURL}' frameborder='0' style='border: 0; width:100%; height: 75vh;'></iframe>" );
                }
                return;
            }
            else
            {
                Sup.LogTraceErrorMessage( $"Prediction : Illegal Forecast system defined - {ForecastSystem}." );
                Sup.LogTraceErrorMessage( "Prediction : Impossible to continue, exiting procedure." );
                return;
            }
        }

        private bool CreateSevenDayPrediction( string XMLresult )
        {
            bool retval = false;

            if ( !String.IsNullOrEmpty( XMLresult ) )
            {
                XElement localWeather = XElement.Parse( XMLresult );

                IEnumerable<XElement> vars = from desc in localWeather.Descendants( "var" ) select desc;

                // Create the list of preditions, to be filled in the next loop

                foreach ( XElement thisvar in vars ) // loop over the items in  the prediction
                {
                    int sequence, FcastType;

                    Sup.LogTraceInfoMessage( $"Prediction The data: {thisvar}" );

                    // As the names are in the language chosen, we must do the order inn which they appear and trust
                    // that order will never change !!

                    IEnumerable<XElement> forecasts = from desc in thisvar.Descendants( "forecast" ) select desc;
                    FcastType = Convert.ToInt32( thisvar.Element( "icon" ).Value, CUtils.Inv );

                    switch ( FcastType )
                    {
                        case 4: // MinTemp
                            foreach ( XElement forecast in forecasts )
                            {
                                sequence = Convert.ToInt32( forecast.Attribute( "data_sequence" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].MinTemp = Convert.ToSingle( forecast.Attribute( "value" ).Value, CUtils.Inv );
                            }
                            break;

                        case 5: // MaxTemp
                            foreach ( XElement forecast in forecasts )
                            {
                                sequence = Convert.ToInt32( forecast.Attribute( "data_sequence" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].MaxTemp = Convert.ToSingle( forecast.Attribute( "value" ).Value, CUtils.Inv );
                            }
                            break;

                        case 9: // Wind
                            foreach ( XElement forecast in forecasts )
                            {
                                sequence = Convert.ToInt32( forecast.Attribute( "data_sequence" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].WindIcon = Convert.ToInt32( forecast.Attribute( "idB" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].WindText = forecast.Attribute( "valueB" ).Value;
                            }
                            break;

                        case 10: // Weer
                            foreach ( XElement forecast in forecasts )
                            {
                                sequence = Convert.ToInt32( forecast.Attribute( "data_sequence" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].WeatherIcon = Convert.ToInt32( forecast.Attribute( "id2" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].WeatherText = forecast.Attribute( "value2" ).Value;
                            }
                            break;

                        case 15: // Name of the Day
                            foreach ( XElement forecast in forecasts )
                            {
                                sequence = Convert.ToInt32( forecast.Attribute( "data_sequence" ).Value, CUtils.Inv );
                                PredictionList[ sequence - 1 ].Day = forecast.Attribute( "value" ).Value;
                            }
                            break;
                    }
                }

                retval = true; //success, we have a go for prediction.
            }
            // else no data, return, no prediction can be made; retval default is false

            Sup.LogTraceInfoMessage( $" XML AddPrediction - retval = {retval}" );

            return retval;
        }

    }
}