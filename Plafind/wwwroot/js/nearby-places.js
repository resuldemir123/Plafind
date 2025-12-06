// Yakın Yer İşaretleri ve Mahalle Bilgileri Gösterme
(function (window) {
    'use strict';

    const NearbyPlacesService = {
        // Google Maps ile yakın yerleri bul
        findNearbyPlacesGoogle: function(map, position, radius = 2000) {
            if (!window.google || !window.google.maps) {
                return Promise.reject('Google Maps yüklenmedi');
            }

            return new Promise((resolve, reject) => {
                const service = new google.maps.places.PlacesService(map);
                const request = {
                    location: position,
                    radius: radius,
                    type: ['neighborhood', 'locality', 'sublocality', 'point_of_interest']
                };

                service.nearbySearch(request, (results, status) => {
                    if (status === google.maps.places.PlacesServiceStatus.OK && results) {
                        resolve(results);
                    } else {
                        // Eğer Places API başarısız olursa, reverse geocoding kullan
                        this.findNearbyPlacesGeocoding(position).then(resolve).catch(reject);
                    }
                });
            });
        },

        // Reverse Geocoding ile yakın yerleri bul
        findNearbyPlacesGeocoding: function(position) {
            if (!window.google || !window.google.maps) {
                return Promise.reject('Google Maps yüklenmedi');
            }

            return new Promise((resolve, reject) => {
                const geocoder = new google.maps.Geocoder();
                
                // Ana konum için reverse geocoding
                geocoder.geocode({ location: position }, (results, status) => {
                    if (status === 'OK' && results && results.length > 0) {
                        const places = [];
                        const mainResult = results[0];
                        
                        // Address components'ten mahalle, semt, ilçe bilgilerini çıkar
                        if (mainResult.address_components) {
                            mainResult.address_components.forEach(component => {
                                const types = component.types;
                                
                                // Mahalle, semt, ilçe gibi yerler
                                if (types.includes('neighborhood') || 
                                    types.includes('sublocality') || 
                                    types.includes('sublocality_level_1') ||
                                    types.includes('locality') ||
                                    types.includes('administrative_area_level_3')) {
                                    
                                    places.push({
                                        name: component.long_name,
                                        type: 'neighborhood',
                                        location: position,
                                        formatted_address: mainResult.formatted_address
                                    });
                                }
                            });
                        }

                        // Yakın yerler için farklı noktalardan reverse geocoding yap
                        const nearbyPoints = [
                            { lat: position.lat + 0.01, lng: position.lng }, // Kuzey
                            { lat: position.lat - 0.01, lng: position.lng }, // Güney
                            { lat: position.lat, lng: position.lng + 0.01 }, // Doğu
                            { lat: position.lat, lng: position.lng - 0.01 }  // Batı
                        ];

                        let completedRequests = 0;
                        nearbyPoints.forEach(point => {
                            geocoder.geocode({ location: point }, (results, status) => {
                                if (status === 'OK' && results && results.length > 0) {
                                    const result = results[0];
                                    if (result.address_components) {
                                        result.address_components.forEach(component => {
                                            const types = component.types;
                                            if ((types.includes('neighborhood') || 
                                                 types.includes('sublocality') ||
                                                 types.includes('locality')) &&
                                                !places.some(p => p.name === component.long_name)) {
                                                
                                                places.push({
                                                    name: component.long_name,
                                                    type: 'neighborhood',
                                                    location: point,
                                                    formatted_address: result.formatted_address
                                                });
                                            }
                                        });
                                    }
                                }
                                completedRequests++;
                                if (completedRequests === nearbyPoints.length) {
                                    resolve(places);
                                }
                            });
                        });

                        if (nearbyPoints.length === 0) {
                            resolve(places);
                        }
                    } else {
                        reject('Geocoding başarısız');
                    }
                });
            });
        },

        // OpenStreetMap ile yakın yerleri bul (Nominatim)
        findNearbyPlacesOSM: function(position, radius = 2000) {
            return new Promise((resolve, reject) => {
                const lat = position.lat;
                const lng = position.lng;
                
                // Reverse geocoding ile ana konum bilgisi
                fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`)
                    .then(response => response.json())
                    .then(data => {
                        const places = [];
                        
                        if (data && data.address) {
                            // Mahalle, semt, ilçe bilgilerini çıkar
                            const address = data.address;
                            
                            if (address.neighbourhood || address.suburb || address.village || address.town) {
                                places.push({
                                    name: address.neighbourhood || address.suburb || address.village || address.town,
                                    type: 'neighborhood',
                                    location: position,
                                    formatted_address: data.display_name
                                });
                            }

                            // Yakın noktalardan da bilgi al
                            const nearbyPoints = [
                                { lat: lat + 0.01, lng: lng },
                                { lat: lat - 0.01, lng: lng },
                                { lat: lat, lng: lng + 0.01 },
                                { lat: lat, lng: lng - 0.01 }
                            ];

                            let completedRequests = 0;
                            nearbyPoints.forEach(point => {
                                fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${point.lat}&lon=${point.lng}&zoom=18&addressdetails=1`)
                                    .then(response => response.json())
                                    .then(data => {
                                        if (data && data.address) {
                                            const addr = data.address;
                                            const name = addr.neighbourhood || addr.suburb || addr.village || addr.town;
                                            if (name && !places.some(p => p.name === name)) {
                                                places.push({
                                                    name: name,
                                                    type: 'neighborhood',
                                                    location: point,
                                                    formatted_address: data.display_name
                                                });
                                            }
                                        }
                                        completedRequests++;
                                        if (completedRequests === nearbyPoints.length) {
                                            resolve(places);
                                        }
                                    })
                                    .catch(() => {
                                        completedRequests++;
                                        if (completedRequests === nearbyPoints.length) {
                                            resolve(places);
                                        }
                                    });
                            });

                            if (nearbyPoints.length === 0) {
                                resolve(places);
                            }
                        } else {
                            resolve(places);
                        }
                    })
                    .catch(reject);
            });
        },

        // Haritada yakın yerleri göster
        showNearbyPlaces: function(map, businessPosition, businessName, useGoogleMaps = true) {
            const placesPromise = useGoogleMaps && window.google && window.google.maps
                ? this.findNearbyPlacesGeocoding(businessPosition)
                : this.findNearbyPlacesOSM(businessPosition);

            placesPromise
                .then(places => {
                    if (!places || places.length === 0) {
                        console.info('Yakın yer işareti bulunamadı');
                        return;
                    }

                    // Bilgi paneli oluştur
                    const infoDiv = document.createElement('div');
                    infoDiv.className = 'nearby-places-info';
                    infoDiv.innerHTML = `
                        <div class="nearby-places-header">
                            <i class="fas fa-map-marker-alt me-2"></i>
                            <strong>${businessName}</strong>
                        </div>
                        <div class="nearby-places-content">
                            <div class="nearby-places-label">Yakın Yerler:</div>
                            <div class="nearby-places-list">
                                ${places.slice(0, 5).map(place => `
                                    <span class="nearby-place-badge">
                                        <i class="fas fa-location-dot me-1"></i>${place.name}
                                    </span>
                                `).join('')}
                            </div>
                        </div>
                    `;

                    // Stil ekle
                    if (!document.getElementById('nearby-places-style')) {
                        const style = document.createElement('style');
                        style.id = 'nearby-places-style';
                        style.textContent = `
                            .nearby-places-info {
                                background: white;
                                border-radius: 12px;
                                padding: 1rem;
                                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                                margin-top: 1rem;
                                font-size: 0.9rem;
                            }
                            .nearby-places-header {
                                color: #667eea;
                                font-weight: 600;
                                margin-bottom: 0.75rem;
                                padding-bottom: 0.5rem;
                                border-bottom: 2px solid #f0f0f0;
                            }
                            .nearby-places-label {
                                color: #6c757d;
                                font-size: 0.85rem;
                                margin-bottom: 0.5rem;
                            }
                            .nearby-places-list {
                                display: flex;
                                flex-wrap: wrap;
                                gap: 0.5rem;
                            }
                            .nearby-place-badge {
                                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                color: white;
                                padding: 0.4rem 0.8rem;
                                border-radius: 20px;
                                font-size: 0.85rem;
                                font-weight: 500;
                                display: inline-flex;
                                align-items: center;
                            }
                        `;
                        document.head.appendChild(style);
                    }

                    // Harita container'ına ekle
                    const mapContainer = map.getDiv ? map.getDiv().parentElement : document.querySelector('[id*="Map"], [id*="map"]');
                    if (mapContainer) {
                        // Eski bilgi panelini kaldır
                        const oldInfo = mapContainer.querySelector('.nearby-places-info');
                        if (oldInfo) oldInfo.remove();
                        
                        mapContainer.appendChild(infoDiv);
                    }

                    // Google Maps için marker'lar ekle
                    if (useGoogleMaps && window.google && window.google.maps) {
                        places.forEach((place, index) => {
                            if (index < 3) { // İlk 3 yakın yeri göster
                                const marker = new google.maps.Marker({
                                    position: place.location,
                                    map: map,
                                    icon: {
                                        path: google.maps.SymbolPath.CIRCLE,
                                        scale: 6,
                                        fillColor: '#667eea',
                                        fillOpacity: 0.8,
                                        strokeColor: '#fff',
                                        strokeWeight: 2
                                    },
                                    title: place.name
                                });

                                const infoWindow = new google.maps.InfoWindow({
                                    content: `<div style="padding: 0.5rem;"><strong>${place.name}</strong></div>`
                                });

                                marker.addListener('click', () => {
                                    infoWindow.open(map, marker);
                                });
                            }
                        });
                    }
                })
                .catch(error => {
                    console.warn('Yakın yerler yüklenemedi:', error);
                });
        }
    };

    window.NearbyPlacesService = NearbyPlacesService;
})(window);

