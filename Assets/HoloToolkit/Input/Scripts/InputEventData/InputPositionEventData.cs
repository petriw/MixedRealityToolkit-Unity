// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_5
using UnityEngine.VR.WSA.Input;
#else
using UnityEngine.XR.WSA.Input;
#endif


namespace HoloToolkit.Unity.InputModule
{
    public class InputPositionEventData : InputEventData
    {
        /// <summary>
        /// Two values, from -1.0 to 1.0 in the X-axis and Y-axis, representing where the input control is positioned.
        /// </summary>
        public Vector2 Position;

        public InputPositionEventData(EventSystem eventSystem) : base(eventSystem)
        {
        }

        public void Initialize(IInputSource inputSource, uint sourceId,
#if UNITY_5
            InteractionPressKind pressType,
#else
            InteractionSourcePressType pressType, 
#endif
            Vector2 position, object tag = null)
        {
            Initialize(inputSource, sourceId, tag, pressType);
            Position = position;
        }
    }
}