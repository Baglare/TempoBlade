# TempoBlade

TempoBlade, Unity 6 ile geliştirilmiş 2D/top-down aksiyon roguelite prototipidir. Proje; tempo tabanlı güçlenme, yakın dövüş, parry/deflect, oda bazlı encounter akışı, hub, kalıcı ilerleme, silah yükseltme ve data-driven düşman/ödül sistemleri üzerine kuruludur.

Bu README, repodaki mevcut dosya yapısı ve kod üzerinden hazırlanmıştır. Sahne, prefab veya oynanış davranışı değiştirilmemiştir.

## Proje Bilgileri

- Unity sürümü: `6000.3.1f1`
- Ürün adı: `TempoBlade`
- Company name: `SilkCoatGames`
- Render pipeline: Universal Render Pipeline `17.3.0`
- Ana input sistemi: Unity Input System `1.17.0`
- Ana sahneler: `MainMenu`, `Hub`, `Gameplay`
- Kayit dosyasi: `Documents/TempoBlade/save.json`
- Ana proje klasörü: `Assets/_Project`

## Hızlı Başlangıç

1. Unity Hub üzerinden projeyi `Unity 6000.3.1f1` ile aç.
2. Açılış sahnesi olarak `Assets/_Project/Scenes/MainMenu.unity` dosyasını aç.
3. Build Settings içinde aktif sahne sırasını kontrol et:
   - `Assets/_Project/Scenes/MainMenu.unity`
   - `Assets/_Project/Scenes/Hub.unity`
   - `Assets/_Project/Scenes/Gameplay.unity`
4. Play Mode başlat.
5. Ana menüden oyuna gir, hub sahnesinde etkileşim noktalarına yaklaş ve `E` ile etkileş.
6. Gameplay sahnesinde oda temizlenince açılan ödül kapılarından birine girerek sonraki odaya geç.

Not: `MainMenuManager` ve `HubManager` içinde bazı sahne adı alanlarının varsayılan değerleri eski isimler gibi görünüyor (`Scene_Hub`, `GameScene`). Sahnedeki Inspector değerleri build sahneleriyle uyuşmuyorsa geçişler çalışmayabilir.

## Build ve Paketler

Projede `Builds` klasörü altında önceki Windows build çıktıları bulunuyor:

- `TempoBlade_v0.1` - `TempoBlade_v0.11` arası build klasörleri
- `TempoBlade_v0.11.rar`

Öne çıkan Unity paketleri:

- `com.unity.render-pipelines.universal`
- `com.unity.inputsystem`
- `com.unity.cinemachine`
- `com.unity.2d.animation`
- `com.unity.2d.aseprite`
- `com.unity.2d.tilemap.extras`
- `com.unity.timeline`
- `com.unity.ugui`
- `com.unity.test-framework`
- `com.coplaydev.unity-mcp`

## Klasör Yapısı

```text
Assets/_Project/
  Art/                 Pixel art, animasyon ve çevre assetleri
  Audio/               VFX ses dosyaları
  Data/                Skill tree axis/node/progression assetleri
  Prefabs/             Player, enemy, room, projectile, UI ve effect prefabları
  Resources/           Runtime Resources varlıkları, özellikle audio cue catalog
  Scenes/              MainMenu, Hub, Gameplay ve test sahneleri
  ScriptableObjects/   Enemy, weapon, reward, room, animation ve tempo visual assetleri
  Scripts/             Oyun kodunun ana gövdesi
  UI/                  Proje UI assetleri için ayrılmış klasör
  VFX/                 Proje VFX assetleri için ayrılmış klasör
```

Kod tarafındaki ana klasörler:

