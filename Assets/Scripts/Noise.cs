using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise {

    private const float STRETCH_3D = -1.0f / 6.0f;  
    private const float SQUISH_3D = 1.0f / 3.0f;
    private const float NORM_3D = 1.0f / 103.0f;
    private byte[] perm = new byte[256];
    private byte[] perm3D = new byte[256];

    private static Contribution3[] lookup3D;

    private static float[] gradients3D =
    {
        -11,  4,  4,     -4,  11,  4,    -4,  4,  11,
            11,  4,  4,      4,  11,  4,     4,  4,  11,
        -11, -4,  4,     -4, -11,  4,    -4, -4,  11,
            11, -4,  4,      4, -11,  4,     4, -4,  11,
        -11,  4, -4,     -4,  11, -4,    -4,  4, -11,
            11,  4, -4,      4,  11, -4,     4,  4, -11,
        -11, -4, -4,     -4, -11, -4,    -4, -4, -11,
            11, -4, -4,      4, -11, -4,     4, -4, -11,
    };

    static Noise(){       
        var base3D = new int[][]
        {
            new int[] { 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1 },
            new int[] { 2, 1, 1, 0, 2, 1, 0, 1, 2, 0, 1, 1, 3, 1, 1, 1 },
            new int[] { 1, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1, 2, 1, 1, 0, 2, 1, 0, 1, 2, 0, 1, 1 }
        };
        var p3D = new int[] { 0, 0, 1, -1, 0, 0, 1, 0, -1, 0, 0, -1, 1, 0, 0, 0, 1, -1, 0, 0, -1, 0, 1, 0, 0, -1, 1, 0, 2, 1, 1, 0, 1, 1, 1, -1, 0, 2, 1, 0, 1, 1, 1, -1, 1, 0, 2, 0, 1, 1, 1, -1, 1, 1, 1, 3, 2, 1, 0, 3, 1, 2, 0, 1, 3, 2, 0, 1, 3, 1, 0, 2, 1, 3, 0, 2, 1, 3, 0, 1, 2, 1, 1, 1, 0, 0, 2, 2, 0, 0, 1, 1, 0, 1, 0, 2, 0, 2, 0, 1, 1, 0, 0, 1, 2, 0, 0, 2, 2, 0, 0, 0, 0, 1, 1, -1, 1, 2, 0, 0, 0, 0, 1, -1, 1, 1, 2, 0, 0, 0, 0, 1, 1, 1, -1, 2, 3, 1, 1, 1, 2, 0, 0, 2, 2, 3, 1, 1, 1, 2, 2, 0, 0, 2, 3, 1, 1, 1, 2, 0, 2, 0, 2, 1, 1, -1, 1, 2, 0, 0, 2, 2, 1, 1, -1, 1, 2, 2, 0, 0, 2, 1, -1, 1, 1, 2, 0, 0, 2, 2, 1, -1, 1, 1, 2, 0, 2, 0, 2, 1, 1, 1, -1, 2, 2, 0, 0, 2, 1, 1, 1, -1, 2, 0, 2, 0 };
        var lookupPairs3D = new int[] { 0, 2, 1, 1, 2, 2, 5, 1, 6, 0, 7, 0, 32, 2, 34, 2, 129, 1, 133, 1, 160, 5, 161, 5, 518, 0, 519, 0, 546, 4, 550, 4, 645, 3, 647, 3, 672, 5, 673, 5, 674, 4, 677, 3, 678, 4, 679, 3, 680, 13, 681, 13, 682, 12, 685, 14, 686, 12, 687, 14, 712, 20, 714, 18, 809, 21, 813, 23, 840, 20, 841, 21, 1198, 19, 1199, 22, 1226, 18, 1230, 19, 1325, 23, 1327, 22, 1352, 15, 1353, 17, 1354, 15, 1357, 17, 1358, 16, 1359, 16, 1360, 11, 1361, 10, 1362, 11, 1365, 10, 1366, 9, 1367, 9, 1392, 11, 1394, 11, 1489, 10, 1493, 10, 1520, 8, 1521, 8, 1878, 9, 1879, 9, 1906, 7, 1910, 7, 2005, 6, 2007, 6, 2032, 8, 2033, 8, 2034, 7, 2037, 6, 2038, 7, 2039, 6 };

        var contributions3D = new Contribution3[p3D.Length / 9];
        for (int i = 0; i < p3D.Length; i += 9)
        {
            var baseSet = base3D[p3D[i]];
            Contribution3 previous = null, current = null;
            for (int k = 0; k < baseSet.Length; k += 4)
            {
                current = new Contribution3(baseSet[k], baseSet[k + 1], baseSet[k + 2], baseSet[k + 3]);
                if (previous == null)
                {
                    contributions3D[i / 9] = current;
                }
                else
                {
                    previous.Next = current;
                }
                previous = current;
            }
            current.Next = new Contribution3(p3D[i + 1], p3D[i + 2], p3D[i + 3], p3D[i + 4]);
            current.Next.Next = new Contribution3(p3D[i + 5], p3D[i + 6], p3D[i + 7], p3D[i + 8]);
        }
        
        lookup3D = new Contribution3[2048];
        for (var i = 0; i < lookupPairs3D.Length; i += 2)
        {
            lookup3D[lookupPairs3D[i]] = contributions3D[lookupPairs3D[i + 1]];
        }
    }

	public Vector4 Density(Vector3 point, Vector3 center, Vector3 offset, float spacing, Vector3 worldBounds, float boundsSize, Vector3[] offsets, int octaves, float lacunarity, float persistence, float noiseScale, float noiseWeight, float floorOffset, float weightMultiplier, bool closeEdges, float hardFloorHeight, float hardFloorWeight, Vector4 shaderParams) {
		Vector3 pos = center + (point * spacing) - (new Vector3(boundsSize/2,boundsSize/2,boundsSize/2));

        float noise = 0;

        float frequency = noiseScale/100;
        float amplitude = 1;
        float weight = 1;
        for (int j =0; j < octaves; j ++) {
            Vector3 ePos = pos * frequency + offsets[j] + offset;
            float n = Evaluate(ePos.x,ePos.y,ePos.z);
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

    public Vector3 AbsVector(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x),Mathf.Abs(vector.y),Mathf.Abs(vector.z));
    }

    // float step(float a, float x)
    // {
    //     if(x >= a)
    //         return 1;
    //     return 0;
    // }

    // Vector4 permute(Vector4 x)
    // {
    //     return new Vector4(((x.x*34.0f + 1.0f)*x.x)%289,((x.y*34.0f + 1.0f)*x.y)%289,((x.z*34.0f + 1.0f)*x.z)%289,((x.w*34.0f + 1.0f)*x.w)%289);
    // }

    // float taylorInvSqrt(float r)
    // {
    //     return 1.79284291400159f - r * 0.85373472095314f;
    // }

    // public float snoise(Vector3 v)
    // {
    //     Vector2 C = new Vector2(1.0f / 6.0f, 1.0f / 3.0f);

    //     // First corner
    //     float dotVY = Vector3.Dot(v, new Vector3(C.y,C.y,C.y));
    //     Vector3 i = new Vector3(Mathf.Floor(v.x + dotVY),Mathf.Floor(v.y + dotVY),Mathf.Floor(v.z + dotVY));
    //     float dotIX = Vector3.Dot(i, new Vector3(C.x,C.x,C.x));
    //     Vector3 x0 = v + new Vector3(-i.x + dotIX,-i.y + dotIX,-i.z + dotIX);

    //     // Other corners
    //     Vector3 g = new Vector3(step(x0.y,x0.x),step(x0.z,x0.y),step(x0.x,x0.z));
    //     Vector3 l = new Vector3(1.0f - g.x,1.0f - g.y,1.0f - g.z);
    //     Vector3 i1 = new Vector3(Mathf.Min(g.x,l.z),Mathf.Min(g.y,l.x),Mathf.Min(g.z,l.y));
    //     Vector3 i2 = new Vector3(Mathf.Max(g.x,l.z),Mathf.Max(g.y,l.x),Mathf.Max(g.z,l.y));

    //     // x1 = x0 - i1  + 1.0 * C.xxx;
    //     // x2 = x0 - i2  + 2.0 * C.xxx;
    //     // x3 = x0 - 1.0 + 3.0 * C.xxx;
    //     Vector3 x1 = x0 - i1 + new Vector3(C.x,C.x,C.x);
    //     Vector3 x2 = x0 - i2 + new Vector3(C.y,C.y,C.y);
    //     Vector3 x3 = x0 - new Vector3(0.5f,0.5f,0.5f);

    //     // Permutations
    //     i = new Vector3(i.x%289,i.y%289,i.z%289); // Avoid truncation effects in permutation
    //     Vector4 p =
    //     permute(permute(permute(new Vector4(0.0f, i1.z, i2.z + i.z, 1.0f))
    //                             + new Vector4(0.0f, i1.y + i.y, i2.y, 1.0f))
    //                             + new Vector4(0.0f + i.x, i1.x, i2.x, 1.0f));

    //     // Gradients: 7x7 points over a square, mapped onto an octahedron.
    //     // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    //     Vector4 j = new Vector4(p.x%49,p.y%49,p.z%49,p.w%49);  // mod(p,7*7)

    //     Vector4 x_ = new Vector4(Mathf.Floor(j.x / 7.0f),Mathf.Floor(j.y / 7.0f),Mathf.Floor(j.z / 7.0f),Mathf.Floor(j.w / 7.0f));
    //     Vector4 y_ = new Vector4(Mathf.Floor(j.x - 7.0f * x_.x),Mathf.Floor(j.y - 7.0f * x_.y),Mathf.Floor(j.z - 7.0f * x_.z),Mathf.Floor(j.w - 7.0f * x_.w));  // mod(j,N)

    //     Vector4 x = new Vector4((x_.x * 2.0f + 0.5f) / 7.0f - 1.0f,(x_.y * 2.0f + 0.5f) / 7.0f - 1.0f,(x_.z * 2.0f + 0.5f) / 7.0f - 1.0f,(x_.w * 2.0f + 0.5f) / 7.0f - 1.0f);
    //     Vector4 y = new Vector4((y_.x * 2.0f + 0.5f) / 7.0f - 1.0f,(y_.y * 2.0f + 0.5f) / 7.0f - 1.0f,(y_.z * 2.0f + 0.5f) / 7.0f - 1.0f,(y_.w * 2.0f + 0.5f) / 7.0f - 1.0f);

    //     Vector4 h = new Vector4(1.0f - Mathf.Abs(x.x) - Mathf.Abs(y.x), 1.0f - Mathf.Abs(x.y) - Mathf.Abs(y.y),1.0f - Mathf.Abs(x.z) - Mathf.Abs(y.z),1.0f - Mathf.Abs(x.w) - Mathf.Abs(y.w));

    //     Vector4 b0 = new Vector4(x.x,x.y,y.x,y.y);
    //     Vector4 b1 = new Vector4(x.z,x.w,y.z,y.w);

    //     //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
    //     //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
    //     Vector4 s0 = new Vector4(Mathf.Floor(b0.x) * 2.0f + 1.0f,Mathf.Floor(b0.y) * 2.0f + 1.0f,Mathf.Floor(b0.z) * 2.0f + 1.0f,Mathf.Floor(b0.w) * 2.0f + 1.0f);
    //     Vector4 s1 = new Vector4(Mathf.Floor(b1.x) * 2.0f + 1.0f,Mathf.Floor(b1.y) * 2.0f + 1.0f,Mathf.Floor(b1.z) * 2.0f + 1.0f,Mathf.Floor(b1.w) * 2.0f + 1.0f);
    //     Vector4 sh = new Vector4(-step(h.x, 0.0f),-step(h.y, 0.0f),-step(h.z, 0.0f),-step(h.w, 0.0f));

    //     Vector4 a0 = new Vector4(b0.x + s0.x * sh.x,b0.z + s0.z * sh.x,b0.y + s0.y * sh.y,b0.w + s0.w * sh.y);
    //     Vector4 a1 = new Vector4(b1.x + s1.x * sh.z,b1.z + s1.z * sh.z,b1.y + s1.y * sh.w,b1.w + s1.w * sh.w);

    //     Vector3 g0 = new Vector3(a0.x, a0.y, h.x);
    //     Vector3 g1 = new Vector3(a0.z, a0.w, h.y);
    //     Vector3 g2 = new Vector3(a1.x, a1.y, h.z);
    //     Vector3 g3 = new Vector3(a1.z, a1.w, h.w);

    //     // Normalise gradients
    //     Vector4 norm = new Vector4(taylorInvSqrt(Vector3.Dot(g0, g0)), taylorInvSqrt(Vector3.Dot(g1, g1)), taylorInvSqrt(Vector3.Dot(g2, g2)), taylorInvSqrt(Vector3.Dot(g3, g3)));
    //     g0 *= norm.x;
    //     g1 *= norm.y;
    //     g2 *= norm.z;
    //     g3 *= norm.w;

    //     // Mix final noise value
    //     Vector4 m = new Vector4(Mathf.Max(0.6f - Vector3.Dot(x0, x0), 0.0f), Mathf.Max(0.6f - Vector3.Dot(x1, x1), 0.0f), Mathf.Max(0.6f - Vector3.Dot(x2, x2), 0.0f), Mathf.Max(0.6f - Vector3.Dot(x3, x3), 0.0f));
    //     m.x = m.x*m.x*m.x;
    //     m.y = m.y*m.y*m.y;
    //     m.z = m.z*m.z*m.z;
    //     m.w = m.w*m.w*m.w;

    //     Vector4 px = new Vector4(Vector3.Dot(x0, g0), Vector3.Dot(x1, g1), Vector3.Dot(x2, g2), Vector3.Dot(x3, g3));
    //     return 42.0f * Vector3.Dot(m, px);
    // }


    int FastFloor(float x)
    {
        var xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }

    public float Evaluate(float x, float y, float z)
    {
        float stretchOffset = (x + y + z) * STRETCH_3D;
        float xs = x + stretchOffset;
        float ys = y + stretchOffset;
        float zs = z + stretchOffset;

        var xsb = FastFloor(xs);
        var ysb = FastFloor(ys);
        var zsb = FastFloor(zs);

        var squishOffset = (xsb + ysb + zsb) * SQUISH_3D;
        var dx0 = x - (xsb + squishOffset);
        var dy0 = y - (ysb + squishOffset);
        var dz0 = z - (zsb + squishOffset);

        var xins = xs - xsb;
        var yins = ys - ysb;
        var zins = zs - zsb;

        var inSum = xins + yins + zins;

        var hash =
            (int)(yins - zins + 1) |
            (int)(xins - yins + 1) << 1 |
            (int)(xins - zins + 1) << 2 |
            (int)inSum << 3 |
            (int)(inSum + zins) << 5 |
            (int)(inSum + yins) << 7 |
            (int)(inSum + xins) << 9;

        var c = lookup3D[hash];

        float value = 0.0f;
        while (c != null)
        {
            var dx = dx0 + c.dx;
            var dy = dy0 + c.dy;
            var dz = dz0 + c.dz;
            var attn = 2 - dx * dx - dy * dy - dz * dz;
            if (attn > 0)
            {
                var px = xsb + c.xsb;
                var py = ysb + c.ysb;
                var pz = zsb + c.zsb;

                var i = perm3D[(perm[(perm[px & 0xFF] + py) & 0xFF] + pz) & 0xFF];
                var valuePart = gradients3D[i] * dx + gradients3D[i + 1] * dy + gradients3D[i + 2] * dz;

                attn *= attn;
                value += attn * attn * valuePart;
            }

            c = c.Next;
        }
        return value * NORM_3D;
    }

    private class Contribution3
    {
        public float dx, dy, dz;
        public int xsb, ysb, zsb;
        public Contribution3 Next;

        public Contribution3(float multiplier, int xsb, int ysb, int zsb)
        {
            dx = -xsb - multiplier * SQUISH_3D;
            dy = -ysb - multiplier * SQUISH_3D;
            dz = -zsb - multiplier * SQUISH_3D;
            this.xsb = xsb;
            this.ysb = ysb;
            this.zsb = zsb;
        }
    }

}
