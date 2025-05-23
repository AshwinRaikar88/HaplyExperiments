/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */

using Haply.Inverse.Unity;
using UnityEngine;

namespace Haply.Samples.Experimental.VerseGripNavigation
{
    public class GameManager : MonoBehaviour
    {
        public Inverse3 inverse3;
        public VerseGrip verseGrip;

        public WorkspaceOffsetController offsetController;
        public WorkspaceScaleController scaleController;

        [Tooltip("Optional GameObject to visualize the cursor's movement scope. Its local position is automatically" +
            " adjusted to the workspace center at runtime." +
            " Enable Gizmos in the Game View for additional workspace visualization.")]
        public GameObject bounds;

        // Saved workspace scale prior to any modification
        private float _baseScale;

        private void Start()
        {
            if (bounds)
            {
                inverse3.Ready.AddListener(device =>
                    bounds.transform.localPosition = device.WorkspaceCenterLocalPosition);
            }

            offsetController.enabled = false;
            scaleController.enabled = false;
        }

        private void OnEnable()
        {
            verseGrip.ButtonDown.AddListener(OnButtonDown);
            verseGrip.ButtonUp.AddListener(OnButtonUp);
        }

        private void OnDisable()
        {
            verseGrip.ButtonDown.RemoveListener(OnButtonDown);
            verseGrip.ButtonUp.RemoveListener(OnButtonUp);
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift)) offsetController.enabled = true;
            if (Input.GetKeyUp(KeyCode.LeftShift)) offsetController.enabled = false;

            if (Input.GetKeyDown(KeyCode.LeftAlt)) scaleController.enabled = true;
            if (Input.GetKeyUp(KeyCode.LeftAlt)) scaleController.enabled = false;

            if (bounds)
                bounds.SetActive(offsetController.enabled || scaleController.enabled);
        }

        private void OnButtonDown(VerseGrip device, VerseGripButton button)
        {
            offsetController.enabled = true;
            scaleController.enabled = true;
        }

        private void OnButtonUp(VerseGrip device, VerseGripButton button)
        {
            offsetController.enabled = false;
            scaleController.enabled = false;
        }

        protected void OnGUI()
        {
            const float width = 600;
            const float height = 60;
            var rect = new Rect((Screen.width - width) / 2, Screen.height - height - 10, width, height);

            var text = "";
            if (!offsetController.enabled && !scaleController.enabled)
            {
                text = "Press HANDLE BUTTON to calibrate workspace\n" +
                    "(press SHIFT to MOVE only, ALT to SCALE only)";
            }
            if (offsetController.enabled)
            {
                text += $"Move the Inverse3 cursor to move the workspace : {offsetController.transform.position}\n";
            }
            if (scaleController.enabled)
            {
                text += $"Rotate the VerseGrip to scale the workspace : ({scaleController.transform.localScale.x:0.000})\n";
            }

            GUI.Box(rect, text, CenteredStyle());
        }

        private static GUIStyle CenteredStyle()
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = Color.white
                },
                fontSize = 14
            };
            return style;
        }

    }
}
