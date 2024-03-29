/* *
 *
 *  (c) 2010-2020 Highsoft AS
 *
 *  Author: Øystein Moseng
 *
 *  License: www.highcharts.com/license
 *
 *  Accessible high-contrast theme for Highcharts. Specifically tailored
 *  towards 3:1 contrast against white/off-white backgrounds. Neighboring
 *  colors are tested for color blindness.
 *
 *  Modified: Hans Rottier for CumulusUtils under the free Non-Comercial Licence
 *  Date: 1-8-2020
 *
 * */
Highcharts.theme = {
    colors: [
        '#5f98cf',
        '#434348',
        '#49a65e',
        '#f45b5b',
        '#708090',
        '#b68c51',
        '#397550',
        '#c0493d',
        '#4f4a7a',
        '#b381b3'
    ],
    navigator: {
        series: {
            color: '#5f98cf',
            lineColor: '#5f98cf'
        }
    }
};
// Apply the theme
var highchartsOptions = Highcharts.setOptions(Highcharts.theme);
