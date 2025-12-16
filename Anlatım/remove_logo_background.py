"""
Logo etrafındaki beyaz arka planı şeffaf yapmak için script
Bu script Logo.png dosyasındaki beyaz pikselleri şeffaf yapar
"""
try:
    from PIL import Image
    import os

    # Logo dosyasının yolu
    logo_path = "wwwroot/images/Logo.png"
    output_path = "wwwroot/images/Logo.png"  # Aynı dosyaya kaydet

    if not os.path.exists(logo_path):
        print(f"HATA: {logo_path} dosyası bulunamadı!")
        exit(1)

    # Görüntüyü yükle
    img = Image.open(logo_path).convert("RGBA")
    
    # Piksel verilerini al
    data = img.getdata()
    
    # Yeni piksel listesi oluştur
    new_data = []
    
    # Beyaz toleransı (beyaza yakın renkleri de şeffaf yapmak için)
    white_threshold = 240  # 0-255 arası, 240 üzeri beyaz kabul edilir
    
    for item in data:
        # RGBA formatında: (R, G, B, A)
        r, g, b, a = item
        
        # Eğer piksel beyaz veya beyaza yakınsa şeffaf yap
        if r >= white_threshold and g >= white_threshold and b >= white_threshold:
            new_data.append((r, g, b, 0))  # Alpha = 0 (şeffaf)
        else:
            new_data.append(item)  # Orijinal pikseli koru
    
    # Yeni veriyi uygula
    img.putdata(new_data)
    
    # Dosyayı kaydet
    img.save(output_path, "PNG")
    
    print(f"Başarılı! Logo arka planı şeffaf yapıldı: {output_path}")
    print(f"Görüntü boyutu: {img.size[0]}x{img.size[1]} piksel")
    
except ImportError:
    print("HATA: PIL (Pillow) kütüphanesi bulunamadı!")
    print("Yüklemek için: pip install Pillow")
    exit(1)
except Exception as e:
    print(f"HATA: {str(e)}")
    exit(1)

