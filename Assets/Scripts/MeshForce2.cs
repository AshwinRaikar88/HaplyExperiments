/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */

using System.Threading;
using Haply.Inverse.Unity;
using Haply.Samples.Tutorials.Utils;
using UnityEngine;

namespace Haply.Samples.Tutorials._4B_DynamicForceWithMesh
{
    /// <summary>
    /// Haptic feedback using a MeshCollider. Calculates contact forces based on proximity to the mesh surface,
    /// safely caching closest points from the main thread for use in the haptic thread.
    /// </summary>
    public class MeshForceFeedback : MonoBehaviour
    {
        [Range(0, 800)]
        public float stiffness = 500f;

        [Range(0, 3)]
        public float damping = 1f;

        public bool cursorProvidesHapticsToEachOther = true;

        private Inverse3[] inverse3s;
        private MeshCollider _meshCollider;
        private MovableObject _movableObject;

        #region Thread-safe cached data

        private struct SceneData
        {
            public Vector3 ballVelocity;
            public float[] cursorRadii;
            public Vector3[] closestPoints;
        }

        private SceneData _cachedSceneData;

        private readonly ReaderWriterLockSlim _cacheLock = new();

        private SceneData GetSceneData()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _cachedSceneData;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        private void SaveSceneData()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                for (int i = 0; i < inverse3s.Length; i++)
                {
                    var cursorTransform = inverse3s[i].Cursor.Model.transform;

                    _cachedSceneData.cursorRadii[i] = cursorTransform.lossyScale.x / 2f;

                    if (_meshCollider != null)
                    {
                        _cachedSceneData.closestPoints[i] = _meshCollider.ClosestPoint(cursorTransform.position);
                    }
                }

                _cachedSceneData.ballVelocity = _movableObject.Velocity;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        #endregion

        private void Awake()
        {
            inverse3s = FindObjectsOfType<Inverse3>();
            _movableObject = GetComponent<MovableObject>();
            _meshCollider = GetComponent<MeshCollider>();

            if (_meshCollider == null)
            {
                Debug.LogError("MeshForceFeedback requires a MeshCollider on the GameObject.");
            }

            _cachedSceneData.cursorRadii = new float[inverse3s.Length];
            _cachedSceneData.closestPoints = new Vector3[inverse3s.Length];
        }

        private void Start() => SaveSceneData();

        private void OnEnable()
        {
            foreach (var inverse3 in inverse3s)
            {
                inverse3.DeviceStateChanged += OnDeviceStateChanged;
            }
        }

        private void OnDisable()
        {
            foreach (var inverse3 in inverse3s)
            {
                inverse3.DeviceStateChanged -= OnDeviceStateChanged;
                inverse3.Release();
            }
        }

        private void Update() => SaveSceneData();

        private Vector3 ForceCalculation(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius,
            Vector3 closestPoint, Vector3 meshVelocity)
        {
            var force = Vector3.zero;
            var distanceVector = cursorPosition - closestPoint;
            var distance = distanceVector.magnitude;

            var penetration = cursorRadius - distance;

            if (penetration > 0f)
            {
                var normal = distanceVector.normalized;

                force = normal * penetration * stiffness;

                var relativeVelocity = cursorVelocity - meshVelocity;
                force -= relativeVelocity * damping;
            }

            return force;
        }

        private void OnDeviceStateChanged(Inverse3 device)
        {
            var index = System.Array.IndexOf(inverse3s, device);
            if (index < 0) return;

            var otherIndex = (index + 1) % inverse3s.Length;
            var sceneData = GetSceneData();

            var force = ForceCalculation(
                device.CursorPosition,
                device.CursorVelocity,
                sceneData.cursorRadii[index],
                sceneData.closestPoints[index],
                sceneData.ballVelocity
            );

            if (cursorProvidesHapticsToEachOther && index != otherIndex)
            {
                force += ForceCalculation(
                    device.CursorPosition,
                    device.CursorVelocity,
                    sceneData.cursorRadii[index],
                    sceneData.closestPoints[otherIndex],
                    sceneData.ballVelocity // or other device's velocity if tracked
                );
            }

            device.CursorSetForce(force);
        }
    }
}
