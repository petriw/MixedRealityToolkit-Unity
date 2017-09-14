// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity.InputModule;
using UnityEngine;
#if UNITY_5
using UnityEngine.VR.WSA.Input;
#else
using UnityEngine.XR.WSA.Input;
#endif


namespace HoloToolkit.Unity.Tests
{
    [RequireComponent(typeof(SetGlobalListener))]
    public class HapticsTest : MonoBehaviour, IInputHandler
    {
        void IInputHandler.OnInputDown(InputEventData eventData)
        {
            InteractionSourceInputSource inputSource = eventData.InputSource as InteractionSourceInputSource;
            if (inputSource != null)
            {
                switch (eventData.PressType)
                {
#if UNITY_5
                    case InteractionPressKind.Grasp:
#else
                    case InteractionSourcePressType.Grasp:
#endif
                        inputSource.StartHaptics(eventData.SourceId, 1.0f);
                        return;
#if UNITY_5
                    case InteractionPressKind.Menu:
#else
                    case InteractionSourcePressType.Menu:
#endif
                        inputSource.StartHaptics(eventData.SourceId, 1.0f, 1.0f);
                        return;
                }
            }
        }

        void IInputHandler.OnInputUp(InputEventData eventData)
        {
            InteractionSourceInputSource inputSource = eventData.InputSource as InteractionSourceInputSource;
            if (inputSource != null)
            {
#if UNITY_5
                if (eventData.PressType == InteractionPressKind.Grasp)
#else
                if (eventData.PressType == InteractionSourcePressType.Grasp)
#endif
                {
                    inputSource.StopHaptics(eventData.SourceId);
                }
            }
        }
    }
}
