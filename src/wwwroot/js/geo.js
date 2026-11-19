function getCurrentLocation() {
    try {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(showCurrentLocation, errorHandler);
        } else {
            errorHandler();
        }
    }
    catch (e) {
        errorHandler();
    }
}

function errorHandler() {
    NerdDinner.getCurrentLocationByIpAddress();
}

function showCurrentLocation(position) {
    NerdDinner.getCurrentLocationByLatLong(position.coords.latitude, position.coords.longitude);
}
