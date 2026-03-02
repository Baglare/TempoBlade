# Worklog

## 2026-02-27 20:05:00
- **What changed:** `PlayerCombat.cs` was updated to support dynamic weapon visuals. Added `weaponSpriteRenderer` field and logic in `EquipWeapon` to assign `WeaponSO.weaponSprite`.
- **Files touched:** 
  - `Assets/_Project/Scripts/Player/PlayerCombat.cs`
- **Next step:** Configure the SpriteRenderer on the player prefab via Unity Editor to attach logical attack points to a visual handle. Then do the same logic for enemies if needed.
- **Risks:** The visuals might clip with the player sprite or rotation might need tweaking so it looks natural in hand.

## 2026-02-27 20:10:00
- **What changed:** Fixed compiler error in `PlayerCombat.cs`. `WeaponSO` uses `icon` instead of `weaponSprite`.
- **Files touched:**
  - `Assets/_Project/Scripts/Player/PlayerCombat.cs`

## 2026-02-27 21:00:00
- **What changed:** Kılıç konumlandırma + saldırı yayı görselleştirmesi eklendi. `WeaponArcVisual` adlı yeni bir VFX component oluşturuldu. Player mouse yönünde menzil ucuna kılıcı konumlandırıyor, saldırıda arc aktif renge geçiyor. EnemyMelee, EnemyDuelist, EnemyBoss (Phase 1) oyuncuya dönük olarak arc güncelliyor; saldırı anında aktif renk.
- **Files touched:**
  - `Assets/_Project/Scripts/VFX/WeaponArcVisual.cs` (YENİ)
  - `Assets/_Project/Scripts/Player/PlayerCombat.cs`
  - `Assets/_Project/Scripts/Enemy/EnemyMelee.cs`
  - `Assets/_Project/Scripts/Enemy/EnemyDuelist.cs`
  - `Assets/_Project/Scripts/Enemy/EnemyBoss.cs`
- **Next step:** Unity Editor'da her Prefab için kurulum yapılmalı:
  1. Player/Enemy altına "WeaponArcVisual" adlı boş child GO oluştur, `WeaponArcVisual` component ekle.
  2. "WeaponSprite" adlı başka bir child oluştur, SpriteRenderer ekle, kılıç sprite'ını ata.
  3. `weaponTransform` alanına "WeaponSprite"ın Transform'unu sürükle.
  4. İlgili script'teki `weaponArcVisual` alanına "WeaponArcVisual" child'ını sürükle.
- **Risks:** URP'de `Sprites/Default` shader çalışmıyorsa yay görünmez. Bu durumda `arcMaterial` alanına URP uyumlu bir materyal Inspector'dan atanmalı. Ayrıca negatif localScale kullanan enemylerde (EnemyDuelist) weapon world-space konumlandırması doğru çalışıyor, ancak sprite orientation gözden geçirilmeli.

## 2026-02-28 01:25:00
- **What changed:** Konsolu temiz tutmak için proje genelindeki tüm `Debug.Log` mesajları (Cinemachine atama vb.) ve yorum satırına alınmış eski konsol logları sildik.
- **Files touched:** 
  - 10+ dosya (PlayerCombat, PlayerController, LevelManager, TempoManager, SimpleArena vb.)
- **Next step:** Silahlara özel kombo sisteminin geliştirilmesi (veya istenen başka bir task).

## 2026-02-28 ~03:00:00
- **What changed:** Silah başına özelleştirilebilir kombo sistemi eklendi.
  - `ComboStepData.cs` (YENİ): Serileştirilebilir kombo adımı verisi — Normal / MultiHit / DashStrike tipleri, hasar çarpanı, menzil bonusu, windup, cooldown, pencere, isUninterruptible alanları.
  - `WeaponSO.cs`: `comboSteps` dizisi eklendi. Boş bırakılan silahlar eski tek vuruş davranışını korur (geriye dönük uyumlu).
  - `PlayerController.cs`: `DashStriking` PlayerState eklendi; `StartExternalDash()` + `UpdateExternalDash()` metodları eklendi. DashStrike sırasında `IsInvulnerable = true`.
  - `PlayerCombat.cs`: `TryAttack()`, `ExecuteComboStep()`, `PerformHit()` metodları eklendi. `OnComboChanged` eventi. Whiff → kombo sıfırlanır. Attack() geriye dönük uyumluluk için korundu.
  - `ComboHUD.cs` (YENİ): `OnComboChanged`'a abone olan UI bileşeni; tamamlanan adımları ●, bekleyenleri ○ olarak gösterir; son adımda/whiff'te fade out.
- **Files touched:**
  - `Assets/_Project/Scripts/ScriptableObjects_Data/ComboStepData.cs` (YENİ)
  - `Assets/_Project/Scripts/ScriptableObjects_Data/WeaponSO.cs`
  - `Assets/_Project/Scripts/Player/PlayerController.cs`
  - `Assets/_Project/Scripts/Player/PlayerCombat.cs`
  - `Assets/_Project/Scripts/UI/ComboHUD.cs` (YENİ)
- **Next step:** Editor kurulumu:
  1. Her WeaponSO'da `Combo Steps` dizisini doldur.
  2. Canvas'a boş GO ekle → `ComboHUD` script → TMP text ata → PlayerCombat referansını sürükle.
  3. Test: Normal adım çalışıyor mu? MultiHit vuruş sayısı doğru mu? DashStrike i-frame veriyor mu? Whiff komboyu sıfırlıyor mu?
