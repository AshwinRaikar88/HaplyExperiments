using Haply.Inverse.Unity;
using UnityEngine;

public class AudioPositionControl : MonoBehaviour
{
    public Inverse3 inverse3;
    public AudioAnalyzer analyzer;

    [Header("Mapping Multipliers")]
    [Range(0f, 0.2f)] public float bassToY = 0.05f;
    [Range(0f, 0.2f)] public float trebleToX = 0.05f;

    [Header("Oscillation Settings")]
    public float oscillationFrequency = 1f; // Hz
    public bool enableOscillation = true;

    private Vector3 workspaceCenter;

    private void Awake()
    {
        if (inverse3 == null)
            inverse3 = FindObjectOfType<Inverse3>();

        inverse3.Ready.AddListener(device =>
        {
            workspaceCenter = device.WorkspaceCenterLocalPosition;
        });
    }

    private void FixedUpdate()
    {
        if (inverse3 == null)
            return;

        float time = Time.time;
        float osc = enableOscillation ? Mathf.Sin(2 * Mathf.PI * oscillationFrequency * time) : 1f;

        float offsetX = analyzer.treble * trebleToX * osc;
        float offsetY = analyzer.bass * bassToY * osc;
        float offsetZ = 0f; // You can add midrange mapping here

        // Compute new cursor position
        Vector3 newPosition = workspaceCenter + new Vector3(offsetX, offsetY, offsetZ);

        inverse3.CursorSetPosition(newPosition);
    }
}
