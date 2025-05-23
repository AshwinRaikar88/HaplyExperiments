/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */
using Haply.Inverse.Unity;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Haply.Samples.Experimental.HapticsAndPhysicsEngine
{
    public class GameManager : MonoBehaviour
    {
        [Range(1, 10000)]
        [Tooltip("Adjust Fixed Timestep directly from here to compare with Haptics Thread frequency")]
        public int physicsFrequency = 1000;

        [SerializeField]
        private ClientConfiguration clientConfiguration;

        public PhysicsHapticEffector physicsEffector;

        public Material enabledForceMaterial;
        public Material disabledForceMaterial;

        [Header("UI")]
        public Text helpText;
        public GameObject frequenciesPanel;
        public Text physicsFrequencyText;
        public Text hapticsFrequencyText;
        public string enableForceMessage = "Move the sphere at center not touching anything and hit SPACE to enable force";
        public string collisionMessage = "Press C to enable/disable collision detection";

        protected void Start()
        {
            physicsEffector = FindObjectOfType<PhysicsHapticEffector>();
        }

        // Update is called once per frame
        protected void Update()
        {
            HandleStateTransitionInputs();
            HandleFrequencyTweakInputs();

            if ( frequenciesPanel.activeSelf )
            {
                // adjust Fixed Timestep from inspector to compare and understand for demo
                // don't do that in real case, prefer change from ProjectSettings>Time panel
                Time.fixedDeltaTime = 1f / physicsFrequency;
                physicsFrequencyText.text = $"physics : {physicsFrequency}Hz";
                hapticsFrequencyText.text = $"haptics : {clientConfiguration.HapticPollFrequency}Hz";
            }
        }

        public void ToggleForceFeedback()
        {
            physicsEffector.forceEnabled = !physicsEffector.forceEnabled;
            physicsEffector.gameObject.GetComponent<MeshRenderer>().enabled = physicsEffector.forceEnabled;

            physicsEffector.Inverse3.Cursor.Model.gameObject.GetComponent<MeshRenderer>().material =
                physicsEffector.forceEnabled ? enabledForceMaterial : disabledForceMaterial;

            helpText.text = physicsEffector.forceEnabled ? collisionMessage : enableForceMessage;
        }

        // Display collision infos
        // ----------------------------------
        protected void OnGUI()
        {
            if (physicsEffector.touched.Count <= 0)
            {
                return;
            }
            // display touched physic material infos on screen
            var physicMaterial = physicsEffector.touched[0].GetComponent<Collider>().material;
            var text =
                $"Physics material: {physicMaterial.name.Replace( "(Instance)", "" )}" +
                $" \ndynamic friction: {physicMaterial.dynamicFriction}," +
                $" static friction: {physicMaterial.staticFriction}\n";

            // display touched rigidbody infos on screen
            var rb = physicsEffector.touched[0].GetComponent<Rigidbody>();
            if (rb)
            {
                text += $"mass: {rb.mass}, drag: {rb.linearDamping}, angular drag: {rb.angularDamping}\n";
            }

            GUI.Label(new Rect(20, 40, 800f, 200f), text);
        }

        #region Inputs

        private void HandleStateTransitionInputs()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleForceFeedback();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                physicsEffector.collisionDetection = !physicsEffector.collisionDetection;
            }
        }

        private void HandleFrequencyTweakInputs()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && physicsFrequency < 10000)
            {
                physicsFrequency += 50;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && physicsFrequency > 200)
            {
                physicsFrequency -= 100;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                physicsFrequency /= 2;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) && frequenciesPanel.activeSelf &&
                clientConfiguration.HapticPollFrequency < 10000)
            {
                clientConfiguration.HapticPollFrequency += 50;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) && frequenciesPanel.activeSelf &&
                clientConfiguration.HapticPollFrequency > 200)
            {
                clientConfiguration.HapticPollFrequency -= 100;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) && frequenciesPanel.activeSelf &&
                clientConfiguration.HapticPollFrequency > 0)
            {
                clientConfiguration.HapticPollFrequency /= 2;
            }
        }

        #endregion
    }
}
