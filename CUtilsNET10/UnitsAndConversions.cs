/*
 * UnitsAndConversions - Part of CumulusUtils
 *
 */

namespace CumulusUtils
{
    /*
     * Unit wrappers: each class holds the station default dimension, unit text,
     * and conversion between dimensions (e.g. °C ↔ °F, m/s ↔ km/h).
     *
     *  1) Constructor(dim) sets the default dimension for this instance
     *  2) Text() / Text(dim) returns the unit string
     *  3) Convert(from, to, val) converts; identity when from == to
     */

    public enum TempDim { celsius, fahrenheit }
    public enum WindDim { ms, mph, kmh, knots }
    public enum RainDim { millimeter, inch }
    public enum PressureDim { millibar, hectopascal, inchHg }
    public enum DistanceDim { meter, mile, kilometer, nauticalmile }
    public enum LaserDim { centimeter, inch }
    public enum HeightDim { meter, feet }

    public class Temp( TempDim t )
    {
        private static readonly string[] UnitTempText = [ "°C", "°F" ];

        public readonly TempDim Dim = t;

        public string Text() => UnitTempText[ (int) Dim ];
        public string Text( TempDim t ) => UnitTempText[ (int) t ];

        public double Convert( TempDim from, TempDim to, double val )
        {
            if ( from == to )
                return val;

            return from == TempDim.fahrenheit
                ? ( val - 32 ) / 1.8
                : val * 1.8 + 32;
        }

        public static int NrOfDecimals => 1;
    }

    public class Wind
    {
        private static readonly double[,] ConversionFactors =
        {
            { 1.0,      2.23694,  3.6,      1.94384  },  // m/s  → mph, km/h, kts
            { 0.44704,  1.0,      1.60934,  0.868976 },  // mph  → m/s, km/h, kts
            { 0.277778, 0.621371, 1.0,      0.539957 },  // km/h → m/s, mph,  kts
            { 0.514444, 1.15078,  1.852,    1.0      }   // kts  → m/s, mph,  km/h
        };

        private readonly string[] unitWindText = [ "m/s", "mph", "km/h", "kts" ];

        public readonly WindDim Dim;

        public Wind( WindDim w, CuSupport s )
        {
            Dim = w;
            unitWindText[ 2 ] = $"km{s.PerHour}";
        }

        public string Text() => unitWindText[ (int) Dim ];
        public string Text( WindDim w ) => unitWindText[ (int) w ];

        public double Convert( WindDim from, WindDim to, double val )
            => from == to ? val : val * ConversionFactors[ (int) from, (int) to ];

        public static int NrOfDecimals => 1;
    }

    public class Distance( DistanceDim d )
    {
        // When speed is m/s, distance is expressed in km
        private static readonly string[] UnitDistanceText = [ "m", "mi", "km", "nm" ];

        private static readonly double[,] ConversionFactors =
        {
            { 1.0,     0.000621371, 0.001,   0.000539957 },  // m  → mi, km, nm
            { 1609.34, 1.0,         1.60934, 0.868976    },  // mi → m,  km, nm
            { 1000,    0.621371,    1.0,     0.539957    },  // km → m,  mi, nm
            { 1852,    1.15078,     1.852,   1.0         }   // nm → m,  mi, km
        };

        public readonly DistanceDim Dim = d;

        public string Text() => UnitDistanceText[ (int) Dim ];
        public string Text( DistanceDim d ) => UnitDistanceText[ (int) d ];

        public double Convert( DistanceDim from, DistanceDim to, double val )
            => from == to ? val : val * ConversionFactors[ (int) from, (int) to ];

        public static int NrOfDecimals => 1;
    }

    public class LaserDist( LaserDim d )
    {
        private static readonly string[] UnitLaserDistText = [ "cm", "in" ];

        private static readonly double[,] ConversionFactors =
        {
            { 1.0,  0.393701 },  // cm → in
            { 2.54, 1.0      }   // in → cm
        };

        public readonly LaserDim Dim = d;

        public string Text() => UnitLaserDistText[ (int) Dim ];
        public string Text( LaserDim d ) => UnitLaserDistText[ (int) d ];

        public double Convert( LaserDim from, LaserDim to, double val )
            => from == to ? val : val * ConversionFactors[ (int) from, (int) to ];

        public int NrOfDecimals() => Dim == LaserDim.inch ? 2 : 1;
    }

    public class Rain( RainDim w )
    {
        private static readonly string[] UnitRainText = [ "mm", "in" ];

        private static readonly double[,] ConversionFactors =
        {
            { 1.0,  0.0393701 },  // mm → in
            { 25.4, 1.0       }   // in → mm
        };

        public readonly RainDim Dim = w;

        public string Text() => UnitRainText[ (int) Dim ];
        public string Text( RainDim r ) => UnitRainText[ (int) r ];

        public double Convert( RainDim from, RainDim to, double val )
            => from == to ? val : val * ConversionFactors[ (int) from, (int) to ];

        public int NrOfDecimals() => Dim == RainDim.inch ? 2 : 1;
    }

    public class Pressure( PressureDim p )
    {
        private static readonly string[] UnitPressureText = [ "mb", "hPa", "inHg" ];

        private static readonly double[,] ConversionFactors =
        {
            { 1.0,     1.0,     0.02953 },  // mb   → hPa, inHg
            { 1.0,     1.0,     0.02953 },  // hPa  → mb,  inHg
            { 33.8639, 33.8639, 1.0     }   // inHg → mb,  hPa
        };

        public readonly PressureDim Dim = p;

        public string Text() => UnitPressureText[ (int) Dim ];
        public string Text( PressureDim p ) => UnitPressureText[ (int) p ];

        public double Convert( PressureDim from, PressureDim to, double val )
            => from == to ? val : val * ConversionFactors[ (int) from, (int) to ];

        public int NrOfDecimals() => Dim == PressureDim.inchHg ? 2 : 0;
    }

    public class Height( HeightDim d )
    {
        private static readonly string[] UnitHeightText = [ "m", "ft" ];

        private static readonly double[,] ConversionFactors =
        {
            { 1.0,    3.28084 },  // m  → ft
            { 0.3048, 1.0     }   // ft → m
        };

        public readonly HeightDim Dim = d;

        public string Text() => UnitHeightText[ (int) Dim ];
        public string Text( HeightDim d ) => UnitHeightText[ (int) d ];

        public double Convert( HeightDim from, HeightDim to, double val )
            => from == to ? val : val * ConversionFactors[ (int) from, (int) to ];

        public static int NrOfDecimals => 1;
    }

    public static class CO2conc
    {
        public static string Text() => "ppm";
    }

    public static class PMconc
    {
        public static string Text() => "μg/m3";
    }
}
