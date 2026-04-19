/*
 * Forecast - Part of CumulusUtils
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CumulusUtils
{
    public class WeatherForecasts( CuSupport s, InetSupport i )
    {
        private readonly CuSupport Sup = s;
        private readonly InetSupport Isup = i;

        public async Task GenerateForecasts()
        {
            string ForecastSystem;

            // Make sure the parameters exist in one run, otherwise the user has to run, define and run again
            ForecastSystem = Sup.GetUtilsIniValue( "Forecasts", "ForecastSystem", "CUtils" );

            if ( ForecastSystem.Equals( "CUtils", CUtils.Cmp ) )
            {
                bool retval = await GetOpenMeteoPredictionAsync();
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
            }
            else
            {
                Sup.LogTraceErrorMessage( $"Prediction : Illegal Forecast system defined - {ForecastSystem}." );
                Sup.LogTraceErrorMessage( "Prediction : Impossible to continue, exiting procedure." );
            }

            return;
        }

        public class WeatherResponse
        {
            [JsonPropertyName( "latitude" )]
            public double Latitude { get; set; }

            [JsonPropertyName( "longitude" )]
            public double Longitude { get; set; }

            [JsonPropertyName( "generationtime_ms" )]
            public double GenerationTimeMs { get; set; }

            [JsonPropertyName( "utc_offset_seconds" )]
            public int UtcOffsetSeconds { get; set; }

            [JsonPropertyName( "timezone" )]
            public string Timezone { get; set; }

            [JsonPropertyName( "timezone_abbreviation" )]
            public string TimezoneAbbreviation { get; set; }

            [JsonPropertyName( "elevation" )]
            public double Elevation { get; set; }

            [JsonPropertyName( "hourly_units" )]
            public HourlyUnits HourlyUnits { get; set; }

            [JsonPropertyName( "hourly" )]
            public HourlyData Hourly { get; set; }
        }

        public class HourlyUnits
        {
            public long Time { get; set; }
            public string Temperature_2m { get; set; }
            public string Precipitation { get; set; }
            public string Pressure_Msl { get; set; }
            public string Wind_Speed_10m { get; set; }
            public string Wind_Gusts_10m { get; set; }
        }

        public class HourlyData
        {
            [JsonPropertyName( "time" )]
            public List<long> Time { get; set; }

            [JsonPropertyName( "temperature_2m" )]
            public List<double> Temperature2m { get; set; }

            [JsonPropertyName( "precipitation" )]
            public List<double> Precipitation { get; set; }

            [JsonPropertyName( "pressure_msl" )]
            public List<double> PressureMsl { get; set; }

            [JsonPropertyName( "wind_speed_10m" )]
            public List<double> WindSpeed10m { get; set; }

            [JsonPropertyName( "wind_gusts_10m" )]
            public List<double> WindGusts10m { get; set; }
        }

        private async Task<bool> GetOpenMeteoPredictionAsync()
        {
            // This is necessary for the use of the units the users has set in Cumulus
            // The order is the same as in the UnitsAndConversions but the wording of the units in the URL is different.
            // values are added to the list in the units the user has chosen
            string[] TempUnitForOpenMeteo = new string[] { "celsius", "fahrenheit" };
            string[] WindUnitForOpenMeteo = new string[] { "ms", "mph", "kmh", "kn" };
            string[] RainUnitForOpenMeteo = new string[] { "mm", "inch" };

            WeatherResponse data;
            HourlyData hourlyData;
            HourlyUnits hourlyUnits;

            try
            {
                string latitude = $"{Sup.GetCumulusIniValue( "Station", "Latitude", "" )}0000";  // make sure the string is long enough
                string longitude = $"{Sup.GetCumulusIniValue( "Station", "Longitude", "" )}0000";

                string thisURL = $"https://api.open-meteo.com/v1/forecast?" +
                    $"latitude={latitude}&" +
                    $"longitude={longitude}&" +
                    $"hourly=temperature_2m,precipitation,pressure_msl,wind_speed_10m,wind_gusts_10m&" +
                    $"models=ecmwf_ifs&" +
                    $"timezone={Sup.GetCumulusIniValue( "Station", "TimeZone", "" )}&" +
                    $"timeformat=unixtime&" +
                    $"start_date={DateTime.Today:yyyy-MM-dd}&end_date={DateTime.Today.AddDays( 10 ):yyyy-MM-dd}&" +
                    $"wind_speed_unit={WindUnitForOpenMeteo[ (int) Sup.StationWind.Dim ]}&" +
                    $"temperature_unit={TempUnitForOpenMeteo[ (int) Sup.StationTemp.Dim ]}&" +
                    $"precipitation_unit={RainUnitForOpenMeteo[ (int) Sup.StationRain.Dim ]}";

                string JSONresult = await Isup.GetUrlDataAsync( new Uri( thisURL ) );
                Sup.LogTraceInfoMessage( $"GetOpenMeteoPrediction: JSONresult: {JSONresult} " );

                data = JsonSerializer.Deserialize<WeatherResponse>( JSONresult );
                hourlyData = data.Hourly;
                hourlyUnits = data.HourlyUnits;
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Open Meteo AddPrediction: {e.Message}" );
                return false;
            }

            // Write the forecast HTML file
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ForecastOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( "<!-- " );
                of.WriteLine( $"This file is generated as part of CumulusUtils - {DateTime.Now} " );
                of.WriteLine( "This header must not be removed and the user must comply to the GNU General Public License" );
                of.WriteLine( "See also License conditions of CumulusUtils at https://meteo-wagenborgen.nl/ " );
                of.WriteLine( "-->" );

                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );
                of.WriteLine( ".buttonFat {border-radius: 4px; margin-right:10px; margin-left:10px; }" );
                of.WriteLine( "</style>" );
                of.WriteLine( "<div id='report'>" );
                of.WriteLine( "<br/>" );
                of.WriteLine( "<div id='chartcontainer' style='min-height: 650px; margin-top: 10px; margin-bottom: 10px;'></div>" );
                of.WriteLine( "<br/>" );
                of.WriteLine( "<input type='button' class='buttonFat' id='Switch' value='Switch' >" );
                of.WriteLine( "<br/>" );
                of.WriteLine( "</div>" );
                of.WriteLine( "<script>" );

                of.Write( "var timeseries = [" );
                int length = hourlyData.Time.Count;
                for ( int n = 0; n < length; n++ )
                {
                    if ( n == length - 1 ) of.Write( $" {hourlyData.Time[ n ].ToString( "F0", CUtils.Inv )}" );
                    else of.Write( $" {hourlyData.Time[ n ].ToString( "F0", CUtils.Inv )}, " );
                }

                of.WriteLine( "];" );

                of.Write( "var tempseries = [" );
                int tempLength = hourlyData.Temperature2m.Count;
                for ( int n = 0; n < tempLength; n++ )
                {
                    if ( n == tempLength - 1 ) of.Write( $" {hourlyData.Temperature2m[ n ].ToString( "F1", CUtils.Inv )}" );
                    else of.Write( $" {hourlyData.Temperature2m[ n ].ToString( "F1", CUtils.Inv )}, " );
                }
                of.WriteLine( "];" );

                of.Write( "var rainseries = [" );
                int rainLength = hourlyData.Precipitation.Count;

                for ( int n = 0; n < rainLength; n++ )
                {
                    if ( n == rainLength - 1 ) of.Write( $" {hourlyData.Precipitation[ n ].ToString( "F1", CUtils.Inv )}" );
                    else of.Write( $" {hourlyData.Precipitation[ n ].ToString( "F1", CUtils.Inv )}, " );
                }
                of.WriteLine( "];" );

                of.Write( "var pressureseries = [" );
                int pressureLength = hourlyData.PressureMsl.Count;
                for ( int n = 0; n < pressureLength; n++ )
                {
                    if ( n == pressureLength - 1 ) of.Write( $" {hourlyData.PressureMsl[ n ].ToString( "F1", CUtils.Inv )}" );
                    else of.Write( $" {hourlyData.PressureMsl[ n ].ToString( "F1", CUtils.Inv )}, " );
                }
                of.WriteLine( "];" );

                of.Write( "var windspeedseries = [" );
                int windSpeedLength = hourlyData.WindSpeed10m.Count;
                for ( int n = 0; n < windSpeedLength; n++ )
                {
                    if ( n == windSpeedLength - 1 ) of.Write( $" {hourlyData.WindSpeed10m[ n ].ToString( "F1", CUtils.Inv )}" );
                    else of.Write( $" {hourlyData.WindSpeed10m[ n ].ToString( "F1", CUtils.Inv )}, " );
                }
                of.WriteLine( "];" );

                of.Write( "var windgustseries = [" );
                int windGustLength = hourlyData.WindGusts10m.Count;
                for ( int n = 0; n < windGustLength; n++ )
                {
                    if ( n == windGustLength - 1 ) of.Write( $" {hourlyData.WindGusts10m[ n ].ToString( "F1", CUtils.Inv )}" );
                    else of.Write( $" {hourlyData.WindGusts10m[ n ].ToString( "F1", CUtils.Inv )}, " );
                }
                of.WriteLine( "];" );

                of.WriteLine( "function chartTPR() {" );
                of.WriteLine( "chart = Highcharts.chart('chartcontainer', {" );
                of.WriteLine( "  chart: {" );
                of.WriteLine( "       type: 'line'," );
                of.WriteLine( "        zoomType: 'x'" );
                of.WriteLine( "    }," );
                of.WriteLine( "    title: {" );
                of.WriteLine( $"        text: '{Sup.GetStringsIniValue( "Forecast", "OpenMeteoTitle", "Open Meteo Forecast" )} - ECMWF IFS HRES 9km'" );
                of.WriteLine( "    }," );
                of.WriteLine( "    xAxis: {" ); of.WriteLine( "        type: 'datetime'," );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoXaxisTitle", "Date and Time" )}' }}" );
                of.WriteLine( "    }," );
                of.WriteLine( "    yAxis: [{ " );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoTempYaxisTitle", "Temperature" )} ({Sup.StationTemp.Text()})' }}," );
                of.WriteLine( "    }, {" );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoPrecipYaxisTitle", "Precipitation" )} ({Sup.StationRain.Text()})' }}," );
                of.WriteLine( "        opposite: true" );
                of.WriteLine( "    }, {" );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoPressureYaxisTitle", "Pressure" )} ({Sup.StationPressure.Text()})'}}," );
                of.WriteLine( "        opposite: true" );
                of.WriteLine( "    }]," );
                of.WriteLine( "    tooltip: {" );
                of.WriteLine( "        shared: true," );
                of.WriteLine( "        xDateFormat: '%A, %b %e, %H:%M'" );
                of.WriteLine( "    }," );
                of.WriteLine( "    series: [{" );
                of.WriteLine( $"        name: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoTempYaxisTitle", "Temperature" )}'," );
                of.WriteLine( "        data: parseWeatherData(timeseries, tempseries)," );
                of.WriteLine( "        color: 'blue'," );
                of.WriteLine( "        softMax: 1.0," );
                of.WriteLine( $"        tooltip: {{ valueSuffix: ' {Sup.StationTemp.Text()}' }}" );
                of.WriteLine( "    }, {" );
                of.WriteLine( $"        name: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoPrecipYaxisTitle", "Precipitation" )}'," );
                of.WriteLine( "        type: 'column'," );
                of.WriteLine( "        yAxis: 1," );
                of.WriteLine( "        data: parseWeatherData(timeseries, rainseries)," );
                of.WriteLine( "        color: 'green'," );
                of.WriteLine( "        softMax: 1.0," );
                of.WriteLine( $"        tooltip: {{ valueSuffix: ' {Sup.StationRain.Text()}' }}" );
                of.WriteLine( "    }, {" );
                of.WriteLine( $"        name: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoPressureYaxisTitle", "Pressure" )}'," );
                of.WriteLine( "        yAxis: 2," );
                of.WriteLine( "        data: parseWeatherData(timeseries, pressureseries)," );
                of.WriteLine( "        color: 'red'," );
                of.WriteLine( $"        tooltip: {{ valueSuffix: ' {Sup.StationPressure.Text()}' }}," );
                of.WriteLine( "    }]" );
                of.WriteLine( "});" );
                of.WriteLine( Sup.ActivateChartInfo( "Forecast" ) );
                of.WriteLine( "}" );

                of.WriteLine( "function chartWind() {" );
                of.WriteLine( "chart = Highcharts.chart('chartcontainer', {" );
                of.WriteLine( "  chart: {" );
                of.WriteLine( "       type: 'line'," );
                //of.WriteLine( "        zoomType: 'x'" );
                of.WriteLine( "    }," );
                of.WriteLine( "    title: {" );
                of.WriteLine( $"        text: '{Sup.GetStringsIniValue( "Forecast", "OpenMeteoTitle", "Open Meteo Forecast" )} - ECMWF IFS HRES 9km'" );
                of.WriteLine( "    }," );
                of.WriteLine( "    xAxis: {" ); of.WriteLine( "        type: 'datetime'," );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoXaxisTitle", "Date and Time" )}' }}" );
                of.WriteLine( "    }," );
                of.WriteLine( "    yAxis: [{ " );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoWindYaxisTitle", "Wind" )} ({Sup.StationWind.Text()})' }}," );
                of.WriteLine( "        softMax: Math.max(...windgustseries)," );
                of.WriteLine( "    }, {" );
                of.WriteLine( $"        title: {{ text: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoGustYaxisTitle", "Gust" )} ({Sup.StationWind.Text()})' }}," );
                of.WriteLine( "        linkedTo: 0," );
                of.WriteLine( "        opposite: true" );
                of.WriteLine( "    }]," );
                of.WriteLine( "    tooltip: {" );
                of.WriteLine( "        shared: true," );
                of.WriteLine( "        xDateFormat: '%A, %b %e, %H:%M'" );
                of.WriteLine( "    }," );
                of.WriteLine( "    series: [{" );
                of.WriteLine( $"        name: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoWindYaxisTitle", "Wind" )}'," );
                of.WriteLine( "        data: parseWeatherData(timeseries, windspeedseries)," );
                of.WriteLine( $"        tooltip: {{ valueSuffix: ' {Sup.StationWind.Text()}' }}" );
                of.WriteLine( "    }, {" );
                of.WriteLine( $"        name: '{Sup.GetStringsIniValue( "Forecasts", "OpenMeteoGustYaxisTitle", "Gust" )}'," );
                of.WriteLine( "        yAxis: 1," );
                of.WriteLine( "        data: parseWeatherData(timeseries, windgustseries)," );
                of.WriteLine( $"        tooltip: {{ valueSuffix: ' {Sup.StationWind.Text()}' }}," );
                of.WriteLine( "    }]" );
                of.WriteLine( "});" );
                of.WriteLine( Sup.ActivateChartInfo( "Forecast" ) );
                of.WriteLine( "}" );

                of.WriteLine( "// Helper functie om de JSON data om te zetten naar Highcharts formaat [tijd, waarde]" );
                of.WriteLine( "function parseWeatherData(timeArray, valueArray) {" );
                of.WriteLine( "    return timeArray.map((t, index) => [t * 1000, valueArray[index]]);" );
                of.WriteLine( "}" );
                of.WriteLine( "" );

                of.WriteLine( "$( function() {" );
                of.WriteLine( "  $( '#Switch' ).click( function() {" );
                of.WriteLine( "    if (switchButton) {switchButton = false; chartTPR();} else {switchButton = true; chartWind(); }" );
                of.WriteLine( "  });" );
                of.WriteLine( "});" );
                of.WriteLine( "console.log('Forecast loaded...');" );
                of.WriteLine( "switchButton = false;" );
                of.WriteLine( "chartTPR();" );

                of.WriteLine( "</script>" );
                of.WriteLine( Sup.GenerateChartInfoModal( chartId: "Forecast", Title: Sup.GetCUstringValue( "Forecast", "Title", "Forecast", true ) ) );
                of.WriteLine( "" );
            }
            return true;
        }

    }
}