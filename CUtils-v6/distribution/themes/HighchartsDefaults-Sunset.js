/* *
 *
 *  (c) 2010-2020 Highsoft AS
 *
 *  Author: Øystein Moseng
 *
 *  License: www.highcharts.com/license
 *
 *  Accessible high-contrast theme for Highcharts. Considers colorblindness and
 *  monochrome rendering.
 *
 *  Modified: Hans Rottier for CumulusUtils under the free Non-Comercial Licence
 *  Date: 1-8-2020
 *
 * */

Highcharts.theme = {
    colors: ['#FDD089', '#FF7F79', '#A0446E', '#251535'],
    colorAxis: {
        maxColor: '#60042E',
        minColor: '#FDD089'
    },
    plotOptions: {
        map: {
            nullColor: '#fefefc'
        }
    },
    navigator: {
        series: {
            color: '#FF7F79',
            lineColor: '#A0446E'
        }
    }
};

// Apply the theme
var highchartsOptions = Highcharts.setOptions(Highcharts.theme);
