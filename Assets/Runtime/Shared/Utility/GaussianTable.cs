using System;
using UnityEngine;

namespace Game.Shared.Utility
{

/// <summary>Provides a deterministic sequence of sampled Gaussian multipliers.</summary>
public static class GaussianTable
{
    private static readonly float[] SAMPLES;
    private static int _index;
    private const int SIZE = 1024;

    static GaussianTable()
    {
        SAMPLES = new float[SIZE];
        for (int i = 0; i < SIZE; i++)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            float g = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            SAMPLES[i] = Mathf.Clamp(1f + g * 0.05f, 0.9f, 1.1f);
        }
    }

    /// <summary>Gets the next sampled multiplier.</summary>
    public static float Next()
    {
        float val = SAMPLES[_index];
        _index = (_index + 1) % SIZE;
        return val;
    }
}
}
