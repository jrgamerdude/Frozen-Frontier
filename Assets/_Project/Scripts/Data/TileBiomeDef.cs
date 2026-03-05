using System;
using UnityEngine;

namespace FrozenFrontier.Data
{
    [CreateAssetMenu(menuName = "Frozen Frontier/Tile Biome Definition", fileName = "TileBiomeDef")]
    public class TileBiomeDef : ScriptableObject
    {
        public string id = "biome_id";
        public string displayName = "Biome";
        public Sprite icon;
        public ResourceAmount[] baseRewards;
        [Range(0, 100)] public int hazardChancePercent = 20;
        [Min(0)] public int exploreDurationTicks = 8;
    }
}
