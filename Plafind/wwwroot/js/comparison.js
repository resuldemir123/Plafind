/**
 * İşletme Karşılaştırma Sistemi - Kullanıcıya Özel Yönetim
 */

(function () {
    'use strict';

    const STORAGE_KEY_PREFIX = 'plafind_comparison_list_';
    const MAX_COMPARISON_ITEMS = 4;

    /**
     * Kullanıcı ID'sini alır (meta tag'den veya data attribute'dan)
     */
    function getUserId() {
        const metaTag = document.querySelector('meta[name="user-id"]');
        if (metaTag) {
            return metaTag.getAttribute('content') || null;
        }
        const dataAttr = document.body.getAttribute('data-user-id');
        return dataAttr || null;
    }

    /**
     * Storage key'i oluşturur (kullanıcı ID'si ile)
     */
    function getStorageKey() {
        const userId = getUserId();
        return userId ? `${STORAGE_KEY_PREFIX}${userId}` : `${STORAGE_KEY_PREFIX}anonymous`;
    }

    /**
     * Local Storage'dan karşılaştırma listesini alır (kullanıcıya özel)
     */
    function getComparisonList() {
        try {
            const storageKey = getStorageKey();
            const stored = localStorage.getItem(storageKey);
            if (!stored) {
                console.log('Local Storage boş, yeni liste oluşturuluyor');
                return [];
            }

            const ids = JSON.parse(stored);
            const filteredIds = Array.isArray(ids) ? ids.filter(id => typeof id === 'number' && id > 0) : [];
            console.log('Local Storage\'dan alınan liste:', filteredIds, 'Key:', storageKey);
            return filteredIds;
        } catch (error) {
            console.error('getComparisonList error:', error);
            const storageKey = getStorageKey();
            console.error('Hatalı JSON:', localStorage.getItem(storageKey));
            return [];
        }
    }

    /**
     * Local Storage'a karşılaştırma listesini kaydeder (kullanıcıya özel)
     */
    function saveComparisonList(ids) {
        try {
            const storageKey = getStorageKey();
            const uniqueIds = [...new Set(ids)].slice(0, MAX_COMPARISON_ITEMS);
            const jsonString = JSON.stringify(uniqueIds);
            localStorage.setItem(storageKey, jsonString);
            console.log('Local Storage kaydedildi:', jsonString, 'Key:', storageKey);
            updateComparisonBadge();
            return true;
        } catch (error) {
            console.error('saveComparisonList error:', error);
            return false;
        }
    }

    /**
     * Son kullanıcı ID'sini saklar (hesap değişimi kontrolü için)
     */
    let lastUserId = null;

    /**
     * Eski kullanıcıların verilerini temizler (hesap değişimi durumunda)
     */
    function clearOldUserData() {
        try {
            const currentUserId = getUserId();
            const currentKey = getStorageKey();
            
            // Kullanıcı değişti mi kontrol et
            if (lastUserId !== null && lastUserId !== currentUserId) {
                console.log('Kullanıcı değişti! Eski kullanıcı:', lastUserId, 'Yeni kullanıcı:', currentUserId);
                
                // Eski kullanıcının verilerini temizle
                if (lastUserId) {
                    const oldKey = `${STORAGE_KEY_PREFIX}${lastUserId}`;
                    localStorage.removeItem(oldKey);
                    console.log('Eski kullanıcı verisi temizlendi:', oldKey);
                }
            }
            
            // Tüm karşılaştırma anahtarlarını bul ve mevcut kullanıcı dışındakileri temizle
            const allKeys = Object.keys(localStorage);
            const comparisonKeys = allKeys.filter(key => key.startsWith(STORAGE_KEY_PREFIX));
            
            comparisonKeys.forEach(key => {
                if (key !== currentKey) {
                    localStorage.removeItem(key);
                    console.log('Eski kullanıcı verisi temizlendi:', key);
                }
            });
            
            // Mevcut kullanıcı ID'sini sakla
            lastUserId = currentUserId;
        } catch (error) {
            console.error('clearOldUserData error:', error);
        }
    }

    /**
     * İşletmeyi karşılaştırmaya ekler
     */
    function addToComparison(businessId) {
        console.log('addToComparison çağrıldı:', businessId);
        
        if (!businessId || businessId <= 0) {
            console.error('Geçersiz işletme ID:', businessId);
            return { success: false, message: 'Geçersiz işletme ID\'si' };
        }

        const currentList = getComparisonList();
        console.log('Mevcut liste:', currentList);

        if (currentList.includes(businessId)) {
            console.log('İşletme zaten listede');
            return { success: false, message: 'Bu işletme zaten karşılaştırma listesinde' };
        }

        if (currentList.length >= MAX_COMPARISON_ITEMS) {
            console.log('Maksimum limit aşıldı');
            return { success: false, message: `Maksimum ${MAX_COMPARISON_ITEMS} işletme karşılaştırılabilir` };
        }

        currentList.push(businessId);
        console.log('Yeni liste:', currentList);
        
        const saveResult = saveComparisonList(currentList);
        console.log('Kaydetme sonucu:', saveResult);
        
        // Kaydetme sonrası kontrol
        const savedList = getComparisonList();
        console.log('Kaydedilen liste kontrolü:', savedList);

        return { success: true, message: 'İşletme karşılaştırmaya eklendi', count: currentList.length };
    }

    /**
     * İşletmeyi karşılaştırmadan çıkarır
     */
    function removeFromComparison(businessId) {
        if (!businessId || businessId <= 0) {
            return { success: false, message: 'Geçersiz işletme ID\'si' };
        }

        const currentList = getComparisonList();
        const filteredList = currentList.filter(id => id !== businessId);

        if (filteredList.length === currentList.length) {
            return { success: false, message: 'Bu işletme karşılaştırma listesinde değil' };
        }

        saveComparisonList(filteredList);

        return { success: true, message: 'İşletme karşılaştırmadan çıkarıldı', count: filteredList.length };
    }

    /**
     * İşletmenin karşılaştırma listesinde olup olmadığını kontrol eder
     */
    function isInComparison(businessId) {
        const currentList = getComparisonList();
        return currentList.includes(businessId);
    }

    /**
     * Karşılaştırma listesini temizler (kullanıcıya özel)
     */
    function clearComparison() {
        try {
            const storageKey = getStorageKey();
            localStorage.removeItem(storageKey);
            updateComparisonBadge();
            return { success: true, message: 'Karşılaştırma listesi temizlendi' };
        } catch (error) {
            console.error('clearComparison error:', error);
            return { success: false, message: 'Liste temizlenirken hata oluştu' };
        }
    }

    /**
     * Tüm karşılaştırma butonlarını günceller (anlık hesaplama)
     */
    function updateAllComparisonButtons() {
        const comparisonList = getComparisonList();
        const buttons = document.querySelectorAll('.comparison-btn, .compare-btn-home, [data-business-id]');
        
        buttons.forEach(button => {
            const businessId = parseInt(button.getAttribute('data-business-id') || button.dataset.businessId);
            if (!businessId) return;
            
            const isInList = comparisonList.includes(businessId);
            
            // Buton durumunu güncelle
            if (isInList) {
                button.classList.add('active');
                button.setAttribute('aria-pressed', 'true');
                const textSpan = button.querySelector('.comparison-btn-text');
                if (textSpan) {
                    textSpan.textContent = 'Karşılaştırmadan Çıkar';
                }
                button.style.backgroundColor = '#0d6efd';
                button.style.color = '#fff';
            } else {
                button.classList.remove('active');
                button.setAttribute('aria-pressed', 'false');
                const textSpan = button.querySelector('.comparison-btn-text');
                if (textSpan) {
                    textSpan.textContent = 'Karşılaştır';
                }
                button.style.backgroundColor = '';
                button.style.color = '';
            }
        });
    }

    /**
     * Karşılaştırma badge'ini günceller
     */
    function updateComparisonBadge() {
        const count = getComparisonList().length;
        const badges = document.querySelectorAll('.comparison-badge-count');
        
        badges.forEach(badge => {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        });

        // Karşılaştır butonunu göster/gizle
        const compareButton = document.getElementById('compare-button');
        if (compareButton) {
            compareButton.style.display = count > 0 ? 'inline-block' : 'none';
        }
        
        // Tüm butonları güncelle (anlık hesaplama)
        updateAllComparisonButtons();
    }

    /**
     * Karşılaştırma sayfasına yönlendirir
     */
    function goToComparison() {
        const ids = getComparisonList();
        if (ids.length === 0) {
            alert('Karşılaştırma için en az bir işletme seçmelisiniz.');
            return;
        }

        const idsParam = ids.join(',');
        window.location.href = `/Comparison/Index?ids=${idsParam}`;
    }

    /**
     * AJAX ile karşılaştırma verilerini yükler
     */
    async function loadComparisonData(ids) {
        try {
            const response = await fetch(`/Comparison/GetComparisonData?ids=${ids.join(',')}`);
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('loadComparisonData error:', error);
            return { success: false, message: 'Veri yüklenirken hata oluştu' };
        }
    }

    /**
     * Karşılaştırma tablosunu render eder
     */
    function renderComparisonTable(data) {
        // Bu fonksiyon Comparison/Index.cshtml sayfasında kullanılacak
        // Şimdilik placeholder
        console.log('renderComparisonTable:', data);
    }

    /**
     * Sadece farklı satırları göster/gizle
     */
    function toggleDifferencesOnly(showOnly) {
        const rows = document.querySelectorAll('.comparison-table tbody tr');
        rows.forEach(row => {
            if (showOnly) {
                if (!row.classList.contains('highlight-diff')) {
                    row.style.display = 'none';
                } else {
                    row.style.display = '';
                }
            } else {
                row.style.display = '';
            }
        });
    }

    // Global fonksiyonları export et
    window.ComparisonManager = {
        addToComparison: addToComparison,
        removeFromComparison: removeFromComparison,
        isInComparison: isInComparison,
        clearComparison: clearComparison,
        getComparisonList: getComparisonList,
        goToComparison: goToComparison,
        loadComparisonData: loadComparisonData,
        renderComparisonTable: renderComparisonTable,
        toggleDifferencesOnly: toggleDifferencesOnly,
        updateComparisonBadge: updateComparisonBadge,
        updateAllComparisonButtons: updateAllComparisonButtons,
        clearOldUserData: clearOldUserData
    };

    /**
     * Karşılaştırma butonları için event handler'ları ekler
     */
    function attachComparisonButtonHandlers() {
        // Tüm karşılaştırma butonlarına click event ekle
        document.addEventListener('click', function(e) {
            const button = e.target.closest('.comparison-btn, .compare-btn-home, [data-business-id]');
            if (!button) return;
            
            const businessId = parseInt(button.getAttribute('data-business-id') || button.dataset.businessId);
            if (!businessId) return;
            
            e.preventDefault();
            e.stopPropagation();
            
            // Kullanıcı değişti mi kontrol et
            clearOldUserData();
            
            const isInList = isInComparison(businessId);
            
            if (isInList) {
                // Çıkar
                const result = removeFromComparison(businessId);
                if (result.success) {
                    updateComparisonBadge();
                    // Kısa bir feedback göster
                    showNotification(result.message, 'success');
                }
            } else {
                // Ekle
                const result = addToComparison(businessId);
                if (result.success) {
                    updateComparisonBadge();
                    showNotification(result.message, 'success');
                } else {
                    showNotification(result.message, 'warning');
                }
            }
        });
    }

    /**
     * Bildirim gösterir
     */
    function showNotification(message, type = 'info') {
        // Basit bir toast notification
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notification);
        
        // 3 saniye sonra otomatik kapat
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    /**
     * Storage değişikliklerini dinle (farklı tab'ler için)
     */
    function setupStorageListener() {
        window.addEventListener('storage', function(e) {
            if (e.key && e.key.startsWith(STORAGE_KEY_PREFIX)) {
                // Kullanıcı değişti mi kontrol et
                clearOldUserData();
                // Badge'i güncelle
                updateComparisonBadge();
            }
        });
    }

    /**
     * Kullanıcı ID'si değişikliklerini izle (meta tag değişirse)
     */
    function watchUserIdChanges() {
        let currentUserId = getUserId();
        
        // Her 500ms'de bir kontrol et
        setInterval(() => {
            const newUserId = getUserId();
            if (newUserId !== currentUserId) {
                console.log('Kullanıcı ID değişti!', currentUserId, '->', newUserId);
                currentUserId = newUserId;
                clearOldUserData();
                updateComparisonBadge();
            }
        }, 500);
    }

    // Sayfa yüklendiğinde badge'i güncelle ve eski verileri temizle
    function initializeComparison() {
        function doInitialize() {
            // İlk kullanıcı ID'sini sakla
            lastUserId = getUserId();
            
            // Eski kullanıcı verilerini temizle (hesap değişimi durumunda)
            clearOldUserData();
            
            // Badge'i güncelle ve tüm butonları güncelle (anlık hesaplama)
            updateComparisonBadge();
            
            // Event handler'ları ekle
            attachComparisonButtonHandlers();
            
            // Storage listener ekle
            setupStorageListener();
            
            // Kullanıcı ID değişikliklerini izle
            watchUserIdChanges();
            
            console.log('ComparisonManager yüklendi ve hazır');
            console.log('Kullanıcı ID:', getUserId());
            console.log('Mevcut karşılaştırma listesi:', getComparisonList());
        }

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', doInitialize);
        } else {
            // DOM zaten yüklü, hemen çalıştır
            setTimeout(doInitialize, 100);
        }
    }

    // Hemen başlat
    initializeComparison();

    // Global olarak erişilebilir olduğunu doğrula
    console.log('ComparisonManager export edildi:', typeof window.ComparisonManager !== 'undefined');

})();

