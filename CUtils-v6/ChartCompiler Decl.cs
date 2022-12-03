/*
 * ChartsCompiler Declaration - Part of CumulusUtils
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
 * Literature:  https://github.com/jstat/jstat
 *              https://jstat.github.io/all.html
 *              https://www.highcharts.com/docs/chart-and-series-types/chart-types
 *              
 *              
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CumulusUtils
{
    #region Global Declarations

    [Flags]
    internal enum AxisType
    {
        None = 0, Temp = 1, Pressure = 2, Rain = 4, Rrate = 8, Wind = 16, Direction = 32, Humidity = 64, Solar = 128, UV = 256, Hours = 512,
        Distance = 1024, DegreeDays = 2048, EVT = 4096, Free = 8192, AQ = 16384, ppm = 32768
    };
    internal enum PlotvarRangeType { Recent, Extra, Daily, All };

    internal struct OutputDef
    {
        internal OutputDef( string filename )
        {
            Filename = filename;
            TheseCharts = new List<ChartDef>();
        }

        internal string Filename { get; set; }
        internal List<ChartDef> TheseCharts;
    }

    internal struct ChartDef
    {
        internal ChartDef( string thisId, string thisTitle )
        {
            Range = PlotvarRangeType.Recent;
            PlotVars = new List<Plotvar>();
            Id = thisId;
            Title = thisTitle;
            Axis = AxisType.None;
            HasScatter = false;
            HasWindBarbs = false;
            WindBarbsBelow = true;
            WindBarbColor = "black";
            ConnectsToDashboardPanel = new List<int>();
            HasInfo = false;
            InfoText = "";
            Zoom = -1;
        }
        internal PlotvarRangeType Range { get; set; }
        internal AxisType Axis;
        internal List<Plotvar> PlotVars { get; set; }
        internal string Id { get; set; }
        internal string Title { get; set; }
        internal bool HasScatter { get; set; }
        internal bool HasWindBarbs { get; set; }
        internal bool WindBarbsBelow { get; set; }
        internal string WindBarbColor { get; set; }
        internal List<int> ConnectsToDashboardPanel { get; set; }
        internal bool HasInfo { get; set; }
        internal string InfoText { get; set; }
        internal int Zoom { get; set; }
    }

    internal struct EqDef
    {
        internal string Id;
        internal string Equation;
    }

    // The structure 
    internal struct Plotvar
    {
        internal string Keyword;            // The actual keyword to use in the graph and make it understandable
        internal string PlotVar;            // like 'Temp', 'wdir' etc... : the id in the JSON
        internal string Equation;           // Any equation the user puts in the EVAL string, checked and translated to javascript
        internal List<AllVarInfo> EqAllVarList;
        internal PlotvarRangeType PlotvarRange; // So is it a Recent, Extra, Daily or All range


        internal string Unit;               // Required knowledge about the parameters unit is stored in an array
        internal string Datafile;           // the actual datafile where the data can be found
        internal string Color;              // the c olour as defined 
        internal int LineWidth;             // The LineWidth
        internal double Opacity;            // The LineWidth
        internal string GraphType;          // like 'line', spline etc...
        internal AxisType Axis;             // For fast access to the type needed
        internal string AxisId;             // For fast access to the type needed
        internal int zIndex;                // the zIndex plane for the plotorder (e.g. to get a  line before an area so it can be seen)
        internal bool IsStats;              // Remember it is a stats var and needs to be linked to the original which must be in the same chart
        internal bool Visible;              // Should the  line be visible at initialisation? true == Yes, fals == No
    }

    internal struct AllVarInfo
    {
        internal string KeywordName;
        internal string TypeName;
        internal string Datafile;
    }


    #endregion

    partial class ChartsCompiler
    {

        #region Declarations

        internal readonly AxisType[] PlotvarAxisRECENT = {
            AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp,
            AxisType.Wind, AxisType.Wind,
            AxisType.Direction, AxisType.Direction,
            AxisType.UV, AxisType.Solar, AxisType.Solar,
            AxisType.Rain,  AxisType.Rrate,
            AxisType.Pressure,
            AxisType.Humidity, AxisType.Humidity,
            AxisType.EVT
        };

        internal readonly AxisType[] PlotvarAxisALL = {
            AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp,
            AxisType.Wind, AxisType.Distance, AxisType.Wind,
            AxisType.Hours, AxisType.Solar, AxisType.UV,
            AxisType.Rain,  AxisType.Rrate,
            AxisType.Pressure, AxisType.Pressure,
            AxisType.Humidity, AxisType.Humidity,
            AxisType.DegreeDays, AxisType.DegreeDays, AxisType.EVT, AxisType.Free
        };

        internal readonly AxisType[] PlotvarAxisEXTRA = {
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,
            AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,
            AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Temp,AxisType.Temp,
            AxisType.Free,AxisType.Free,
            AxisType.ppm,AxisType.ppm,AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.Temp,AxisType.Humidity
        };

        internal readonly string[] PlotvarTypesRECENT = {
          "intemp", "dew", "apptemp", "feelslike", "wchill", "heatindex", "temp", "humidex",
          "wgust", "wspeed",
          "bearing", "avgbearing",
          "UV", "SolarRad", "CurrentSolarMax",
          "rfall", "rrate",
          "press",
          "hum", "inhum",
          "evapotranspiration"
        };

        internal readonly string[] PlotvarTypesALL = {
          "minTemp", "maxTemp", "avgTemp", "windChill", "maxDew", "minDew", "maxFeels", "minFeels",
          "maxGust", "windRun", "maxWind",
          "sunHours", "solarRad", "uvi",
          "rain", "maxRainRate",
          "minBaro", "maxBaro",
          "minHum", "maxHum",
          "heatingdegreedays", "coolingdegreedays", "evapotranspiration"
        };

        // Static because needed in ExtraSensors
        static internal string[] PlotvarTypesEXTRA = {
            "Temp1","Temp2","Temp3","Temp4","Temp5","Temp6","Temp7","Temp8","Temp9","Temp10",
            "Humidity1","Humidity2","Humidity3","Humidity4","Humidity5","Humidity6","Humidity7","Humidity8","Humidity9","Humidity10",
            "Dewpoint1","Dewpoint2","Dewpoint3","Dewpoint4","Dewpoint5","Dewpoint6","Dewpoint7","Dewpoint8","Dewpoint9","Dewpoint10",
            "SoilTemp1","SoilTemp2","SoilTemp3","SoilTemp4","SoilTemp5","SoilTemp6","SoilTemp7","SoilTemp8","SoilTemp9","SoilTemp10","SoilTemp11","SoilTemp12","SoilTemp13","SoilTemp14","SoilTemp15","SoilTemp16",
            "SoilMoisture1","SoilMoisture2","SoilMoisture3","SoilMoisture4","SoilMoisture5","SoilMoisture6","SoilMoisture7","SoilMoisture8","SoilMoisture9","SoilMoisture10","SoilMoisture11","SoilMoisture12","SoilMoisture13","SoilMoisture14","SoilMoisture15","SoilMoisture16",
            "AirQuality1","AirQuality2","AirQuality3","AirQuality4",
            "AirQualityAvg1","AirQualityAvg2","AirQualityAvg3","AirQualityAvg4",
            "UserTemp1","UserTemp2","UserTemp3","UserTemp4","UserTemp5","UserTemp6","UserTemp7","UserTemp8",
            "LeafTemp1","LeafTemp2", "LeafWetness1","LeafWetness2",
            "CO2", "CO2_24h", "CO2_pm2p5", "CO2_pm2p5_24h","CO2_pm10","CO2_pm10_24h","CO2_temp","CO2_hum"
        };

        internal readonly string[] DatafilesRECENT = {
          "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json",
          "winddata.json", "winddata.json",
          "wdirdata.json", "wdirdata.json",
          "solardata.json", "solardata.json", "solardata.json",
          "raindata.json", "raindata.json",
          "pressdata.json",
          "humdata.json", "humdata.json",
          "CUserdataRECENT.json"
        };

        internal readonly string[] DatafilesALL = {
          "alldailytempdata.json","alldailytempdata.json","alldailytempdata.json","alldailytempdata.json",
          "alldailytempdata.json","alldailytempdata.json","alldailytempdata.json","alldailytempdata.json",
          "alldailywinddata.json","alldailywinddata.json", "alldailywinddata.json",
          "alldailysolardata.json","alldailysolardata.json", "alldailysolardata.json",
          "alldailyraindata.json","alldailyraindata.json",
          "alldailypressdata.json","alldailypressdata.json",
          "alldailyhumdata.json","alldailyhumdata.json",
          "CUserdataALL.json", "CUserdataALL.json", "CUserdataALL.json"
        };

        internal readonly string[] DatafilesEXTRA = {
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
        };

        internal readonly string[] PlotvarKeywordRECENT = {
          "InsideTemp", "Dewpoint", "ApparentTemp", "FeelsLike", "WindChill", "HeatIndex", "Temperature", "Humidex",
          "WindGust", "WindSpeed",
          "Bearing", "AverageBearing",
          "UV", /*"SolarRadiation",*/ "CurrentSolarRad", "TheoreticalSolarMax",
          "RainFall", "RainRate",
          "Pressure",
          "Humidity", "InsideHumidity",
          "EvapoTranspiration"
        };

        internal readonly string[] PlotvarKeywordALL = {
          "MinTemp", "MaxTemp", "AverageTemp", "AvgWindChill", /*"WindChill",*/ "MaxDewpoint", "MinDewpoint", "MaxFeelsLike", "MinFeelsLike",
          "MaxGust", "WindRun", "HighAvgWindSpeed", /* "WindSpeed",*/
          "SunHours", "SolarRadiation", "UVIndex",
          /*"RainFall",*/ "DayRain", "MaxRainRate",
          "MinBarometer", "MaxBarometer",
          "MinHumidity", "MaxHumidity",
          "HeatingDegreeDays","CoolingDegreeDays","DayEVT" /*"EvapoTranspiration"*/
        };

        static internal string[] PlotvarKeywordEXTRA = {
            "Temp1","Temp2","Temp3","Temp4","Temp5","Temp6","Temp7","Temp8","Temp9","Temp10",
            "Humidity1","Humidity2","Humidity3","Humidity4","Humidity5","Humidity6","Humidity7","Humidity8","Humidity9","Humidity10",
            "Dewpoint1","Dewpoint2","Dewpoint3","Dewpoint4","Dewpoint5","Dewpoint6","Dewpoint7","Dewpoint8","Dewpoint9","Dewpoint10",
            "SoilTemp1","SoilTemp2","SoilTemp3","SoilTemp4","SoilTemp5","SoilTemp6","SoilTemp7","SoilTemp8","SoilTemp9","SoilTemp10","SoilTemp11","SoilTemp12","SoilTemp13","SoilTemp14","SoilTemp15","SoilTemp16",
            "SoilMoisture1","SoilMoisture2","SoilMoisture3","SoilMoisture4","SoilMoisture5","SoilMoisture6","SoilMoisture7","SoilMoisture8","SoilMoisture9","SoilMoisture10","SoilMoisture11","SoilMoisture12","SoilMoisture13","SoilMoisture14","SoilMoisture15","SoilMoisture16",
            "AirQuality1","AirQuality2","AirQuality3","AirQuality4",
            "AirQualityAvg1","AirQualityAvg2","AirQualityAvg3","AirQualityAvg4",
            "UserTemp1","UserTemp2","UserTemp3","UserTemp4","UserTemp5","UserTemp6","UserTemp7","UserTemp8",
            "LeafTemp1","LeafTemp2","LeafWetness1","LeafWetness2",
            "CO2", "CO2_24h", "CO2_pm2p5", "CO2_pm2p5_24h","CO2_pm10","CO2_pm10_24h","CO2_temp","CO2_hum"
        };

        internal readonly string[] ValidColumnRangeVars = {
              "MinTemp", "MaxTemp", "AverageTemp", "MaxDewpoint", "MinDewpoint", "MaxFeelsLike", "MinFeelsLike",
              "MinBarometer", "MaxBarometer",
              "MinHumidity", "MaxHumidity"
            };

        internal AxisType[] PlotvarAxis;
        internal string[] PlotvarUnits;
        internal string[] PlotvarTypes;
        internal string[] PlotvarKeyword;
        internal string[] Datafiles;

        internal readonly string[] PlotvarUnitsRECENT, PlotvarUnitsALL, PlotvarUnitsEXTRA;     // Init in constructor
        internal readonly string[] LinetypeKeywords = { "Line", "SpLine", "Area", "Column", "Scatter", "ColumnRange" };
        internal readonly string[] AxisKeywords = { "Temp", "Wind", "Distance", "Hours", "Solar", "UV", "Rain", "Rrate", "Pressure", "Humidity", "DegreeDays", "EVT", "Free", "AQ", "ppm" };
        internal readonly string[] StatsTypeKeywords = { "SMA" };

        readonly CuSupport Sup;
        readonly float MaxPressure, MinPressure;
        readonly CultureInfo ci = CultureInfo.InvariantCulture;
        readonly StringComparison cmp = StringComparison.OrdinalIgnoreCase;

        internal string[] ClickEvents = new string[ 24 ] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };

        #endregion

        #region Constructor

        internal ChartsCompiler( CuSupport s )
        {
            // Constructor
            Sup = s;

            PlotvarUnitsRECENT = new string[ PlotvarTypesRECENT.Length ];
            PlotvarUnitsRECENT[ 0 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 1 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 2 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 3 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 4 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 5 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 6 ] = Sup.StationTemp.Text();
            PlotvarUnitsRECENT[ 7 ] = Sup.StationTemp.Text();

            PlotvarUnitsRECENT[ 8 ] = Sup.StationWind.Text();
            PlotvarUnitsRECENT[ 9 ] = Sup.StationWind.Text();

            PlotvarUnitsRECENT[ 10 ] = "";
            PlotvarUnitsRECENT[ 11 ] = "";

            PlotvarUnitsRECENT[ 12 ] = "";
            PlotvarUnitsRECENT[ 13 ] = "W/m²";
            PlotvarUnitsRECENT[ 14 ] = "W/m²";

            PlotvarUnitsRECENT[ 15 ] = Sup.StationRain.Text();
            PlotvarUnitsRECENT[ 16 ] = Sup.StationRain.Text() + "/hr";

            PlotvarUnitsRECENT[ 17 ] = Sup.StationPressure.Text();

            PlotvarUnitsRECENT[ 18 ] = "%";
            PlotvarUnitsRECENT[ 19 ] = "%";

            PlotvarUnitsRECENT[ 20 ] = Sup.StationRain.Text();


            PlotvarUnitsALL = new string[ PlotvarTypesALL.Length ];
            PlotvarUnitsALL[ 0 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 1 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 2 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 3 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 4 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 5 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 6 ] = Sup.StationTemp.Text();
            PlotvarUnitsALL[ 7 ] = Sup.StationTemp.Text();

            PlotvarUnitsALL[ 8 ] = Sup.StationWind.Text();
            PlotvarUnitsALL[ 9 ] = Sup.StationDistance.Text();
            PlotvarUnitsALL[ 10 ] = Sup.StationWind.Text();

            PlotvarUnitsALL[ 11 ] = $"{Sup.GetCUstringValue( "General", "Hours", "Hours", true )}";
            PlotvarUnitsALL[ 12 ] = "W/m²";
            PlotvarUnitsALL[ 13 ] = "";

            PlotvarUnitsALL[ 14 ] = Sup.StationRain.Text();
            PlotvarUnitsALL[ 15 ] = Sup.StationRain.Text() + "/hr";

            PlotvarUnitsALL[ 16 ] = Sup.StationPressure.Text();
            PlotvarUnitsALL[ 17 ] = Sup.StationPressure.Text();

            PlotvarUnitsALL[ 18 ] = "%";
            PlotvarUnitsALL[ 19 ] = "%";

            PlotvarUnitsALL[ 20 ] = "degree days";
            PlotvarUnitsALL[ 21 ] = "degree days";
            PlotvarUnitsALL[ 22 ] = Sup.StationRain.Text();


            PlotvarUnitsEXTRA = new string[ PlotvarTypesEXTRA.Length ];
            PlotvarUnitsEXTRA[ 0 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 1 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 2 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 3 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 4 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 5 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 6 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 7 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 8 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 9 ] = Sup.StationTemp.Text();

            PlotvarUnitsEXTRA[ 10 ] = "%";
            PlotvarUnitsEXTRA[ 11 ] = "%";
            PlotvarUnitsEXTRA[ 12 ] = "%";
            PlotvarUnitsEXTRA[ 13 ] = "%";
            PlotvarUnitsEXTRA[ 14 ] = "%";
            PlotvarUnitsEXTRA[ 15 ] = "%";
            PlotvarUnitsEXTRA[ 16 ] = "%";
            PlotvarUnitsEXTRA[ 17 ] = "%";
            PlotvarUnitsEXTRA[ 18 ] = "%";
            PlotvarUnitsEXTRA[ 19 ] = "%";

            PlotvarUnitsEXTRA[ 20 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 21 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 22 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 23 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 24 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 25 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 26 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 27 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 28 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 29 ] = Sup.StationTemp.Text();

            PlotvarUnitsEXTRA[ 30 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 31 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 32 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 33 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 34 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 35 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 36 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 37 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 38 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 39 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 40 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 41 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 42 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 43 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 44 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 45 ] = Sup.StationTemp.Text();

            PlotvarUnitsEXTRA[ 46 ] = "%";
            PlotvarUnitsEXTRA[ 47 ] = "%";
            PlotvarUnitsEXTRA[ 48 ] = "%";
            PlotvarUnitsEXTRA[ 49 ] = "%";
            PlotvarUnitsEXTRA[ 50 ] = "%";
            PlotvarUnitsEXTRA[ 51 ] = "%";
            PlotvarUnitsEXTRA[ 52 ] = "%";
            PlotvarUnitsEXTRA[ 53 ] = "%";
            PlotvarUnitsEXTRA[ 54 ] = "%";
            PlotvarUnitsEXTRA[ 55 ] = "%";
            PlotvarUnitsEXTRA[ 56 ] = "%";
            PlotvarUnitsEXTRA[ 57 ] = "%";
            PlotvarUnitsEXTRA[ 58 ] = "%";
            PlotvarUnitsEXTRA[ 59 ] = "%";
            PlotvarUnitsEXTRA[ 60 ] = "%";
            PlotvarUnitsEXTRA[ 61 ] = "%";

            PlotvarUnitsEXTRA[ 62 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 63 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 64 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 65 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 66 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 67 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 68 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 69 ] = "μg/m3";

            PlotvarUnitsEXTRA[ 70 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 71 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 72 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 73 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 74 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 75 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 76 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 77 ] = Sup.StationTemp.Text();

            PlotvarUnitsEXTRA[ 78 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 79 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 80 ] = "%";
            PlotvarUnitsEXTRA[ 81 ] = "%";

            PlotvarUnitsEXTRA[ 82 ] = "ppm";
            PlotvarUnitsEXTRA[ 83 ] = "ppm";

            PlotvarUnitsEXTRA[ 84 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 85 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 86 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 87 ] = "μg/m3";
            PlotvarUnitsEXTRA[ 88 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 89 ] = "%";

            // Init the Compiler section in language file for the keywords just to  make sure they are there
            // Even if it is the millionth time or more... Certainly when adding more keywords later on.
            foreach ( string k in PlotvarKeywordRECENT )
                if ( !string.IsNullOrEmpty( k ) )
                    Sup.GetCUstringValue( "Compiler", k, k, false );
            foreach ( string k in PlotvarKeywordALL )
                if ( !string.IsNullOrEmpty( k ) )
                    Sup.GetCUstringValue( "Compiler", k, k, false );

            try
            {
                MaxPressure = Convert.ToSingle( Sup.GetAlltimeRecordValue( "Pressure", "highpressurevalue", "" ), ci );
                MinPressure = Convert.ToSingle( Sup.GetAlltimeRecordValue( "Pressure", "lowpressurevalue", "" ), ci );
            }
            catch
            {
                /* Don't take any action just make sure we can continue */
                Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Constructor - can't convert Min/Max barometer values: " +
                  $"{MinPressure}/{MaxPressure}" +
                  $"{Sup.GetAlltimeRecordValue( "Pressure", "highpressurevalue", "" )}/{Sup.GetAlltimeRecordValue( "Pressure", "lowpressurevalue", "" )}" );
            }

            // Prepare for possible ExternalExtraSensors!
            string[] ExternalExtraSensors = Sup.GetUtilsIniValue( "ExtraSensors", "ExternalExtraSensors", "" ).Split( ',' );

            if ( !string.IsNullOrEmpty( ExternalExtraSensors[ 0 ] ) )
            {

                foreach ( string thisExternal in ExternalExtraSensors )
                {
                    List<string> tmpStr;

                    List<AxisType> tmp = PlotvarAxisEXTRA.ToList();
                    tmp.Add( AxisType.Free );
                    PlotvarAxisEXTRA = tmp.ToArray();

                    tmpStr = PlotvarUnitsEXTRA.ToList();
                    tmpStr.Add( "" );
                    PlotvarUnitsEXTRA = tmpStr.ToArray();

                    tmpStr = PlotvarTypesEXTRA.ToList();
                    tmpStr.Add( thisExternal );
                    PlotvarTypesEXTRA = tmpStr.ToArray();

                    tmpStr = PlotvarKeywordEXTRA.ToList();
                    tmpStr.Add( thisExternal );
                    PlotvarKeywordEXTRA = tmpStr.ToArray();

                    tmpStr = DatafilesEXTRA.ToList();
                    tmpStr.Add( "extrasensorsdata.json" );
                    DatafilesEXTRA = tmpStr.ToArray();
                }
            }

        } // ChartsCompiler Constructor End

        #endregion

        #region Divers

        private int ApproximateSolarMax()
        {
            // See: https://www.sciencedirect.com/science/article/pii/S221260901400051X
            //

            int i = DateTime.Now.DayOfYear;
            const double Deg2Rad = Math.PI / 180;
            const int SolarConstant = 1375;
            int Estimation;
            double Latitude = Convert.ToDouble( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CultureInfo.InvariantCulture );

            double Gamma = 0.796 - 0.01 * Math.Sin( 0.986 * ( i + 284 ) * Deg2Rad );
            double EarthSunDist = 1 + 0.034 * Math.Cos( ( i - 2 ) * Deg2Rad );
            double Delta = 23.45 * Math.Sin( 0.986 * ( i + 284 ) * Deg2Rad );
            double HeightOfSun = Math.Asin( Math.Sin( Latitude * Deg2Rad ) * Math.Sin( Delta * Deg2Rad ) + Math.Cos( Latitude * Deg2Rad ) * Math.Cos( Delta * Deg2Rad ) );
            double ExponentialComponent = Math.Exp( -0.13 / Math.Sin( HeightOfSun ) ) * Math.Sin( HeightOfSun );

            Estimation = (int) ( SolarConstant * Gamma * EarthSunDist * ExponentialComponent );

            return Estimation;
        }

        #endregion
    } // Class DefineCharts
}// Namespace
