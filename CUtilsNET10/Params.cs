using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CumulusUtils
{
    public class IniFile
    {
        private readonly string _filePath;
        private readonly List<IniLine> _lines = new List<IniLine>();
        readonly CuSupport Sup;

        private class IniLine
        {
            public string RawLine { get; set; }
            public bool IsSection { get; set; }
            public bool IsKeyValuePair { get; set; }
            public bool IsCommentOrEmpty { get; set; }
            public string SectionName { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public IniFile( string filePath, CuSupport s )
        {
            _filePath = filePath;
            Sup = s;

            LoadFromFile();
        }

        private void LoadFromFile()
        {
            if ( !File.Exists( _filePath ) ) return;

            string currentSection = "";
            foreach ( var line in File.ReadAllLines( _filePath ) )
            {
                var trimmed = line.Trim();

                if ( string.IsNullOrEmpty( trimmed ) || trimmed.StartsWith( ";" ) || trimmed.StartsWith( "#" ) )
                {
                    _lines.Add( new IniLine { RawLine = line, IsCommentOrEmpty = true } );
                }
                else if ( trimmed.StartsWith( "[" ) && trimmed.EndsWith( "]" ) )
                {
                    currentSection = trimmed.Substring( 1, trimmed.Length - 2 ).Trim();
                    _lines.Add( new IniLine { RawLine = line, IsSection = true, SectionName = currentSection } );
                }
                else if ( trimmed.Contains( "=" ) )
                {
                    int idx = line.IndexOf( '=' );
                    string key = line.Substring( 0, idx ).Trim();
                    string val = line.Substring( idx + 1 ).Trim();
                    _lines.Add( new IniLine { RawLine = line, IsKeyValuePair = true, SectionName = currentSection, Key = key, Value = val } );
                }
                else
                {
                    _lines.Add( new IniLine { RawLine = line, IsCommentOrEmpty = true } );
                }
            }
        }

        // 1) GetValue: Geef waarde terug. Als deze niet bestaat, maak hem aan.
        public string GetValue( string section, string param, string defaultValue )
        {
            var item = FindKeyValuePair( section, param );
            if ( item != null )
            {
                return item.Value;
            }

            SetValue( section, param, defaultValue );
            return defaultValue;
        }

        // 2) SetValue: Update een parameter of voeg deze (en eventueel de sectie) toe.
        public void SetValue( string section, string param, string value )
        {
            var item = FindKeyValuePair( section, param );
            if ( item != null )
            {
                item.Value = value;
                item.RawLine = $"{item.Key}={value}";
                return;
            }

            EnsureSectionExists( section );

            var newLine = new IniLine
            {
                IsKeyValuePair = true,
                SectionName = section,
                Key = param,
                Value = value,
                RawLine = $"{param}={value}"
            };

            int lastIndexInSection = _lines.FindLastIndex( l => l.SectionName == section );
            _lines.Insert( lastIndexInSection + 1, newLine );
        }

        // 3) Remove: Verwijder een parameter uit een specifieke sectie.
        public void Remove( string section, string param )
        {
            var item = FindKeyValuePair( section, param );
            if ( item != null )
            {
                _lines.Remove( item );
            }
        }

        // 4) SaveToFile: Schrijf alles weg in de originele volgorde inclusief commentaar.
        public void SaveToFile()
        {
            Sup.LogMessage( $"Writing out: {_filePath}", TraceLevel.Info );

            var output = _lines.Select( l => l.RawLine );
            File.WriteAllLines( _filePath, output );
        }

        // 6) CleanUp functionaliteit tijdens initialisatie
        public void CheckAndCleanUp()
        {
            string cleanUpTrigger = Sup.GetUtilsIniValue( "General", "ParamCleanUp", "false" );

            if ( cleanUpTrigger.Equals( "true", CUtils.Cmp ) )
            {
                string invalidParamsPath = "utils/InValidParameters.txt";

                if ( !File.Exists( invalidParamsPath ) ) return;

                Sup.LogMessage( $"Cleaning up {_filePath}", TraceLevel.Info );

                // Haal de naam van het huidige INI-bestand op (bijv. "cumulus.ini")
                string currentFileName = Path.GetFileName( _filePath );
                bool targetFileFound = false;

                foreach ( var line in File.ReadAllLines( invalidParamsPath ) )
                {
                    var trimmed = line.Trim();

                    // Controleer of de regel een bestandskop is (bijv. === cumulus.ini)
                    if ( trimmed.StartsWith( "===" ) )
                    {
                        // Als de kop de naam van dit bestand bevat, starten we met opschonen
                        targetFileFound = trimmed.Contains( currentFileName, CUtils.Cmp );
                        continue;
                    }

                    // Als we in de juiste sectie van het bestand zijn, verwerk de parameter
                    if ( targetFileFound )
                    {
                        if ( trimmed.StartsWith( "[" ) && trimmed.Contains( "]" ) )
                        {
                            int closeBracketIdx = trimmed.IndexOf( ']' );
                            string section = trimmed.Substring( 1, closeBracketIdx - 1 ).Trim();
                            string param = trimmed.Substring( closeBracketIdx + 1 ).Trim();

                            if ( !string.IsNullOrEmpty( section ) && !string.IsNullOrEmpty( param ) )
                            {
                                Remove( section, param );
                            }
                        }
                    }
                }
            }
        }

        private IniLine FindKeyValuePair( string section, string param )
        {
            return _lines.FirstOrDefault( l => l.IsKeyValuePair &&
                                              l.SectionName.Equals( section, StringComparison.OrdinalIgnoreCase ) &&
                                              l.Key.Equals( param, StringComparison.OrdinalIgnoreCase ) );
        }

        private void EnsureSectionExists( string section )
        {
            bool sectionExists = _lines.Any( l => l.IsSection && l.SectionName.Equals( section, StringComparison.OrdinalIgnoreCase ) );
            if ( !sectionExists )
            {
                _lines.Add( new IniLine { RawLine = "", IsCommentOrEmpty = true } ); // Witregel voor nette opmaak
                _lines.Add( new IniLine { RawLine = $"[{section}]", IsSection = true, SectionName = section } );
            }
        }
    }
}
