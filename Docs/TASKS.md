# Görevler (TASKS)

## Tamamlanan Görevler

### [x] Silah Başına Kombo Sistemi
**Açıklama:** Her silahın kendi kombo sekansını tanımlayabildiği bir sistem.
**Durum:** Kod tamamlandı. Editor kurulumu gerekiyor (WeaponSO Inspector + ComboHUD GameObject).

### [x] Parry Sistemi Revizyonu ve Aktif Tarama
**Açıklama:** Kalkan açıldığında sadece oyuncuya çarpanları değil, 45-180 derecelik alandaki tüm kılıç/mermi objelerini havada algılayıp sektiren (OverlapCircleAll) sistem geliştirildi. Mermilerin "arkadan çarpınca da sekmesi" (ters açı) hatası giderildi.

### [x] Dinamik Mermi & Sektirme Mimarisi (IDeflectable)
**Açıklama:** Oyundaki Boss ve Standart tüm düşman mermileri `IDeflectable` arayüzü (Interface) altında birleştirildi. Artık Parry radarı ve Player hitleri mermi isimlerini aramak yerine tek bir Interface'i algılayıp dinamik sektirme uygulayabiliyor.
---

## Aktif Görevler

### [x] Yeni Kılıç Assetlerinin Oyuna Eklenmesi
**Açıklama:** Kullanıcının elindeki free kılıç assetlerini projeye entegre edip oynanabilir hale getirmek.
**Kabul Kriterleri (Acceptance Criteria):**
- Kılıç sprite'ları Unity'ye 'Sprite (2D and UI)' ve 'Point (no filter)' ayarlarıyla aktarılmalı.
- Her yeni kılıç için `WeaponSO` oluşturulup istatistikleri (hasar, hız vb.) girilmeli.
- Yeni oluşturulan `WeaponSO` dosyaları `WeaponDatabase.asset` listesine eklenmeli.
- Hub'daki Shop veya silah seçim menüsünde yeni kılıçlar görülmeli ve kuşanılabilmeli.

---

## Gelecekteki Planlar & Hata Düzeltmeleri (Backlog & Bugs)

### [ ] Bosslar ve Boss Odaları Mantığı Düzenlenecek
**Açıklama:** Çoklu boss, faz geçişleri ve boss'a özel ödüller (Boss Sandığı vs.) eklenecek.

### [ ] Kapı Geçiş Sistemi (Fiziksel Kapılar)
**Açıklama:** Portal sistemi iptal edilip Hades'teki gibi, üzerinde çıkacak ödülün (altın, silah vb.) önceden belli olduğu fiziksel kapılar eklenecek.

### [ ] Kalıcı Ödül ve Ekonomi Sistemi
**Açıklama:** Silah veya para / run türü daha fazla meta-progression ödülleri geliştirilecek.

### [ ] Daha Fazla Silah Türü (Mekaniksel Olarak)
**Açıklama:** Oyuna yeni kılıç ve silah türleri eklenecek. Bunlar (balyoz, bıçak vb.) sadece statifik olarak değil oynanış/kombo ve hız olarak da ayrışacak.
