/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */

using Haply.Inverse.Unity;
using Haply.Samples.Experimental.Utils;
using UnityEngine;

namespace Haply.Samples.Experimental.VerseGripNavigation
{
    public class WorkspaceScaleController : MonoBehaviour
    {
        [Tooltip("Rotating object to use to control scaling.")]
        public RotationCounter rotationCounter;

        [Tooltip("Rotation scaling sensitivity.")]
        [Range(0, 1)]
        public float scalingFactor = 0.5f;

        [Range(0, 10)]
        public float minimumScale = 1f;

        [Range(1, 10)]
        public float maximumScale = 5f;

        private HapticOrigin _hapticOrigin;

        // Saved workspace scale prior to any modification
        private float _baseScale;

        private void OnEnable()
        {
            if (_hapticOrigin == null)
            {
                _hapticOrigin = GetComponent<HapticOrigin>();
            }

            _baseScale = _hapticOrigin.UniformScale;
            rotationCounter.ResetCounter();
        }

        protected void Update()
        {
            // Calculate scale relative to cursor roll on Z-axis rotation
            var scale = _baseScale - rotationCounter.TotalRotation * scalingFactor * 10f;

            // Limit between minimumScale and maximumScale
            scale = Mathf.Clamp(scale, minimumScale, maximumScale);

            // Set new scale
            _hapticOrigin.UniformScale = scale;
        }
    }
}
