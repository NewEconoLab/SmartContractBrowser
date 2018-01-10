//function getRequestIP(){
	$.getJSON('http://gd.geobytes.com/GetCityDetails?callback=?', function(data) {
		$('#ip').val(data.geobytesipaddress);
		//console.log(JSON.stringify(data, null, 2));
	});
//}