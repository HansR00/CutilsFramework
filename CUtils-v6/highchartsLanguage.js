https://docs.microsoft.com/en-gb/openspecs/windows_protocols/ms-lcid/70feba9f-294e-491e-b6eb-56532684c37f

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(lang);

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(lang);


Highcharts.lang = {
lang:
  {
	months: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
	shortMonths: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
	weekdays: [["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
  }
};

// Apply the language
var highchartsOptions = Highcharts.setOptions(Highcharts.lang);
