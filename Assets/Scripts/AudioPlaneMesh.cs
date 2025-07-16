using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class AudioPlaneMesh : MonoBehaviour
{
    public int width = 64;
    public int height = 64;
    public float scale = 10f;

    public AudioSource audioSource;
    public FFTWindow fftWindow = FFTWindow.Blackman;
    private float[] spectrum;

    private Mesh mesh;
    private MeshCollider meshCollider;
    private Vector3[] vertices;

    void Start()
    {
        spectrum = new float[512];

        mesh = new Mesh();
        mesh.name = "Audio Mesh";

        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        GenerateMesh();
    }

    void GenerateMesh()
    {
        vertices = new Vector3[(width + 1) * (height + 1)];
        int[] triangles = new int[width * height * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int z = 0, i = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                vertices[i] = new Vector3(x, 0, z);
                uvs[i] = new Vector2((float)x / width, (float)z / height);
            }
        }

        for (int z = 0, t = 0, v = 0; z < height; z++, v++)
        {
            for (int x = 0; x < width; x++, v++, t += 6)
            {
                triangles[t + 0] = v;
                triangles[t + 1] = v + width + 1;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + width + 1;
                triangles[t + 5] = v + width + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Set initial collider
        meshCollider.sharedMesh = mesh;
    }

    void Update()
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        audioSource.GetSpectrumData(spectrum, 0, fftWindow);

        for (int z = 0, i = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                int band = (x * spectrum.Length / width) % spectrum.Length;
                float y = spectrum[band] * scale;
                vertices[i].y = y;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Update collider (reassign sharedMesh)
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}