```text
Assets/_Project/Scripts/
  Combat/                  Damage, parry, projectile, deflect ve hitbox altyapısı
  Core/                    Room flow, reward doors, arena, miniboss ve spawn layout
  Editor/                  Unity Editor yardımcı menüleri ve setup araçları
  Enemy/                   EnemyBase ve tüm düşman davranışları
  Environment/             TrapArea gibi çevre tehlikeleri
  Hub/                     Hub etkileşim noktaları
  Managers/                Game, run, save, tempo, audio, economy, progression yöneticileri
  Player/                  Hareket, combat, perk, finisher ve weapon runtime
  Progression/             Resource wallet, core/weapon progression, pact ve reward skeleton
  ScriptableObjects_Data/  Runtime veriyi taşıyan ScriptableObject tanımları
  SkillTree/               Skill tree node effect, visibility ve build modelleri
  UI/                      HUD, shop, blacksmith, modal, skill tree ve feedback UI
  VFX/                     Weapon arc, directional animation, hit flash, ghost trail, sorting
```

## Sahne Akışı

```text
MainMenu
  -> Hub
      -> Gameplay
          -> RoomManager oda prefabını kurar
          -> wave düşmanlarını spawn eder
          -> oda temizlenince reward door açar
          -> RewardDoor seçimi RunManager'a yazar
          -> LevelManager aynı Gameplay sahnesini sonraki oda için yeniden yükler
```

Aktif build sahneleri:

- `Assets/_Project/Scenes/MainMenu.unity`
- `Assets/_Project/Scenes/Hub.unity`
- `Assets/_Project/Scenes/Gameplay.unity`

Ek sahneler:

- `Boot.unity`: build sırasına ekli değil.
- `Gameplay_VisualTest.unity`: görsel/test amaçlı sahne.

## Ana Oyun Döngüsü

1. Oyuncu ana menüden hub'a geçer.
2. Hub'da mağaza, demirci ve run başlatma etkileşimleri bulunur.
3. Run başlayınca `RunManager` run state'i sıfırlar.
4. `RoomManager`, `RunManager.roomSequence` içindeki sıradaki `RoomSO` verisini alır.
5. `RoomSO.roomPrefab` sahneye instantiate edilir.
6. `RoomLayout` üzerinden oyuncu başlangıç noktası, düşman spawn noktaları ve reward door referansları okunur.
7. Wave'ler sırayla spawn edilir.
8. Düşmanlar ölünce `RoomManager.OnEnemyDied` aktif listeyi günceller.
9. Oda temizlenince bekleyen ödül uygulanır, yeni kapı ödülleri atanır ve kapılar açılır.
10. Oyuncu kapıdan geçince oyuncu canı/tempo gibi run state kaydedilir ve sonraki oda yüklenir.
11. Oda sırası biterse run tamamlanır, run altını kalıcı cüzdana yatırılır ve oyun `GameOver` state'ine geçer.

## Kontroller

Input asset içinde doğrulanan varsayılanlar:

- Hareket: `WASD` veya ok tuşları
- Saldırı: Sol fare tuşu veya `Enter`
- Hub etkileşimi: `E`
- UI iptal/kapatma: `Escape`
- UI navigasyon: `WASD`, ok tuşları, gamepad d-pad/stick

Kod tarafında `PlayerController` içinde `OnDodge` ve `OnParry` callback'leri bulunur. Ancak mevcut input action dosyasında `Dodge` ve `Parry` adlı action görünmüyor. Dodge/parry test edilirken Player prefabındaki `PlayerInput` bağlantıları ve input action adları ayrıca kontrol edilmelidir.

## Ana Sistemler

### Game State

`GameManager`, temel oyun durumunu tutar:

- `Menu`
- `Gameplay`
- `Paused`
- `GameOver`

`PauseManager`, `GameManager` ve UI panelleri birlikte kullanılarak zaman ölçeği ve menü durumu yönetilir.

### Scene Transition

`SceneTransitionManager`, sahneler arası fade efektini runtime'da oluşturduğu overlay canvas ile yapar. Singleton olarak `DontDestroyOnLoad` davranışı kullanır.

### Run State

`RunManager`, run boyunca taşınması gereken değerleri tutar:

- temizlenen oda sayısı
- sıradaki oda verisi
- pending reward
- oyuncu max/current health
- damage multiplier
- tempo
- son kullanılan kapı yönü
- run kaynak bankası
- mini-boss temizleme kayıtları

### Room ve Encounter

Ana dosyalar:

- `RoomSO`
- `RoomManager`
- `RoomLayout`
- `RewardDoor`
- `EliteSpawnLayer`
- `MiniBossRoomController`

