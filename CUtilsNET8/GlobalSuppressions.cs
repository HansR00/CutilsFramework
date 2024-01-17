/*
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

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.StructFWI.dayFWI" )]
[assembly: SuppressMessage( "Style", "IDE0054:Use compound assignment", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CuExtensions.CountFlags(CumulusUtils.AxisType)~System.UInt64" )]
[assembly: SuppressMessage( "Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Top10.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0090:Use 'new(...)'", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrWindData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.InetSupport.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.InetSupport.UploadFileAsync(System.String,System.String)~System.Threading.Tasks.Task{System.Boolean}" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.AirLink.Concentrations" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.AirLink.Series" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.AxisKeywords" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.Brackets" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.ClickEvents" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.DatafilesALL" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.DatafilesEXTRA" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.DatafilesRECENT" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.Functions" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.LinetypeKeywords" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.Operators" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarAxisALL" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarAxisEXTRA" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarAxisRECENT" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarKeywordALL" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarKeywordEXTRA" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarKeywordRECENT" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarTypesALL" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarTypesEXTRA" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.PlotvarTypesRECENT" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.StatsTypeKeywords" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.ValidColumnRangeVars" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.CustomLogs.Frequencies" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.CUtils.PossibleIntervals" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.DayRecords.m" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Graphx.m" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.NOAAdisplay.tmpIntArray" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.PwsFWI.dngrLevelValue" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.PwsFWI.fmtstring" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Records.m" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Records.tmpIntArray" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Website.Package" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.WebtagInfo.Tagname" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.HumColorFormat" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.PressColorFormat" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.RainColorFormat" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.TempColorFormat" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.WindColorFormat" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.WindRanges" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Yadr.WindrunColorFormat" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.AirLink.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.ParseChartDefinitions~System.Collections.Generic.List{CumulusUtils.OutputDef}" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CustomLogs.ReadDailyCustomLog(CumulusUtils.CustomLogs.CustomLog)~System.Collections.Generic.List{CumulusUtils.CustomLogs.CustomLogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CustomLogs.ReadRecentCustomLog(CumulusUtils.CustomLogs.CustomLog,System.DateTime,System.DateTime)~System.Collections.Generic.List{CumulusUtils.CustomLogs.CustomLogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CuSupport.GetCUstringValue(System.String,System.String,System.String,System.Boolean)~System.String" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CuSupport.StationInUse(System.Int32)~System.String" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.DayRecords.GenerateDayRecords(System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ExtraSensors.InitialiseExtraSensorList" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateWindRosePrevNextYearMonthMenu(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Maps.CreateMap" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Records.GenerateHTMLRecords(System.Int32,System.Int32)" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.SysInfo.GetLinuxDialect~CumulusUtils.SysInfo.LinuxDialects" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Website.#ctor(CumulusUtils.CuSupport,CumulusUtils.InetSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Website.GenerateCUlib" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Website.GenerateMenu~System.Threading.Tasks.Task{System.String}" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.WebtagInfo.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.Distance.UnitDistanceText" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.Height.UnitHeightText" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.Pressure.UnitPressureText" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.Rain.UnitRainText" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.Temp.UnitTempText" )]
[assembly: SuppressMessage( "Style", "IDE0300:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.Wind.UnitWindText" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.AllCharts" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.AllEquations" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.ChartsCompiler.AllOutputs" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.CustomLogs.CustomLogsList" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.CUtils.MainList" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.Graphx.DailySolarValuesList" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.HelpTexts.Helptexts" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.IniFile.m_Sections" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~F:CumulusUtils.PwsFWI.FWIlist" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Airlinklog.ReadAirlinklog~System.Collections.Generic.List{CumulusUtils.AirlinklogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartDef.#ctor(System.String,System.String)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.CheckAllVariablesInThisSetOfCharts(System.Collections.Generic.List{CumulusUtils.ChartDef})~System.Collections.Generic.List{CumulusUtils.AllVarInfo}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.ParseChartDefinitions~System.Collections.Generic.List{CumulusUtils.OutputDef}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.ParseSingleEval(System.String)~System.String" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.PrepareRawExpression(System.String)~System.Collections.Generic.List{System.String}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.Term(System.String[],System.Int32@,System.Boolean@)~System.String" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CustomLogs.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CustomLogs.ReadDailyCustomLog(CumulusUtils.CustomLogs.CustomLog)~System.Collections.Generic.List{CumulusUtils.CustomLogs.CustomLogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CustomLogs.ReadRecentCustomLog(CumulusUtils.CustomLogs.CustomLog,System.DateTime,System.DateTime)~System.Collections.Generic.List{CumulusUtils.CustomLogs.CustomLogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CUtils.RealMainAsync(System.String[])~System.Threading.Tasks.Task" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Dayfile.DayfileRead~System.Collections.Generic.List{CumulusUtils.DayfileValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ExternalExtraSensorslog.ReadExternalExtraSensorslog~System.Collections.Generic.List{CumulusUtils.ExternalExtraSensorslogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ExtraSensors.InitialiseExtraSensorList" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ExtraSensorslog.ReadExtraSensorslog~System.Collections.Generic.List{CumulusUtils.ExtraSensorslogValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateNOAAparameters(System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateWindrunStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder,System.Int32)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthRainStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthSolarEnergyStatistics(CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthSolarHoursStatistics(CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthTempStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearRainStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearSolarEnergyStatistics(System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearSolarHoursStatistics(System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearTempStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenGrowingDegreeDays(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenMonthlyEVTGraphData(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenMonthlyRainvsNOAAGraphData(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenMonthlyTempvsNOAAGraphData(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenStackedWarmDaysGraphData(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenTempSum(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.YearlySeasons(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.IniFile.Refresh" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.IniFile.SetValue(System.String,System.String,System.String)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Monthfile.ReadMonthlyLogs~System.Collections.Generic.List{CumulusUtils.MonthfileValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Monthfile.ReadPartialMonthlyLogs(System.DateTime,System.DateTime)~System.Collections.Generic.List{CumulusUtils.MonthfileValue}" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.OutputDef.#ctor(System.String)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.SysInfo.StartProcess(System.String,System.String)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Top10.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Top10.GenerateTop10List(System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Website.GenerateMenu~System.Threading.Tasks.Task{System.String}" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.ChartsCompiler.GenerateUserAskedData(System.Collections.Generic.List{CumulusUtils.ChartDef})~System.DateTime" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.DayRecords.GenerateDayRecords(System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateDataArray(System.String,System.Collections.Generic.List{CumulusUtils.MonthfileValue})~System.Text.StringBuilder" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateNOAAparameters(System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateWindRose(System.Collections.Generic.List{CumulusUtils.MonthfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthRainStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthSolarEnergyStatistics(CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthSolarHoursStatistics(CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearMonthTempStatistics(System.Collections.Generic.List{CumulusUtils.DayfileValue},CumulusUtils.Graphx.Months,System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearSolarEnergyStatistics(System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenerateYearSolarHoursStatistics(System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenGrowingDegreeDays(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenMonthlyRainvsNOAAGraphData(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.GenTempSum(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Graphx.YearlySeasons(System.Collections.Generic.List{CumulusUtils.DayfileValue},System.Text.StringBuilder)" )]
[assembly: SuppressMessage( "Performance", "CA1860:Avoid using 'Enumerable.Any()' extension method", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Maps.CreateMap" )]
