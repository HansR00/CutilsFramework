/*
 * ChartsCompiler Declaration - Part of CumulusUtils
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
 * Literature:  https://github.com/jstat/jstat
 *              https://jstat.github.io/all.html
 *              https://www.highcharts.com/docs/chart-and-series-types/chart-types
 *              
 *              
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CumulusUtils
{
    #region Global Declarations

    [Flags]
    public enum AxisType
    {
        None = 0, Temp = 1, Pressure = 2, Rain = 4, Rrate = 8, Wind = 16, Direction = 32, Humidity = 64, Solar = 128, UV = 256, Hours = 512,
        Distance = 1024, Height = 2048, DegreeDays = 4096, EVT = 8192, Free = 16384, AQ = 32768, ppm = 65536, SoilMoisture = 131072
    };
    public enum PlotvarRangeType { Recent, Extra, Daily, All };

    public struct OutputDef( string filename )
    {
        public string Filename { get; set; } = filename;
        public List<ChartDef> TheseCharts = new List<ChartDef>();
    }

    public struct ChartDef( string thisId, string thisTitle )
    {
        public PlotvarRangeType Range { get; set; } = PlotvarRangeType.Recent;
        public AxisType Axis = AxisType.None;
        public List<Plotvar> PlotVars { get; set; } = new List<Plotvar>();
        public string Id { get; set; } = thisId;
        public string Title { get; set; } = thisTitle;
        public bool HasScatter { get; set; } = false;
        public bool HasWindBarbs { get; set; } = false;
        public bool WindBarbsBelow { get; set; } = true;
        public string WindBarbColor { get; set; } = "black";
        public List<int> ConnectsToDashboardPanel { get; set; } = new List<int>();
        public bool HasInfo { get; set; } = false;
        public string InfoText { get; set; } = "";
        public int Zoom { get; set; } = -1;
    }

    public struct EqDef
    {
        public string Id;
        public string Equation;
    }

    // The structure 
    public struct Plotvar
    {
        public string Keyword;            // The actual keyword to use in the graph and make it understandable
        public string PlotVar;            // like 'Temp', 'wdir' etc... : the id in the JSON
        public string Equation;           // Any equation the user puts in the EVAL string, checked and translated to javascript
        public List<AllVarInfo> EqAllVarList;
        public PlotvarRangeType PlotvarRange; // So is it a Recent, Extra, Daily or All range


        public string Unit;               // Required knowledge about the parameters unit is stored in an array
        public string Datafile;           // the actual datafile where the data can be found
        public string Color;              // the c olour as defined 
        public int LineWidth;             // The LineWidth
        public double Opacity;            // The LineWidth
        public string GraphType;          // like 'line', spline etc...
        public int Period;                // For the Period for the SMA, if not given then parameter: [Compiler] SmaPeriod 
        public AxisType Axis;             // For fast access to the type needed
        public string AxisId;             // For fast access to the type needed
        public int zIndex;                // the zIndex plane for the plotorder (e.g. to get a  line before an area so it can be seen)
        public bool IsStats;              // Remember it is a stats var and needs to be linked to the original which must be in the same chart
        public bool Visible;              // Should the  line be visible at initialisation? true == Yes, fals == No
    }

    public struct AllVarInfo
    {
        public string KeywordName;
        public string TypeName;
        public string Datafile;
    }


    #endregion

    partial class ChartsCompiler
    {

        #region Declarations

        public readonly AxisType[] PlotvarAxisRECENT = {
            AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp,
            AxisType.Wind, AxisType.Wind,
            AxisType.Direction, AxisType.Direction,
            AxisType.UV, AxisType.Solar, AxisType.Solar,
            AxisType.Rain,  AxisType.Rrate,
            AxisType.Pressure,
            AxisType.Humidity, AxisType.Humidity,
            AxisType.EVT
        };

        public readonly AxisType[] PlotvarAxisALL = {
            AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp,
            AxisType.Wind, AxisType.Distance, AxisType.Wind,
            AxisType.Hours, AxisType.Solar, AxisType.UV,
            AxisType.Rain,  AxisType.Rrate,
            AxisType.Pressure, AxisType.Pressure,
            AxisType.Humidity, AxisType.Humidity,
            AxisType.DegreeDays, AxisType.DegreeDays, AxisType.EVT
        };

        public readonly AxisType[] PlotvarAxisEXTRA = {
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,
            AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,
            AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,
            AxisType.ppm,AxisType.ppm,AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.Temp,AxisType.Humidity,
            AxisType.Free
        };

        public readonly string[] PlotvarTypesRECENT = {
          "intemp", "dew", "apptemp", "feelslike", "wchill", "heatindex", "temp", "humidex",
          "wgust", "wspeed",
          "bearing", "avgbearing",
          "UV", "SolarRad", "CurrentSolarMax",
          "rfall", "rrate",
          "press",
          "hum", "inhum",
          "evapotranspiration"
        };

        public readonly string[] PlotvarTypesALL = {
          "minTemp", "maxTemp", "avgTemp", "windChill", "maxDew", "minDew", "maxFeels", "minFeels",
          "maxGust", "windRun", "maxWind",
          "sunHours", "solarRad", "uvi",
          "rain", "maxRainRate",
          "minBaro", "maxBaro",
          "minHum", "maxHum",
          "heatingdegreedays", "coolingdegreedays", "evapotranspiration"
        };

        // Static because needed in ExtraSensors
        public static string[] PlotvarTypesEXTRA = {
            "Temp1","Temp2","Temp3","Temp4","Temp5","Temp6","Temp7","Temp8","Temp9","Temp10",
            "Humidity1","Humidity2","Humidity3","Humidity4","Humidity5","Humidity6","Humidity7","Humidity8","Humidity9","Humidity10",
            "Dewpoint1","Dewpoint2","Dewpoint3","Dewpoint4","Dewpoint5","Dewpoint6","Dewpoint7","Dewpoint8","Dewpoint9","Dewpoint10",
            "SoilTemp1","SoilTemp2","SoilTemp3","SoilTemp4","SoilTemp5","SoilTemp6","SoilTemp7","SoilTemp8","SoilTemp9","SoilTemp10","SoilTemp11","SoilTemp12","SoilTemp13","SoilTemp14","SoilTemp15","SoilTemp16",
            "SoilMoisture1","SoilMoisture2","SoilMoisture3","SoilMoisture4","SoilMoisture5","SoilMoisture6","SoilMoisture7","SoilMoisture8","SoilMoisture9","SoilMoisture10","SoilMoisture11","SoilMoisture12","SoilMoisture13","SoilMoisture14","SoilMoisture15","SoilMoisture16",
            "AirQuality1","AirQuality2","AirQuality3","AirQuality4",
            "AirQualityAvg1","AirQualityAvg2","AirQualityAvg3","AirQualityAvg4",
            "UserTemp1","UserTemp2","UserTemp3","UserTemp4","UserTemp5","UserTemp6","UserTemp7","UserTemp8",
            "LeafWetness1","LeafWetness2","LeafWetness3","LeafWetness4","LeafWetness5","LeafWetness6","LeafWetness7","LeafWetness8",
            "CO2", "CO2_24h", "CO2_pm2p5", "CO2_pm2p5_24h","CO2_pm10","CO2_pm10_24h","CO2_temp","CO2_hum",
            "Lightning"
        };

        public readonly string[] DatafilesRECENT = {
          "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json", "tempdata.json",
          "winddata.json", "winddata.json",
          "wdirdata.json", "wdirdata.json",
          "solardata.json", "solardata.json", "solardata.json",
          "raindata.json", "raindata.json",
          "pressdata.json",
          "humdata.json", "humdata.json",
          "CUserdataRECENT.json"
        };

        public readonly string[] DatafilesALL = {
          "alldailytempdata.json","alldailytempdata.json","alldailytempdata.json","alldailytempdata.json",
          "alldailytempdata.json","alldailytempdata.json","alldailytempdata.json","alldailytempdata.json",
          "alldailywinddata.json","alldailywinddata.json", "alldailywinddata.json",
          "alldailysolardata.json","alldailysolardata.json", "alldailysolardata.json",
          "alldailyraindata.json","alldailyraindata.json",
          "alldailypressdata.json","alldailypressdata.json",
          "alldailyhumdata.json","alldailyhumdata.json",
          "CUserdataALL.json", "CUserdataALL.json", "CUserdataALL.json"
        };

        public readonly string[] DatafilesEXTRA = {
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json","extrasensorsdata.json",
            "extrasensorsdata.json"
        };

        public readonly string[] PlotvarKeywordRECENT = {
          "InsideTemp", "Dewpoint", "ApparentTemp", "FeelsLike", "WindChill", "HeatIndex", "Temperature", "Humidex",
          "WindGust", "WindSpeed",
          "Bearing", "AverageBearing",
          "UV", /*"SolarRadiation",*/ "CurrentSolarRad", "TheoreticalSolarMax",
          "RainFall", "RainRate",
          "Pressure",
          "Humidity", "InsideHumidity",
          "EvapoTranspiration"
        };

        public readonly string[] PlotvarKeywordALL = {
          "MinTemp", "MaxTemp", "AverageTemp", "AvgWindChill", /*"WindChill",*/ "MaxDewpoint", "MinDewpoint", "MaxFeelsLike", "MinFeelsLike",
          "MaxGust", "WindRun", "HighAvgWindSpeed", /* "WindSpeed",*/
          "SunHours", "SolarRadiation", "UVIndex",
          /*"RainFall",*/ "DayRain", "MaxRainRate",
          "MinBarometer", "MaxBarometer",
          "MinHumidity", "MaxHumidity",
          "HeatingDegreeDays","CoolingDegreeDays","DayEVT"      /*"EvapoTranspiration"*/
        };

        public static string[] PlotvarKeywordEXTRA = {
            "Temp1","Temp2","Temp3","Temp4","Temp5","Temp6","Temp7","Temp8","Temp9","Temp10",
            "Humidity1","Humidity2","Humidity3","Humidity4","Humidity5","Humidity6","Humidity7","Humidity8","Humidity9","Humidity10",
            "Dewpoint1","Dewpoint2","Dewpoint3","Dewpoint4","Dewpoint5","Dewpoint6","Dewpoint7","Dewpoint8","Dewpoint9","Dewpoint10",
            "SoilTemp1","SoilTemp2","SoilTemp3","SoilTemp4","SoilTemp5","SoilTemp6","SoilTemp7","SoilTemp8","SoilTemp9","SoilTemp10","SoilTemp11","SoilTemp12","SoilTemp13","SoilTemp14","SoilTemp15","SoilTemp16",
            "SoilMoisture1","SoilMoisture2","SoilMoisture3","SoilMoisture4","SoilMoisture5","SoilMoisture6","SoilMoisture7","SoilMoisture8","SoilMoisture9","SoilMoisture10","SoilMoisture11","SoilMoisture12","SoilMoisture13","SoilMoisture14","SoilMoisture15","SoilMoisture16",
            "AirQuality1","AirQuality2","AirQuality3","AirQuality4",
            "AirQualityAvg1","AirQualityAvg2","AirQualityAvg3","AirQualityAvg4",
            "UserTemp1","UserTemp2","UserTemp3","UserTemp4","UserTemp5","UserTemp6","UserTemp7","UserTemp8",
            "LeafWetness1","LeafWetness2","LeafWetness3","LeafWetness4","LeafWetness5","LeafWetness6","LeafWetness7","LeafWetness8",
            "CO2", "CO2_24h", "CO2_pm2p5", "CO2_pm2p5_24h","CO2_pm10","CO2_pm10_24h","CO2_temp","CO2_hum",
            "Lightning"
        };

        public readonly string[] ValidColumnRangeVars = {
              "MinTemp", "MaxTemp", "AverageTemp", "MaxDewpoint", "MinDewpoint", "MaxFeelsLike", "MinFeelsLike",
              "MinBarometer", "MaxBarometer",
              "MinHumidity", "MaxHumidity"
            };

        public AxisType[] PlotvarAxis;
        public string[] PlotvarUnits;
        public string[] PlotvarTypes;
        public string[] PlotvarKeyword;
        public string[] Datafiles;

        public readonly string[] PlotvarUnitsRECENT, PlotvarUnitsALL, PlotvarUnitsEXTRA;     // Init in constructor
        public readonly string[] LinetypeKeywords = { "Line", "SpLine", "Area", "Column", "Scatter", "ColumnRange" };
        public readonly string[] AxisKeywords = { "Temp", "Wind", "Distance", "Height", "Hours", "Solar", "UV", "Rain", "Rrate", "Pressure", "Humidity", "DegreeDays", "EVT", "Free", "AQ", "ppm", "SoilMoisture" };
        public readonly string[] StatsTypeKeywords = { "SMA" };

        private readonly string[] soilMoistureUnitArray = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
        private int soilMoistureUnitArrayIndex;

        readonly CuSupport Sup;
        readonly float MaxPressure, MinPressure;

        public string[] ClickEvents = new string[ 24 ] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };

        #endregion

        #region Constructor

        public ChartsCompiler( CuSupport s )
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
            PlotvarUnitsRECENT[ 16 ] = Sup.StationRain.Text() + Sup.PerHour;

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
            PlotvarUnitsALL[ 15 ] = Sup.StationRain.Text() + Sup.PerHour;

            PlotvarUnitsALL[ 16 ] = Sup.StationPressure.Text();
            PlotvarUnitsALL[ 17 ] = Sup.StationPressure.Text();

            PlotvarUnitsALL[ 18 ] = "%";
            PlotvarUnitsALL[ 19 ] = "%";

            PlotvarUnitsALL[ 20 ] = "degree days";
            PlotvarUnitsALL[ 21 ] = "degree days";
            PlotvarUnitsALL[ 22 ] = Sup.StationRain.Text();


            PlotvarUnitsEXTRA = new string[ PlotvarTypesEXTRA.Length ];
            //"Temp1","Temp2","Temp3","Temp4","Temp5","Temp6","Temp7","Temp8","Temp9","Temp10",
            for (int i=0; i<10; i++ )
                PlotvarUnitsEXTRA[ 0 + i] = Sup.StationTemp.Text();

            //"Humidity1","Humidity2","Humidity3","Humidity4","Humidity5","Humidity6","Humidity7","Humidity8","Humidity9","Humidity10",
            for ( int i=0; i < 10; i++ )
                PlotvarUnitsEXTRA[ 10 + i ] = "%";

            //"Dewpoint1","Dewpoint2","Dewpoint3","Dewpoint4","Dewpoint5","Dewpoint6","Dewpoint7","Dewpoint8","Dewpoint9","Dewpoint10",
            for ( int i=0; i < 10; i++ )
                PlotvarUnitsEXTRA[ 20 + i ] = Sup.StationTemp.Text();

            //"SoilTemp1","SoilTemp2","SoilTemp3","SoilTemp4","SoilTemp5","SoilTemp6","SoilTemp7","SoilTemp8","SoilTemp9","SoilTemp10","SoilTemp11","SoilTemp12","SoilTemp13","SoilTemp14","SoilTemp15","SoilTemp16",
            for ( int i=0; i < 16; i++ )
                PlotvarUnitsEXTRA[ 30 + i ] = Sup.StationTemp.Text();

            GetSoilMoistureUnitArray();

            //"SoilMoisture1","SoilMoisture2","SoilMoisture3","SoilMoisture4","SoilMoisture5","SoilMoisture6","SoilMoisture7","SoilMoisture8","SoilMoisture9","SoilMoisture10","SoilMoisture11","SoilMoisture12","SoilMoisture13","SoilMoisture14","SoilMoisture15","SoilMoisture16",
            for (int i = 0; i<16; i++ )
                PlotvarUnitsEXTRA[ 46 + i ] = soilMoistureUnitArray[ 0 + i ];

            //"AirQuality1","AirQuality2","AirQuality3","AirQuality4",
            //"AirQualityAvg1","AirQualityAvg2","AirQualityAvg3","AirQualityAvg4",
            for ( int i = 0; i < 8; i++ )
                PlotvarUnitsEXTRA[ 62 + i ] = "μg/m3";

            //"UserTemp1","UserTemp2","UserTemp3","UserTemp4","UserTemp5","UserTemp6","UserTemp7","UserTemp8",
            for ( int i = 0; i < 8; i++ )
                PlotvarUnitsEXTRA[ 70 + i ] = Sup.StationTemp.Text();

            //"LeafWetness1","LeafWetness2","LeafWetness3","LeafWetness4","LeafWetness5","LeafWetness6","LeafWetness7","LeafWetness8",
            for ( int i=0; i < 8; i++ )
                PlotvarUnitsEXTRA[ 78 + i ] = "";

            //"CO2", "CO2_24h", "CO2_pm2p5", "CO2_pm2p5_24h","CO2_pm10","CO2_pm10_24h","CO2_temp","CO2_hum",
            PlotvarUnitsEXTRA[ 86 ] = CO2conc.Text();
            PlotvarUnitsEXTRA[ 87 ] = CO2conc.Text();
            PlotvarUnitsEXTRA[ 88 ] = PMconc.Text();
            PlotvarUnitsEXTRA[ 89 ] = PMconc.Text();
            PlotvarUnitsEXTRA[ 90 ] = PMconc.Text();
            PlotvarUnitsEXTRA[ 91 ] = PMconc.Text();
            PlotvarUnitsEXTRA[ 92 ] = Sup.StationTemp.Text();
            PlotvarUnitsEXTRA[ 93 ] = "%";
            PlotvarUnitsEXTRA[ 94 ] = "count";

            //"Lightning"

            // Init the Compiler section in language file for the keywords just to  make sure they are there
            // Even if it is the millionth time or more... Certainly when adding more keywords later on.
            foreach ( string k in PlotvarKeywordRECENT )
                if ( !string.IsNullOrEmpty( k ) )
                    _ = Sup.GetCUstringValue( "Compiler", k, k, false );
            foreach ( string k in PlotvarKeywordALL )
                if ( !string.IsNullOrEmpty( k ) )
                    _ = Sup.GetCUstringValue( "Compiler", k, k, false );

            try
            {
                MaxPressure = Convert.ToSingle( Sup.GetAlltimeRecordValue( "Pressure", "highpressurevalue", "" ), CUtils.Inv );
                MinPressure = Convert.ToSingle( Sup.GetAlltimeRecordValue( "Pressure", "lowpressurevalue", "" ), CUtils.Inv );
            }
            catch
            {
                /* Don't take any action just make sure we can continue */
                Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Constructor - can't convert Min/Max barometer values: " +
                  $"{MinPressure}/{MaxPressure}" +
                  $"{Sup.GetAlltimeRecordValue( "Pressure", "highpressurevalue", "" )}/{Sup.GetAlltimeRecordValue( "Pressure", "lowpressurevalue", "" )}" );
            }

            // Prepare for possible ExternalExtraSensors!
            string[] ExternalExtraSensors = Sup.GetUtilsIniValue( "ExtraSensors", "ExternalExtraSensors", "" ).Split( GlobConst.CommaSeparator );

            if ( !string.IsNullOrEmpty( ExternalExtraSensors[ 0 ] ) )
            {
                // There is a lot to optimize here I guess
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

            // Prepare for possible CustomLogs!

            CustomLogs thisCustomLogs = new CustomLogs( Sup );

            if ( thisCustomLogs.CustomLogsList.Count != 0 )
            {
                // There is a lot to optimize here I guess
                foreach ( CustomLogs.CustomLog thisList in thisCustomLogs.CustomLogsList )
                {
                    if ( thisList.Frequency == -1 )
                    {
                        foreach ( string webtag in thisList.TagNames )
                        {
                            List<string> tmpStr;

                            List<AxisType> tmp = PlotvarAxisALL.ToList();
                            tmp.Add( thisCustomLogs.WebTags.GetTagAxis( webtag ) );
                            PlotvarAxisALL = tmp.ToArray();

                            tmpStr = PlotvarUnitsALL.ToList();
                            tmpStr.Add( thisCustomLogs.WebTags.GetTagUnit( webtag ) );
                            PlotvarUnitsALL = tmpStr.ToArray();

                            tmpStr = PlotvarTypesALL.ToList();
                            tmpStr.Add( thisList.Name + webtag );
                            PlotvarTypesALL = tmpStr.ToArray();

                            tmpStr = PlotvarKeywordALL.ToList();
                            tmpStr.Add( thisList.Name + webtag );
                            PlotvarKeywordALL = tmpStr.ToArray();

                            tmpStr = DatafilesALL.ToList();
                            tmpStr.Add( Sup.CustomLogsDailyJSON );
                            DatafilesALL = tmpStr.ToArray();
                        }
                    }
                    else
                    {
                        foreach ( string webtag in thisList.TagNames )
                        {
                            List<string> tmpStr;
                            //string w = char.IsDigit( webtag, webtag.Length - 1 ) ? webtag.Substring(0, webtag.Length - 1) : webtag;
                            string w = webtag;

                            List<AxisType> tmp = PlotvarAxisEXTRA.ToList();
                            tmp.Add( thisCustomLogs.WebTags.GetTagAxis( w ) );
                            PlotvarAxisEXTRA = tmp.ToArray();

                            tmpStr = PlotvarUnitsEXTRA.ToList();
                            tmpStr.Add( thisCustomLogs.WebTags.GetTagUnit( w ) );
                            PlotvarUnitsEXTRA = tmpStr.ToArray();

                            tmpStr = PlotvarTypesEXTRA.ToList();
                            tmpStr.Add( thisList.Name + webtag );
                            PlotvarTypesEXTRA = tmpStr.ToArray();

                            tmpStr = PlotvarKeywordEXTRA.ToList();
                            tmpStr.Add( thisList.Name + webtag );
                            PlotvarKeywordEXTRA = tmpStr.ToArray();

                            tmpStr = DatafilesEXTRA.ToList();
                            tmpStr.Add( Sup.CustomLogsRecentJSON );
                            DatafilesEXTRA = tmpStr.ToArray();
                        }
                    }
                }
            }

            // Is there need to destroy the CustomLogs object?? Don't think so...
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
            double Latitude = Convert.ToDouble( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );

            double Gamma = 0.796 - 0.01 * Math.Sin( 0.986 * ( i + 284 ) * Deg2Rad );
            double EarthSunDist = 1 + 0.034 * Math.Cos( ( i - 2 ) * Deg2Rad );
            double Delta = 23.45 * Math.Sin( 0.986 * ( i + 284 ) * Deg2Rad );
            double HeightOfSun = Math.Asin( Math.Sin( Latitude * Deg2Rad ) * Math.Sin( Delta * Deg2Rad ) + Math.Cos( Latitude * Deg2Rad ) * Math.Cos( Delta * Deg2Rad ) );
            double ExponentialComponent = Math.Exp( -0.13 / Math.Sin( HeightOfSun ) ) * Math.Sin( HeightOfSun );

            // Return the total estimation and add 50 to make sure the scaling has some space
            //
            Estimation = (int) ( SolarConstant * Gamma * EarthSunDist * ExponentialComponent ) + 50;

            return Estimation;
        }

        private void GetSoilMoistureUnitArray()
        {
            const string SoilMoisture = "\"soilmoisture\":{\"units\":[";
            string tmp;

            CmxIPC thisCmxIPC = new CmxIPC( CUtils.Sup, CUtils.Isup );

            Task<string> AsyncTask = thisCmxIPC.GetCMXGraphdataAsync( "graphconfig.json" );
            AsyncTask.Wait();
            tmp = AsyncTask.Result;

            if ( tmp.Length < 1 ) return; // No string returned, CMX probably not running

            soilMoistureUnitArrayIndex = tmp.IndexOf( SoilMoisture ) + SoilMoisture.Length;

            int i = 0;
            int a = soilMoistureUnitArrayIndex;

            for ( ; tmp[ a ] != ']'; a++ )
            {
                while ( tmp[ a ] != '"' && tmp[ a ] != ',' )
                    soilMoistureUnitArray[ i ] += tmp[ a++ ];

                if ( tmp[ a ] == ',' ) { i++; soilMoistureUnitArray[ i ] = ""; }
            }
        }

        #endregion
    } // Class DefineCharts
}// Namespace
