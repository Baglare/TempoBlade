using UnityEngine;

public enum RewardType
{
    None,           // Baslangic odasi veya odulsuz
    MaxHealth,      // Maksimum cani kalici arttirir
    Heal,           // Sadece mevcut cani doldurur
    DamageUp,       // Silah hasarini arttirir
    TempoBoost,     // Tempo kazanimini hizlandirir veya bonus verir
    Gold            // Kalici altin (meta currency) — EconomyManager'a eklenir
}
