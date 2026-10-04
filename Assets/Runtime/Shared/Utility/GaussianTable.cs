using System;
using UnityEngine;

public static class GaussianTable
{
    private static readonly float[] _samples;
    private static int _index;
    private const int Size = 1024;

    static GaussianTable()
    {
        _samples = new float[Size];
        for (int i = 0; i < Size; i++)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            float g = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            _samples[i] = Mathf.Clamp(1f + g * 0.05f, 0.9f, 1.1f);
        }
    }

    public static float Next()
    {
        float val = _samples[_index];
        _index = (_index + 1) % Size;
        return val;
    }
}