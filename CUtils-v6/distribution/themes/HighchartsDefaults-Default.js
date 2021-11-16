/*
 * Grid theme for Highcharts JS
 * Based on a the default Cumulus Theme - grid.js in the HighCharts distribution
 *
 *  Modified: Hans Rottier for CumulusUtils under the free Non-Comercial Licence
 *  Date: 1-8-2020
 *
 */

Highcharts.theme = {
  colors: ['#058DC7', '#50B432', '#ED561B', '#DDDF00', '#24CBE5', '#64E572', '#FF9655', '#FFF263', '#6AF9C4'],

    chart: {
        backgroundColor: {
            linearGradient: { x1: 0, y1: 0, x2: 1, y2: 1 },
            stops: [
                [0, '#FFFFFF'],
                [1, '#F0F0FF']
            ]
        },
        borderWidth:2,
        plotBorderColor: '#E6FFFFFF',
        plotShadow: true,
        plotBorderWidth: 1
    },

  title: {
      style: {
         color: '#000000',
         font: 'bold 16px Verdana, sans-serif'
      }
   },

   subtitle: {
      style: {
         color: '#666666',
         font: 'bold 12px Verdana, sans-serif'
      }
   },

   xAxis: {
      lineColor: '#000000',
      labels: {
         style: {
            color: '#000000',
            font: 'bold 11px Verdana, sans-serif'
         }
      },
      gridLineWidth: 1,
//      gridLineColor: '#0000B4',
//      minorGridLineColor: '#0000B4',
      tickColor: '#000000',
      title: {
         style: {
            color: '#333333',
            fontSize: 'bold',
            fontWeight: '12px',
            font: 'Verdana, sans-serif'
         }
      }
   },

   yAxis: {
      minorTickInterval: 'auto',
      lineColor: '#000000',
      lineWidth: 1,
      tickColor: '#000000',
      tickWidth: 1,
//      gridLineColor: '#0000B4',
//      minorGridLineColor: '#000000',
      labels: {
         style: {
            color: '#000000',
            font: '11px Verdana, sans-serif'
         }
      },
      title: {
         style: {
            color: '#333333',
            font: 'bold 12px Verdana, sans-serif'
         }
      },
   },
/*
	tooltip: {
		backgroundColor:{
			linearGradient: { x1: 0, y1: 0, x2: 0, y2: 1 },
			stops:[
				[0, '#ffffff'],
				[1, '#909090']
			]
		},
		borderWidth: 0,
		style:{
			color:'#20202F'
		}
	},
*/
   plotOptions: {
      series: {
         dataLabels: {
            color: '#000000'
         },
         marker: {
            lineColor: '#333333'
         },
         lineWidth: 2
      },

/*
      area: {
         color: 'rgba(100,200,255,0.8)',
         fillColor: {
               linearGradient: {
                 x1: 0,
                 y1: 1,
                 x2: 0,
                 y2: 0
               },
               stops: [
                 [0, "#99FFFFFF"],
                 [1, "#99000000"]
               ]
          },
      },
*/

      scatter: {
         marker: {symbol:'circle',radius:2}
      }
   },
   legend: {
      itemStyle: {
         color: 'black',
         font: 'bold 9pt Verdana, sans-serif'
      },
      itemHoverStyle: {
         color: '#039'
      },
      itemHiddenStyle: {
         color: 'grey'
      }
   },
   credits: {
      style: {
         color: '#grey'
      }
   },
   labels: {
      style: {
         color: '##9999BB'
      }
   },

   navigation: {
      buttonOptions: {
         theme: {
            stroke: '#CCCCCC'
         }
      }
   }
};

// Apply the theme
var highchartsOptions = Highcharts.setOptions(Highcharts.theme);
