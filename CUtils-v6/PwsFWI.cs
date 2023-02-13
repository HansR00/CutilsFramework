/*
 * pwsFWI - Part of CumulusUtils
 *
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CumulusUtils
{

    #region structFWI
    internal struct StructFWI
    {
        public double ActualVaporPressure { get; set; }
        public string Date { get; set; }

        public double dayFWI { get; set; }
        public int DryPeriod { get; set; }
        public double Psat { get; set; }
        public double RH { get; set; }
        public double SmoothedFWI { get; set; }
        public double TempUnit { get; set; }
        public double VPD { get; set; }

        public double Pressure { get; set; }
        public double Rain { get; set; }
        public double T { get; set; }
        public double Wind { get; set; }
    }

    #endregion

    internal class PwsFWI : IDisposable
    {
        #region Declarations

        // for generating the HTML page
        private const int DaysRequiredForHTML = 30;
        private const int NrOfPredictiveDays = 5;
        private const int NrOfSmoothingDays = 5;

        // in Percentage
        private const int TableWidth = 95;

        // Parameters from the inifile
        private readonly bool BeteljuiceFormat;
        private readonly bool UseFireImage;
        private readonly int Analyse;
        private readonly int WarningLevel;
        private readonly int ExtremeFWIvalue = 800;

        private enum DngrLevel { green, blue, yellow, orange, red, purple };
        private readonly int[] dngrLevelValue = { 210, 350, 490, 630, 800, 1000 };

        private readonly string[] fmtstring = { "green;color: white", "blue;color: white", "yellow;color: black", "orange;color: black", "red;color: black", "Purple;color: white" };
        private double greenblueSeperation, blueyellowSeparation, yelloworangeSeparation, orangeredSeparation, redpurpleSeperation;
        private double greenPercentage, bluePercentage, yellowPercentage, orangePercentage, redPercentage, purplePercentage;

        private readonly List<StructFWI> FWIlist = new List<StructFWI>();
        private readonly CuSupport Sup;
        private readonly InetSupport Isup;

        private string PredictionBackground;
        private string predictionURL;
        private bool DoPrediction;

        #endregion

        #region Contructor
        public PwsFWI( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;

            Sup.LogDebugMessage( $"PwsFWIfuncs constructor" );

            Analyse = Convert.ToInt32( Sup.GetUtilsIniValue( "pwsFWI", "Analyse", "30" ), CultureInfo.InvariantCulture );
            WarningLevel = Convert.ToInt32( Sup.GetUtilsIniValue( "pwsFWI", "WarningLevel", "5" ), CultureInfo.InvariantCulture );

            if ( WarningLevel != 5 && WarningLevel != 6 )
            {
                Sup.LogTraceInfoMessage( $" WarningLevel for pwsFWI MUST be 5 or 6 and it is: {WarningLevel}" );
                Sup.LogTraceInfoMessage( $" Setting default WarningLevel = 5" );
                WarningLevel = 5;
            }
            else if ( WarningLevel == 6 )
                ExtremeFWIvalue = dngrLevelValue[ (int) DngrLevel.purple ];
            // else the default remains

            if ( Sup.GetUtilsIniValue( "pwsFWI", "ResultFormat", "Standard" ).Equals( "Beteljuice", CUtils.cmp ) )
                BeteljuiceFormat = true;
            else
                BeteljuiceFormat = false;

            if ( Sup.GetUtilsIniValue( "pwsFWI", "FireImage", "true" ).Equals( "true", CUtils.cmp ) )
                UseFireImage = true;
            else
                UseFireImage = false;
        }

        #endregion

        #region CalculatePwsFWI
        public async Task CalculatePwsFWI( List<DayfileValue> ThisList )
        {
            int i, j, startValue;

            StructFWI localFWI = new StructFWI();
            CultureInfo provider = CultureInfo.CurrentCulture;
            string separator = provider.TextInfo.ListSeparator;

            string csvFilename = "pwsFWIanalyse.csv";

            // Add the prediction days to thisList if wanted by the user. This means the predictionURL must be set
            predictionURL = Sup.GetUtilsIniValue( "pwsFWI", "predictionURL", "" );

            if ( !String.IsNullOrEmpty( predictionURL ) )
            {
                // The prediciton has to be removed after generation of pwsFWI
                // That is being done in the dispose sequence
                //
                if ( !await AddPrediction( predictionURL, ThisList ) )
                {
                    // When returning false something went wrong. Not a problem, only DoPrediction becomes false
                    DoPrediction = false;
                }
                else
                    DoPrediction = true;

                if ( DoPrediction )
                {
                    // Override table background
                    PredictionBackground = Sup.GetUtilsIniValue( "pwsFWI", "PredictionBackground", "Moccasin" );
                    PredictionBackground = "background:" + PredictionBackground + ";";
                }
                else
                {
                    PredictionBackground = "";
                }

                // On return, ThisList contains 5 extra days from the prediction. They should automatically be taken into the system
                // of pwsFWI calculation. Afterwards, they must be deleted from the list.
            }
            else
                DoPrediction = false;

            Sup.LogTraceInfoMessage( $" PwsFWIfuncs BeteljuiceFormat = {BeteljuiceFormat}" );
            Sup.LogTraceInfoMessage( $" PwsFWIfuncs USerFireImage = {UseFireImage}" );
            Sup.LogTraceInfoMessage( $" PwsFWIfuncs DoPrediction = {DoPrediction}" );

            // Initialising done, start the whole sequence
            // Always open this file, otherwise unassigned variable occurs at compiletime
            //
            using ( StreamWriter af = new StreamWriter( $"{Sup.PathUtils}{csvFilename}", false, Encoding.UTF8 ) )
            {
                // Calculate for each day the actual pwsFWI and create the csv file for the whole dayfile.txt
                Sup.LogDebugMessage( "calculatePwsFWI : starting" );

                if ( Analyse > DaysRequiredForHTML )
                {
                    Sup.LogDebugMessage( "calculatePwsFWI : ANALYSE is ON" );
                    Sup.LogDebugMessage( "calculatePwsFWI : Opening ANALYSIS output" );

                    if ( Analyse > ThisList.Count )
                        startValue = 0;
                    else
                        startValue = ThisList.Count - Analyse - NrOfSmoothingDays;

                    af.WriteLine( $"Date{separator}Temp{separator}Wind{separator}Rain{separator}RH{separator}Psat{separator}VPD{separator}dayFWI{separator}No Rain{separator}SmoothedFWI" );
                }
                else
                {
                    Sup.LogDebugMessage( "calculatePwsFWI : ANALYSE is OFF" );

                    startValue = ThisList.Count - DaysRequiredForHTML - NrOfSmoothingDays;
                    startValue = startValue < 0 ? 0 : startValue;
                }

                // N = NrOfSmoothingDays + 1; Sum of all numbers up to N = (N * (1 + N)) / 2; (so: 1+2+3+4=10)

                Sup.LogTraceVerboseMessage( $"calculatePwsFWI : i={startValue} to LineCount={ThisList.Count}" );


                for ( i = startValue; i < ThisList.Count; i++ )
                {
                    double A, B, C;

                    localFWI.Date = ThisList[ i ].ThisDate.ToString( "dd/MM/yy", CultureInfo.InvariantCulture );
                    localFWI.T = Sup.StationTemp.Convert( Sup.StationTemp.Dim, TempDim.celsius, ThisList[ i ].MaxTemp );                // if Temp is in F, convert it to C. Otherwise it remains in C
                    localFWI.RH = ThisList[ i ].LowHumidity / 100;                                                                      // always percentage
                    localFWI.Wind = Sup.StationWind.Convert( Sup.StationWind.Dim, WindDim.kmh, ThisList[ i ].HighAverageWindSpeed );    // if Wind other than km/h convert it otherwise it remains in km/h
                    localFWI.Rain = Sup.StationRain.Convert( Sup.StationRain.Dim, RainDim.millimeter, ThisList[ i ].TotalRainThisDay ); // if Rain other than mm convert it otherwise it remains in mm

                    if ( localFWI.T >= 0 )
                    {
                        // Use the August-Roche-Magnus equation as found in the Wikipedia
                        // changed that to the CIMO guide 2008
                        A = 6.112F;
                        B = 17.62F;
                        C = 243.12F;

                        localFWI.Psat = A * Math.Exp( B * localFWI.T / ( localFWI.T + C ) );
                    }
                    else
                    {
                        // Use formula for below zero temperatures from the CIMO Guide 2008
                        A = 6.112F;
                        B = 22.46F;
                        C = 272.62F;

                        localFWI.Psat = A * Math.Exp( B * localFWI.T / ( localFWI.T + C ) );
                    }

                    localFWI.VPD = localFWI.Psat - localFWI.RH * localFWI.Psat; // localFWI.ActualVaporPressure;
                    localFWI.dayFWI = localFWI.Wind < 1 ? localFWI.VPD : localFWI.VPD * localFWI.Wind;

                    if ( i >= startValue + NrOfSmoothingDays ) // Need at least NrOfSmoothingDays days for the algorithm to work
                    {
                        // Init smoothing
                        localFWI.SmoothedFWI = localFWI.dayFWI;

                        if ( localFWI.Rain < 5.0 && localFWI.Rain > 1.0 )
                            localFWI.SmoothedFWI /= 2;
                        else if ( localFWI.Rain > 5.0 )
                            localFWI.SmoothedFWI /= 3;

                        // do the smooting
                        for ( j = i - NrOfSmoothingDays; j < i; j++ )
                        {
                            //            if (FWIlist[j - startValue].Rain < 2.0 && FWIlist[j - startValue].Rain > 0.6)
                            if ( FWIlist[ j - startValue ].Rain < 5.0 && FWIlist[ j - startValue ].Rain > 1.0 )
                            {
                                ;
                                //Sup.LogTraceVerboseMessage( DateTime.Now.ToString( CultureInfo.CurrentCulture ) + " calculatePwsFWI : Smoothing, skip day j; i, j" );
                                /*
                                 * only this day will be reduced
                                 * skip this day, so do not add dayFWI to the Smoothed FWI
                                 */
                            }
                            else if ( FWIlist[ j - startValue ].Rain > 5.0 )
                            {
                                //Sup.LogTraceVerboseMessage( DateTime.Now.ToString( CultureInfo.CurrentCulture ) + " calculatePwsFWI : Smoothing, skip day j and j+1" );
                                /*
                                 * reduce two (or possibly eventually more) days
                                 */
                                j += 1;
                            }
                            else
                            {
                                //Sup.LogTraceVerboseMessage( DateTime.Now.ToString( CultureInfo.CurrentCulture ) + " calculatePwsFWI : Smoothing: " +
                                //"                                         add dayFWI to the Smoothed; i, j" );
                                /*
                                 * Do the normal Smoothing without quenching
                                 */
                                localFWI.SmoothedFWI += FWIlist[ j - startValue ].dayFWI;
                            }
                        }

                        localFWI.SmoothedFWI /= NrOfSmoothingDays;
                    }

                    Sup.LogTraceVerboseMessage( "calculatePwsFWI : SmoothedFWI before DryPeriod addition i:" +
                                        i.ToString( CultureInfo.CurrentCulture ) + " smoothedFWI:" + localFWI.SmoothedFWI.ToString( CultureInfo.CurrentCulture ) );

                    // 17/09/2019: New item in the pwsFWI: length of drought. Have to think about the weight
                    localFWI.DryPeriod = ThisList[ i ].DryPeriod;
                    localFWI.SmoothedFWI += ThisList[ i ].DryPeriod;

                    // Testing, testing, 123...
                    // localFWI.SmoothedFWI = i*10;

                    Sup.LogTraceVerboseMessage( "calculatePwsFWI : SmoothedFWI after DryPeriod addition i: " + i.ToString( CultureInfo.CurrentCulture ) +
                                                 " DryPeriod: " + ThisList[ i ].DryPeriod.ToString( CultureInfo.CurrentCulture ) +
                                                 " smoothedFWI:" + localFWI.SmoothedFWI.ToString( CultureInfo.CurrentCulture ) );

                    Sup.LogTraceVerboseMessage( $"calculatePwsFWI : creating Listentry FWI for i={i}" );

                    FWIlist.Add( localFWI );

                    Sup.LogTraceVerboseMessage( $"   {localFWI.Date}; {localFWI.T:F1}; {localFWI.Wind:F1}; {localFWI.Rain:F1}; " +
                                        $"{localFWI.RH:F2}; {localFWI.Psat:F2}; {localFWI.VPD:F2}; " +
                                        $"{localFWI.dayFWI:F2}; {localFWI.DryPeriod:F0}; {localFWI.SmoothedFWI:F2}" );

                    if ( Analyse > DaysRequiredForHTML )
                    {
                        af.WriteLine( localFWI.Date + $"{separator}{localFWI.T:F1}{separator}{localFWI.Wind:F1}{separator}{localFWI.Rain:F1}" +
                          $"{separator}{localFWI.RH:F2}{separator}{localFWI.Psat:F2}{separator}{localFWI.VPD:F2}{separator}{localFWI.dayFWI:F2}" +
                          $"{separator}{localFWI.DryPeriod:D}{separator}{localFWI.SmoothedFWI:F1}" );
                    }
                } //end for loop over all dayfile entries (ANALYSE) or just the 30 line subset (PRODUCTION)

                HTMLexportPwsFWI();

                // The prediction is removed here and not in the destructor because the destrucor is called too late by the Garbage Collector
                // As a consequence, the following modules - e.g. graphs might use the predictions.
                // Therefore handling the list (adding, deleting) must be done synchrone.
                if ( DoPrediction )
                {
                    if ( !RemovePrediction( ThisList ) )
                    {
                        // The prediction could not be removed correctly so stop the program as the next usage of the dayfile-list
                        // carries errors.
                        Sup.LogTraceErrorMessage( "calculatePwsFWI : Error removing the prediction!" );
                    }
                }
            } // End using of (Analysis file)

            if ( Analyse <= DaysRequiredForHTML )
            {
                // No analysis so delete the file
                Sup.LogTraceInfoMessage( "calculatePwsFWI : Deleting empty ANALYSIS csv file" );
                File.Delete( $"{Sup.PathUtils}{csvFilename}" );
            }

            return;
        }

        #endregion

        #region generate pwsFWI HTML
        private void HTMLexportPwsFWI()
        {
            int IndexOfCurrent, i;
            DngrLevel fmtindex;

            Sup.LogTraceInfoMessage( "HTMLexportPwsFWI : starting" );
            Sup.LogTraceInfoMessage( "HTMLexportPwsFWI : starting pwsFWIcurrent" );

            //
            // First do the current value for Frontpage or elsewhere single use
            //
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.PwsFWICurrentOutputFilename}", false, Encoding.UTF8 ) )
            {
                if ( DoPrediction )
                {
                    // UseTodaysValue is true will use the prediction value of today as current otherwise it will use the true calculated value on observations
                    bool UseTodaysValue = Sup.GetUtilsIniValue( "pwsFWI", "CurrentIndexDay", "Yesterday" ).Equals( "Today", CUtils.cmp );
                    IndexOfCurrent = FWIlist.Count - NrOfPredictiveDays - ( UseTodaysValue ? 0 : 1 );
                }
                else
                    IndexOfCurrent = FWIlist.Count - 1;

                fmtindex = SetFmtIndex( IndexOfCurrent );

                if ( Sup.GetUtilsIniValue( "pwsFWI", "CurrentIndexFormat", "Standard" ).Equals( "Standard", CUtils.cmp ) )
                {
                    // Use the standard Style
                    of.WriteLine( $"<span id='CurrentPwsFWI' style=\"border: 1px solid black;cursor:pointer;text-align:center;background:{fmtstring[ (int) fmtindex ]}\"> " +
                                $"&nbsp;{FWIlist[ IndexOfCurrent ].SmoothedFWI.ToString( "F1", CultureInfo.InvariantCulture )}&nbsp;pwsFWI</span>" );
                    Sup.SetUtilsIniValue( "pwsFWI", "CurrentPwsFWI", $"<span id='CurrentPwsFWI' style=\"border: 1px solid black;cursor:pointer;text-align:center;background:{fmtstring[ (int) fmtindex ]}\"> " +
                                        $"&nbsp;{FWIlist[ IndexOfCurrent ].SmoothedFWI.ToString( "F1", CultureInfo.InvariantCulture )}&nbsp;pwsFWI</span>" );
                }
                else if ( Sup.GetUtilsIniValue( "pwsFWI", "CurrentIndexFormat", "Standard" ).Equals( "Betel-Kocher", CUtils.cmp ) )
                {
                    // Use the Betel-Kocher style
                    const string Base64Img = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAFAAAAAGCAIAAADbpI4QAAAABnRSTlMAAAAAAABupgeRAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAA7ElEQVQ4jd2TvU7CYBSGn++3PxZDE0ZCC62DF2Di6mWQmDBzCd6rCo5KRasSSTBfXcpSMLELJDzTGd6c8z7DEdzRgnWX95QyYZlRDljmlAM++jjdCErp4vgtz++T5Gk0ehwOZ2k6z7KHXq+Q0jXXVrCGFXzCCr6282ZfBwcFvMIzLGABL1DA978MNF4bYSewCq3RFuUjQsQZRGD2RMVGqUDrwPOs75swVFEkOh2k3MlWoEFAtVVy8APqD2EPLBjQIEG0MNg9fuLUwkaYgOC4VQ5DLZyfX95ejC3NVzw9auGr/s30ehL73eO2OQC/GPkzEsKfXDgAAAAASUVORK5CYII=";

                    of.WriteLine( $"<span id='CurrentPwsFWI' style='border: 1px solid black;padding:0px;cursor:pointer;text-align:center;line-height:100%;display:block;background:{fmtstring[ (int) fmtindex ]}'>" +
                                 $"pwsFWI<br>&nbsp;{FWIlist[ IndexOfCurrent ].SmoothedFWI.ToString( "F1", CultureInfo.InvariantCulture )}&nbsp;<br>" +
                                 $"<img alt='Colour Bar' src='{Base64Img}'></span>" );
                    Sup.SetUtilsIniValue( "pwsFWI", "CurrentPwsFWI", $"<span id='CurrentPwsFWI' style=\"border: 1px solid black;padding:0px;cursor:pointer;text-align:center;line-height:100%;display:block;background:{fmtstring[ (int) fmtindex ]}\">" +
                                 $"pwsFWI<br>&nbsp;{FWIlist[ IndexOfCurrent ].SmoothedFWI.ToString( "F1", CultureInfo.InvariantCulture )}&nbsp;<br>" +
                                 $"<img alt='Colour Bar' src='{Base64Img}'></span>" );
                }
            }

            // Now start the main output pwsFWI.txt
            //
            Sup.LogTraceInfoMessage( "HTMLexportPwsFWI : starting pwsFWI.txt" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.PwsFWIOutputFilename}", false, Encoding.UTF8 ) )
            {
                greenPercentage = dngrLevelValue[ (int) DngrLevel.green ] / (double) ExtremeFWIvalue * 100.0;
                bluePercentage = ( dngrLevelValue[ (int) DngrLevel.blue ] - dngrLevelValue[ (int) DngrLevel.green ] ) / (double) ExtremeFWIvalue * 100.0;
                yellowPercentage = ( dngrLevelValue[ (int) DngrLevel.yellow ] - dngrLevelValue[ (int) DngrLevel.blue ] ) / (double) ExtremeFWIvalue * 100.0;
                orangePercentage = ( dngrLevelValue[ (int) DngrLevel.orange ] - dngrLevelValue[ (int) DngrLevel.yellow ] ) / (double) ExtremeFWIvalue * 100.0;
                redPercentage = ( dngrLevelValue[ (int) DngrLevel.red ] - dngrLevelValue[ (int) DngrLevel.orange ] ) / (double) ExtremeFWIvalue * 100.0;
                purplePercentage = ( dngrLevelValue[ (int) DngrLevel.purple ] - dngrLevelValue[ (int) DngrLevel.red ] ) / (double) ExtremeFWIvalue * 100.0;

                // Now generate the linear-gradient... God, should never have done it. Just sharp limits. What a life!
                //
                // next line is consequence of beteljuice interface investigation
                // this is the beteljuice line: "background-image: linear-gradient(to right, green 25 %, blue 40 %, yellow 55 %, orange 89 %, red 92 %);\">");
                // This is the double stop version: "background-image:linear-gradient(to right,green 0% 27%,blue 32% 47%,yellow 52% 67% ,orange 72% 87%, red 92%);\">");

                greenblueSeperation = greenPercentage;
                blueyellowSeparation = greenPercentage + bluePercentage;
                yelloworangeSeparation = greenPercentage + bluePercentage + yellowPercentage;
                orangeredSeparation = greenPercentage + bluePercentage + yellowPercentage + orangePercentage;
                redpurpleSeperation = greenPercentage + bluePercentage + yellowPercentage + orangePercentage + redPercentage;

                string Title = Sup.GetCUstringValue( "pwsFWI", "Title", "A Fire Weather Index for personal weather stations", false );
                string SubTitle = Sup.GetCUstringValue( "pwsFWI", "SubTitle", "(See below the table for background)", false );

                if ( !string.IsNullOrEmpty( Title ) && !string.IsNullOrEmpty( SubTitle ) )
                {
                    // Do some title for the Steinars of this world
                    of.WriteLine( $"<h3>{Title}</h3>" );
                    of.WriteLine( $"<h5>{SubTitle}</h5>" );
                }

                if ( BeteljuiceFormat )
                {
                    Sup.LogTraceInfoMessage( "HTMLexportPwsFWI : Using Beteljuice format" );

                    InjectBeteljuiceStyle( of, 1 );
                    InjectBeteljuiceStyle( of, FWIlist[ IndexOfCurrent ].SmoothedFWI );
                    InjectBeteljuiceStyle( of, 3 );
                    InjectBeteljuiceStyle( of, 4 );
                }
                else // no beteljuices, so: Standard
                {
                    Sup.LogTraceInfoMessage( "HTMLexportPwsFWI : Using Standard format" );

                    string tmp = Sup.GetCUstringValue( "pwsFWI", "Low", "Low", false );

                    of.WriteLine( $"<table style='width:{TableWidth}%; margin-left:auto; margin-right:auto; border-collapse: collapse;'><tbody>" );
                    of.WriteLine( "<tr style='text-align:center;'>" );
                    of.WriteLine( $"<td style='width:{greenPercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                 $"background-color:{fmtstring[ (int) DngrLevel.green ]};'>{Sup.GetCUstringValue( "pwsFWI", "Low", "Low", false )}<br/>" +
                                 $"(0 - {dngrLevelValue[ (int) DngrLevel.green ]})</td>" );
                    of.WriteLine( $"<td style='width:{bluePercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                 $"background-color:{fmtstring[ (int) DngrLevel.blue ]};'>{Sup.GetCUstringValue( "pwsFWI", "Moderate", "Moderate", false )}<br/>" +
                                 $"({dngrLevelValue[ (int) DngrLevel.green ]} - {dngrLevelValue[ (int) DngrLevel.blue ]})</td>" );
                    of.WriteLine( $"<td style='width:{yellowPercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                 $"background-color:{fmtstring[ (int) DngrLevel.yellow ]};'>{Sup.GetCUstringValue( "pwsFWI", "High", "High", false )}<br/>" +
                                 $"({dngrLevelValue[ (int) DngrLevel.blue ]} - {dngrLevelValue[ (int) DngrLevel.yellow ]})</td>" );
                    of.WriteLine( $"<td style='width:{orangePercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                 $"background-color:{fmtstring[ (int) DngrLevel.orange ]};'>{Sup.GetCUstringValue( "pwsFWI", "VeryHigh", "Very High", false )}<br/>" +
                                 $"({dngrLevelValue[ (int) DngrLevel.yellow ]} - {dngrLevelValue[ (int) DngrLevel.orange ]})</td>" );
                    if ( WarningLevel == 5 )
                    {
                        of.WriteLine( $"<td style='width:{redPercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                     $"background-color:{fmtstring[ (int) DngrLevel.red ]};'>{Sup.GetCUstringValue( "pwsFWI", "Extreme", "Extreme", false )}<br/>" +
                                     $"({dngrLevelValue[ (int) DngrLevel.orange ]}+)</td>" );
                    }
                    else // if (WarningLevel==6)
                    {
                        of.WriteLine( $"<td style='width:{redPercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                     $"background-color:{fmtstring[ (int) DngrLevel.red ]};'>{Sup.GetCUstringValue( "pwsFWI", "Extreme", "Extreme", false )}<br/>" +
                                     $"({dngrLevelValue[ (int) DngrLevel.orange ]} - {dngrLevelValue[ (int) DngrLevel.red ]})</td>" );
                        of.WriteLine( $"<td style='width:{purplePercentage.ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                                   $"background-color:{fmtstring[ (int) DngrLevel.purple ]};'>{Sup.GetCUstringValue( "pwsFWI", "Catastrophic", "Catastrophic", false )}<br/>" +
                                   $"({dngrLevelValue[ (int) DngrLevel.red ]}+)</td>" );
                    }

                    of.WriteLine( "</tr>" );
                    of.WriteLine( "</tbody></table>" );

                    of.WriteLine( "<br/>" );

                    if ( WarningLevel == 5 )
                    {
                        // more or less this:  green 25%, blue 40%, yellow 55%, orange 89%, red 93%);
                        of.WriteLine( $"<div style='width:{TableWidth}%; margin-left:auto; margin-right:auto; padding:10px;" +
                              $"background-image: linear-gradient(to right, green {( greenblueSeperation - 10 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"blue {( blueyellowSeparation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"yellow {( yelloworangeSeparation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"orange {( orangeredSeparation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"red {( 110 - redPercentage ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%);' >" );
                    }
                    else // WarningLevel == 6
                    {
                        of.WriteLine( $"<div style='width: {TableWidth}%; margin-left: auto; margin-right: auto; padding: 10px;" +
                              $"background-image: linear-gradient(to right, green {( greenblueSeperation - 10 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"blue {( blueyellowSeparation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"yellow {( yelloworangeSeparation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"orange {( orangeredSeparation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"red {( redpurpleSeperation - 8 ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%," +
                              $"purple {( 110 - purplePercentage ).ToString( "F0", NumberFormatInfo.InvariantInfo )}%);' >" );
                    }

                    of.WriteLine( $"<div style='align: left; width: {Math.Min( FWIlist[ IndexOfCurrent ].SmoothedFWI / ExtremeFWIvalue * 100, 100 ).ToString( "F1", NumberFormatInfo.InvariantInfo )}%;" +
                               "background: #f0f0f0; text-align: right;'>" );
                    of.WriteLine( "<span >></span></div></div>" );
                    of.WriteLine( $"<div style='width: {TableWidth}%; margin-left: auto; margin-right: auto; text-align: left;'>" +
                          $"Current FWI value = {FWIlist[ IndexOfCurrent ].SmoothedFWI.ToString( "F1", CultureInfo.InvariantCulture )} " +
                          $"(maximum scale = {ExtremeFWIvalue})</div>" );
                    of.WriteLine( $"<div style ='width: {TableWidth}%; margin-left: auto; margin-right: auto; text-align:left;'>" +
                                 "<small>For more information on the Fire Weather Index, see the end of this page.</small></div>" );
                    of.WriteLine( "<br/><br/>" );

                    of.WriteLine( $"<table style='width: {TableWidth}%; margin-left: auto; margin-right: auto; border-collapse: collapse; background-color: #d7d7d7;'><tbody>" );
                    of.WriteLine( "<tr>" ); //style=\"background-color: #b0b0b0;\"
                    of.WriteLine( $"<th style='width: 16%;text-align: center;'>{Sup.GetCUstringValue( "pwsFWI", "Date", "Date", false )}</th>" );
                    of.WriteLine( $"<th style='width: 14%;text-align: center;'>{Sup.GetCUstringValue( "pwsFWI", "Temperature", "Temp.", false )}\n(&deg;C)</th>" );
                    of.WriteLine( $"<th style='width: 14%;text-align: center;'>{Sup.GetCUstringValue( "pwsFWI", "WindSpeed", "Wind Speed", false )}\n(km{Sup.PerHour})</th>" );
                    of.WriteLine( $"<th style='width: 14%;text-align: center;'>{Sup.GetCUstringValue( "pwsFWI", "Rain", "Rain", false )}\n(mm)</th>" );
                    of.WriteLine( $"<th style='width: 14%;text-align: center;'>{Sup.GetCUstringValue( "pwsFWI", "RelativeHumidity", "RH", false )}\n</th>" );
                    of.WriteLine( $"<th style='width: 14%;text-align: center;'>{Sup.GetCUstringValue( "pwsFWI", "DaysSinceRain", "Days since Rain", false )}</th>" );
                    of.WriteLine( "<th style='width: 14%;text-align: center;'>pwsFWI</th>" );
                    of.WriteLine( "</tr>" );

                    if ( DoPrediction )
                    {
                        of.WriteLine( $"<tr style='text-align: center; {PredictionBackground}'><td colspan='7'>" );
                        of.WriteLine( $"{Sup.GetCUstringValue( "pwsFWI", "PredictionMessage", "Prediction is active (including today!), values are above red line.", false )}" );
                        of.WriteLine( "</td></tr>" );
                    }

                    for ( i = FWIlist.Count - 1; i >= NrOfSmoothingDays; i-- )
                    {
                        string PredictionSeparation = "border-bottom: 1px solid red;";
                        string tmpBackground;

                        fmtindex = SetFmtIndex( i );

                        if ( DoPrediction && i > FWIlist.Count - NrOfPredictiveDays )
                            tmpBackground = PredictionBackground;
                        else if ( DoPrediction && i == FWIlist.Count - NrOfPredictiveDays )
                            tmpBackground = PredictionBackground + PredictionSeparation;
                        else
                            tmpBackground = "";

                        of.WriteLine( $"<tr style='text-align:center;{tmpBackground}'>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].Date}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].T.ToString( "F1", CultureInfo.InvariantCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].Wind.ToString( "F1", CultureInfo.InvariantCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].Rain.ToString( "F1", CultureInfo.InvariantCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].RH.ToString( "F2", CultureInfo.InvariantCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].DryPeriod.ToString( "D", CultureInfo.InvariantCulture )}</td>" );
                        of.WriteLine( $"<td style='background:{fmtstring[ (int) fmtindex ]}'>{FWIlist[ i ].SmoothedFWI.ToString( "F1", CultureInfo.InvariantCulture )}</td>" );

                        of.WriteLine( "</tr>" );
                    }

                    of.WriteLine( "</tbody></table>" );

                    of.WriteLine( "</div>" );
                    of.WriteLine( "</div>" );
                }

                of.WriteLine( $"<div style ='width: {TableWidth}%; margin-left: auto; margin-right: auto; text-align: center; font-size: 12px;'><br/>" );

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"{CuSupport.FormattedVersion()} - {CuSupport.Copyright()} <br/>" );
                }

                of.WriteLine( "<a href='https://cumuluswiki.org/a/Theoretical_background_on_pwsFWI' target='_blank'>Science background here.</a> Prediction: <a href='https://www.yourweather.co.uk/' target='_blank'>https://www.yourweather.co.uk/</a>" );
                of.WriteLine( " </div>" );

                // Done with HTML generation
            } // End using of (output file)
        }

        #endregion

        #region Generate Beteljuice style
        private void InjectBeteljuiceStyle( StreamWriter of, int part )
        {
            int i;
            DngrLevel fmtindex;

            switch ( part )
            {
                case 1: // All the CSS stuff by Beteljuice
                    of.WriteLine( "<!-- start of FWI inject -->" );
                    of.WriteLine( "<!--betel magic-->" );
                    of.WriteLine( "<style>" );
                    of.WriteLine( "#fwi_content {" );
                    of.WriteLine( "  margin: auto;" );
                    of.WriteLine( "  min-width: 450px;" );
                    of.WriteLine( "  max-width: 1000px;" );
                    of.WriteLine( "  color: black;" );
                    of.WriteLine( "  background-color: transparent;" );
                    of.WriteLine( "  font-size: 14px;" );
                    of.WriteLine( "  font-family: arial;" );
                    of.WriteLine( "  line-height: normal;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content .fwi_key {" );
                    of.WriteLine( "  border: 1px solid black;" );
                    of.WriteLine( "  border-radius: 10px;" );
                    of.WriteLine( "  border-spacing: 0;" );
                    of.WriteLine( "  border-collapse: inherit;" );
                    of.WriteLine( "	margin: auto;" );
                    of.WriteLine( "	margin-bottom: 30px;" );
                    of.WriteLine( "	margin-top: 15px;" );
                    of.WriteLine( "	position: relative;" );
                    of.WriteLine( "	padding: 0px;" );
                    of.WriteLine( "	width: 100%;" );
                    of.WriteLine( "	height: 30px;" );

                    of.WriteLine( "  background-repeat:no-repeat;" );
                    of.WriteLine( "  background-image:" );
                    of.WriteLine( "    linear-gradient(to right, Green, Blue)," );
                    of.WriteLine( "    linear-gradient(to right, Blue, Yellow)," );
                    of.WriteLine( "    linear-gradient(to right, Yellow, Orange)," );
                    of.WriteLine( "    linear-gradient(to right, Orange, Red)," );
                    if ( WarningLevel == 6 )
                        of.WriteLine( "    linear-gradient(to right, Red, Purple)," );
                    of.WriteLine( "    linear-gradient(to right, Green, Green)," );
                    of.WriteLine( "    linear-gradient(to right, Blue, Blue)," );
                    of.WriteLine( "    linear-gradient(to right, Yellow, Yellow)," );
                    of.WriteLine( "    linear-gradient(to right, Orange, Orange)," );
                    if ( WarningLevel == 5 )
                    {
                        of.WriteLine( "    linear-gradient(to right, Red, Red);" );
                    }
                    else // WarningLevel == 6
                    {
                        of.WriteLine( "    linear-gradient(to right, Red, Red)," );
                        of.WriteLine( "    linear-gradient(to right, Purple, Purple);" );
                    }

                    of.WriteLine( "  background-position:" );
                    of.WriteLine( $"    {greenblueSeperation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 0px," );
                    of.WriteLine( $"    {blueyellowSeparation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 0px," );
                    of.WriteLine( $"    {yelloworangeSeparation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 0px," );
                    of.WriteLine( $"    {orangeredSeparation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 0px," );
                    if ( WarningLevel == 6 )
                        of.WriteLine( $"    {redpurpleSeperation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 0px," );
                    of.WriteLine( "    0% 0px," );
                    of.WriteLine( "    0% 0px," );
                    of.WriteLine( "    0% 0px," );
                    of.WriteLine( "    0% 0px," );
                    if ( WarningLevel == 6 )
                        of.WriteLine( "    0% 0px," );
                    of.WriteLine( "    0% 0px;" );

                    of.WriteLine( "  background-size:" );
                    of.WriteLine( "    8% 100%," );
                    of.WriteLine( "    8% 100%," );
                    of.WriteLine( "    8% 100%," );
                    of.WriteLine( "    8% 100%," );
                    if ( WarningLevel == 6 )
                        of.WriteLine( "    8% 100%," );
                    of.WriteLine( $"    {greenblueSeperation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 100%," );
                    of.WriteLine( $"    {blueyellowSeparation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 100%," );
                    of.WriteLine( $"    {yelloworangeSeparation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 100%," );
                    of.WriteLine( $"    {orangeredSeparation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 100%," );
                    if ( WarningLevel == 6 )
                        of.WriteLine( $"  {redpurpleSeperation.ToString( "F1", NumberFormatInfo.InvariantInfo )}% 100%," );
                    of.WriteLine( $"    100% 100%;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content #fwi_data {" );
                    of.WriteLine( "  border-radius: 15px;" );
                    of.WriteLine( "  border-spacing: 0;" );
                    of.WriteLine( "  border: 2px solid #b0b0b0;" );
                    of.WriteLine( "  border-collapse: inherit;" );
                    of.WriteLine( $"  width: {TableWidth}%;" );
                    of.WriteLine( "  margin-left:auto;" );
                    of.WriteLine( "  margin-right:auto;" );
                    of.WriteLine( "  background-color: #d7d7d7;" );
                    of.WriteLine( "  }" );

                    of.WriteLine( "#fwi_content #fwi_data TD {" );
                    of.WriteLine( "  padding: 3px;" );
                    of.WriteLine( "	text-align: center;" );
                    of.WriteLine( "	width: 17%;" );
                    of.WriteLine( "	font-weight: normal;" );
                    of.WriteLine( "	font-size: 14px;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content #fwi_data th {" );
                    of.WriteLine( "  padding: 3px;" );
                    of.WriteLine( "	text-align: center;" );
                    of.WriteLine( "	width: 15%;" );
                    of.WriteLine( "	background-color: #b0b0b0;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content #pointer{" );
                    of.WriteLine( "  position: relative;" );
                    of.WriteLine( "  float: right;" );
                    of.WriteLine( "  left: 19px;" );
                    of.WriteLine( "  width: 38px; height: 18px;" );
                    of.WriteLine( "  font-size: 25px; font-weight: bold; font-family: arial;" );
                    of.WriteLine( "  text-align: center;" );
                    of.WriteLine( "  text-shadow: 0 0 4px #000000;" );
                    of.WriteLine( "  color: white;" );
                    of.WriteLine( "  animation-name: color;" );
                    of.WriteLine( "  animation-duration: 2s;" );
                    of.WriteLine( "  animation-iteration-count: infinite;" );
                    of.WriteLine( "}" );

                    if ( !DoPrediction )
                        fmtindex = SetFmtIndex( FWIlist.Count - 1 );
                    else
                        fmtindex = SetFmtIndex( FWIlist.Count - 1 - NrOfPredictiveDays );

                    of.WriteLine( "@keyframes color{" );
                    of.WriteLine( "0% { color: white;}" );

                    if ( fmtindex == DngrLevel.red )
                        of.WriteLine( "50% { color: red;}" );
                    else if ( fmtindex == DngrLevel.purple )
                        of.WriteLine( "50% { color: purple;}" );
                    else
                        of.WriteLine( "50% { color: white;}" );

                    of.WriteLine( "100% { color: white;}" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content #cv {" );
                    of.WriteLine( "  position: relative; top: 0px;" );
                    of.WriteLine( "  color: black;" );
                    of.WriteLine( "  font-size: 12px;" );
                    of.WriteLine( "  text-shadow: none;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content .key {" );
                    of.WriteLine( "  display: inline-block;" );
                    of.WriteLine( "  height: 18px;" );
                    of.WriteLine( "  width: 10px;" );
                    of.WriteLine( "  border: 1px solid black;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "#fwi_content .keytext {" );
                    of.WriteLine( "  display: inline-block;" );
                    of.WriteLine( "  height: 20px;" );
                    of.WriteLine( "  width: 90px;" );
                    of.WriteLine( "  padding-left: 5px;" );
                    of.WriteLine( "  vertical-align: top;" );
                    of.WriteLine( "  text-align: left;" );
                    of.WriteLine( "  padding-top: 2px;" );
                    of.WriteLine( "  font-size: 14px;" );
                    of.WriteLine( "  font-family: arial;" );
                    of.WriteLine( "  font-weight: bold;" );
                    of.WriteLine( "}" );

                    // Modification to the original CSS by beteljuice
                    of.WriteLine( "#fwi_content .fwi_key {" ); // because I like it better
                    of.WriteLine( "   height: 45px;" );
                    of.WriteLine( "}" );
                    of.WriteLine( "#fwi_content .keytext {" ); // to facilitate the value ranges in the Legend
                    of.WriteLine( "  width: 180px;" );
                    of.WriteLine( "}" );

                    // Beteljuice PATCH for Large Lists (eg 10 year data from pwd112
                    of.WriteLine( "/* Beteljuice PATCH for Large Lists (eg 10 year data from pwd112 */" );
                    of.WriteLine( "#fwi_data { overflow: hidden; }" );
                    of.WriteLine( "#fwi_data thead, #fwi_data tbody, #fwi_data tr, #fwi_data td, #fwi_data th { display: block; }" );
                    of.WriteLine( "#fwi_data tr::after {" );
                    of.WriteLine( "        content: ' ';" );
                    of.WriteLine( "        display: block;" );
                    of.WriteLine( "        visibility: hidden;" );
                    of.WriteLine( "        clear: both;" );
                    of.WriteLine( "      }" );
                    of.WriteLine( "#fwi_data tbody {" );
                    of.WriteLine( "    scrollbar-width: thin;" );
                    of.WriteLine( "    height: 500px;" ); /* this is the height of the scrollable table content */
                    of.WriteLine( "    -webkit-overflow-scrolling: touch;" ); /* Lets it scroll lazy HAR: Like this best */
                    //          of.WriteLine("    -webkit-overflow-scrolling: auto; /* Stops scrolling immediately */");
                    of.WriteLine( "    overflow-y: auto;" );
                    of.WriteLine( "    }" );
                    of.WriteLine( "#fwi_data thead {" );
                    //          of.WriteLine("    /* fallback */");
                    of.WriteLine( "    width: 95%;" );
                    //          of.WriteLine("    /* minus scroll bar width */");
                    of.WriteLine( "    width: calc(100% - 17px);" );
                    of.WriteLine( "    background-color: #b0b0b0;" );
                    of.WriteLine( "}" );
                    of.WriteLine( "#fwi_content #fwi_data td, #fwi_content #fwi_data th {" );
                    //          of.WriteLine("/*  width: 13.4%;*/");
                    of.WriteLine( "  width: 13.25%;" );
                    of.WriteLine( "    text-align: center;" );
                    of.WriteLine( "    float: left;" );
                    of.WriteLine( "}" );

                    of.WriteLine( "</style>" );
                    break;

                case 2:
                    // overload call met CurrentValue voor de colour bar met slider
                    break;

                case 3: // Start the table and the header. No variables here because the units are fixed because of the equations
                    of.WriteLine( $"<table id=\"fwi_data\">" );
                    of.WriteLine( "<thead>" );
                    of.WriteLine( "<tr >" );
                    of.WriteLine( $"<th style=\"text-align:center;border-radius: 13px 0px 0px 0px; \">{Sup.GetCUstringValue( "pwsFWI", "Date", "Date", false )}</th>" );
                    of.WriteLine( $"<th style=\"text-align:center;\">{Sup.GetCUstringValue( "pwsFWI", "Temperature", "Temp.", false )} (&deg;C)</th>" );
                    of.WriteLine( $"<th style=\"text-align:center;\">{Sup.GetCUstringValue( "pwsFWI", "WindSpeed", "Wind Speed", false )} (km{Sup.PerHour})</th >" );
                    of.WriteLine( $"<th style=\"text-align:center;\">{Sup.GetCUstringValue( "pwsFWI", "Rain", "Rain", false )} (mm)</th>" );
                    of.WriteLine( $"<th style=\"text-align:center;\">{Sup.GetCUstringValue( "pwsFWI", "RelativeHumidity", "RH", false )}</th>" );
                    of.WriteLine( $"<th style=\"text-align:center;\">{Sup.GetCUstringValue( "pwsFWI", "DaysSinceRain", "Days since Rain", false )}</th>" );
                    of.WriteLine( "<th style=\"text-align:center; min-width: 70px; border-radius: 0px 13px 0px 0px;\">pwsFWI</th>" );
                    of.WriteLine( "</tr>" );
                    of.WriteLine( "</thead>" );
                    of.WriteLine( "<tbody>" );
                    break;

                case 4:
                    // Write out the table. This has become mopre complex since the prediction has been introduced.
                    //
                    if ( DoPrediction )
                    {
                        of.WriteLine( $"<tr style=\"text-align:center; {PredictionBackground};\"><td colspan=\"7\" style=\"width: calc(100% - 17px);\">" );
                        of.WriteLine( $"{Sup.GetCUstringValue( "pwsFWI", "PredictionMessage", "Prediction is active (including today!), values are above red line.", false )}" );
                        of.WriteLine( "</td></tr>" );
                    }

                    for ( i = FWIlist.Count - 1; i >= NrOfSmoothingDays; i-- )
                    {
                        string PredictionSeparation = "border-bottom: 1px solid red;";
                        string tmpBackground;

                        if ( DoPrediction && i > FWIlist.Count - NrOfPredictiveDays )
                            tmpBackground = PredictionBackground;
                        else if ( DoPrediction && i == FWIlist.Count - NrOfPredictiveDays )
                        {
                            tmpBackground = PredictionBackground + PredictionSeparation;
                        }
                        else
                            tmpBackground = "";

                        fmtindex = SetFmtIndex( i );

                        of.WriteLine( $"<tr style=\"{tmpBackground}\">\n" );
                        of.WriteLine( $"<td>{FWIlist[ i ].Date}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].T.ToString( "F1", CultureInfo.CurrentCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].Wind.ToString( "F1", CultureInfo.CurrentCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].Rain.ToString( "F1", CultureInfo.CurrentCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].RH.ToString( "F2", CultureInfo.CurrentCulture )}</td>" );
                        of.WriteLine( $"<td>{FWIlist[ i ].DryPeriod.ToString( "D", CultureInfo.CurrentCulture )}</td>" );

                        if ( i == NrOfSmoothingDays )
                            of.WriteLine( $"<td style=\"background:{fmtstring[ (int) fmtindex ]};border-radius: 0px 0px 13px 0px; \">{FWIlist[ i ].SmoothedFWI.ToString( "F1", CultureInfo.CurrentCulture )}</td>" );
                        else
                            of.WriteLine( $"<td style=\"background:{fmtstring[ (int) fmtindex ]};border-radius: 0px 0px 0px 0px; \">{FWIlist[ i ].SmoothedFWI.ToString( "F1", CultureInfo.CurrentCulture )}</td>" );

                        of.WriteLine( $"</tr>" );
                    }

                    of.WriteLine( "</tbody></table>" );
                    of.WriteLine( "</div> <!--END fwi_inner-->" );
                    of.WriteLine( "</div> <!--END fwi_content-->" );
                    of.WriteLine( "<!--END FWI inject -->" );
                    break;
            }
        }

        private void InjectBeteljuiceStyle( StreamWriter of, double CurrentValue )
        {
            of.WriteLine( "<!--start messing by beteljuice-->" );
            of.WriteLine( "<div id =\"fwi_content\">" );
            of.WriteLine( $"<div id=\"fwi_inner\" style=\"width:100%; margin: auto;\">" );

            of.WriteLine( "<div style=\"position: relative; top: 10px; text-align: center; width: 100%;\">" ); //id=\"Legend\"

            of.WriteLine( "<div style=\"position: relative; float: left; margin-right: 10px;  height: 40px;\" >" );
            of.WriteLine( $"<span class=\"keytext\" style=\"position: relative; top: 15px; font-weight: bold;\" >{Sup.GetCUstringValue( "pwsFWI", "Legend", "Legend", false )}:</span>" ); //display: none;
            of.WriteLine( "</div>" );

            of.WriteLine( "  <div style=\"display: inline-block; text-align: left;\">" );

            of.WriteLine( $"    <div class=\"key\" style=\"background-color: green;\"></div>" +
                         $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "Low", "Low", false )} (0-{dngrLevelValue[ (int) DngrLevel.green ]})</div>" );
            of.WriteLine( $"    <div class=\"key\" style=\"background-color: orange;\"></div>" +
                         $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "VeryHigh", "Very High", false )} ({dngrLevelValue[ (int) DngrLevel.yellow ]} - {dngrLevelValue[ (int) DngrLevel.orange ]})</div><br />" );

            of.WriteLine( $"    <div class=\"key\" style=\"margin-left: 25px; background-color: blue;\"></div>" +
                         $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "Moderate", "Moderate", false )} ({dngrLevelValue[ (int) DngrLevel.green ]} - {dngrLevelValue[ (int) DngrLevel.blue ]})</div>" );

            if ( WarningLevel == 5 )
                of.WriteLine( $"    <div class=\"key\" style=\"background-color: red;\"></div>" +
                             $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "Extreme", "Extreme", false )} ({dngrLevelValue[ (int) DngrLevel.orange ]}+)</div><br />" );
            else // WarningLevel == 6
                of.WriteLine( $"    <div class=\"key\" style=\"background-color: red;\"></div>" +
                             $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "Extreme", "Extreme", false )} ({dngrLevelValue[ (int) DngrLevel.orange ]} - {dngrLevelValue[ (int) DngrLevel.red ]})</div><br />" );

            of.WriteLine( $"    <div class=\"key\" style=\"margin-left: 50px; background-color: yellow;\" ></div>" +
                         $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "High", "High", false )} ({dngrLevelValue[ (int) DngrLevel.blue ]} - {dngrLevelValue[ (int) DngrLevel.yellow ]})</div>" );

            if ( WarningLevel == 6 )
                of.WriteLine( $"    <div class=\"key\" style=\"background-color: purple;\"></div>" +
                             $"<div class=\"keytext\">{Sup.GetCUstringValue( "pwsFWI", "Catastrophic", "Catastrophic", false )} ({dngrLevelValue[ (int) DngrLevel.red ]}+)</div><br />" );
            else
                of.WriteLine( "    <br />" );

            of.WriteLine( "  <br />" );
            of.WriteLine( "  </div>" );
            of.WriteLine( "</div>" );

            // So now continue with the actual key and slider
            //
            of.WriteLine( "<table class=\"fwi_key\">" );
            of.WriteLine( "<tbody>" );
            of.WriteLine( "<tr><td>" );

            if ( UseFireImage )
            {
                const string Base64Img = "data:image/gif;base64,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";

                of.WriteLine( "<span style='position: absolute; display: inline-block; top: 1px; width: 45px; left: calc(100% - 48px); padding-top: 2px;'>" );
                of.WriteLine( $"    <img alt='Flame' src='{Base64Img}' height='24'/>&nbsp;" );
                of.WriteLine( $"    <img alt='Flame' src='{Base64Img}' height='24'/>" );
                of.WriteLine( "</span>" );
            }

            of.WriteLine( $"<div id=\"moveit\" style=\"padding: 0px; margin: 0px; position: relative; left: 0px; top: 8px; " +
              $"width: {Math.Min( CurrentValue / ExtremeFWIvalue * 100, 100 ).ToString( "F1", NumberFormatInfo.InvariantInfo )}%;\">" );

            of.WriteLine( "<div id='pointer'>&#9650;" );
            //of.WriteLine("  <!-- Current Value - INSERT CURRENT VALUE TEXT -->");
            of.WriteLine( $"  <div id ='cv'> {CurrentValue.ToString( "F1", NumberFormatInfo.InvariantInfo )} </div>" );
            of.WriteLine( "</div>" );
            of.WriteLine( "</div>" );
            of.WriteLine( "</td>" );
            of.WriteLine( "</tr>" );
            of.WriteLine( "</tbody>" );
            of.WriteLine( "</table>" );
        }

        #endregion

        #region Supportfunctions Prediction and Format

        async Task<bool> AddPrediction( string URL, List<DayfileValue> ThisList )
        {
            // Get the interface and loop over the days to extract the data. Fill in the DayfileValue structure
            // Note: the units in the interface always seem to be metric. That in itself may be a problem because the conversion
            // takes place by assignment to the localFWI struct. Which is the locigal place to do because the owner of the weatherstation
            // has given this as units.
            //
            // If the user uses imperial units and the prediction is metric, the metric numbers will be amended by the imperial
            // to metric conversion despite the fact that they are already metric. I do not have a solution at the moment.
            //
            // Next is my API URL for testing etc... I deliver without a default because it is userspecific
            // string from http://api.yourweather.co.uk/
            //

            Sup.LogDebugMessage( "XML AddPrediction - start" );

            Uri xmlAPI = new Uri( URL );
            string XMLresult = await Isup.GetUrlDataAsync( xmlAPI );

            DayfileValue ThisValue = new DayfileValue();
            bool retval = false;

            if ( !String.IsNullOrEmpty( XMLresult ) )
            {
                try
                {
                    XElement localWeather = XElement.Parse( XMLresult );

                    IEnumerable<XElement> de =
                      from el in localWeather.Descendants( "day" )
                      select el;

                    // Required to use the SetExtraValues procedure
                    Dayfile tmpDayfile = new Dayfile( Sup );

                    foreach ( XElement el in de ) // loop over the days in  the prediction
                    {
                        float lowHum = 0;

                        Sup.LogTraceVerboseMessage( $"XML AddPrediction: {el}" );

                        ThisValue.LowHumidity = 100; // init at highest value possible

                        IEnumerable<XElement> hour =
                          from hr in el.Descendants( "hour" )
                          select hr;

                        foreach ( XElement hr in hour ) // loop over the days in  the prediction to get the lowest estimate of RH
                        {
                            lowHum = Convert.ToSingle( hr.Element( "humidity" ).Attribute( "value" ).Value, CultureInfo.InvariantCulture );

                            if ( lowHum < ThisValue.LowHumidity )
                                ThisValue.LowHumidity = lowHum;
                        }

                        ThisValue.ThisDate = DateTime.ParseExact( el.Attribute( "value" ).Value, "yyyyMMdd", CultureInfo.InvariantCulture );

                        // We require conversions if the units used are not deg Celsius, km/h, mm, hPa
                        // So we make an intermediate value while storing in xxxxAPI valiable. That variable contains the non standard value
                        // (eg m/s iso km/hr) Then we assign the API variable into the DayfileValue list entry which will be added to the list.
                        // As a result that assignment converts back to the km/hr unit. 
                        // Ridiculous, but thats life of a free interface.

                        // If user uses Fahrenheit, but interface delivers Celsius so we convert that to Fahrenheit and then later
                        // assign it to the FWIValue structure element which assignment converts it back to Celsius
                        // For Wind and Rain similar 
                        Temp t = new Temp( TempDim.celsius );
                        Wind w = new Wind( WindDim.kmh, Sup );
                        Rain r = new Rain( RainDim.millimeter );

                        ThisValue.MaxTemp = (float) Sup.StationTemp.Convert( TempDim.celsius, Sup.StationTemp.Dim, Convert.ToSingle( el.Element( "tempmax" ).Attribute( "value" ).Value, CultureInfo.InvariantCulture ) );
                        ThisValue.HighAverageWindSpeed = (float) Sup.StationWind.Convert( WindDim.kmh, Sup.StationWind.Dim, Convert.ToSingle( el.Element( "wind" ).Attribute( "value" ).Value, CultureInfo.InvariantCulture ) );
                        ThisValue.TotalRainThisDay = (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, Convert.ToSingle( el.Element( "rain" ).Attribute( "value" ).Value, CultureInfo.InvariantCulture ) );

                        Sup.LogTraceInfoMessage( "XML AddPrediction - The data:" );
                        Sup.LogTraceInfoMessage( $"ThisValue converted date: {ThisValue.ThisDate:dd-MM-yyyy}" );
                        Sup.LogTraceInfoMessage( $"ThisValue converted MaxTemp: {ThisValue.MaxTemp:F1}" );
                        Sup.LogTraceInfoMessage( $"ThisValue converted LowHumidity: {ThisValue.LowHumidity:F1}" );
                        Sup.LogTraceInfoMessage( $"ThisValue converted High Av. Windspeed: {ThisValue.HighAverageWindSpeed:F1}" );
                        Sup.LogTraceInfoMessage( $"ThisValue converted Rain This Day: {ThisValue.TotalRainThisDay:F1}" );

                        // The actual carry over from the history is done in SetExtraValues
                        //
                        if ( ThisValue.TotalRainThisDay > 0 ) { ThisValue.WetPeriod = 1; ThisValue.DryPeriod = 0; }
                        else { ThisValue.DryPeriod = 1; ThisValue.WetPeriod = 0; }

                        ThisList.Add( ThisValue );

                        tmpDayfile.SetExtraValues( ThisList );
                    }

                    tmpDayfile.Dispose();
                    retval = true; //success, we have a go for prediction.
                }
                catch ( Exception e )
                {
                    Sup.LogTraceErrorMessage( $"XML AddPrediction: {e.Message}" );
                    throw;
                }
            }
            // else no data, return, no prediction can be made; retval default is false

            Sup.LogTraceInfoMessage( $"XML AddPrediction - retval = {retval}" );

            return retval;
        }

        private bool RemovePrediction( List<DayfileValue> ThisList )
        {
            int i;
            bool retval = true;

            //Sup.LogTraceInfoMessage( "RemovePrediction - start" );

            // There are five (5) days of prediction, all added at the end of the list
            // So we're going to remove the 5 last elements of the list
            for ( i = 5; i > 0; i-- )
            {
                if ( !ThisList.Remove( ThisList.Last() ) )
                {
                    //Sup.LogTraceErrorMessage( "RemovePrediction - Fail" );
                    retval = false;
                }
            }

            return retval;
        }

        private DngrLevel SetFmtIndex( int i )
        {
            DngrLevel fmtindex;

            if ( FWIlist[ i ].SmoothedFWI <= dngrLevelValue[ (int) DngrLevel.green ] )
                fmtindex = DngrLevel.green;
            else if ( FWIlist[ i ].SmoothedFWI <= dngrLevelValue[ (int) DngrLevel.blue ] )
                fmtindex = DngrLevel.blue;
            else if ( FWIlist[ i ].SmoothedFWI <= dngrLevelValue[ (int) DngrLevel.yellow ] )
                fmtindex = DngrLevel.yellow;
            else if ( FWIlist[ i ].SmoothedFWI <= dngrLevelValue[ (int) DngrLevel.orange ] )
                fmtindex = DngrLevel.orange;
            else if ( WarningLevel == 5 )
            {
                fmtindex = DngrLevel.red;
            }
            else // WarningLevel == 6
            {
                if ( FWIlist[ i ].SmoothedFWI <= dngrLevelValue[ (int) DngrLevel.red ] )
                    fmtindex = DngrLevel.red;
                else
                    fmtindex = DngrLevel.purple;
            }

            return ( fmtindex );
        }

        #endregion

        #region IDisposable

        private bool disposedValue; // To detect redundant calls

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~PwsFWI()
        {
            Dispose( false );
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( true );
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        #endregion IDisposable
    }
}