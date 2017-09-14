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
    /// Describes an input event that has a source id and a press kind. 
    /// </summary>
    public class InputEventData : BaseInputEventData
    {
        /// <summary>
        /// Button type that initiated the event.
        /// </summary>
        /// 
#if UNITY_5
        public InteractionPressKind PressType { get; private set; }
#else
        public InteractionSourcePressType PressType { get; private set; }
#endif

        public InputEventData(EventSystem eventSystem) : base(eventSystem)
        {
        }

        public void Initialize(IInputSource inputSource, uint sourceId, object tag,
#if UNITY_5
            InteractionPressKind pressType)
#else
            InteractionSourcePressType pressType)
#endif
        {
            BaseInitialize(inputSource, sourceId, tag);
            PressType = pressType;
        }
    }
}