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

[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:CumulusUtils.Top10.compareHighestMonthlyRain" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:CumulusUtils.Top10.compareLongestDryPeriod" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:CumulusUtils.Top10.compareLongestWetPeriod" )]
[assembly: SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~P:CumulusUtils.StructFWI.dayFWI" )]
[assembly: SuppressMessage( "Style", "IDE0054:Use compound assignment", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.CuExtensions.CountFlags(CumulusUtils.AxisType)~System.UInt64" )]
[assembly: SuppressMessage( "Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Top10.#ctor(CumulusUtils.CuSupport)" )]
[assembly: SuppressMessage( "Style", "IDE0090:Use 'new(...)'", Justification = "<Pending>", Scope = "member", Target = "~M:CumulusUtils.Yadr.GenerateYadrWindData(System.Int32,System.Int32,System.Collections.Generic.List{CumulusUtils.DayfileValue})" )]
