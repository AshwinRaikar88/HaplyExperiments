/*
 * Copyright 2024 Haply Robotics Inc. All rights reserved.
 */

using UnityEngine;

namespace Haply.Samples.Experimental.VerseGripNavigation
{
    public class WorkspaceOffsetController : MonoBehaviour
    {
        public Transform cursor;

        // Saved workspace and cursor positions prior to any modification
        private Vector3 _basePosition;
        private Vector3 _cursorBasePosition;

        protected void Awake()
        {
            // Check if cursor is a child of the cursor offset
            if (!cursor.IsChildOf(transform))
            {
                Debug.LogError($"Cursor '{cursor.name}' must be a child of '{name}'", gameObject);
            }
        }

        private void OnEnable()
        {
            _basePosition = transform.localPosition;
            _cursorBasePosition = cursor.localPosition;
        }

        protected void Update()
        {
            // Move cursor offset relative to cursor position
            transform.position = _basePosition - Vector3.Scale(cursor.localPosition -
                _cursorBasePosition, transform.lossyScale);
        }
    }
}
