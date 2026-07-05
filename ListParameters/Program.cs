using System.Text;
using System.Text.RegularExpressions;

namespace IniParser
{
    class Program
    {
        static void Main( string[] args )
        {
            // Paden configureren
            string sourceFolder = @"C:\Users\hansr\Documents\GitHub\CutilsFramework\CUtilsNET10\";
            string outputPath = @"C:\Users\hansr\Documents\GitHub\CutilsFramework\ListParameters\InvalidParameters.txt";
            string baseFolder = @"C:\Users\hansr\Documents\GitHub\CutilsFramework\ListParameters\";

            string cumulusIniPath = Path.Combine( baseFolder, "cumulusutils.ini" );

            try
            {
                // 1. Bepaal de taal dynamisch uit cumulusutils.ini
                string languageCode = GetLanguageFromIni( cumulusIniPath );
                string cuStringsIniPath = Path.Combine( baseFolder, $"CUstrings{languageCode}.ini" );

                // 2. Verzamel alle gebruikte parameters apart per type
                var usedCumulusParams = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
                var usedCuStringsParams = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

                // Scan de code eenmalig voor beide patronen
                FindAllUsedParametersInCode( sourceFolder, usedCumulusParams, usedCuStringsParams );

                var allUnusedEntries = new List<string>();

                // 3. Scan cumulusutils.ini (Excluseer ExtraSensors)
                if ( File.Exists( cumulusIniPath ) )
                {
                    allUnusedEntries.Add( "=== Ongebruikte parameters in: cumulusutils.ini ===" );
                    var unused = FindUnusedIniEntries( cumulusIniPath, usedCumulusParams, "ExtraSensors" );
                    allUnusedEntries.AddRange( unused.Count > 0 ? unused : new List<string> { "Geen ongebruikte parameters gevonden." } );
                    allUnusedEntries.Add( string.Empty );
                }

                // 4. Scan de dynamische CUstringsXX.ini (Excluseer Compiler)
                if ( File.Exists( cuStringsIniPath ) )
                {
                    allUnusedEntries.Add( $"=== Ongebruikte parameters in: {Path.GetFileName( cuStringsIniPath )} ===" );
                    var unused = FindUnusedIniEntries( cuStringsIniPath, usedCuStringsParams, "Compiler" );
                    allUnusedEntries.AddRange( unused.Count > 0 ? unused : new List<string> { "Geen ongebruikte parameters gevonden." } );
                    allUnusedEntries.Add( string.Empty );
                }
                else
                {
                    allUnusedEntries.Add( $"=== Fout: Bestand niet gevonden: {Path.GetFileName( cuStringsIniPath )} ===" );
                    allUnusedEntries.Add( string.Empty );
                }

                // 5. Schrijf resultaten weg
                File.WriteAllLines( outputPath, allUnusedEntries, Encoding.UTF8 );

                Console.WriteLine( $"Analyse voltooid. Resultaat geschreven naar: {outputPath}" );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( $"Er is een fout opgetreden: {ex.Message}" );
            }
        }

        static string GetLanguageFromIni( string iniPath )
        {
            string defaultLanguage = "NL"; // Terugvaloptie
            if ( !File.Exists( iniPath ) ) return defaultLanguage;

            var lines = File.ReadAllLines( iniPath );
            string currentSection = string.Empty;

            foreach ( var line in lines )
            {
                string trimmed = line.Trim();

                if ( string.IsNullOrEmpty( trimmed ) || trimmed.StartsWith( ";" ) || trimmed.StartsWith( "#" ) )
                    continue;

                if ( trimmed.StartsWith( "[" ) && trimmed.EndsWith( "]" ) )
                {
                    currentSection = trimmed.Substring( 1, trimmed.Length - 2 ).Trim();
                    continue;
                }

                if ( string.Equals( currentSection, "General", StringComparison.OrdinalIgnoreCase ) )
                {
                    int equalIndex = trimmed.IndexOf( '=' );
                    if ( equalIndex > 0 )
                    {
                        string key = trimmed.Substring( 0, equalIndex ).Trim();
                        if ( string.Equals( key, "Language", StringComparison.OrdinalIgnoreCase ) )
                        {
                            string value = trimmed.Substring( equalIndex + 1 ).Trim();
                            if ( value.Length >= 2 )
                            {
                                return value.Substring( 0, 2 ).ToUpper(); // Pak eerste 2 tekens (bijv. 'nl' van 'nl-NL')
                            }
                        }
                    }
                }
            }

            return defaultLanguage;
        }

        static void FindAllUsedParametersInCode( string folderPath, HashSet<string> cumulusParams, HashSet<string> cuStringsParams )
        {
            // Patroon 1: GetUtilsIniValue("Sectie", "Sleutel")
            string cumulusPattern = @"GetUtilsIniValue\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""";

            // Patroon 2: GetCUstringValue("Sectie", "Sleutel", "Waarde", boolean)
            string cuStringsPattern = @"GetCUstringValue\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""";

            var regexCumulus = new Regex( cumulusPattern, RegexOptions.Compiled );
            var regexCuStrings = new Regex( cuStringsPattern, RegexOptions.Compiled );

            var files = Directory.GetFiles( folderPath, "*.cs", SearchOption.TopDirectoryOnly );

            foreach ( var file in files )
            {
                string content = File.ReadAllText( file );

                // Match cumulusutils parameters
                var cumulusMatches = regexCumulus.Matches( content );
                foreach ( Match match in cumulusMatches )
                {
                    string section = match.Groups[ 1 ].Value.Trim();
                    string key = match.Groups[ 2 ].Value.Trim();
                    cumulusParams.Add( $"{section}|{key}" );
                }

                // Match CUstringsXX parameters
                var cuStringsMatches = regexCuStrings.Matches( content );
                foreach ( Match match in cuStringsMatches )
                {
                    string section = match.Groups[ 1 ].Value.Trim();
                    string key = match.Groups[ 2 ].Value.Trim();
                    cuStringsParams.Add( $"{section}|{key}" );
                }
            }
        }

        static List<string> FindUnusedIniEntries( string iniPath, HashSet<string> usedParameters, string sectionToExclude )
        {
            var unusedReport = new List<string>();
            var lines = File.ReadAllLines( iniPath );
            string currentSection = string.Empty;

            foreach ( var line in lines )
            {
                string trimmed = line.Trim();

                if ( string.IsNullOrEmpty( trimmed ) || trimmed.StartsWith( ";" ) || trimmed.StartsWith( "#" ) )
                {
                    continue;
                }

                if ( trimmed.StartsWith( "[" ) && trimmed.EndsWith( "]" ) )
                {
                    currentSection = trimmed.Substring( 1, trimmed.Length - 2 ).Trim();
                    continue;
                }

                if ( !string.IsNullOrEmpty( sectionToExclude ) && string.Equals( currentSection, sectionToExclude, StringComparison.OrdinalIgnoreCase ) )
                {
                    continue;
                }

                int equalIndex = trimmed.IndexOf( '=' );
                if ( equalIndex > 0 && !string.IsNullOrEmpty( currentSection ) )
                {
                    string key = trimmed.Substring( 0, equalIndex ).Trim();
                    string combinedKey = $"{currentSection}|{key}";

                    if ( !usedParameters.Contains( combinedKey ) )
                    {
                        unusedReport.Add( $"[{currentSection}] {key}" );
                    }
                }
            }

            return unusedReport;
        }
    }
}
