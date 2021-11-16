/*
 * Add2Cumulus for CumulusMX
 *
 * © Copyright 2019  Hans Rottier <hans.rottier@gmail.com>
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

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage( "Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrHumidityData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrPressionData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrRainData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrTempData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrWindData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrWindrunData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.CuSupport.Languages" )]
[assembly: SuppressMessage( "Style", "IDE0057:Use range operator", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.IniFile.Refresh" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.DavisInfoFromCMX.battery" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.DavisInfoFromCMX.txbattery" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.InfoFromCMX.build" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.InfoFromCMX.version" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.StructFWI.dayFWI" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:CumulusUtils.Top10.compareHighestMonthlyRain" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:CumulusUtils.Top10.compareLongestDryPeriod" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:CumulusUtils.Top10.compareLongestWetPeriod" )]
[assembly: SuppressMessage( "Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CMXutils.RealMainAsync(System.String[])~System.Threading.Tasks.Task" )]
[assembly: SuppressMessage( "Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CMXutils.Main(System.String[])~System.Threading.Tasks.Task" )]
[assembly: SuppressMessage( "Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.NOAAdisplay.GenerateNOAATxtfile(System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.StationMap.GenerateStationMap" )]
[assembly: SuppressMessage( "Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Website.GenerateCUlib" )]
[assembly: SuppressMessage( "Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CmxIPC.GetCMXInfoAsync~System.Threading.Tasks.Task{CumulusUtils.InfoFromCMX}" )]
[assembly: SuppressMessage( "Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CmxIPC.GetDavisInfoAsync~System.Threading.Tasks.Task{CumulusUtils.DavisInfoFromCMX}" )]
[assembly: SuppressMessage( "Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CmxIPC.GetSingleWebtagValueFromCMXAsync(System.String)~System.Threading.Tasks.Task{System.String}" )]
[assembly: SuppressMessage( "Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CmxIPC.ReplaceWebtagsPostAsync(System.String)~System.Threading.Tasks.Task{System.String}" )]
[assembly: SuppressMessage( "Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.PwsFWI.AddPrediction(System.String,System.Collections.Generic.List{CumulusUtils.DayfileValue})~System.Threading.Tasks.Task{System.Boolean}" )]
[assembly: SuppressMessage( "Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.PwsFWI.CalculatePwsFWI(System.Collections.Generic.List{CumulusUtils.DayfileValue})~System.Threading.Tasks.Task" )]
[assembly: SuppressMessage( "Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.UserReports.DoUserReports~System.Threading.Tasks.Task" )]
[assembly: SuppressMessage( "Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.WeatherForecasts.GenerateForecasts~System.Threading.Tasks.Task" )]
[assembly: SuppressMessage( "Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Website.GenerateWebsite~System.Threading.Tasks.Task" )]
