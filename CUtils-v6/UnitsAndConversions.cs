﻿/*
 * UnitsAndConversions - Part of CumulusUtils
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
 */

namespace CumulusUtils
{
    /*
     * These classes contain the units and the correspponding texts and provide the conversiones between the different units 
     * (e.g. celsius -> fahrenheit and v.v. and m/s -> km/h and v.v. etc...)
     * Each class has the following methods:
     *   1) Constructor(TempDim) which defines the default dimension of this instance (e.g. °C , mm or hPa etc...)
     *   2) UnitText() or UnitText(TempDim) outputs a string for the unit either the default (no argument) or for the specified Dimension (argument)
     *   3) Convert( val, dim) converts the value (in the default/defined dimension to the given dimension. If dim is equal to CreationTime dimension no conversion is made
     *   
     */

    public enum DistanceDim { meter, mile, kilometer, nauticalmile }
    public enum RainDim { millimeter, inch }
    public enum PressureDim { millibar, hectopascal, inchHg }
    public enum WindDim { ms, mph, kmh, knots }
    public enum TempDim { celsius, fahrenheit }

    public class Temp
    {
        static string[] UnitTempText { get; } = { "°C", "°F" };
        readonly public TempDim Dim;

        public Temp( TempDim t ) { Dim = t; } // Constructor

        public string Text() { return UnitTempText[ (int) Dim ]; }
        public string Text( TempDim t ) { return UnitTempText[ (int) t ]; }

        public double Convert( TempDim from, TempDim to, double val )
        {
            if ( from == TempDim.fahrenheit )
                if ( to == TempDim.fahrenheit )
                    return val;
                else
                    return ( val - 32 ) / 1.8;
            else // Dim must be celsius
              if ( to == TempDim.celsius )
                return val;
            else
                return val * 1.8 + 32;
        }

        public string Format( float value ) => $"{value:F1}";
    }

    public class Wind
    {
        string[] UnitWindText { get; } = { "m/s", "mph", "km/h", "kts" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,     2.23694, 3.6,     1.94384 } ,  // m/s to mph, km/h, kts
      { 0.44704, 1.0,     1.60934, 0.868976} ,  // mph to m/s, km/h, kts
      { 0.277778,0.621371,1.0,     0.539957} ,  // kmh to m/s, mph, kts
      { 0.514444,1.15078, 1.852,   1.0}         // kts to m/s, mph, km/h
    };

        readonly public WindDim Dim;

        public Wind( WindDim w ) { Dim = w; }

        public string Text() { return UnitWindText[ (int) Dim ]; }
        public string Text( WindDim w ) { return UnitWindText[ (int) w ]; }

        public double Convert( WindDim from, WindDim to, double val )
        {
            return val * ConversionFactors[ (int) from, (int) to ];
        }

        public string Format( float value ) => $"{value:F1}";
    }

    public class Rain
    {
        string[] UnitRainText { get; } = { "mm", "in" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,  0.0393701 } ,  // mm to in
      { 25.4, 1.0 }          // in to mm
    };

        public readonly RainDim Dim;
        public Rain( RainDim w ) { Dim = w; }

        public string Text() { return UnitRainText[ (int) Dim ]; }
        public string Text( RainDim r ) { return UnitRainText[ (int) r ]; }

        public double Convert( RainDim from, RainDim to, double val )
        {
            return val * ConversionFactors[ (int) from, (int) to ];
        }

        public string Format( float value )
        {
            if ( Dim == RainDim.inch ) return $"{value:F2}";
            else return $"{value:F1}";
        }
    }

    public class Pressure
    {
        string[] UnitPressureText { get; } = { "mb", "hPa", "inHg" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,     1.0,     0.02953 } ,   // mb to hPa, inHg
      { 1.0,     1.0,     0.02953 } ,   // hPa to mb, inHg
      { 33.8639, 33.8639, 1.0 }         // inHg to mb, hPa
    };

        readonly public PressureDim Dim;
        public Pressure( PressureDim p ) { Dim = p; }

        public string Text() { return UnitPressureText[ (int) Dim ]; }
        public string Text( PressureDim p ) { return UnitPressureText[ (int) p ]; }

        public double Convert( PressureDim from, PressureDim to, double val )
        {
            return val * ConversionFactors[ (int) from, (int) to ];
        }

        public string Format( float value )
        {
            if ( Dim == PressureDim.inchHg ) return $"{value:F2}";
            else return $"{value:F0}";
        }
    }

    public class Distance
    {
        string[] UnitDistanceText { get; } = { "km", "mi", "km", "nm" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,     0.621371, 1.0,     0.539957 } ,  // m to mi, km, nm
      { 1.60934, 1.0,      1.60934, 0.868976 } ,  // mi to km, km, nm
      { 1.0,     0.621371, 1.0,     0.539957 } ,  // km to km, mp, nm
      { 1.852,   1.15078,  1.852,   1.0}          // nm to km, km, mi
    };

        readonly public DistanceDim Dim;

        public Distance( DistanceDim d ) { Dim = d; }

        public string Text() { return UnitDistanceText[ (int) Dim ]; }
        public string Text( WindDim d ) { return UnitDistanceText[ (int) d ]; }

        public double Convert( DistanceDim from, DistanceDim to, double val )
        {
            return val * ConversionFactors[ (int) from, (int) to ];
        }

        public string Format( float value ) => $"{value:F1}";
    }
}