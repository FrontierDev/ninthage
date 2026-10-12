using UnityEngine;

namespace Game.Shared.Terrain
{
    /// <summary>
    /// Defines the texture assets for a named zone.
    ///
    /// Terrain tiles reference a zone by integer ID. The terrain shader samples
    /// control map channel weights and uses these Texture2DArrays to blend
    /// terrain materials at runtime. Layer indices correspond to control map
    /// channels as follows:
    ///
    ///   control_0 : R = layer0, G = layer1, B = layer2, A = layer3
    ///   control_1 : R = layer4, G = layer5, B = layer6, A = layer7
    ///
    /// Texture array slices must be ordered so that slice N matches layer N.
    /// </summary>
    [CreateAssetMenu(menuName = "NinthAge/Terrain/Zone Definition", fileName = "NewZoneDefinition")]
    public class ZoneDefinition : ScriptableObject
    {
        /// <summary>Gets or sets the zone display name.</summary>
        public string zoneName;
        /// <summary>Gets or sets the zone albedo texture array.</summary>
        public Texture2DArray albedoArray;
        /// <summary>Gets or sets the zone normal texture array.</summary>
        public Texture2DArray normalArray;
    }
}
