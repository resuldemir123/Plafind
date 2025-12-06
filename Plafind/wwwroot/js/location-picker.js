(function (window) {
    function init(options) {
        // MapsService fallback mekanizmasını kullan
        if (window.MapsService) {
            const apiKey = options.apiKey || '';
            if (!window.MapsService.isGoogleMapsLoaded && apiKey) {
                window.MapsService.init(apiKey);
            }
        }

        // Google Maps yüklenene kadar bekle
        const checkGoogleMaps = setInterval(() => {
            if (window.google && window.google.maps) {
                clearInterval(checkGoogleMaps);
                initializeMap(options);
            } else if (window.MapsService && window.MapsService.fallbackEnabled) {
                clearInterval(checkGoogleMaps);
                initializeFallbackMap(options);
            }
        }, 100);

        setTimeout(() => {
            clearInterval(checkGoogleMaps);
            if (!window.google || !window.google.maps) {
                if (window.MapsService && !window.MapsService.fallbackEnabled) {
                    console.warn('Google Maps yüklenemedi. Fallback kullanılıyor.');
                    window.MapsService.enableFallback();
                    initializeFallbackMap(options);
                }
            }
        }, 10000);
    }

    function initializeMap(options) {
        if (!window.google || !window.google.maps) {
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

    function initializeFallbackMap(options) {
        // OpenStreetMap fallback kullan
        const mapEl = document.getElementById(options.mapElementId || 'businessFormMap');
        if (!mapEl) return;

        // Leaflet yükle
        if (!window.L) {
            const leafletCSS = document.createElement('link');
            leafletCSS.rel = 'stylesheet';
            leafletCSS.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
            document.head.appendChild(leafletCSS);

            const leafletJS = document.createElement('script');
            leafletJS.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
            leafletJS.onload = () => {
                createLeafletMap(options);
            };
            document.head.appendChild(leafletJS);
        } else {
            createLeafletMap(options);
        }
    }

    function createLeafletMap(options) {
        const mapEl = document.getElementById(options.mapElementId || 'businessFormMap');
        if (!mapEl || !window.L) return;

        const defaultCenter = {
            lat: options.defaultLat || 36.5437,
            lng: options.defaultLng || 31.9995
        };

        const latInput = document.getElementById(options.latInputId || 'Latitude');
        const lngInput = document.getElementById(options.lngInputId || 'Longitude');
        const addressInput = document.getElementById(options.addressInputId || 'Address');
        const statusElement = options.statusElementId ? document.getElementById(options.statusElementId) : null;

        const startLat = latInput ? parseFloat(latInput.value) : NaN;
        const startLng = lngInput ? parseFloat(lngInput.value) : NaN;
        const hasInitialPosition = !isNaN(startLat) && !isNaN(startLng);

        const center = hasInitialPosition ? [startLat, startLng] : [defaultCenter.lat, defaultCenter.lng];
        const map = L.map(mapEl).setView(center, hasInitialPosition ? 15 : 12);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(map);

        let marker = null;
        if (hasInitialPosition) {
            marker = L.marker(center).addTo(map);
        }

        map.on('click', function(e) {
            const lat = e.latlng.lat;
            const lng = e.latlng.lng;
            if (latInput) latInput.value = lat.toFixed(6);
            if (lngInput) lngInput.value = lng.toFixed(6);
            if (marker) {
                marker.setLatLng([lat, lng]);
            } else {
                marker = L.marker([lat, lng]).addTo(map);
            }
            if (statusElement) {
                statusElement.textContent = 'Konum güncellendi.';
                statusElement.className = 'map-status-text text-success';
            }
        });

        // Geocoding için Nominatim API kullan
        const searchInput = document.getElementById(options.searchInputId || 'mapSearchInput');
        const searchButton = document.getElementById(options.searchButtonId || 'mapSearchBtn');

        function performSearch() {
            const query = (searchInput && searchInput.value.trim()) ||
                (addressInput && addressInput.value.trim());

            if (!query) {
                if (statusElement) {
                    statusElement.textContent = 'Lütfen mahalle, sokak veya yer adı girin.';
                    statusElement.className = 'map-status-text text-warning';
                }
                return;
            }

            if (statusElement) {
                statusElement.textContent = 'Adres aranıyor...';
                statusElement.className = 'map-status-text text-info';
            }

            fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`)
                .then(response => response.json())
                .then(data => {
                    if (data && data.length > 0) {
                        const lat = parseFloat(data[0].lat);
                        const lng = parseFloat(data[0].lon);
                        map.setView([lat, lng], 15);
                        if (marker) {
                            marker.setLatLng([lat, lng]);
                        } else {
                            marker = L.marker([lat, lng]).addTo(map);
                        }
                        if (latInput) latInput.value = lat.toFixed(6);
                        if (lngInput) lngInput.value = lng.toFixed(6);
                        if (searchInput) searchInput.value = data[0].display_name;
                        if (statusElement) {
                            statusElement.textContent = 'Adres bulundu: ' + data[0].display_name;
                            statusElement.className = 'map-status-text text-success';
                        }
                    } else {
                        if (statusElement) {
                            statusElement.textContent = 'Adres bulunamadı. Daha spesifik bir tanım deneyin.';
                            statusElement.className = 'map-status-text text-danger';
                        }
                    }
                })
                .catch(error => {
                    console.error('Geocoding hatası:', error);
                    if (statusElement) {
                        statusElement.textContent = 'Adres arama hatası. Lütfen tekrar deneyin.';
                        statusElement.className = 'map-status-text text-danger';
                    }
                });
        }

        if (searchButton) {
            searchButton.addEventListener('click', function(e) {
                e.preventDefault();
                performSearch();
            });
        }

        if (searchInput) {
            searchInput.addEventListener('keydown', function(e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    performSearch();
                }
            });
        }
    }

    window.LocationPicker = {
        init: init
    };
})(window);