`RoomSO`, oda prefabını, wave listesini, encounter tipini, zorluğu, mini-boss verisini, elite spawn ayarlarını ve ödül değerlerini taşır.

`RoomManager`, oda kurulumunu, düşman spawn planını, elite conversion katmanını, mini-boss yönlendirmesini, oda temizlenmesini ve reward door açılmasını yönetir.

### Reward Doors

`RewardDoor`, oda temizlenene kadar kilitli duran fiziksel kapı davranışıdır. Oda temizlenince reward icon görünür, kapı açılır ve oyuncu kapıya girince:

- seçilen reward `RunManager` içine yazılır
- çıkış yönü kaydedilir
- oyuncu run state'i kaydedilir
- bir sonraki oda yüklenir

### Player Movement

`PlayerController`, oyuncu hareketi ve state geçişlerini yönetir:

- `Idle`
- `Moving`
- `Dodging`
- `Parrying`
- `DashStriking`

Hareket; input, modal UI durumu, movement lock, external stagger ve dash strike gibi özel durumlara göre kesilebilir.

### Player Combat

`PlayerCombat`, oyuncunun canını, silahını, saldırılarını, combo sistemini, hasar/tempo etkileşimini ve finisher entegrasyonunu yönetir.

Öne çıkan noktalar:

- `WeaponSO` üzerinden hasar, saldırı hızı, menzil ve combo adımları okunur.
- Silah yoksa geriye dönük tek vuruş davranışı korunur.
- Combo adımları `ComboStepData` ile tanımlanır.
- `WeaponArcVisual` saldırı/parry yön ve menzil görselini günceller.
- `OnComboChanged`, `OnHealthChanged`, `OnAttackStarted`, `OnAttackHit`, `OnAttackEnded` eventleri UI/VFX tarafına veri taşır.

### Combo Sistemi

`ComboStepData` üç tip combo adımı destekler:

- `Normal`
- `MultiHit`
- `DashStrike`

Her `WeaponSO` kendi `comboSteps` dizisini taşıyabilir. Dizi boşsa silah klasik tek saldırı gibi çalışır.

### Parry ve Deflect

Ana dosyalar:

- `ParrySystem`
- `ParryIdentity`
- `IDeflectable`
- `DeflectContext`
- `Projectile`
- `BossProjectile`

`ParrySystem`, yönlü parry penceresini, perfect window'u, counter window'u, projectile scan'i ve deflect davranışını yönetir. Projectile tarafı `IDeflectable` arayüzüyle birleştirilmiştir.

`ParryIdentity`, bir objenin melee/projectile parry için zorla parry edilebilir, parry edilemez veya varsayılan davranışta olmasını sağlar.

### Tempo Sistemi

`TempoManager`, 0-100 arası tempo değerini ve tier geçişlerini yönetir:

- `T0`
- `T1`
- `T2`
- `T3`

Tempo arttıkça hasar ve hız çarpanları değişir:

- `T1`: hasar `x1.2`, hız `x1.1`
- `T2`: hasar `x1.5`, hız `x1.25`
- `T3`: hasar `x2.0`, hız `x1.5`

Tempo; zamanla düşebilir, hasar alınca cezalandırılabilir, perk/zone sistemleriyle çarpan alabilir ve UI/VFX sistemlerine event gönderir.

### Düşman Sistemi

`EnemyBase`, tüm düşmanların ortak tabanıdır:

- health ve death
- damage/stun
- defense controller
- elite profile
- support buff receiver
- tempo tier subscription
- room/economy bağlantısı
- damage popup ve hit feedback

Mevcut düşman scriptleri:

- `EnemyMelee`: yakın dövüş, swing arc, tempo bazlı combo/recovery
- `EnemyCaster`: menzilli büyücü, projectile, cast telegraph, overcharge
- `EnemyDuelist`: guard/parry reactive yakın dövüşçü
- `EnemyBoss`: faz 1 melee, faz 2 bullet hell
- `EnemyKamikaze`: koşup patlayan düşman, telegraph ve unstable core elite mekaniği
- `EnemyAssassin`: görünmez takip, lunge, shadow echo elite mekaniği
- `EnemyDasher`: kiting, projectile ve dash evade
- `EnemyDeadeye`: aim telegraph, uzun menzilli lock shot, reposition
- `EnemyTrapper`: tuzak bırakma, tether trap elite mekaniği
- `EnemyResonator`: support pulse, tempo static zone, crescendo elite mekaniği
- `EnemyWarden`: koruma, shield angle, living wall, berserk
- `EnemyWardenLinker`: guardian link, summon ve çoklu link elite davranışı

