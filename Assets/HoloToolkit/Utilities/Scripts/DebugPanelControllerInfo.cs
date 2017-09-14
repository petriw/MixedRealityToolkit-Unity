﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5
using UnityEngine.VR.WSA.Input;
#else
using UnityEngine.XR.WSA.Input;
#endif


namespace HoloToolkit.Unity
{
    public class DebugPanelControllerInfo : MonoBehaviour
    {
        private class ControllerState
        {
            public InteractionSourceHandedness Handedness;
            public Vector3 PointerPosition;
            public Quaternion PointerRotation;
            public Vector3 GripPosition;
            public Quaternion GripRotation;
            public bool Grasped;
            public bool MenuPressed;
            public bool SelectPressed;
            public float SelectPressedAmount;
            public bool ThumbstickPressed;
            public Vector2 ThumbstickPosition;
            public bool TouchpadPressed;
            public bool TouchpadTouched;
            public Vector2 TouchpadPosition;
        }

        private Dictionary<uint, ControllerState> controllers;

        private void Awake()
        {
            controllers = new Dictionary<uint, ControllerState>();

            #if UNITY_WSA
#if UNITY_5
            InteractionManager.SourceDetected += InteractionManager_InteractionSourceDetected;

            InteractionManager.SourceLost += InteractionManager_InteractionSourceLost;
            InteractionManager.SourceUpdated += InteractionManager_InteractionSourceUpdated;
#else
            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;

            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
#endif
            #endif
        }

        private void Start()
        {
            if (DebugPanel.Instance != null)
            {
                DebugPanel.Instance.RegisterExternalLogCallback(GetControllerInfo);
            }
        }

#if UNITY_5
        private void InteractionManager_InteractionSourceDetected(InteractionManager.SourceEventArgs obj)
#else
        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj)
#endif
        {
            Debug.LogFormat("{0} {1} Detected", obj.state.source.handedness,
#if UNITY_5
                obj.state.source.sourceKind);
#else
                obj.state.source.kind);
#endif

            if (
#if UNITY_5
                obj.state.source.sourceKind == InteractionSourceKind.Controller &&
#else
                obj.state.source.kind == InteractionSourceKind.Controller && 
#endif
                !controllers.ContainsKey(obj.state.source.id))
            {
                controllers.Add(obj.state.source.id, new ControllerState { Handedness = obj.state.source.handedness });
            }
        }

#if UNITY_5
        private void InteractionManager_InteractionSourceLost(InteractionManager.SourceEventArgs obj)
#else
        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs obj)
#endif
        {
            Debug.LogFormat("{0} {1} Lost", obj.state.source.handedness,
#if UNITY_5
                obj.state.source.sourceKind);
#else
                obj.state.source.kind);
#endif

            controllers.Remove(obj.state.source.id);
        }

#if UNITY_5
        private void InteractionManager_InteractionSourceUpdated(InteractionManager.SourceEventArgs obj)
#else
        private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
#endif
        {
            ControllerState controllerState;
            if (controllers.TryGetValue(obj.state.source.id, out controllerState))
            {
#if UNITY_5
                obj.state.sourcePose.TryGetPointerPosition(out controllerState.PointerPosition);
                obj.state.sourcePose.TryGetPointerRotation(out controllerState.PointerRotation);
                obj.state.sourcePose.TryGetPosition(out controllerState.GripPosition);
                obj.state.sourcePose.TryGetRotation(out controllerState.GripRotation);

#else
                obj.state.sourcePose.TryGetPosition(out controllerState.PointerPosition, InteractionSourceNode.Pointer);
                obj.state.sourcePose.TryGetRotation(out controllerState.PointerRotation, InteractionSourceNode.Pointer);
                obj.state.sourcePose.TryGetPosition(out controllerState.GripPosition, InteractionSourceNode.Grip);
                obj.state.sourcePose.TryGetRotation(out controllerState.GripRotation, InteractionSourceNode.Grip);
#endif

                controllerState.Grasped = obj.state.grasped;
                controllerState.MenuPressed = obj.state.menuPressed;
                controllerState.SelectPressed = obj.state.selectPressed;
#if UNITY_5
                controllerState.SelectPressedAmount = (float)obj.state.selectPressedValue;
                controllerState.ThumbstickPressed = obj.state.controllerProperties.thumbstickPressed;
                controllerState.ThumbstickPosition = new Vector2((float)obj.state.controllerProperties.thumbstickX, (float)obj.state.controllerProperties.thumbstickY);
                controllerState.TouchpadPressed = obj.state.controllerProperties.touchpadPressed;
                controllerState.TouchpadTouched = obj.state.controllerProperties.touchpadTouched;
                controllerState.TouchpadPosition = new Vector2((float)obj.state.controllerProperties.touchpadX, (float)obj.state.controllerProperties.touchpadY);
#else
                controllerState.SelectPressedAmount = obj.state.selectPressedAmount;
                controllerState.ThumbstickPressed = obj.state.thumbstickPressed;
                controllerState.ThumbstickPosition = obj.state.thumbstickPosition;
                controllerState.TouchpadPressed = obj.state.touchpadPressed;
                controllerState.TouchpadTouched = obj.state.touchpadTouched;
                controllerState.TouchpadPosition = obj.state.touchpadPosition;
#endif
            }
        }

        private string GetControllerInfo()
        {
            string toReturn = "";
            foreach (ControllerState controllerState in controllers.Values)
            {
                toReturn += string.Format("Hand: {0}\nPointer: Position: {1} Rotation: {2}\n" +
                                          "Grip: Position: {3} Rotation: {4}\nGrasped: {5} " +
                                          "MenuPressed: {6}\nSelect: Pressed: {7} PressedAmount: {8}\n" +
                                          "Thumbstick: Pressed: {9} Position: {10}\nTouchpad: Pressed: {11} " +
                                          "Touched: {12} Position: {13}\n\n",
                                          controllerState.Handedness, controllerState.PointerPosition, controllerState.PointerRotation.eulerAngles,
                                          controllerState.GripPosition, controllerState.GripRotation.eulerAngles, controllerState.Grasped,
                                          controllerState.MenuPressed, controllerState.SelectPressed, controllerState.SelectPressedAmount,
                                          controllerState.ThumbstickPressed, controllerState.ThumbstickPosition, controllerState.TouchpadPressed,
                                          controllerState.TouchpadTouched, controllerState.TouchpadPosition);
            }
            return toReturn.Substring(0, Math.Max(0, toReturn.Length - 2));
        }
    }
}