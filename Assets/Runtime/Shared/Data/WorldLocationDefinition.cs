using System;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Defines a stable authored location in a public world map.</summary>
    [CreateAssetMenu(
        fileName = "WorldLocation_",
        menuName = "NinthAge/Definitions/World Location Definition")]
    public sealed class WorldLocationDefinition : DataDefinition
    {
        private const WorldLocationPurpose SUPPORTED_PURPOSES =
            WorldLocationPurpose.Starting
            | WorldLocationPurpose.SafeReturn
            | WorldLocationPurpose.RegionalFallback;

        [SerializeField]
        private string _mapID;

        [SerializeField]
        private double _positionX;

        [SerializeField]
        private double _positionY;

        [SerializeField]
        private double _positionZ;

        [SerializeField]
        private float _facingX;

        [SerializeField]
        private float _facingY;

        [SerializeField]
        private float _facingZ;

        [SerializeField]
        private float _facingW = 1f;

        [SerializeField]
        private WorldLocationPurpose _purposes;

        [SerializeField]
        private double _regionalFallbackRadius;

        /// <summary>Gets the canonical logical map ID.</summary>
        public string MapID => _mapID;

        /// <summary>Gets the double-precision world X coordinate.</summary>
        public double PositionX => _positionX;

        /// <summary>Gets the double-precision world Y coordinate.</summary>
        public double PositionY => _positionY;

        /// <summary>Gets the double-precision world Z coordinate.</summary>
        public double PositionZ => _positionZ;

        /// <summary>Gets the authored facing quaternion X component.</summary>
        public float FacingX => _facingX;

        /// <summary>Gets the authored facing quaternion Y component.</summary>
        public float FacingY => _facingY;

        /// <summary>Gets the authored facing quaternion Z component.</summary>
        public float FacingZ => _facingZ;

        /// <summary>Gets the authored facing quaternion W component.</summary>
        public float FacingW => _facingW;

        /// <summary>Gets the roles assigned to this location.</summary>
        public WorldLocationPurpose Purposes => _purposes;

        /// <summary>Gets the regional fallback coverage radius.</summary>
        public double RegionalFallbackRadius => _regionalFallbackRadius;

        protected override string AssetPrefix => "WorldLocation";

        /// <inheritdoc />
        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

            bool isValid = true;
            if (string.IsNullOrWhiteSpace(_mapID)
                || !StringComparer.Ordinal.Equals(_mapID, _mapID.Trim()))
            {
                Debug.LogError($"[{GetType().Name}] Definition '{DefinitionID}' has empty MapID");
                isValid = false;
            }

            if (!IsFinite(_positionX) || !IsFinite(_positionY) || !IsFinite(_positionZ))
            {
                Debug.LogError($"[{GetType().Name}] Definition '{DefinitionID}' has non-finite position coordinates");
                isValid = false;
            }

            double facingSquaredLength = (double)_facingX * _facingX
                + (double)_facingY * _facingY
                + (double)_facingZ * _facingZ
                + (double)_facingW * _facingW;
            if (!IsFinite(_facingX)
                || !IsFinite(_facingY)
                || !IsFinite(_facingZ)
                || !IsFinite(_facingW)
                || Math.Abs(facingSquaredLength - 1d) > 0.001d)
            {
                Debug.LogError($"[{GetType().Name}] Definition '{DefinitionID}' has an invalid facing quaternion");
                isValid = false;
            }

            if (_purposes == WorldLocationPurpose.None
                || (_purposes & ~SUPPORTED_PURPOSES) != 0)
            {
                Debug.LogError(
                    $"[{GetType().Name}] Definition '{DefinitionID}' must have at least one supported purpose");
                isValid = false;
            }

            if ((_purposes & WorldLocationPurpose.RegionalFallback) != 0)
            {
                if (!IsFinite(_regionalFallbackRadius) || _regionalFallbackRadius <= 0d)
                {
                    Debug.LogError(
                        $"[{GetType().Name}] Definition '{DefinitionID}' has an invalid regional fallback radius");
                    isValid = false;
                }
            }
            else if (_regionalFallbackRadius != 0d)
            {
                Debug.LogError(
                    $"[{GetType().Name}] Definition '{DefinitionID}' has a regional fallback radius without that purpose");
                isValid = false;
            }

            return isValid;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