### Enemy Defense

`EnemyDefenseController`, `EnemyDefenseSettings` üzerinden şu davranışları çözer:

- stability
- broken state
- poise/interrupt resistance
- armor
- guard arc
- counter/special defense hook

Damage hesaplamaları `EnemyDamagePayload` ve `EnemyDamageResult` ile taşınır.

### Elite Layer

Elite sistemi iki parçadan oluşur:

- `EliteSpawnLayer`: wave spawn planı üstünde elite conversion yapar.
- `EliteProfileSO`: stat çarpanları ve düşman tipine özel elite mekanik ayarlarını taşır.

Mevcut elite mechanic tipleri:

- `CasterBurstOrb`
- `MeleeRendCombo`
- `DasherFalseExit`
- `AssassinShadowEcho`
- `TrapperTetherTrap`
- `DuelistGuardDebt`
- `DeadeyeEchoLine`
- `KamikazeUnstableCore`
- `WardenLivingDefenceWall`
- `ResonatorCrescendo`
- `WardenLinkerMultipleLink`

### Hub

Ana dosyalar:

- `HubManager`
- `HubInteractable`
- `ShopUI`
- `BlacksmithUI`
- `ProgressionWalletUI`
- `ModalUIManager`

Hub içinde oyuncu objelere yaklaşınca prompt gösterilir. `E` ile etkileşim tetiklenir. Mevcut interaction tipleri:

- mağaza açma
- run başlatma
- ana menüye dönme
- demirci açma

Modal UI açıkken oyuncu hareketi ve prompt gösterimi kilitlenebilir.

### Shop

`ShopUI`, kalıcı core yükseltmeler ve silah satın alma akışını yönetir.

Core yükseltmeler:

- maksimum can
- hasar çarpanı
- tempo kazanımı

Maliyet ve seviye ayarları `UpgradeConfigSO` içinde tutulur.

### Blacksmith

`BlacksmithUI`, açılmış silahları listeler ve silah yükseltme akışını yönetir.

Silah yükseltme tarafında:

- `WeaponSO` base upgrade dizileri
- `WeaponProgressionService`
- milestone tanımları
- ek kaynak maliyetleri
- success booster
- specialization seçimleri

kullanılır.

### Save Sistemi

`SaveManager`, kalıcı veriyi JSON olarak yazar/okur:

```text
Documents/TempoBlade/save.json
```

Kaydedilen ana bilgiler:

- toplam altın
- oynanan run sayısı
- en iyi oda ilerlemesi
- core upgrade seviyeleri
- açılmış silahlar
- kuşanılan silah
- silah upgrade ve specialization kayıtları
- persistent resource wallet
- skill tree node/form/commitment state
- pact contract state

### Economy

`EconomyManager`, run içinde kazanılan altını ve run sonunda kalıcı cüzdana yatırmayı yönetir. Düşman altın drop değerleri `EnemySO.goldDrop` üzerinden gelir.

### Progression

Projede birden fazla progression katmanı vardır:

- `CoreProgressionService`: kalıcı core upgrade verisi
- `WeaponProgressionService`: silah upgrade, milestone ve specialization akışı
- `ProgressionResourceWalletService`: kalıcı ve run içi kaynak cüzdanı
- `PactContractService`: run modifier context iskeleti
- `AxisProgressionManager`: skill-tree eksenleri, node unlock, commitment ve build derleme

### Skill Tree

Ana veri tipleri:

- `AxisDatabaseSO`
- `ProgressionAxisSO`
- `SkillNodeSO`
- `OpposingPairSO`
- `FormOverlaySO`
- `TreeProgressionConfigSO`

Ana runtime yöneticisi:

