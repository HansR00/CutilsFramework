/*
 * Grid theme for Highcharts JS
 * Based on an original Highcharmts Theme
 *
 * DARK Theme - generalisation: Hans Rottier
 *              Based on a theme by Tony of BeaumarisWX
 *              
 */

Highcharts.theme = {
  colors: ["#2b908f", "#90ee7e", "#7798BF", "#f45b5b", "#aaeeee", "#ff0066", "#eeaaee", "#55BF3B", "#7798BF", "#DF5353", "#aaeeee"],
//  colors: ['red', 'lime', 'yellow', 'aqua', 'lightblue', 'orange', 'pink'],

    chart: {
        backgroundColor: {
            linearGradient: { x1: 0, y1: 0, x2: 1, y2: 1 },
            stops: [
                [0, '#2a2a2b'],
                [1, '#3e3e40']
            ]
        },
        plotBorderColor: '#606063',
    },

  title: {
      style: {
         color: '#E0E0E3',
      }
   },

   subtitle: {
      style: {
         color: '#E0E0E3',
      }
   },

   xAxis: {
      gridLineColor: '#707073',
      labels: {
         style: {
            color: '#E0E0E3'
         }
      },
      lineColor: '#707073',
      minorGridLineColor: '#505053',
      tickColor: '#707073',
      title: {
         style: {
            color: '#A0A0A3'

         }
      }
   },

   yAxis: {
      gridLineColor: '#707073',
      labels: {
         style: {
            color: '#E0E0E3'
         }
      },
      lineColor: '#707073',
      minorGridLineColor: '#505053',
      tickColor: '#707073',
      tickWidth: 1,
      title: {
         style: {
            color: '#A0A0A3'
         }
      },
   },

	tooltip: {
		backgroundColor:{
			linearGradient: { x1: 0, y1: 0, x2: 0, y2: 1 },
			stops:[
				[0, 'rgba(96, 96, 96, .8)'],
				[1, 'rgba(16, 16, 16, .8)']
			]
		},
		borderWidth: 0,
		style:{
			color:'#F0F0F0'
		}
	},

   plotOptions: {
      series: {
         dataLabels: {
            color: '#B0B0B3'
         },
         marker: {
            lineColor: '#333'
         },
         lineWidth: 2
      },
/*
      area: {
         color: 'rgba(100,200,255,0.8)',
         fillColor: {
               linearGradient: {
                 x1: 0,
                 y1: 0,
                 x2: 0,
                 y2: 1
               },
               stops: [
                 [0, "rgba(10,80,255,0.6)"],
                 [1, "rgba(10,150,50,0.1)"]
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
         color: '#E0E0E3'
      },
      itemHoverStyle: {
         color: '#FFF'
      },
      itemHiddenStyle: {
         color: '#606063'
      }
   },
   credits: {
      style: {
         color: '#666'
      }
   },
   labels: {
      style: {
         color: '#E0E0E3'
      }
   },

   navigation: {
      buttonOptions: {
         symbolStroke: '#DDDDDD',
         theme: {
            fill: '#505053'
         }
      }
   },

   // stock charts (range selection)

   rangeSelector: {
      buttonTheme: {
         fill: '#505053',
         stroke: '#000000',
         style: {
            color: '#CCC'
         },
        states: {
           hover: {
               fill: '#707073',
              stroke: '#000000',
               style: {
                  color: 'white'
               }
            },
            select: {
               fill: '#000003',
               stroke: '#000000',
               style: {
                  color: 'white'
              }
            }
         }
      },

      inputBoxBorderColor: '#505053',
      inputStyle: {
         backgroundColor: '#333',
         color: 'silver'
      },
      labelStyle: {
         color: 'silver'
      }
   },
};


// Apply the theme
var highchartsOptions = Highcharts.setOptions(Highcharts.theme);
