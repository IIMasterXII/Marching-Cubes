using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

	public static Vector4 Density(Vector3 point, Vector3 center, Vector3 offset, float spacing, Vector3 worldBounds, float boundsSize, Vector3[] offsets, int octaves, float lacunarity, float persistence, float noiseScale, float noiseWeight, float floorOffset, float weightMultiplier, bool closeEdges, float hardFloorHeight, float hardFloorWeight, Vector4 shaderParams) {
		Vector3 pos = center + (point * spacing) - (new Vector3(boundsSize/2,boundsSize/2,boundsSize/2));

        float noise = 0;

        float frequency = noiseScale/100;
        float amplitude = 1;
        float weight = 1;
        for (int j =0; j < octaves; j ++) {
            float n = Perlin3D(pos * frequency + offsets[j] + offset);
            float v = 1-Mathf.Abs(n);
            v = v*v;
            v *= weight;
            weight = Mathf.Max(Mathf.Min(v*weightMultiplier,1),0);
            noise += v * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        
        float finalVal = -(pos.y + floorOffset) + noise * noiseWeight + (pos.y%shaderParams.x) * shaderParams.y;

        if (pos.y < hardFloorHeight) {
            finalVal += hardFloorWeight;
        }

        if (closeEdges) {
            Vector3 edgeOffset = (AbsVector(pos)*2) - worldBounds;
            edgeOffset.x += spacing/2;
            edgeOffset.y += spacing/2;
            edgeOffset.z += spacing/2;
            float edgeWeight = Mathf.Clamp01(Mathf.Sign(Mathf.Max(Mathf.Max(edgeOffset.x,edgeOffset.y),edgeOffset.z)));
            finalVal = finalVal * (1-edgeWeight) - 100 * edgeWeight;
            
        }

        return new Vector4(pos.x,pos.y,pos.z,finalVal);
	}

    public static Vector3 AbsVector(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x),Mathf.Abs(vector.y),Mathf.Abs(vector.z));
    }

    public static float Perlin3D(Vector3 point) {
        float ab = Mathf.PerlinNoise(point.x, point.y);
        float bc = Mathf.PerlinNoise(point.y, point.z);
        float ac = Mathf.PerlinNoise(point.x, point.z);

        float ba = Mathf.PerlinNoise(point.y, point.x);
        float cb = Mathf.PerlinNoise(point.z, point.y);
        float ca = Mathf.PerlinNoise(point.z, point.x);

        float abc = ab + bc + ac + ba + cb + ca;
        return abc / 6f;
    }

}