// Google Maps Fallback ve Retry Mekanizması
(function (window) {
    'use strict';

    const MapsService = {
        apiKey: null,
        retryCount: 0,
        maxRetries: 3,
        retryDelay: 2000,
        isGoogleMapsLoaded: false,
        fallbackEnabled: false,

        init: function (apiKey) {
            this.apiKey = apiKey;
            this.loadGoogleMaps();
        },

        loadGoogleMaps: function () {
            if (this.isGoogleMapsLoaded) {
                return Promise.resolve();
            }

            if (!this.apiKey || this.apiKey.trim() === '') {
                console.warn('Google Maps API anahtarı bulunamadı. OpenStreetMap fallback kullanılacak.');
                this.enableFallback();
                return Promise.reject('API key not found');
            }

            return new Promise((resolve, reject) => {
                // Google Maps zaten yüklenmiş mi kontrol et
                if (window.google && window.google.maps) {
                    this.isGoogleMapsLoaded = true;
                    resolve();
                    return;
                }

                // Script zaten yükleniyor mu kontrol et
                if (document.querySelector('script[src*="maps.googleapis.com"]')) {
                    // Script yükleniyor, callback'i bekle
                    const checkInterval = setInterval(() => {
                        if (window.google && window.google.maps) {
                            clearInterval(checkInterval);
                            this.isGoogleMapsLoaded = true;
                            resolve();
                        }
                    }, 100);

                    setTimeout(() => {
                        clearInterval(checkInterval);
                        if (!this.isGoogleMapsLoaded) {
                            reject('Google Maps yüklenemedi');
                        }
                    }, 10000);
                    return;
                }

                // Google Maps script'ini yükle
                const script = document.createElement('script');
                script.src = `https://maps.googleapis.com/maps/api/js?key=${this.apiKey}&libraries=places&callback=window.mapsServiceCallback`;
                script.async = true;
                script.defer = true;
                script.onerror = () => {
                    this.retryCount++;
                    if (this.retryCount < this.maxRetries) {
                        console.warn(`Google Maps yüklenemedi. ${this.retryDelay / 1000} saniye sonra tekrar denenecek... (${this.retryCount}/${this.maxRetries})`);
                        setTimeout(() => {
                            this.loadGoogleMaps().then(resolve).catch(reject);
                        }, this.retryDelay);
                    } else {
                        console.error('Google Maps yüklenemedi. OpenStreetMap fallback kullanılacak.');
                        this.enableFallback();
                        reject('Max retries reached');
                    }
                };

                window.mapsServiceCallback = () => {
                    this.isGoogleMapsLoaded = true;
                    this.retryCount = 0;
                    resolve();
                };

                document.head.appendChild(script);
            });
        },

        enableFallback: function () {
            if (this.fallbackEnabled) return;
            this.fallbackEnabled = true;
            console.info('OpenStreetMap fallback aktif edildi.');
        },

        createMap: function (elementId, options) {
            if (!this.isGoogleMapsLoaded && !this.fallbackEnabled) {
                return this.loadGoogleMaps().then(() => {
                    return this.createMap(elementId, options);
                }).catch(() => {
                    return this.createFallbackMap(elementId, options);
                });
            }

            if (this.isGoogleMapsLoaded && window.google && window.google.maps) {
                return this.createGoogleMap(elementId, options);
            } else {
                return this.createFallbackMap(elementId, options);
            }
        },

        createGoogleMap: function (elementId, options) {
            const element = document.getElementById(elementId);
            if (!element) {
                throw new Error(`Element bulunamadı: ${elementId}`);
            }

            const defaultOptions = {
                center: { lat: 36.5437, lng: 31.9995 },
                zoom: 12,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: true
            };

            const mapOptions = Object.assign({}, defaultOptions, options);
            const map = new google.maps.Map(element, mapOptions);

            return {
                map: map,
                google: google,
                addMarker: function (position, title) {
                    return new google.maps.Marker({
                        position: position,
                        map: map,
                        title: title || ''
                    });
                },
                addGeocoder: function () {
                    return new google.maps.Geocoder();
                }
            };
        },

        createFallbackMap: function (elementId, options) {
            const element = document.getElementById(elementId);
            if (!element) {
                throw new Error(`Element bulunamadı: ${elementId}`);
            }

            // Leaflet kullanarak OpenStreetMap fallback
            if (!window.L) {
                // Leaflet CSS ve JS yükle
                const leafletCSS = document.createElement('link');
                leafletCSS.rel = 'stylesheet';
                leafletCSS.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
                document.head.appendChild(leafletCSS);

                const leafletJS = document.createElement('script');
                leafletJS.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
                leafletJS.onload = () => {
                    this.initFallbackMap(elementId, options);
                };
                document.head.appendChild(leafletJS);
            } else {
                this.initFallbackMap(elementId, options);
            }

            return {
                map: null,
                isFallback: true,
                addMarker: function (position, title) {
                    if (this.map) {
                        return L.marker([position.lat, position.lng])
                            .addTo(this.map)
                            .bindPopup(title || '');
                    }
                }
            };
        },

        initFallbackMap: function (elementId, options) {
            const element = document.getElementById(elementId);
            if (!element || !window.L) return;

            const center = options?.center || [36.5437, 31.9995];
            const zoom = options?.zoom || 12;

            const map = L.map(elementId).setView(center, zoom);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors',
                maxZoom: 19
            }).addTo(map);

            // Info mesajı göster
            const infoDiv = document.createElement('div');
            infoDiv.className = 'alert alert-info';
            infoDiv.style.cssText = 'position: absolute; top: 10px; right: 10px; z-index: 1000; padding: 8px 12px; margin: 0; font-size: 12px;';
            infoDiv.innerHTML = '<i class="fas fa-info-circle"></i> OpenStreetMap kullanılıyor';
            element.parentElement.style.position = 'relative';
            element.parentElement.appendChild(infoDiv);

            setTimeout(() => {
                infoDiv.style.opacity = '0';
                infoDiv.style.transition = 'opacity 0.5s';
                setTimeout(() => infoDiv.remove(), 500);
            }, 3000);
        }
    };

    window.MapsService = MapsService;
})(window);

