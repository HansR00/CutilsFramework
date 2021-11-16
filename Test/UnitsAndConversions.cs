/*
 * CuSupport - Part of CumulusUtils
 *
 * © Copyright 2019 - 2020 Hans Rottier <hans.rottier@gmail.com>
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
 *              Initial release: Website Generator (3.0) 
 *              Branched to 4.0.0 on 27 july 2020 to accomodate CMX version 3.7.0
 *              
 * Environment: Raspberry 3B+
 *              Raspbian / Linux 
 *              C# / Visual Studio
 *              
 */

namespace TestSpace
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
    public enum RainDim { millemeter, inch }
    public enum PressureDim { millibar, hectopascal, inchHg }
    public enum WindDim { ms, mph, kmh, knots }
    public enum TempDim { celsius, fahrenheit }

    public class Temp
    {
        static string[] UnitTempText { get; } = { " °C", " °F" };
        readonly TempDim Dim;

        public Temp( TempDim t ) { Dim = t; } // Constructor

        public string Text() { return UnitTempText[ (int) Dim ]; }
        public string Text( TempDim t ) { return UnitTempText[ (int) t ]; }

        public double Convert( TempDim t, double val )
        {
            if ( Dim == TempDim.fahrenheit )
                if ( t == TempDim.fahrenheit ) return val;
                else return ( ( val - 32 ) / 1.8 );
            else // Dim must be celsius
              if ( t == TempDim.celsius ) return val;
            else return ( val * 1.8 + 32 );
        }
    }

    public class Wind
    {
        string[] UnitWindText { get; } = { " m/s", " mph", " km/h", " kts" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,     2.23694, 3.6,     1.94384 } ,  // m/s to mph, km/h, kts
      { 0.44704, 1.0,     1.60934, 0.868976} ,  // mph to m/s, km/h, kts
      { 0.277778,0.621371,1.0,     0.539957} ,  // kmh to m/s, mph, kts
      { 0.514444,1.15078, 1.852,   1.0}         // kts to m/s, mph, km/h
    };

        readonly WindDim Dim;

        public Wind( WindDim w ) { Dim = w; }

        public string Text() { return UnitWindText[ (int) Dim ]; }
        public string Text( WindDim w ) { return UnitWindText[ (int) w ]; }

        public double Convert( WindDim w, double val )
        {
            return val * ConversionFactors[ (int) Dim, (int) w ];
        }
    }

    public class Rain
    {
        string[] UnitRainText { get; } = { " mm", " in" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,  0.0393701 } ,  // mm to in
      { 25.4, 1.0 }          // in to mm
    };

        readonly RainDim Dim;
        public Rain( RainDim w ) { Dim = w; }

        public string Text() { return UnitRainText[ (int) Dim ]; }
        public string Text( RainDim r ) { return UnitRainText[ (int) r ]; }

        public double Convert( RainDim r, double val )
        {
            return val * ConversionFactors[ (int) Dim, (int) r ];
        }

    }

    public class Pressure
    {
        string[] UnitPressureText { get; } = { " mb", " hPa", " inHg" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,     1.0,     0.02953 } ,   // mb to hPa, inHg
      { 1.0,     1.0,     0.02953 } ,   // hPa to mb, inHg
      { 33.8639, 33.8639, 1.0 }         // inHg to mb, hPa
    };

        readonly PressureDim Dim;
        public Pressure( PressureDim p ) { Dim = p; }

        public string Text() { return UnitPressureText[ (int) Dim ]; }
        public string Text( PressureDim p ) { return UnitPressureText[ (int) p ]; }

        public double Convert( PressureDim p, double val )
        {
            return val * ConversionFactors[ (int) Dim, (int) p ];
        }
    }

    //string[] DistanceDimText { get; } = { " km", " mi", " km", " nm" };

    public class Distance
    {
        string[] UnitDistanceText { get; } = { " km", " mph", " km", " nm" };

        readonly double[,] ConversionFactors =
        {
      { 1.0,     0.621371, 1.0,     0.539957 } ,  // m to mi, km, nm
      { 1.60934, 1.0,      1.60934, 0.868976 } ,  // mi to km, km, nm
      { 1.0,     0.621371, 1.0,     0.539957 } ,  // km to km, mp, nm
      { 1.852,   1.15078,  1.852,   1.0}          // nm to km, km, mi
    };

        readonly DistanceDim Dim;

        public Distance( DistanceDim d ) { Dim = d; }

        public string Text() { return UnitDistanceText[ (int) Dim ]; }
        public string Text( WindDim d ) { return UnitDistanceText[ (int) d ]; }

        public double Convert( DistanceDim d, double val )
        {
            return val * ConversionFactors[ (int) Dim, (int) d ];
        }
    }

}







