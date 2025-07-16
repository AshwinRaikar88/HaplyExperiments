using UnityEngine;

public class AudioAnalyzer : MonoBehaviour
{
    public AudioSource audioSource;
    public float[] spectrum = new float[512];

    [Header("Processed Output")]
    public float bass;
    public float treble;

    [Header("Dynamic Normalization")]
    [Range(0.01f, 1f)] public float normalizationSpeed = 0.1f;
    public float targetRangeMin = -2f;
    public float targetRangeMax = 2f;

    private float maxBassSeen = 0.01f;
    private float maxTrebleSeen = 0.01f;

    void Update()
    {
        if (!audioSource.isPlaying) return;

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);

        float rawBass = 0f;
        float rawTreble = 0f;

        // Low frequencies (bass): bins 0–20
        for (int i = 0; i < 20; i++)
            rawBass += spectrum[i];

        // High frequencies (treble): bins 100–512
        for (int i = 100; i < spectrum.Length; i++)
            rawTreble += spectrum[i];

        // Update max values over time (with decay)
        maxBassSeen = Mathf.Lerp(maxBassSeen, rawBass, normalizationSpeed);
        maxTrebleSeen = Mathf.Lerp(maxTrebleSeen, rawTreble, normalizationSpeed);

        // Avoid divide-by-zero
        maxBassSeen = Mathf.Max(maxBassSeen, 0.0001f);
        maxTrebleSeen = Mathf.Max(maxTrebleSeen, 0.0001f);

        // Normalize to [-1, 1]
        float normalizedBass = Mathf.Clamp(rawBass / maxBassSeen, 0f, 1f);
        float normalizedTreble = Mathf.Clamp(rawTreble / maxTrebleSeen, 0f, 1f);

        // Map to [-2, 2]
        bass = Mathf.Lerp(targetRangeMin, targetRangeMax, normalizedBass);
        treble = Mathf.Lerp(targetRangeMin, targetRangeMax, normalizedTreble);
    }
}