- `AxisProgressionManager`

Skill node'lar benzersiz `nodeId` ile kaydedilir. Node'lar tier, prerequisite, visibility, effect, commitment ve başlangıçta açık olma bilgilerini taşır.

### Perk Controller'ları

Oyuncu üstündeki perk davranışları ayrı controller'larda toplanmıştır:

- `DashPerkController`
- `ParryPerkController`
- `OverdrivePerkController`
- `CadencePerkController`

Bu controller'lar `AxisProgressionManager.CurrentBuild` ve combat eventleri üzerinden aktif etkileri uygular.

### Finisher

Finisher sistemi şu dosyalardan oluşur:

- `FinisherSO`
- `PlayerFinisherController`
- `FinisherTargetResolver`
- `FinisherExecutor`

Silahlar `WeaponSO.finisher` üzerinden finisher verisi taşıyabilir. Specialization verileri finisher override kullanabilir.

### UI

Öne çıkan UI scriptleri:

- `PlayerHealthUI`
- `TempoUI`
- `ParryIndicatorUI`
- `ComboHUD`
- `DamagePopup`
- `StatsPanel`
- `SkillTreePanelUI`
- `EncounterBreakdownUI`
- `ProgressionWalletUI`
- `ShopUI`
- `BlacksmithUI`
- `ModalUIManager`

UI tarafında bazı paneller runtime layout yardımcılarıyla normalize edilir (`ModalUIRuntimeUtility`, `ResponsiveSplitLayout`).

### Audio

Audio sistemi event tabanlıdır:

- `AudioEventId`: oyun içi ses event enum'u
- `AudioCueDefinition`: clip, volume, pitch, spatial blend, cooldown ayarı
- `AudioCueCatalogSO`: event-cue listesi
- `AudioManager`: event çalımı ve catalog lookup
- `AudioEmitter`: geçici veya objeye bağlı AudioSource davranışı

Varsayılan catalog:

```text
Assets/_Project/Resources/Audio/DefaultAudioCueCatalog.asset
```

### VFX ve Görsel Yardımcılar

Öne çıkanlar:

- `WeaponArcVisual`: saldırı/parry arc gösterimi
- `AttackVFXPresenter`: saldırı/parry/perfect parry görsel sunumu
- `HitFlash`
- `HitParticle`
- `GhostTrail`
- `GroundShadow`
- `TempoTransitionFX`
- `TempoEnemyEffect`
- `CharacterDirectionalAnimator`
- `DirectionalAnimationSetSO`
- `IsoFacingController`
- `YSortByPosition`

Sorting ve isometric görünüm için `WorldSortingUtility`, `IsoVisualRoot`, `IsoCameraFramingController` gibi yardımcılar bulunur.

## ScriptableObject Varlıkları

Mevcut data-driven varlık grupları:

- Weapons: `Assets/_Project/ScriptableObjects/Weapons`
- Enemy data: `Assets/_Project/ScriptableObjects/Enemies/Normal`
- Elite profiles: `Assets/_Project/ScriptableObjects/Enemies/Elite`
- Rewards: `Assets/_Project/ScriptableObjects/Rewards`
- Rooms: `Assets/_Project/ScriptableObjects/Rooms`
- Directional animation sets: `Assets/_Project/ScriptableObjects/Animations`
- Tempo visuals: `Assets/_Project/ScriptableObjects/TempoVisuals`
- Skill tree data: `Assets/_Project/Data/SkillTree_*`

## Yeni İçerik Eklerken

### Yeni Silah

1. `Assets/_Project/ScriptableObjects/Weapons` altında yeni `WeaponSO` oluştur.
2. `weaponName`, açıklama, ikon, weapon type ve base combat değerlerini doldur.
3. Gerekiyorsa `comboSteps` dizisini doldur.
4. Upgrade dizilerini veya `upgradeScalingData` değerlerini ayarla.
5. Silahın kullanılmasını istiyorsan `WeaponDatabase.asset` içine ekle.
6. Satılmasını istiyorsan `ShopUI.weaponsForSale` listesine ekle.
7. Demirci yükseltmesi için silahın unlock/save akışını kontrol et.

### Yeni Düşman

