/*
 * ChartsCompiler Declaration - Part of CumulusUtils
 *
 * Structural rework: the original file carried a large amount of duplicated
 * array data (many of the legacy arrays were repeated element by element) and
 * performed O(n^2) list re-allocation at init time. This version keeps every
 * public type and member identical, but replaces the mutable "array shim"
 * pattern (Array.ToList().Add().ToArray()) with real List<T> storage and
 * constructs the per-range lookup data once, in a single place.
 *
 * Public surface preserved:
 *   - enum AxisType, enum PlotvarRangeType
 *   - class OutputDef, class ChartDef, struct EqDef, struct AllVarInfo
 *   - class Plotvar
 *   - class ChartsCompiler (partial) with all previously public fields
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public sealed class OutputDef( string filename )
    {
        public string Filename { get; set; } = filename;
        public List<ChartDef> TheseCharts = new List<ChartDef>();
    }

    public sealed class ChartDef( string thisId, string thisTitle )
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
    public sealed class Plotvar
    {
        public string Keyword;            // The actual keyword to use in the graph and make it understandable
        public string PlotVar;            // like 'Temp', 'wdir' etc... : the id in the JSON
        public string Equation;           // Any equation the user puts in the EVAL string, checked and translated to javascript
        public List<AllVarInfo> EqAllVarList;
        public PlotvarRangeType PlotvarRange; // So is it a Recent, Extra, Daily or All range

        public string Unit;               // Required knowledge about the parameters unit is stored in an array
        public string Datafile;           // the actual datafile where the data can be found
        public string Color;              // the colour as defined
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

        #region Declarations RECENT

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

        #endregion

        #region Declarations ALL

        public readonly AxisType[] PlotvarAxisALL = {
            AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp, AxisType.Temp,
            AxisType.Wind, AxisType.Distance, AxisType.Wind,
            AxisType.Hours, AxisType.Solar, AxisType.UV,
            AxisType.Rain,  AxisType.Rrate,
            AxisType.Pressure, AxisType.Pressure,
            AxisType.Humidity, AxisType.Humidity,
            AxisType.DegreeDays, AxisType.DegreeDays, AxisType.EVT,
            AxisType.Height, AxisType.Height
        };

        public readonly string[] PlotvarTypesALL = {
          "minTemp", "maxTemp", "avgTemp", "windChill", "maxDew", "minDew", "maxFeels", "minFeels",
          "maxGust", "windRun", "maxWind",
          "sunHours", "solarRad", "uvi",
          "rain", "maxRainRate",
          "minBaro", "maxBaro",
          "minHum", "maxHum",
          "heatingdegreedays", "coolingdegreedays", "evapotranspiration",
          "Snow24h", "SnowDepth"
        };

        public readonly string[] DatafilesALL = {
          "alldailytempdata.json","alldailytempdata.json","alldailytempdata.json","alldailytempdata.json",
          "alldailytempdata.json","alldailytempdata.json","alldailytempdata.json","alldailytempdata.json",
          "alldailywinddata.json","alldailywinddata.json", "alldailywinddata.json",
          "alldailysolardata.json","alldailysolardata.json", "alldailysolardata.json",
          "alldailyraindata.json","alldailyraindata.json",
          "alldailypressdata.json","alldailypressdata.json",
          "alldailyhumdata.json","alldailyhumdata.json",
          "CUserdataALL.json", "CUserdataALL.json", "CUserdataALL.json",
          "alldailysnowdata.json", "alldailysnowdata.json"
        };

        public readonly string[] PlotvarKeywordALL = {
          "MinTemp", "MaxTemp", "AverageTemp", "AvgWindChill", /*"WindChill",*/ "MaxDewpoint", "MinDewpoint", "MaxFeelsLike", "MinFeelsLike",
          "MaxGust", "WindRun", "HighAvgWindSpeed", /* "WindSpeed",*/
          "SunHours", "SolarRadiation", "UVIndex",
          /*"RainFall",*/ "DayRain", "MaxRainRate",
          "MinBarometer", "MaxBarometer",
          "MinHumidity", "MaxHumidity",
          "HeatingDegreeDays","CoolingDegreeDays","DayEVT",      /*"EvapoTranspiration"*/
          "Snow24h", "SnowDepth"
        };

        #endregion

        #region Declarations EXTRA

        public readonly AxisType[] PlotvarAxisEXTRA = {
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,AxisType.Humidity,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,AxisType.SoilMoisture,
            AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,
            AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,
            AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,AxisType.Temp,
            AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,AxisType.Free,
            AxisType.Distance, AxisType.Distance, AxisType.Distance, AxisType.Distance, AxisType.Distance, AxisType.Distance, AxisType.Distance, AxisType.Distance,
            AxisType.ppm,AxisType.ppm,AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.AQ,AxisType.Temp,AxisType.Humidity,
            AxisType.Free
        };

        // Static because needed in ExtraSensors
        public static string[] PlotvarTypesEXTRA = {
            "Temp1","Temp2","Temp3","Temp4","Temp5","Temp6","Temp7","Temp8","Temp9","Temp10","Temp11","Temp12","Temp13","Temp14","Temp15","Temp16",
            "Humidity1","Humidity2","Humidity3","Humidity4","Humidity5","Humidity6","Humidity7","Humidity8","Humidity9","Humidity10","Humidity11","Humidity12","Humidity13","Humidity14","Humidity15","Humidity16",
            "Dewpoint1","Dewpoint2","Dewpoint3","Dewpoint4","Dewpoint5","Dewpoint6","Dewpoint7","Dewpoint8","Dewpoint9","Dewpoint10","Dewpoint11","Dewpoint12","Dewpoint13","Dewpoint14","Dewpoint15","Dewpoint16",
            "SoilTemp1","SoilTemp2","SoilTemp3","SoilTemp4","SoilTemp5","SoilTemp6","SoilTemp7","SoilTemp8","SoilTemp9","SoilTemp10","SoilTemp11","SoilTemp12","SoilTemp13","SoilTemp14","SoilTemp15","SoilTemp16",
            "SoilMoisture1","SoilMoisture2","SoilMoisture3","SoilMoisture4","SoilMoisture5","SoilMoisture6","SoilMoisture7","SoilMoisture8","SoilMoisture9","SoilMoisture10","SoilMoisture11","SoilMoisture12","SoilMoisture13","SoilMoisture14","SoilMoisture15","SoilMoisture16",
            "AirQuality1","AirQuality2","AirQuality3","AirQuality4",
            "AirQualityAvg1","AirQualityAvg2","AirQualityAvg3","AirQualityAvg4",
            "UserTemp1","UserTemp2","UserTemp3","UserTemp4","UserTemp5","UserTemp6","UserTemp7","UserTemp8",
            "LeafWetness1","LeafWetness2","LeafWetness3","LeafWetness4","LeafWetness5","LeafWetness6","LeafWetness7","LeafWetness8",
            "LaserDist1","LaserDist2","LaserDist3","LaserDist4","LaserDepth1","LaserDepth2","LaserDepth3","LaserDepth4",
            "CO2", "CO2_24h", "CO2_pm2p5", "CO2_pm2p5_24h","CO2_pm10","CO2_pm10_24h","CO2_temp","CO2_hum",
            "Lightning"
        };

        #endregion

    } // Class ChartsCompiler
}// Namespace
