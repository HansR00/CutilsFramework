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
