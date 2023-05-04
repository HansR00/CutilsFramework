/*
 * StationMap - Part of CumulusUtils
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
using System.Globalization;
using System.IO;
using System.Text;

namespace CumulusUtils
{
    class StationMap
    {
        readonly CuSupport Sup;

        public StationMap( CuSupport s )
        {
            Sup = s;
        }

        // Image rotation docu for the WindArrow (image)
        //   https://github.com/Leaflet/Leaflet/issues/4029
        //   https://stackoverflow.com/questions/54882500/imageoverlay-position-by-center-point-set-rotation-and-scale
        //!! https://stackoverflow.com/questions/27092113/rotate-marker-based-on-driving-direction
        //   https://medium.com/@rikdeboer/leaflet-maps-marker-fun-games-53d81fdd2f52 (en zie referenties)
        //   http://plnkr.co/edit/72ywrO8pgmmxLW6Y8mcL?p=preview&preview // Interesting and Important link
        //   http://plnkr.co/edit/QioXRLA8PYnrlb2ZH7kr?p=preview&preview  Rotate marker extension
        //   https://github.com/bbecquet/Leaflet.RotatedMarker -> This is what it actually became. Included in Index.html

        // Image fixation techniques
        // https://stackoverflow.com/questions/42461756/how-to-set-correct-image-dimensions-by-latlngbounds-using-imageoverlay Interesting functions to convert points to latlon
        // https://stackoverflow.com/questions/37701211/custom-legend-image-as-legend-in-leaflet-map
        // 

        public void GenerateStationMap()
        {
            const int RoseSize = 200;
            const int ArrowSize = 150;

            float Latitude, Longitude, ArrowLatitude, ArrowLongitude;
            int CompassRoseType, WindArrowType;
            string StationName, StationDesc;

            Sup.LogDebugMessage( " GenerateStationMap: start" );

            if ( !CUtils.HasStationMapMenu ) return; // Don't generate, ignore everything, just get back.

            Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );
            Longitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Longitude", "" ), CUtils.Inv );
            ArrowLatitude = Convert.ToSingle( Sup.GetUtilsIniValue( "StationMap", "ArrowLatitude", Latitude.ToString( "F4", CUtils.Inv ) ), CUtils.Inv );
            ArrowLongitude = Convert.ToSingle( Sup.GetUtilsIniValue( "StationMap", "ArrowLongitude", Longitude.ToString( "F4", CUtils.Inv ) ), CUtils.Inv ) - (float) 0.0200;

            CompassRoseType = Convert.ToInt32( Sup.GetUtilsIniValue( "StationMap", "CompassRoseType", "1" ), CUtils.Inv );
            WindArrowType = Convert.ToInt32( Sup.GetUtilsIniValue( "StationMap", "WindArrowType", "1" ), CUtils.Inv );

            StationName = $"{Sup.GetCumulusIniValue( "Station", "LocName", "" )}";
            StationDesc = $"{Sup.GetCumulusIniValue( "Station", "LocDesc", "" )}";

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.StationMapOutputFilename}", false, Encoding.UTF8 ) )
            {
                if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                {
                    of.WriteLine( Sup.GenLeafletIncludes().ToString() );
                }

                of.WriteLine( "<style>" );
                of.WriteLine( "#CUStationMap {height: 750px; width: 100%;}" );
                of.WriteLine( "</style>" );
                of.WriteLine( "<div id='CUStationMap'></div>" );

                of.WriteLine( "<script>" );

                of.WriteLine( $"{GenerateLocalLeafletPluginRotatedMarker()}" );

                of.WriteLine( $"var StationMap = L.map('CUStationMap', {{scrollWheelZoom: false, zoomControl: false }});" );

                of.WriteLine( "L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', " +
                  "{attribution: '&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors'}).addTo(StationMap);" );

                // Place the windrose in an acceptable corner of the map in an acceptable size
                string RosePos = Sup.GetUtilsIniValue( "StationMap", "CompassRosePosition", "topright" ).ToLowerInvariant();
                of.WriteLine( $"var thisWindRose = L.control({{ position: '{RosePos}'}});" ); // Posible positions: 'topleft', 'topright', 'bottomleft' or 'bottomright'

                of.WriteLine( "thisWindRose.onAdd = function(StationMap) {" );
                of.WriteLine( "  var div = L.DomUtil.create('div', 'Wind Rose');" );
                of.WriteLine( $"  div.innerHTML = \"<img src='./CUicons/wind/CompassRose-{CompassRoseType}.png' height='{RoseSize}' width='{RoseSize}'>\";" );
                of.WriteLine( "  return div;" );
                of.WriteLine( "};" );

                of.WriteLine( "thisWindRose.addTo(StationMap);" );

                of.WriteLine( "var thisWindArrow = L.icon({" );
                of.WriteLine( $"  iconUrl: './CUicons/wind/WindArrow-{WindArrowType}.png'," );
                of.WriteLine( $"  iconSize:[{ArrowSize},{ArrowSize}]" );
                of.WriteLine( "});" );

                of.WriteLine( $"var markerWindArrow = L.marker([{ArrowLatitude.ToString( "F4", CUtils.Inv )}, " +
                                       $"{ArrowLongitude.ToString( "F4", CUtils.Inv )}], " +
                                       $"{{  icon: thisWindArrow, zIndexOffset: 10, opacity: 0.8 }}).addTo(StationMap);" );

                of.WriteLine( "markerWindArrow.setRotationOrigin('center center');" );
                of.WriteLine( "markerWindArrow.bindTooltip('<div style=\\\'font-size:200%; text-align:left; \\\'>" +
                  $"{Sup.GetCUstringValue( "StationMap", "WindInBf", "Wind", true )}: <span id=\\\'TT1\\\'></span> Bf<br/>" +
                  $"{Sup.GetCUstringValue( "StationMap", "Temperature", "Temperature", true )}: <span id=\\\'TT2\\\'></span> {Sup.StationTemp.Text()}<br/>" +
                  $"{Sup.GetCUstringValue( "StationMap", "Barometer", "Barometer", true )}: <span id=\\\'TT3\\\'></span> {Sup.StationPressure.Text()}<br/>" +
                  $"{Sup.GetCUstringValue( "StationMap", "Humidity", "Humidity", true )}: <span id=\\\'TT4\\\'></span> %<br/>" +
                  $"{Sup.GetCUstringValue( "StationMap", "Rain", "Rain", true )}: <span id =\\\'TT5\\\'></span> {Sup.StationRain.Text()}<br/>" +
                  $"</div>',{{offset: L.point(0, {RoseSize / 2}), permanent: true, direction: 'bottom'}});" ); // possibilities: right, left, top, bottom, center, auto

                of.WriteLine( $"var markerStation = L.marker([{Latitude.ToString( CUtils.Inv )}, {Longitude.ToString( CUtils.Inv )}]).addTo(StationMap);" );
                of.WriteLine( $"markerStation.bindPopup(\"<b>{StationName}</b><br/>{StationDesc}<br/>Lat: {Latitude} / Lon: {Longitude}<br/>\");" );

                of.WriteLine( $"StationMap.setView([{Latitude.ToString( "F4", CUtils.Inv )}, " +
                                                 $"{Longitude.ToString( "F4", CUtils.Inv )}], " +
                                                 $"{Sup.GetUtilsIniValue( "StationMap", "Zoomlevel", "13" )});" );

                of.WriteLine( "function UpdateWindArrow( newAngle, beaufort, temp, baro, humidity, rain ){" );
#if !RELEASE
                of.WriteLine( "  console.log('UpdateWindArrow (Angle)     : ' + newAngle);" );
                of.WriteLine( "  console.log('UpdateWindArrow (Wind force): ' + beaufort);" );
                of.WriteLine( "  console.log('UpdateWindArrow (Temperature): ' + temp);" );
                of.WriteLine( "  console.log('UpdateWindArrow (Barometer): ' + baro);" );
                of.WriteLine( "  console.log('UpdateWindArrow (Humidity): ' + humidity);" );
                of.WriteLine( "  console.log('UpdateWindArrow (Rain): ' + rain);" );
#endif
                of.WriteLine( "  markerWindArrow.setRotationAngle(newAngle);" );
                of.WriteLine( "  $('#TT1').html(beaufort);" );
                of.WriteLine( "  $('#TT2').html(temp);" );
                of.WriteLine( "  $('#TT3').html(baro);" );
                of.WriteLine( "  $('#TT4').html(humidity);" );
                of.WriteLine( "  $('#TT5').html(rain);" );
                of.WriteLine( "}" );

                of.WriteLine( "</script>" );
            }

            Sup.LogDebugMessage( " GenerateStationMap: End" );

            return;
        }

        #region MapObjectRotation

        static string GenerateLocalLeafletPluginRotatedMarker()
        {
            StringBuilder transport = new StringBuilder();

            transport.Append( "(function() {" );
            transport.Append( "var proto_initIcon = L.Marker.prototype._initIcon;" );
            transport.Append( "var proto_setPos = L.Marker.prototype._setPos;" );
            transport.Append( "var oldIE = (L.DomUtil.TRANSFORM === 'msTransform');" );
            transport.Append( "L.Marker.addInitHook(function() {var iconOptions = this.options.icon && this.options.icon.options;" );
            transport.Append( "var iconAnchor = iconOptions && this.options.icon.options.iconAnchor;if (iconAnchor){iconAnchor = (iconAnchor[0] + 'px ' + iconAnchor[1] + 'px');}" );
            transport.Append( "this.options.rotationOrigin = this.options.rotationOrigin || iconAnchor || 'center bottom';this.options.rotationAngle = this.options.rotationAngle || 0;this.on('drag', function(e) { e.target._applyRotation(); });});" );
            transport.Append( "L.Marker.include({_initIcon: function() {proto_initIcon.call(this);}," );
            transport.Append( "  _setPos: function(pos) {proto_setPos.call(this, pos);this._applyRotation();}," );
            transport.Append( "_applyRotation: function() {if (this.options.rotationAngle){this._icon.style[L.DomUtil.TRANSFORM + 'Origin'] = this.options.rotationOrigin;" );
            transport.Append( "if (oldIE){this._icon.style[L.DomUtil.TRANSFORM] = 'rotate(' + this.options.rotationAngle + 'deg)';}" );
            transport.Append( "else{this._icon.style[L.DomUtil.TRANSFORM] += ' rotateZ(' + this.options.rotationAngle + 'deg)';}}}," );
            transport.Append( "setRotationAngle: function(angle) {this.options.rotationAngle = angle;this.update();return this;}," );
            transport.Append( "setRotationOrigin: function(origin) {this.options.rotationOrigin = origin;this.update();return this;}});})();" );

            return transport.ToString();
        }

        #endregion

    }
}
