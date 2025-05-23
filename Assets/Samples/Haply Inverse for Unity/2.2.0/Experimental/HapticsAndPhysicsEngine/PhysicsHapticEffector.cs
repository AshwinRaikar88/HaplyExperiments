/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */

using Haply.Inverse.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Haply.Samples.Experimental.HapticsAndPhysicsEngine
{
    public class LowPassFilter
    {
        public float alpha = 0.2f;  // Smoothing factor [0, 1]
        private Vector3 filteredValue;

        public Vector3 Update(Vector3 newValue)
        {
            filteredValue = alpha * newValue + (1 - alpha) * filteredValue;
            return filteredValue;
        }

        public void Reset(Vector3 initialValue)
        {
            filteredValue = initialValue;
        }
    }


    /// <summary>
    /// <b>EXPERIMENTAL: </b><br/>
    /// This is a more advanced example of using Unity's physics engine to implement haptics feedback in a complex scene.
    ///
    /// <para>
    /// The following two effectors are being used in the scene:
    /// </para>
    /// <list type="bullet">
    ///     <item>The Cursor (kinematic) directly controlled by the device on each haptic frame</item>
    ///     <item>The PhysicEffector (non-kinematic) with colliders linked to the Cursor by a joint
    /// attached to a sprint and damper configured with a large constant</item>
    /// </list>
    ///
    /// The haptic force is relative to the distance between the two effectors such that when the physics effector is blocked by
    /// an object in the scene, an opposing force proportional to the distance will be generated.
    ///
    /// <para>
    /// Thanks to the spring/damper, movable objects mass can be felt and Unity Physics Materials on scene objects can be used
    /// to have different haptic feelings.
    /// </para>
    ///
    /// <remarks>
    /// The haptics feeling of drag or friction that occurs on moving effector is caused by the difference in update frequency between
    /// Unity's physics engine (between 60Hz to 120Hz) and the haptics thread (~1000Hz). This difference means that the physics
    /// effector will always be lagging behind the Cursor's true position which leads to forces that resemble a step function
    /// instead of having continuous forces.
    ///
    /// <para>Solutions :</para>
    /// <list type="bullet">
    ///     <item>Decrease the value of ProjectSettings.FixedTimestep as close to 0.001 as possible which will have
    ///     significant impact on performances for complex scenes.</item>
    ///     <item>Apply forces only when collisions occur (see <see cref="collisionDetection"/>)</item>
    ///     <item>Use a third-party physic/haptic engine like (TOIA, SOFA, etc...) as a middleware between the two physics
    ///     engine to simulate the contact points.</item>
    /// </list>
    /// </remarks>
    ///
    /// </summary>
    public class PhysicsHapticEffector : MonoBehaviour
    {
        // HAPTICS
        [Header("Haptics")]
        [Tooltip("Enable/Disable force feedback")]
        public bool forceEnabled;

        [SerializeField]
        [Range(0, 800)]
        private float stiffness = 400f;

        [SerializeField]
        [Range(0, 3)]
        private float damping = 1;

        [SerializeField]
        [Range(0, 10)]
        private float viscosity = 1.0f; 

        [SerializeField]
        [Range(0, 3)]
        private float exponent = 2.5f;

        // PHYSICS
        [Header("Physics")]

        [SerializeField]
        private float drag = 20f;

        [SerializeField]
        private float linearLimit = 0.001f;

        [SerializeField]
        private float limitSpring = 500000f;

        [SerializeField]
        private float limitDamper = 10000f;

        private ConfigurableJoint _joint;
        private Rigidbody _rigidbody;

        #region Thread-safe cached data

        /// <summary>
        /// Represents scene data that can be updated in the Update() call.
        /// </summary>
        private struct PhysicsCursorData
        {
            public Vector3 position;
            public bool collision;
            public string objectTag;
        }

        /// <summary>
        /// Cached version of the scene data.
        /// </summary>
        private PhysicsCursorData _cachedPhysicsCursorData;

        /// <summary>
        /// Lock to ensure thread safety when reading or writing to the cache.
        /// </summary>
        private readonly ReaderWriterLockSlim _cacheLock = new();

        /// <summary>
        /// Safely reads the cached data.
        /// </summary>
        /// <returns>The cached scene data.</returns>
        private PhysicsCursorData GetSceneData()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _cachedPhysicsCursorData;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Safely updates the cached data.
        /// </summary>
        private void SaveSceneData()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cachedPhysicsCursorData.position = transform.localPosition;
                _cachedPhysicsCursorData.collision = collisionDetection && touched.Count > 0;

                if (touched.Count > 0)
                {
                    Collider col = touched[0].GetComponent<Collider>();

                    _cachedPhysicsCursorData.objectTag = col != null
                        ? col.gameObject.tag
                        : "None";
                }
                else
                {
                    _cachedPhysicsCursorData.objectTag = "None";
                }


            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        #endregion

        [Header("Collision detection")]
        [Tooltip("Apply force only when a collision is detected (prevent air friction feeling)")]
        public bool collisionDetection;
        public List<Collider> touched = new();

        public Inverse3 Inverse3 { get; private set; }

        protected void Awake()
        {
            Inverse3 = GetComponentInParent<Inverse3>();

            // create the physics link between physic effector and device cursor
            AttachToInverseCursor();
            SetupCollisionDetection();
        }

        protected void OnEnable()
        {
            //TODO use world position
            Inverse3.DeviceStateChanged += OnDeviceStateChanged;
        }

        protected void OnDisable()
        {
            Inverse3.DeviceStateChanged -= OnDeviceStateChanged;
        }

        protected void FixedUpdate()
        {
            SaveSceneData();
        }

        //PHYSICS
        #region Physics Joint

        /// <summary>
        /// Attach the current physics effector to device Cursor with a joint
        /// </summary>
        private void AttachToInverseCursor()
        {
            // Add kinematic rigidbody to cursor
            var rbCursor = Inverse3.Cursor.gameObject.GetComponent<Rigidbody>();
            if (!rbCursor)
            {
                rbCursor = Inverse3.Cursor.gameObject.AddComponent<Rigidbody>();
                rbCursor.useGravity = false;
                rbCursor.isKinematic = true;
            }

            // Add non-kinematic rigidbody to self
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            if (!_rigidbody)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = false;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            // Connect with cursor rigidbody with a spring/damper joint and locked rotation
            _joint = gameObject.GetComponent<ConfigurableJoint>();
            if (!_joint)
            {
                _joint = gameObject.AddComponent<ConfigurableJoint>();
            }
            _joint.connectedBody = rbCursor;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = _joint.connectedAnchor = Vector3.zero;
            _joint.axis = _joint.secondaryAxis = Vector3.zero;

            // limited linear movements
            _joint.xMotion = _joint.yMotion = _joint.zMotion = ConfigurableJointMotion.Limited;

            // lock rotation to avoid sphere roll caused by physics material friction instead of feel it
            _joint.angularXMotion = _joint.angularYMotion = _joint.angularZMotion = ConfigurableJointMotion.Locked;

            // configure limit, spring and damper
            _joint.linearLimit = new SoftJointLimit() { limit = linearLimit };
            _joint.linearLimitSpring = new SoftJointLimitSpring() { spring = limitSpring, damper = limitDamper };

            // stabilize spring connection
            _rigidbody.linearDamping = drag;
        }

        #endregion

        // HAPTICS
        #region Haptics

        /// <summary>
        /// Calculate the force to apply based on the cursor position and the scene data
        /// <para>This method is called once per haptic frame (~1000Hz) and needs to be efficient</para>
        /// </summary>
        /// <param name="hapticCursorPosition">cursor position</param>
        /// <param name="hapticCursorVelocity">cursor velocity</param>
        /// <param name="physicsCursorPosition">physics cursor position</param>
        /// <returns>Force to apply</returns>
        private Vector3 ForceCalculation(Vector3 hapticCursorPosition, Vector3 hapticCursorVelocity,
            Vector3 physicsCursorPosition)
        {
            var force = physicsCursorPosition - hapticCursorPosition;
            force *= stiffness;
            force -= hapticCursorVelocity * damping;
            return force;
        }
        
        private LowPassFilter forceFilter = new LowPassFilter { alpha = 0.2f };

        private Vector3 FluidDensityField(float viscosity, Vector3 hapticCursorVelocity){
            float maxForce = 2;
            Vector3 force = -viscosity * hapticCursorVelocity; 
            
            // Debug.Log(force);
            force = forceFilter.Update(force);
            
            return Vector3.ClampMagnitude(force, maxForce);
        }

        // This is a prototype
        private Vector3 FluidicForce(Vector3 hapticCursorPosition, Vector3 hapticCursorVelocity, 
                             Vector3 physicsCursorPosition)
        {
            var invforce =  hapticCursorPosition - physicsCursorPosition;
            
            Vector3 penetrationDir = invforce.normalized;         
            Vector3 invProjectedVelocity = Vector3.Scale(hapticCursorVelocity, penetrationDir);
            
            invforce -= invProjectedVelocity * exponent;
            return invforce;
        }
        #endregion

        // COLLISION DETECTION
        #region Collision Detection

        private void SetupCollisionDetection()
        {
            // Add collider if not exists
            var col = gameObject.GetComponent<Collider>();
            if (!col)
            {
                col = gameObject.AddComponent<SphereCollider>();
            }

            // Neutral PhysicMaterial to interact with others
            if (!col.material)
            {
                col.material = new PhysicsMaterial { dynamicFriction = 0, staticFriction = 0 };
            }

            collisionDetection = true;
        }

        /// <summary>
        /// Called when effector touch other game object
        /// </summary>
        /// <param name="collision">collision information</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (forceEnabled && collisionDetection && !touched.Contains(collision.collider))
            {
                // store touched object
                touched.Add(collision.collider);
            }
        }

        /// <summary>
        /// Called when effector move away from another game object
        /// </summary>
        /// <param name="collision">collision information</param>
        private void OnCollisionExit(Collision collision)
        {
            if (forceEnabled && collisionDetection && touched.Contains(collision.collider))
            {
                touched.Remove(collision.collider);
            }
        }

        #endregion

        private void OnDeviceStateChanged(Inverse3 inverse3)
        {
            var physicsCursorData = GetSceneData();
            string objectTag = physicsCursorData.objectTag;

            if (!forceEnabled || (collisionDetection && !physicsCursorData.collision))
            {
                // Don't compute forces if there are no collisions which prevents feeling drag/friction while moving through air.
                inverse3.Release();
                return;
            }
            
            var force = new Vector3();
    
            if (objectTag.Equals("Fluid"))
            {
                force = FluidDensityField(viscosity, inverse3.CursorLocalVelocity);                         
            }
            else if (objectTag.Equals("Fluid2"))
            {
                // force = FluidicForce(inverse3.CursorLocalPosition, inverse3.CursorLocalVelocity, physicsCursorData.position);   
                force = FluidDensityField(5, inverse3.CursorLocalVelocity);                         
            }
            else {
                force = ForceCalculation(inverse3.CursorLocalPosition, inverse3.CursorLocalVelocity, physicsCursorData.position);
            }

            inverse3.CursorSetLocalForce(force);    
        }
    }
}
