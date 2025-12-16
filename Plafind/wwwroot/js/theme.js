/**
 * Theme Management Script
 * Handles light/dark theme switching with localStorage persistence
 * and system preference detection
 */

(function() {
    'use strict';

    const ThemeManager = {
        // Tema tercihini localStorage'dan al
        getTheme: function() {
            const savedTheme = localStorage.getItem('theme');
            if (savedTheme) {
                return savedTheme;
            }
            
            // Sistem tercihini kontrol et
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                return 'dark';
            }
            
            return 'light';
        },

        // Tema tercihini localStorage'a kaydet
        saveTheme: function(theme) {
            localStorage.setItem('theme', theme);
        },

        // Temayı uygula
        applyTheme: function(theme) {
            const html = document.documentElement;
            html.setAttribute('data-theme', theme);
            this.saveTheme(theme);
            
            // Tema değiştiğinde event fırlat (diğer scriptler dinleyebilir)
            window.dispatchEvent(new CustomEvent('themechanged', { detail: { theme: theme } }));
        },

        // Tema değiştir (toggle)
        toggleTheme: function() {
            const currentTheme = this.getTheme();
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            this.applyTheme(newTheme);
            return newTheme;
        },

        // Sistem tercihini dinle (kullanıcı manuel tema seçmediyse)
        watchSystemPreference: function() {
            if (!window.matchMedia) return;

            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            
            // Kullanıcı manuel bir tercih yapmışsa sistem tercihini dinleme
            if (localStorage.getItem('theme')) {
                return;
            }

            const handleChange = (e) => {
                this.applyTheme(e.matches ? 'dark' : 'light');
            };

            // Modern tarayıcılar için
            if (mediaQuery.addEventListener) {
                mediaQuery.addEventListener('change', handleChange);
            } else {
                // Eski tarayıcılar için
                mediaQuery.addListener(handleChange);
            }
        },

        // Tema toggle butonunu güncelle
        updateToggleButton: function(theme) {
            const toggleButtons = document.querySelectorAll('.theme-toggle, [data-theme-toggle]');
            toggleButtons.forEach(btn => {
                const icon = btn.querySelector('i');
                if (icon) {
                    if (theme === 'dark') {
                        icon.className = 'fas fa-sun';
                        btn.setAttribute('title', 'Aydınlık Temaya Geç');
                        btn.setAttribute('aria-label', 'Aydınlık Temaya Geç');
                    } else {
                        icon.className = 'fas fa-moon';
                        btn.setAttribute('title', 'Karanlık Temaya Geç');
                        btn.setAttribute('aria-label', 'Karanlık Temaya Geç');
                    }
                }
            });
        },

        // İlk yükleme ve başlatma
        init: function() {
            const theme = this.getTheme();
            this.applyTheme(theme);
            this.updateToggleButton(theme);
            this.watchSystemPreference();

            // Tema toggle butonları için event listener ekle
            document.addEventListener('click', (e) => {
                const toggleBtn = e.target.closest('.theme-toggle, [data-theme-toggle]');
                if (toggleBtn) {
                    e.preventDefault();
                    const newTheme = this.toggleTheme();
                    this.updateToggleButton(newTheme);
                }
            });
        }
    };

    // Sayfa yüklendiğinde başlat
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => ThemeManager.init());
    } else {
        ThemeManager.init();
    }

    // Global erişim için
    window.ThemeManager = ThemeManager;
})();