1. `EnemySO` oluştur ve combat statlarını doldur.
2. Düşmanın prefabını `Assets/_Project/Prefabs/Enemies` altında hazırla.
3. Prefab üstüne uygun `EnemyBase` türevi scripti ekle veya yeni davranış gerekiyorsa `EnemyBase` üzerinden türet.
4. `EnemySO.prefab` alanına prefabı bağla.
5. Defense ayarlarını `EnemySO.defense` içinde yap.
6. Elite olabilmesini istiyorsan `eliteEligible` ve ilgili `EliteProfileSO` verilerini ayarla.
7. Odaya eklemek için ilgili `RoomSO.waves.enemyGroups` listesine bağla.

### Yeni Oda

1. Oda prefabı oluştur.
2. Prefab üstünde `RoomLayout` olduğundan emin ol.
3. `playerStartPoint`, `enemySpawnPoints` ve `rewardDoors` referanslarını bağla.
4. Yeni `RoomSO` oluştur.
5. `roomPrefab` alanına oda prefabını bağla.
6. Wave listelerini doldur.
7. Odanın run içinde görünmesi için `RunManager.roomSequence` listesine ekle.

### Yeni Reward

1. `RewardDefinitionSO` türevi bir ScriptableObject sınıfı kullan veya yeni sınıf oluştur.
2. `rewardName`, icon, tint, category, rarity ve weight değerlerini doldur.
3. `GrantReward` davranışının oyuncuya beklenen etkiyi verdiğini kontrol et.
4. Reward assetini `RoomManager.possibleRewards` listesine ekle.

### Yeni Skill Tree Node

1. İlgili axis klasöründe `SkillNodeSO` oluştur.
2. `nodeId` değerinin benzersiz olduğundan emin ol.
3. Display name, açıklama, tier, prerequisites ve effects alanlarını doldur.
4. Node'u ilgili `ProgressionAxisSO.nodes` dizisine ekle.
5. Effect key için `EffectKeyRegistry` uyarılarını kontrol et.
6. UI'da görünüm ve unlock davranışını `SkillTreePanelUI` üzerinden test et.

## Geliştirme Notları

- Ana oyun kodu için ayrı `.asmdef` dosyası görünmüyor; kodlar varsayılan `Assembly-CSharp` altında derlenir.
- Unity tarafından üretilen `.sln` ve `.csproj` dosyaları repoda mevcut.
- `Library`, `obj`, `.vs` gibi Unity/IDE çıktıları geliştirme sırasında değişebilir.
- `Docs/AI_RULES.md`, çalışma sırasında plan/görev/worklog kaydı bekleyen proje içi kuralları tanımlar.
- Otomatik edit/play mode test klasörü görünmüyor. Mevcut doğrulama ağırlıklı olarak Unity Play Mode, sahne testi ve manuel kontrol üzerinden yapılmalıdır.

## Bilinen Dikkat Noktaları

- Git çalışma alanında bu README hazırlanırken `Assets/_Project/Prefabs/Player.prefab` ve `Assets/_Project/ScriptableObjects/Rooms/Room_Test.asset` dosyalarında önceden var olan değişiklikler görünüyordu. README çalışması bu dosyalara dokunmaz.
- Input asset ve `PlayerController` callback adları dodge/parry için yeniden kontrol edilmelidir.
- `Docs/PLAN.md` ve `Docs/TASKS.md` içindeki bazı backlog notları kodun mevcut haliyle tamamen güncel olmayabilir; güncel davranış için önce kod ve sahne/prefab ayarları kontrol edilmelidir.
- Save sistemi `Documents/TempoBlade/save.json` dosyasını kullanır. Build testlerinde eski save verisi davranışı etkileyebilir.

## Dokümantasyon

Proje içi dokümanlar:

- `Docs/PLAN.md`: yüksek seviye plan ve sıradaki hedefler
- `Docs/TASKS.md`: görev listesi ve kabul kriterleri
- `Docs/WORKLOG.md`: yapılan değişiklik kayıtları
- `Docs/DECISIONS.md`: karar kayıtları
- `Docs/AI_RULES.md`: AI/ajan çalışma kuralları