- **Risks:** DashStrike sırasında PlayerController hareketi override eder; dodge ile çakışma önlendi (`if Dodging return`). MultiHit adımında `isExecutingComboStep = true` olmadığından oyuncu başka bir adım başlatamaz (sadece uninterruptible adımlar bloke eder).

## 2026-02-28 ~05:00:00
- **What changed:** 3 yeni düşman tipi eklendi.
  - `EnemyKamikaze.cs` (YENİ): Oyuncuyu fark edince koşar, patlama menziline girince telegraph (0.8 s sarı→kırmızı büyüme), ardından AoE patlama. Perfect Parry → patlama iptal, kamikaze ölür. Dodge i-frame → hasar atlatılır.
  - `EnemyAssassin.cs` (YENİ): Başlangıçta görünmez. Menzil dışı → görünmez gezinir. Tespit menziline girince görünmez takip eder. Saldırıdan 0.15 s önce görünür olur, lunge + hasar, sonra görünmez geri çekilir. Özel: sahnede yalnızca suikastçılar kaldıysa VEYA toplam mob ≤ 3 ise yarı saydam (0.3 alpha) görünür.
  - `EnemyDasher.cs` (YENİ): Yüksek hızlı uzakçı. Hareket ederken ateş edebilir (Caster'dan farklı). Hasar alınca %45 ihtimalle "perfect dash" tetikler: i-frame + geriye fırlama + "EVADE!" popup. Strafe + geri çekilme kiting mantığı.
- **Files touched:**
  - `Assets/_Project/Scripts/Enemy/EnemyKamikaze.cs` (YENİ)
  - `Assets/_Project/Scripts/Enemy/EnemyAssassin.cs` (YENİ)
  - `Assets/_Project/Scripts/Enemy/EnemyDasher.cs` (YENİ)
- **Next step:** Editor kurulumu:
  1. Her düşman için EnemySO oluştur (moveSpeed, damage, maxHealth vb. ayarla).
  2. Her düşman için Prefab oluştur → ilgili script'i ekle → EnemySO sürükle.
  3. Kamikaze: `explosionRange`, `explosionRadius`, `explosionDamage`, `telegraphDuration` ayarla.
  4. Assassin: `detectionRange`, `attackDamage`, `attackCooldown`, `semiVisibleAlpha` ayarla.
  5. Dasher: `projectilePrefab`, `firePoint` ata; `preferredRange`, `minRange`, `dodgeChance` ayarla.
  6. Yeni EnemySO'ları ve Prefabları RoomSO wave listelerine ekle.
- **Risks:** Assassin görünmezliği SpriteRenderer alpha'sına dayanır — başka renderer (TMP, partikül) varsa ayrıca ele alınmalı. Dasher Rigidbody2D linearVelocity kullanır; `linearDamping` çok yüksekse kiting hareketi çok yavaş hissedebilir (EnemySO'da `moveSpeed` yükseğe al: ~7-9).

## 2026-02-28 ~17:30:00
- **What changed:** Yeni eklenen düşmanlardan bazıları oyuncu geri bildirimiyle güncellendi ve dengelendi.
  - `EnemyKamikaze.cs`: Patlama tepki süresi (Telegraph) 0.8s'den **0.45s**'ye düşürüldü. Koşma hızı **7.5f** yapıldı. Patlama menzili **1.4f**'e çekildi (daha dipten patlıyor). En önemlisi patlama başladığı an etrafına gerçek patlama alanını çizen (`LineRenderer` ile) hedef belirleyici kırmızı bir halka görseli eklendi.
  - `EnemyDasher.cs`: "Perfect Dash" mekaniğinin oyuncudan uzaklaşma ivmesi ve süresi önemli ölçüde uzatıldı (`dodgeSpeed` 11f -> 18f, `dodgeDuration` 0.22f -> 0.28f). Atılma sonrası menzil açması çok daha belirgin hale getirildi.
  - **SO Rules Açıklaması:** Yeni düşmanlardaki hareket/bekleme gibi mantıksal AI karar değişkenlerinin SO'dan değil direkt kendi scriptlerinden çalıştığı, sadece Hasar/Can/Genel Hız/Altın verilerinin `EnemySO` üzerinden okunduğu standartlaştırıldı/doğrulandı.
- **Files touched:**
  - `Assets/_Project/Scripts/Enemy/EnemyKamikaze.cs`
  - `Assets/_Project/Scripts/Enemy/EnemyDasher.cs`

## 2026-02-28 ~17:40:00
- **What changed:** `SimpleArena.cs` güncellenerek görünmez collider olan Arena duvarlarına Fiziksel/Görsel bir yapı eklendi.
  - Script içerisine `wallSprite` ve `wallColor` parametreleri eklendi.
  - Duvarlar oluşturulurken Tiled (Döşemeli) SpriteDrawMode kullanılacak şekilde güncellendi. Artık duvarlar sündürülmek yerine belirlenen Sprite ile tekrar edilerek kaplanıyor. `transform.localScale` = Vector3.one olarak kilitlenip büyüklükler direkt `BoxCollider2D.size` ve `SpriteRenderer.size` üzerinden yapılmaya başlandı.
- **Files touched:**
  - `Assets/_Project/Scripts/Core/SimpleArena.cs`
- **Next step:** Unity Editor üzerinden `Room_xxx` prefablarında SimpleArena bileşenine bir "Wall Sprite" (Örn: Square) atanmalı.
- **Risks:** Sprite atamalarında resimlerin "Mesh Type" ayarı "Tight" kalırsa Tile işlemi bozulup sündürülebilir. Kullanılacak duvar resimlerinin Mesh Type'ı Inspector'dan `Full Rect` yapılmalıdır.