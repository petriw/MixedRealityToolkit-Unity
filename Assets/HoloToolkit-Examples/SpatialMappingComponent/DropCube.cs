// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
#if UNITY_5
    using UnityEngine.VR.WSA.Input;
#else
    using UnityEngine.XR.WSA.Input;
#endif

namespace HoloToolkit.Examples.SpatialMappingComponent
{
    /// <summary>
    /// Simple test script for dropping cubes with physics to observe interactions
    /// </summary>
    public class DropCube : MonoBehaviour
    {
        private GestureRecognizer recognizer;

        private void Start()
        {
            recognizer = new GestureRecognizer();
            recognizer.SetRecognizableGestures(GestureSettings.Tap);
#if UNITY_5
            recognizer.TappedEvent += Recognizer_Tapped;
#else
            recognizer.Tapped += Recognizer_Tapped;
#endif
            recognizer.StartCapturingGestures();
        }

        private void OnDestroy()
        {
#if UNITY_5
            recognizer.TappedEvent -= Recognizer_Tapped;
#else
            recognizer.Tapped -= Recognizer_Tapped;
#endif
        }

        private void Recognizer_Tapped(TappedEventArgs obj)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube); // Create a cube
            cube.transform.localScale = Vector3.one * 0.3f; // Make the cube smaller
            cube.transform.position = Camera.main.transform.position + Camera.main.transform.forward; // Start to drop it in front of the camera
            cube.AddComponent<Rigidbody>(); // Apply physics
        }
    }
}