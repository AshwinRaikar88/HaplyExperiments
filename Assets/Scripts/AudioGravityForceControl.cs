using Haply.Inverse.Unity;
using UnityEngine;

public class AudioGravityForceControl : MonoBehaviour
{
    [Header("Audio Analyzer")]
    public AudioAnalyzer analyzer;

    [Header("Force Axis Mapping")]
    public bool useBassForY = true;

    [Header("Force Scaling")]
    [Range(0f, 5f)] public float bassForceMultiplier = 1.0f;
    [Range(0f, 5f)] public float trebleForceMultiplier = 1.0f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothSpeed = 0.2f;
    [Range(1f, 5f)] public float maxForce = 1.5f;

    [Header("Z-Axis Gravity")]
    [Range(-2f, 2f)] public float xAxisOffset = -0.5f;
    [Range(-2f, 2f)] public float yAxisOffset = -0.5f;
    [Range(-2f, 2f)] public float zAxisOffset = -0.5f;
    


    private Inverse3 _inverse3;

    private float currentForceX = 0f;
    private float currentForceY = 0f;

    private void Awake()
    {
        _inverse3 = GetComponentInChildren<Inverse3>();
    }

    private void Update()
    {
        if (_inverse3 == null)
            return;

        float targetForceY = useBassForY 
            ? analyzer.bass * bassForceMultiplier 
            : analyzer.treble * trebleForceMultiplier;

        // Smooth force changes
        currentForceY = Mathf.Lerp(currentForceY, targetForceY, smoothSpeed);

        // Clamp for safety
        currentForceY = Mathf.Clamp(currentForceY, -maxForce, maxForce);

        // Apply final force
        _inverse3.CursorSetForce(xAxisOffset, currentForceY-yAxisOffset, zAxisOffset);
    }
}
