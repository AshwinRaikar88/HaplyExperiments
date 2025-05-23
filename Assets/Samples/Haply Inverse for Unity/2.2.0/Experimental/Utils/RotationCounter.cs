/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */

using UnityEngine;

namespace Haply.Samples.Experimental.Utils
{
    /// <summary>
    /// Tracks the total rotation of a GameObject around a specified axis.
    /// This class calculates the cumulative rotation, accounting for multiple full rotations
    /// beyond the typical 0-360 degree range. It can be used to monitor rotations around the X, Y, or Z axis.
    /// </summary>
    public class RotationCounter : MonoBehaviour
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        [Tooltip("Select the axis to track rotations.")]
        public Axis rotationAxis;

        [Tooltip("Threshold for detecting a full rotation. Adjust if rotations are very fast.")]
        [Range(0, 360f)]
        public float rotationThreshold = 330f;

        private float _baseAngle;
        private float _previousAngle;
        private int _rotationCount;

        [SerializeField]
        private float totalRotation;

        /// <summary>
        /// Gets the total rotation around the selected axis in terms of full rotations.
        /// </summary>
        public float TotalRotation
        {
            get => totalRotation;
        }

        private void Start()
        {
            ResetCounter();
        }

        /// <summary>
        /// Resets the rotation counter to zero.
        /// </summary>
        public void ResetCounter()
        {
            _rotationCount = 0;
            _previousAngle = _baseAngle = GetCurrentAngle();
        }

        private void Update()
        {
            var currentAngle = GetCurrentAngle();
            if (currentAngle - _previousAngle > rotationThreshold)
            {
                _rotationCount--;
            }
            else if (_previousAngle - currentAngle > rotationThreshold)
            {
                _rotationCount++;
            }
            _previousAngle = currentAngle;

            var angleSupp = _rotationCount >= 0 ?
                currentAngle - _baseAngle :
                -(360 - (currentAngle - _baseAngle));
            totalRotation = _rotationCount + angleSupp / 360f;
        }

        /// <summary>
        /// Gets the current angle of the GameObject based on the selected axis.
        /// </summary>
        /// <returns>The angle in degrees around the selected axis.</returns>
        private float GetCurrentAngle()
        {
            switch (rotationAxis)
            {
                case Axis.X:
                    return transform.localEulerAngles.x;
                case Axis.Y:
                    return transform.localEulerAngles.y;
                case Axis.Z:
                    return transform.localEulerAngles.z;
                default:
                    return 0f;
            }
        }
    }
}
