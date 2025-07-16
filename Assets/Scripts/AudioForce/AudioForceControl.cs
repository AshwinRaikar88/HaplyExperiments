using Haply.Inverse.Unity;
using UnityEngine;

public class AudioForceControl : MonoBehaviour
{
    [Header("Audio Analyzer")]
    public AudioAnalyzer analyzer;

    [Header("Force Axis Mapping")]
    public bool useBassForY = true;
    public bool useTrebleForX = true;

    [Header("Force Scaling")]
    [Range(0f, 5f)]
    public float bassForceMultiplier = 1.0f;
    [Range(0f, 5f)]
    public float trebleForceMultiplier = 1.0f;


    [Header("Oscillation")]
    public float oscillationFrequency = 2f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.2f;

    private Inverse3 _inverse3;

    [Range(-1f, 1f)]
    public float zAxisOffset = -0.5f;

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

        float time = Time.time;
        float direction = Mathf.Sin(2 * Mathf.PI * oscillationFrequency * time);

        // float targetForceX = useTrebleForX ? analyzer.treble * trebleForceMultiplier * direction : 0f;
        // float targetForceY = useBassForY ? analyzer.bass * bassForceMultiplier * direction : 0f;


        float targetForceX = useTrebleForX ? analyzer.bass * trebleForceMultiplier * direction : 0f;
        float targetForceY = useBassForY ? analyzer.treble * bassForceMultiplier * direction : 0f;

        // Smooth interpolation
        currentForceX = Mathf.Lerp(currentForceX, targetForceX, smoothSpeed);
        currentForceY = Mathf.Lerp(currentForceY, targetForceY, smoothSpeed);

        // Clamp for safety
        float maxForce = 2f;
        currentForceX = Mathf.Clamp(currentForceX, -maxForce, maxForce);
        currentForceY = Mathf.Clamp(currentForceY, -maxForce, maxForce);

        // Now safe: called from Update (main thread)
        _inverse3.CursorSetForce(currentForceX, currentForceY, zAxisOffset);
    }
}
