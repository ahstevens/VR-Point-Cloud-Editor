using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "HapticPattern", menuName = "Haptic Pattern")]
public class HapticPattern : ScriptableObject
{
    [Tooltip("Amplitude of impulse from 0-1")]
    [Range(0f,1f)]
    public float amplitude;

    [Tooltip("Frequency of impulse in Hz")]
    [Range(0f, 3300f)]
    public float frequency;

    [Tooltip("Duration of impulse in seconds")]
    [Min(0.005f)]
    public float duration; 
}
