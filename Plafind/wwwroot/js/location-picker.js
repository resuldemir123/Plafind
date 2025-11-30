(function (window) {
    function init(options) {
        if (!window.google || !google.maps) {
            console.warn('Google Maps henüz yüklenmedi.');
            return;
        }

        var mapEl = document.getElementById(options.mapElementId || 'businessFormMap');
        if (!mapEl) {
            console.warn('Harita elementi bulunamadı.');
            return;
        }

        var latInput = document.getElementById(options.latInputId || 'Latitude');
        var lngInput = document.getElementById(options.lngInputId || 'Longitude');
        var addressInput = document.getElementById(options.addressInputId || 'Address');
        var searchInput = document.getElementById(options.searchInputId || 'mapSearchInput');
        var searchButton = document.getElementById(options.searchButtonId || 'mapSearchBtn');
        var statusElement = options.statusElementId ? document.getElementById(options.statusElementId) : null;

        var defaultCenter = {
            lat: options.defaultLat || 36.5437,
            lng: options.defaultLng || 31.9995
        };

        var startLat = latInput ? parseFloat(latInput.value) : NaN;
        var startLng = lngInput ? parseFloat(lngInput.value) : NaN;
        var hasInitialPosition = !isNaN(startLat) && !isNaN(startLng);

        var map = new google.maps.Map(mapEl, {
            center: hasInitialPosition ? { lat: startLat, lng: startLng } : defaultCenter,
            zoom: hasInitialPosition ? 15 : 12,
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: false
        });

        var marker = null;
        if (hasInitialPosition) {
            marker = new google.maps.Marker({
                position: { lat: startLat, lng: startLng },
                map: map
            });
        }

        var geocoder = new google.maps.Geocoder();

        function updateStatus(message, type) {
            if (!statusElement) return;
            statusElement.textContent = message;
            statusElement.className = 'map-status-text text-' + (type || 'muted');
        }

        function setMarker(position) {
            if (!position) return;
            if (!marker) {
                marker = new google.maps.Marker({
                    map: map
                });
            }
            marker.setPosition(position);
            map.panTo(position);
            if (latInput) latInput.value = position.lat.toFixed(6);
            if (lngInput) lngInput.value = position.lng.toFixed(6);
            updateStatus('Konum güncellendi.', 'success');
        }

        function performSearch() {
            var query = (searchInput && searchInput.value.trim()) ||
                (addressInput && addressInput.value.trim());

            if (!query) {
                updateStatus('Lütfen mahalle, sokak veya yer adı girin.', 'warning');
                return;
            }

            updateStatus('Adres aranıyor...', 'info');

            geocoder.geocode({ address: query }, function (results, status) {
                if (status === 'OK' && results[0]) {
                    var location = results[0].geometry.location;
                    map.fitBounds(results[0].geometry.viewport || null);
                    setMarker(location);
                    if (searchInput) {
                        searchInput.value = results[0].formatted_address;
                    }
                    updateStatus('Adres bulundu: ' + results[0].formatted_address, 'success');
                } else {
                    updateStatus('Adres bulunamadı. Daha spesifik bir tanım deneyin.', 'danger');
                }
            });
        }

        map.addListener('click', function (event) {
            setMarker(event.latLng);
        });

        if (searchButton) {
            searchButton.addEventListener('click', function (event) {
                event.preventDefault();
                performSearch();
            });
        }

        if (searchInput) {
            searchInput.addEventListener('keydown', function (event) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    performSearch();
                }
            });
        }
    }

    window.LocationPicker = {
        init: init
    };
})(window);

