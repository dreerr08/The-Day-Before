using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeBlinker : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public int blendshapeIndex;

    [Header("Piscar")]
    public float blinkInterval = 5.0f;
    public float blinkEyesCloseDuration = 1.0f; // Make sure this matches usage
    public float blinkOpeningSeconds = 0.03f;
    public float blinkClosingSeconds = 0.1f;

    public Coroutine blinkCoroutine; // Fixed typo "Courotine" to "Coroutine" for consistency

    private void Awake()
    {
        // Fix 1: Corrected capitalization to match the method defined below
        blendshapeIndex = GetBlendShapeIndex("Fcl_EYE_Close");
    }

    // Fix 2: Changed "GetBlendshapeIndex" to "GetBlendShapeIndex" (Capital S)
    private int GetBlendShapeIndex(string blendshapeName)
    {
        Mesh mesh = skinnedMesh.sharedMesh;
        // Fix 3: The Unity method is named GetBlendShapeIndex (Capital S)
        int index = mesh.GetBlendShapeIndex(blendshapeName);
        return index;
    }

    private IEnumerator BlinkRoutine() // Fixed typo "Blick" to "Blink"
    {
        while (true)
        {
            // Fix 4: WaitForSeconds (plural), not WaitForSecond
            yield return new WaitForSeconds(blinkInterval);

            // Closing Eye
            var value = 0f;
            var closeSpeed = 1.0f / blinkClosingSeconds;

            while (value < 1)
            {
                skinnedMesh.SetBlendShapeWeight(blendshapeIndex, value * 100);
                value += Time.deltaTime * closeSpeed;
                yield return null;
            }
            skinnedMesh.SetBlendShapeWeight(blendshapeIndex, 100);

            // Fix 5: WaitForSeconds (plural) and fixed variable name typo (blinkEyesCloseDuration)
            yield return new WaitForSeconds(blinkEyesCloseDuration);

            // Opening Eye
            // Fix 6: Removed 'var' to re-use the 'value' variable instead of redeclaring it
            value = 1f;

            // Fix 7: Calculated openSpeed (was missing) instead of redeclaring closeSpeed
            var openSpeed = 1.0f / blinkOpeningSeconds;

            while (value > 0)
            {
                skinnedMesh.SetBlendShapeWeight(blendshapeIndex, value * 100);
                // Fix 8: Used the new openSpeed variable
                value -= Time.deltaTime * openSpeed;
                yield return null;
            }
            skinnedMesh.SetBlendShapeWeight(blendshapeIndex, 0);
        }
    }

    private void OnEnable()
    {
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }
}