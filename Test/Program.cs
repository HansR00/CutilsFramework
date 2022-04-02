﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        
        static void Main( string[] args )
        {
            Distance TestDistance = new Distance(DistanceDim.kilometer);
            Temp TestTemp = new Temp(TempDim.celsius);
            Wind TestWind = new Wind( WindDim.kmh );
            Rain TestRain = new Rain( RainDim.millimeter );
            Pressure TestPress = new Pressure( PressureDim.inchHg);

            Console.WriteLine( $"TestRain: {TestRain.Dim}" );
            Console.WriteLine( $"TestRain Converting from 25.4 mm to inch: {TestRain.Convert( RainDim.millimeter, RainDim.inch, 25.4 ):F4}" );
            Console.WriteLine( $"TestRain Converting from 1 inch to mm: {TestRain.Convert( RainDim.inch, RainDim.millimeter, 1 ):F4}" );
            Console.WriteLine( $"TestRain converting from 10 mm to inch: {TestRain.Convert( RainDim.millimeter, RainDim.inch, 10 )}" );
            Console.WriteLine( $"TestRain converting from 10 inch to mm: {TestRain.Convert( RainDim.inch, RainDim.millimeter, 10 )}" );
            Console.WriteLine( "" );

            Console.WriteLine( $"TestPressure: {TestPress.Dim}" );
            Console.WriteLine( $"TestPressure Converting from 1000 mb to inHg: {TestPress.Convert( PressureDim.millibar, PressureDim.inchHg, 1000 ):F4}" );
            Console.WriteLine( $"TestPressure Converting from 1000 mb to hPa: {TestPress.Convert( PressureDim.millibar, PressureDim.hectopascal, 1000 ):F4}" );
            Console.WriteLine( $"TestPressure Converting from 76 inHg to hPa: {TestPress.Convert( PressureDim.inchHg, PressureDim.hectopascal, 76 ):F4}" );
            Console.WriteLine( "" );

            Console.WriteLine( $"TestDistance: {TestDistance.Dim}" );
            Console.WriteLine( $"TestDistance converting from 1 km to mi: {TestDistance.Convert( DistanceDim.kilometer, DistanceDim.mile, 1 )}" );
            Console.WriteLine( $"TestDistance converting from 1 km to nm: {TestDistance.Convert( DistanceDim.kilometer, DistanceDim.nauticalmile, 1 )}" );
            Console.WriteLine( $"TestDistance converting from 1 km to m: {TestDistance.Convert( DistanceDim.kilometer, DistanceDim.meter, 1 )}" );
            Console.WriteLine( $"TestDistance converting from 1 mi to km: {TestDistance.Convert( DistanceDim.mile, DistanceDim.kilometer, 1 )}" );
            Console.WriteLine( $"" );

            Console.WriteLine( $"TestTemp: {TestTemp.Dim}" );
            Console.WriteLine( $"TestTemp converting from 0 C to F: {TestTemp.Convert( TempDim.celsius, TempDim.fahrenheit, 0 )}" );
            Console.WriteLine( $"TestTemp converting from 32 F to C: {TestTemp.Convert( TempDim.fahrenheit, TempDim.celsius, 32 )}" );
            Console.WriteLine( $"TestTemp converting from 100 C to F: {TestTemp.Convert( TempDim.celsius, TempDim.fahrenheit, 100 )}" );
            Console.WriteLine( $"TestTemp converting from 0 F to C: {TestTemp.Convert( TempDim.fahrenheit, TempDim.celsius, 0 )}" );
            Console.WriteLine( $"" );
            Console.WriteLine( $"" );
            Console.WriteLine( $"" );
            Console.WriteLine( $"" );
            Console.WriteLine( $"" );
            Console.WriteLine( $"" );
            Console.ReadKey();
        }
    }
}
