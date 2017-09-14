// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine.EventSystems;
#if UNITY_5
using UnityEngine.VR.WSA.Input;
#else
using UnityEngine.XR.WSA.Input;
#endif


namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Describes an input event that involves a tap.
    /// </summary>
    public class InputClickedEventData : InputEventData
    {
        /// <summary>
        /// Number of taps that triggered the event.
        /// </summary>
        public int TapCount { get; private set; }

        public InputClickedEventData(EventSystem eventSystem) : base(eventSystem)
        {
        }

        public void Initialize(IInputSource inputSource, uint sourceId, object tag,
#if UNITY_5
            InteractionPressKind pressType,
#else
            InteractionSourcePressType pressType, 
#endif
            int tapCount)
        {
            Initialize(inputSource, sourceId, tag, pressType);
            TapCount = tapCount;
        }
    }
}