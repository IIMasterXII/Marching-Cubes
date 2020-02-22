using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityGenerator : MonoBehaviour
{
    [Header ("Noise")]
    public int seed;
    public int numOctaves = 4;
    public float lacunarity = 2;
    public float persistence = .5f;
    public float noiseScale = 1;
    public float noiseWeight = 1;
    public bool closeEdges;
    public float floorOffset = 1;
    public float weightMultiplier = 1;

    public float hardFloorHeight;
    public float hardFloorWeight;

    public Vector4 shaderParams;

    Noise noise = new Noise();

    void OnValidate() {
        if (FindObjectOfType<MeshGenerator>()) {
            FindObjectOfType<MeshGenerator>().RequestMeshUpdate();
        }
    }

    int indexFromCoord(int x, int y, int z, int pointsPerAxis){
        return z * pointsPerAxis * pointsPerAxis + y * pointsPerAxis + x;
    }

    public Vector4[] Generate(int pointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 center, Vector3 offset, float spacing){

        // Noise parameters
        var prng = new System.Random (seed);
        var offsets = new Vector3[numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector3 ((float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1) * offsetRange;
        }

        Vector4[] points = new Vector4[pointsPerAxis*pointsPerAxis*pointsPerAxis];
        for(int z = 0; z < pointsPerAxis; z++)
        {
            for(int y = 0; y < pointsPerAxis; y++)
            {
                for(int x = 0; x < pointsPerAxis; x++)
                {
                    int index = indexFromCoord(x,y,z, pointsPerAxis);
                    points[index] = noise.Density(new Vector3(x,y,z), center, offset, spacing, worldBounds, boundsSize, offsets, numOctaves, lacunarity, persistence, noiseScale, noiseWeight, floorOffset, weightMultiplier, closeEdges, hardFloorHeight, hardFloorWeight, shaderParams);
                }
            }
        }
        return points;
    }
}
