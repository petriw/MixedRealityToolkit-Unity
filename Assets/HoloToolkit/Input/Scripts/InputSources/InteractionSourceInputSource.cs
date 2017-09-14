// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
#if UNITY_5
    using UnityEngine.VR.WSA.Input;
#else
    using UnityEngine.XR.WSA.Input;
#endif

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Input source for gestures and interaction source information from the WSA APIs, which gives access to various system-supported gestures
    /// and positional information for the various inputs that Windows gestures supports.
    /// This is mostly a wrapper on top of GestureRecognizer and InteractionManager.
    /// </summary>
    public class InteractionSourceInputSource : BaseInputSource
    {
        // This enumeration gives the manager two different ways to handle the recognizer. Both will
        // set up the recognizer. The first causes the recognizer to start
        // immediately. The second allows the recognizer to be manually started at a later time.
        public enum RecognizerStartBehavior { AutoStart, ManualStart }

        [Tooltip("Whether the recognizer should be activated on start.")]
        public RecognizerStartBehavior RecognizerStart;

        [Tooltip("Set to true to use the use rails (guides) for the navigation gesture, as opposed to full 3D navigation.")]
        public bool UseRailsNavigation = false;

        protected GestureRecognizer gestureRecognizer;
        protected GestureRecognizer navigationGestureRecognizer;

        #region IInputSource Capabilities and SourceData

        private struct SourceCapability<TReading>
        {
            public bool IsSupported;
            public bool IsAvailable;
            public TReading CurrentReading;
        }

        private struct AxisButton2D
        {
            public bool Pressed;
            public Vector2 Position;

            public static AxisButton2D GetThumbstick(InteractionSourceState interactionSource)
            {
                return new AxisButton2D
                {
#if UNITY_5
                    Pressed = interactionSource.controllerProperties.thumbstickPressed,
                    Position = new Vector2((float)interactionSource.controllerProperties.thumbstickX, (float)interactionSource.controllerProperties.thumbstickY),
#else
                    Pressed = interactionSource.thumbstickPressed,
                    Position = interactionSource.thumbstickPosition,
#endif
                };
            }

            public static AxisButton2D GetTouchpad(InteractionSourceState interactionSource)
            {
                return new AxisButton2D
                {
#if UNITY_5
                    Pressed = interactionSource.controllerProperties.touchpadPressed,
                    Position = new Vector2((float)interactionSource.controllerProperties.touchpadX, (float)interactionSource.controllerProperties.touchpadY),
#else
                    Pressed = interactionSource.touchpadPressed,
                    Position = interactionSource.touchpadPosition,
#endif
                };
            }
        }

        private struct TouchpadData
        {
            public AxisButton2D AxisButton;
            public bool Touched;

            public static TouchpadData GetTouchpad(InteractionSourceState interactionSource)
            {
                return new TouchpadData
                {
                    AxisButton = AxisButton2D.GetTouchpad(interactionSource),
#if UNITY_5
                    Touched = interactionSource.controllerProperties.touchpadTouched,
#else
                    Touched = interactionSource.touchpadTouched,
#endif
                };
            }
        }

        private struct AxisButton1D
        {
            public bool Pressed;
            public double PressedAmount;

            public static AxisButton1D GetSelect(InteractionSourceState interactionSource)
            {
                return new AxisButton1D
                {
                    Pressed = interactionSource.selectPressed,
#if UNITY_5
                    PressedAmount = interactionSource.selectPressedValue
#else
                    PressedAmount = interactionSource.selectPressedAmount,
#endif
                };
            }
        }

        /// <summary>
        /// Data for an interaction source.
        /// </summary>
        private class SourceData
        {
            public SourceData(InteractionSource interactionSource)
            {
                Source = interactionSource;
            }

            public void ResetUpdatedBooleans()
            {
                ThumbstickPositionUpdated = false;
                TouchpadPositionUpdated = false;
                TouchpadTouchedUpdated = false;
                PositionUpdated = false;
                RotationUpdated = false;
                SelectPressedAmountUpdated = false;
            }

            public uint SourceId { get { return Source.id; } }
#if UNITY_5
            public InteractionSourceKind SourceKind { get { return Source.sourceKind; } }
#else
            public InteractionSourceKind SourceKind { get { return Source.kind; } }
#endif

            public readonly InteractionSource Source;
            public SourceCapability<Vector3> PointerPosition;
            public SourceCapability<Quaternion> PointerRotation;
            public SourceCapability<Ray> PointingRay;
            public SourceCapability<Vector3> GripPosition;
            public SourceCapability<Quaternion> GripRotation;
            public SourceCapability<AxisButton2D> Thumbstick;
            public SourceCapability<TouchpadData> Touchpad;
            public SourceCapability<AxisButton1D> Select;
            public SourceCapability<bool> Grasp;
            public SourceCapability<bool> Menu;

            public bool ThumbstickPositionUpdated;
            public bool TouchpadPositionUpdated;
            public bool TouchpadTouchedUpdated;
            public bool PositionUpdated;
            public bool RotationUpdated;
            public bool SelectPressedAmountUpdated;
        }

        /// <summary>
        /// Dictionary linking each source ID to its data.
        /// </summary>
        private readonly Dictionary<uint, SourceData> sourceIdToData = new Dictionary<uint, SourceData>(4);

#endregion

#region MonoBehaviour Functions

        private void Awake()
        {
            gestureRecognizer = new GestureRecognizer();
#if UNITY_5
            gestureRecognizer.TappedEvent += GestureRecognizer_Tapped;

            gestureRecognizer.HoldStartedEvent += GestureRecognizer_HoldStarted;
            gestureRecognizer.HoldCompletedEvent += GestureRecognizer_HoldCompleted;
            gestureRecognizer.HoldCanceledEvent += GestureRecognizer_HoldCanceled;

            gestureRecognizer.ManipulationStartedEvent += GestureRecognizer_ManipulationStarted;
            gestureRecognizer.ManipulationUpdatedEvent += GestureRecognizer_ManipulationUpdated;
            gestureRecognizer.ManipulationCompletedEvent += GestureRecognizer_ManipulationCompleted;
            gestureRecognizer.ManipulationCanceledEvent += GestureRecognizer_ManipulationCanceled;
#else
            gestureRecognizer.Tapped += GestureRecognizer_Tapped;

            gestureRecognizer.HoldStarted += GestureRecognizer_HoldStarted;
            gestureRecognizer.HoldCompleted += GestureRecognizer_HoldCompleted;
            gestureRecognizer.HoldCanceled += GestureRecognizer_HoldCanceled;

            gestureRecognizer.ManipulationStarted += GestureRecognizer_ManipulationStarted;
            gestureRecognizer.ManipulationUpdated += GestureRecognizer_ManipulationUpdated;
            gestureRecognizer.ManipulationCompleted += GestureRecognizer_ManipulationCompleted;
            gestureRecognizer.ManipulationCanceled += GestureRecognizer_ManipulationCanceled;
#endif

            gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap |
                                                      GestureSettings.ManipulationTranslate |
                                                      GestureSettings.Hold);

            // We need a separate gesture recognizer for navigation, since it isn't compatible with manipulation
            navigationGestureRecognizer = new GestureRecognizer();

#if UNITY_5
            navigationGestureRecognizer.NavigationStartedEvent += NavigationGestureRecognizer_NavigationStarted;
            navigationGestureRecognizer.NavigationUpdatedEvent += NavigationGestureRecognizer_NavigationUpdated;
            navigationGestureRecognizer.NavigationCompletedEvent += NavigationGestureRecognizer_NavigationCompleted;
            navigationGestureRecognizer.NavigationCanceledEvent += NavigationGestureRecognizer_NavigationCanceled;
#else
            navigationGestureRecognizer.NavigationStarted += NavigationGestureRecognizer_NavigationStarted;
            navigationGestureRecognizer.NavigationUpdated += NavigationGestureRecognizer_NavigationUpdated;
            navigationGestureRecognizer.NavigationCompleted += NavigationGestureRecognizer_NavigationCompleted;
            navigationGestureRecognizer.NavigationCanceled += NavigationGestureRecognizer_NavigationCanceled;
#endif

            if (UseRailsNavigation)
            {
                navigationGestureRecognizer.SetRecognizableGestures(GestureSettings.NavigationRailsX |
                                                                    GestureSettings.NavigationRailsY |
                                                                    GestureSettings.NavigationRailsZ);
            }
            else
            {
                navigationGestureRecognizer.SetRecognizableGestures(GestureSettings.NavigationX |
                                                                    GestureSettings.NavigationY |
                                                                    GestureSettings.NavigationZ);
            }
        }

        protected virtual void OnDestroy()
        {
#if UNITY_5
            InteractionManager.SourceUpdated -= InteractionManager_InteractionSourceUpdated;

            InteractionManager.SourceReleased -= InteractionManager_InteractionSourceReleased;
            InteractionManager.SourcePressed -= InteractionManager_InteractionSourcePressed;

            InteractionManager.SourceLost -= InteractionManager_InteractionSourceLost;
            InteractionManager.SourceDetected -= InteractionManager_InteractionSourceDetected;
#else
            InteractionManager.InteractionSourceUpdated -= InteractionManager_InteractionSourceUpdated;

            InteractionManager.InteractionSourceReleased -= InteractionManager_InteractionSourceReleased;
            InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;

            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
#endif

            if (gestureRecognizer != null)
            {
#if UNITY_5
                gestureRecognizer.TappedEvent -= GestureRecognizer_Tapped;

                gestureRecognizer.HoldStartedEvent -= GestureRecognizer_HoldStarted;
                gestureRecognizer.HoldCompletedEvent -= GestureRecognizer_HoldCompleted;
                gestureRecognizer.HoldCanceledEvent -= GestureRecognizer_HoldCanceled;

                gestureRecognizer.ManipulationStartedEvent -= GestureRecognizer_ManipulationStarted;
                gestureRecognizer.ManipulationUpdatedEvent -= GestureRecognizer_ManipulationUpdated;
                gestureRecognizer.ManipulationCompletedEvent -= GestureRecognizer_ManipulationCompleted;
                gestureRecognizer.ManipulationCanceledEvent -= GestureRecognizer_ManipulationCanceled;
#else
                gestureRecognizer.Tapped -= GestureRecognizer_Tapped;

                gestureRecognizer.HoldStarted -= GestureRecognizer_HoldStarted;
                gestureRecognizer.HoldCompleted -= GestureRecognizer_HoldCompleted;
                gestureRecognizer.HoldCanceled -= GestureRecognizer_HoldCanceled;

                gestureRecognizer.ManipulationStarted -= GestureRecognizer_ManipulationStarted;
                gestureRecognizer.ManipulationUpdated -= GestureRecognizer_ManipulationUpdated;
                gestureRecognizer.ManipulationCompleted -= GestureRecognizer_ManipulationCompleted;
                gestureRecognizer.ManipulationCanceled -= GestureRecognizer_ManipulationCanceled;
#endif

                gestureRecognizer.Dispose();
            }

            if (navigationGestureRecognizer != null)
            {
#if UNITY_5
                navigationGestureRecognizer.NavigationStartedEvent -= NavigationGestureRecognizer_NavigationStarted;
                navigationGestureRecognizer.NavigationUpdatedEvent -= NavigationGestureRecognizer_NavigationUpdated;
                navigationGestureRecognizer.NavigationCompletedEvent -= NavigationGestureRecognizer_NavigationCompleted;
                navigationGestureRecognizer.NavigationCanceledEvent -= NavigationGestureRecognizer_NavigationCanceled;
#else
                navigationGestureRecognizer.NavigationStarted -= NavigationGestureRecognizer_NavigationStarted;
                navigationGestureRecognizer.NavigationUpdated -= NavigationGestureRecognizer_NavigationUpdated;
                navigationGestureRecognizer.NavigationCompleted -= NavigationGestureRecognizer_NavigationCompleted;
                navigationGestureRecognizer.NavigationCanceled -= NavigationGestureRecognizer_NavigationCanceled;
#endif

                navigationGestureRecognizer.Dispose();
            }
        }

        protected override void OnEnableAfterStart()
        {
            base.OnEnableAfterStart();

            if (RecognizerStart == RecognizerStartBehavior.AutoStart)
            {
                StartGestureRecognizers();
            }

            foreach (InteractionSourceState iss in InteractionManager.GetCurrentReading())
            {
                GetOrAddSourceData(iss.source);
                InputManager.Instance.RaiseSourceDetected(this, iss.source.id);
            }

#if UNITY_5
            InteractionManager.SourceUpdated += InteractionManager_InteractionSourceUpdated;

            InteractionManager.SourceReleased += InteractionManager_InteractionSourceReleased;
            InteractionManager.SourcePressed += InteractionManager_InteractionSourcePressed;

            InteractionManager.SourceLost += InteractionManager_InteractionSourceLost;
            InteractionManager.SourceDetected += InteractionManager_InteractionSourceDetected;
#else
            InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;

            InteractionManager.InteractionSourceReleased += InteractionManager_InteractionSourceReleased;
            InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;

            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
#endif
        }

        protected override void OnDisableAfterStart()
        {
            StopGestureRecognizers();

#if UNITY_5
            InteractionManager.SourceUpdated -= InteractionManager_InteractionSourceUpdated;

            InteractionManager.SourceReleased -= InteractionManager_InteractionSourceReleased;
            InteractionManager.SourcePressed -= InteractionManager_InteractionSourcePressed;

            InteractionManager.SourceLost -= InteractionManager_InteractionSourceLost;
            InteractionManager.SourceDetected -= InteractionManager_InteractionSourceDetected;
#else
            InteractionManager.InteractionSourceUpdated -= InteractionManager_InteractionSourceUpdated;

            InteractionManager.InteractionSourceReleased -= InteractionManager_InteractionSourceReleased;
            InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;

            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
#endif

            foreach (InteractionSourceState iss in InteractionManager.GetCurrentReading())
            {
                // NOTE: We don't care whether the source ID previously existed or not, so we blindly call Remove:
                sourceIdToData.Remove(iss.source.id);
                InputManager.Instance.RaiseSourceLost(this, iss.source.id);
            }

            base.OnDisableAfterStart();
        }

#endregion MonoBehaviour Functions

        public void StartGestureRecognizers()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.StartCapturingGestures();
            }

            if (navigationGestureRecognizer != null)
            {
                navigationGestureRecognizer.StartCapturingGestures();
            }
        }

        public void StopGestureRecognizers()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.StopCapturingGestures();
            }

            if (navigationGestureRecognizer != null)
            {
                navigationGestureRecognizer.StopCapturingGestures();
            }
        }

#region BaseInputSource implementations

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            SupportedInputInfo retVal = SupportedInputInfo.None;

            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                retVal |= GetSupportFlag(sourceData.PointerPosition, SupportedInputInfo.Position);
                retVal |= GetSupportFlag(sourceData.PointerRotation, SupportedInputInfo.Rotation);
                retVal |= GetSupportFlag(sourceData.PointingRay, SupportedInputInfo.Pointing);
                retVal |= GetSupportFlag(sourceData.Thumbstick, SupportedInputInfo.Thumbstick);
                retVal |= GetSupportFlag(sourceData.Touchpad, SupportedInputInfo.Touchpad);
                retVal |= GetSupportFlag(sourceData.Select, SupportedInputInfo.Select);
                retVal |= GetSupportFlag(sourceData.Menu, SupportedInputInfo.Menu);
                retVal |= GetSupportFlag(sourceData.Grasp, SupportedInputInfo.Grasp);
            }

            return retVal;
        }

        public override bool TryGetSourceKind(uint sourceId, out InteractionSourceKind sourceKind)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                sourceKind = sourceData.SourceKind;
                return true;
            }
            else
            {
                sourceKind = default(InteractionSourceKind);
                return false;
            }
        }

        public override bool TryGetPointerPosition(uint sourceId, out Vector3 position)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.PointerPosition, out position))
            {
                return true;
            }
            else
            {
                position = default(Vector3);
                return false;
            }
        }

        public override bool TryGetPointerRotation(uint sourceId, out Quaternion rotation)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.PointerRotation, out rotation))
            {
                return true;
            }
            else
            {
                rotation = default(Quaternion);
                return false;
            }
        }

        public override bool TryGetPointingRay(uint sourceId, out Ray pointingRay)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.PointingRay, out pointingRay))
            {
                return true;
            }
            else
            {
                pointingRay = default(Ray);
                return false;
            }
        }

        public override bool TryGetGripPosition(uint sourceId, out Vector3 position)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.GripPosition, out position))
            {
                return true;
            }
            else
            {
                position = default(Vector3);
                return false;
            }
        }

        public override bool TryGetGripRotation(uint sourceId, out Quaternion rotation)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.GripRotation, out rotation))
            {
                return true;
            }
            else
            {
                rotation = default(Quaternion);
                return false;
            }
        }

        public override bool TryGetThumbstick(uint sourceId, out bool thumbstickPressed, out Vector2 thumbstickPosition)
        {
            SourceData sourceData;
            AxisButton2D thumbstick;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Thumbstick, out thumbstick))
            {
                thumbstickPressed = thumbstick.Pressed;
                thumbstickPosition = thumbstick.Position;
                return true;
            }
            else
            {
                thumbstickPressed = false;
                thumbstickPosition = Vector2.zero;
                return false;
            }
        }

        public override bool TryGetTouchpad(uint sourceId, out bool touchpadPressed, out bool touchpadTouched, out Vector2 touchpadPosition)
        {
            SourceData sourceData;
            TouchpadData touchpad;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Touchpad, out touchpad))
            {
                touchpadPressed = touchpad.AxisButton.Pressed;
                touchpadTouched = touchpad.Touched;
                touchpadPosition = touchpad.AxisButton.Position;
                return true;
            }
            else
            {
                touchpadPressed = false;
                touchpadTouched = false;
                touchpadPosition = Vector2.zero;
                return false;
            }
        }

        public override bool TryGetSelect(uint sourceId, out bool selectPressed, out double selectPressedAmount)
        {
            SourceData sourceData;
            AxisButton1D select;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Select, out select))
            {
                selectPressed = select.Pressed;
                selectPressedAmount = select.PressedAmount;
                return true;
            }
            else
            {
                selectPressed = false;
                selectPressedAmount = 0;
                return false;
            }
        }

        public override bool TryGetGrasp(uint sourceId, out bool graspPressed)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Grasp, out graspPressed))
            {
                return true;
            }
            else
            {
                graspPressed = false;
                return false;
            }
        }

        public override bool TryGetMenu(uint sourceId, out bool menuPressed)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Menu, out menuPressed))
            {
                return true;
            }
            else
            {
                menuPressed = false;
                return false;
            }
        }

        private bool TryGetReading<TReading>(SourceCapability<TReading> capability, out TReading reading)
        {
            if (capability.IsAvailable)
            {
                Debug.Assert(capability.IsSupported);

                reading = capability.CurrentReading;
                return true;
            }
            else
            {
                reading = default(TReading);
                return false;
            }
        }

        private SupportedInputInfo GetSupportFlag<TReading>(SourceCapability<TReading> capability, SupportedInputInfo flagIfSupported)
        {
            return (capability.IsSupported ? flagIfSupported : SupportedInputInfo.None);
        }

#endregion

        public void StartHaptics(uint sourceId, float intensity)
        {
#if UNITY_WSA
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                sourceData.Source.StartHaptics(intensity);
            }
#endif
        }

        public void StartHaptics(uint sourceId, float intensity, float durationInSeconds)
        {
#if UNITY_WSA
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                sourceData.Source.StartHaptics(intensity, durationInSeconds);
            }
#endif
        }

        public void StopHaptics(uint sourceId)
        {
#if UNITY_WSA
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                sourceData.Source.StopHaptics();
            }
#endif
        }

#region InteractionManager Events

#if UNITY_5
        private void InteractionManager_InteractionSourceUpdated(InteractionManager.SourceEventArgs args)
#else
        private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs args)
#endif
        {
            SourceData sourceData = GetOrAddSourceData(args.state.source);

            sourceData.ResetUpdatedBooleans();

            UpdateSourceData(args.state, sourceData);

            if (sourceData.PositionUpdated)
            {
                InputManager.Instance.RaiseSourcePositionChanged(this, sourceData.SourceId, sourceData.PointerPosition.CurrentReading, sourceData.GripPosition.CurrentReading);
            }

            if (sourceData.RotationUpdated)
            {
                InputManager.Instance.RaiseSourceRotationChanged(this, sourceData.SourceId, sourceData.PointerRotation.CurrentReading, sourceData.GripRotation.CurrentReading);
            }

            if (sourceData.ThumbstickPositionUpdated)
            {
                InputManager.Instance.RaiseInputPositionChanged(this, sourceData.SourceId,
#if UNITY_5
                    InteractionPressKind.Thumbstick,
#else
                    InteractionSourcePressType.Thumbstick, 
#endif
                    sourceData.Thumbstick.CurrentReading.Position);
            }

            if (sourceData.TouchpadPositionUpdated)
            {
                InputManager.Instance.RaiseInputPositionChanged(this, sourceData.SourceId,
#if UNITY_5
                    InteractionPressKind.Touchpad,
#else
                    InteractionSourcePressType.Touchpad, 
#endif
                    sourceData.Touchpad.CurrentReading.AxisButton.Position);
            }

            if (sourceData.TouchpadTouchedUpdated)
            {
                if (sourceData.Touchpad.CurrentReading.Touched)
                {
                    InputManager.Instance.RaiseTouchpadTouched(this, sourceData.SourceId);
                }
                else
                {
                    InputManager.Instance.RaiseTouchpadReleased(this, sourceData.SourceId);
                }
            }

            if (sourceData.SelectPressedAmountUpdated)
            {
                InputManager.Instance.RaiseSelectPressedAmountChanged(this, sourceData.SourceId, sourceData.Select.CurrentReading.PressedAmount);
            }
        }

#if UNITY_5
        private void InteractionManager_InteractionSourceReleased(InteractionManager.SourceEventArgs args)
#else
        private void InteractionManager_InteractionSourceReleased(InteractionSourceReleasedEventArgs args)
#endif
        {
            InputManager.Instance.RaiseSourceUp(this, args.state.source.id, 
#if UNITY_5
            args.pressKind);
#else
            args.pressType);
#endif
        }

#if UNITY_5
        private void InteractionManager_InteractionSourcePressed(InteractionManager.SourceEventArgs args)
#else
        private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs args)
#endif
        {
            InputManager.Instance.RaiseSourceDown(this, args.state.source.id,
#if UNITY_5
            args.pressKind);
#else
            args.pressType);
#endif
        }

#if UNITY_5
        private void InteractionManager_InteractionSourceLost(InteractionManager.SourceEventArgs args)
#else
        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs args)
#endif
        {
            // NOTE: We don't care whether the source ID previously existed or not, so we blindly call Remove:
            sourceIdToData.Remove(args.state.source.id);

            InputManager.Instance.RaiseSourceLost(this, args.state.source.id);
        }

#if UNITY_5
        private void InteractionManager_InteractionSourceDetected(InteractionManager.SourceEventArgs args)
#else
        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs args)
#endif
        {
            // NOTE: We update the source state data, in case an app wants to query it on source detected.
            UpdateSourceData(args.state, GetOrAddSourceData(args.state.source));

            InputManager.Instance.RaiseSourceDetected(this, args.state.source.id);
        }

#endregion InteractionManager Events

        /// <summary>
        /// Gets the source data for the specified interaction source if it already exists, otherwise creates it.
        /// </summary>
        /// <param name="interactionSource">Interaction source for which data should be retrieved.</param>
        /// <returns>The source data requested.</returns>
        private SourceData GetOrAddSourceData(InteractionSource interactionSource)
        {
            SourceData sourceData;
            if (!sourceIdToData.TryGetValue(interactionSource.id, out sourceData))
            {
                sourceData = new SourceData(interactionSource);
                sourceIdToData.Add(sourceData.SourceId, sourceData);

                // TODO: robertes: whenever we end up adding, should we first synthesize a SourceDetected? Or
                //       perhaps if we keep strict track of all sources, we should never need to just-in-time add anymore.
            }

            return sourceData;
        }

        /// <summary>
        /// Updates the source information.
        /// </summary>
        /// <param name="interactionSourceState">Interaction source to use to update the source information.</param>
        /// <param name="sourceData">SourceData structure to update.</param>
        private void UpdateSourceData(InteractionSourceState interactionSourceState, SourceData sourceData)
        {
            Debug.Assert(interactionSourceState.source.id == sourceData.SourceId, "An UpdateSourceState call happened with mismatched source ID.");
#if UNITY_5
            Debug.Assert(interactionSourceState.source.sourceKind == sourceData.SourceKind, "An UpdateSourceState call happened with mismatched source kind.");
#else
            Debug.Assert(interactionSourceState.source.kind == sourceData.SourceKind, "An UpdateSourceState call happened with mismatched source kind.");
#endif

            Vector3 newPointerPosition;
#if UNITY_5
            sourceData.PointerPosition.IsAvailable = interactionSourceState.sourcePose.TryGetPointerPosition(out newPointerPosition);
#else
            sourceData.PointerPosition.IsAvailable = interactionSourceState.sourcePose.TryGetPosition(out newPointerPosition, InteractionSourceNode.Pointer);
#endif
            // Using a heuristic for IsSupported, since the APIs don't yet support querying this capability directly.
            sourceData.PointerPosition.IsSupported |= sourceData.PointerPosition.IsAvailable;

            Vector3 newGripPosition;
#if UNITY_5
            sourceData.PointerPosition.IsAvailable = interactionSourceState.sourcePose.TryGetPosition(out newGripPosition);
#else
            sourceData.GripPosition.IsAvailable = interactionSourceState.sourcePose.TryGetPosition(out newGripPosition, InteractionSourceNode.Grip);
#endif
            // Using a heuristic for IsSupported, since the APIs don't yet support querying this capability directly.
            sourceData.GripPosition.IsSupported |= sourceData.GripPosition.IsAvailable;

            if (sourceData.PointerPosition.IsAvailable || sourceData.GripPosition.IsAvailable)
            {
                sourceData.PositionUpdated = !(sourceData.PointerPosition.CurrentReading.Equals(newPointerPosition) && sourceData.GripPosition.CurrentReading.Equals(newGripPosition));
            }
            sourceData.PointerPosition.CurrentReading = newPointerPosition;
            sourceData.GripPosition.CurrentReading = newGripPosition;

            Quaternion newPointerRotation;
#if UNITY_5
            sourceData.PointerRotation.IsAvailable = interactionSourceState.sourcePose.TryGetPointerRotation(out newPointerRotation);
#else
            sourceData.PointerRotation.IsAvailable = interactionSourceState.sourcePose.TryGetRotation(out newPointerRotation, InteractionSourceNode.Pointer);
#endif
            // Using a heuristic for IsSupported, since the APIs don't yet support querying this capability directly.
            sourceData.PointerRotation.IsSupported |= sourceData.PointerRotation.IsAvailable;

            Quaternion newGripRotation;
#if UNITY_5
            sourceData.GripRotation.IsAvailable = interactionSourceState.sourcePose.TryGetRotation(out newGripRotation);
#else
            sourceData.GripRotation.IsAvailable = interactionSourceState.sourcePose.TryGetRotation(out newGripRotation, InteractionSourceNode.Grip);
#endif
            // Using a heuristic for IsSupported, since the APIs don't yet support querying this capability directly.
            sourceData.GripRotation.IsSupported |= sourceData.GripRotation.IsAvailable;
            if (sourceData.PointerRotation.IsAvailable || sourceData.GripRotation.IsAvailable)
            {
                sourceData.RotationUpdated = !(sourceData.PointerRotation.CurrentReading.Equals(newPointerRotation) && sourceData.GripRotation.CurrentReading.Equals(newGripRotation));
            }
            sourceData.PointerRotation.CurrentReading = newPointerRotation;
            sourceData.GripRotation.CurrentReading = newGripRotation;

            Vector3 pointerForward = Vector3.zero;
            sourceData.PointingRay.IsSupported = interactionSourceState.source.supportsPointing;
#if UNITY_5
            Ray rayForward = default(Ray);
            sourceData.PointingRay.IsAvailable = sourceData.PointerPosition.IsAvailable && interactionSourceState.sourcePose.TryGetPointerRay(out rayForward);
            if(sourceData.PointingRay.IsAvailable)
            {
                pointerForward = rayForward.direction;
            }
#else
            sourceData.PointingRay.IsAvailable = sourceData.PointerPosition.IsAvailable && interactionSourceState.sourcePose.TryGetForward(out pointerForward, InteractionSourceNode.Pointer);
#endif
            sourceData.PointingRay.CurrentReading = new Ray(sourceData.PointerPosition.CurrentReading, pointerForward);

#if UNITY_5
            InteractionController ctrl;
            if (interactionSourceState.source.TryGetController(out ctrl))
                sourceData.Thumbstick.IsSupported = ctrl.hasThumbstick;
            else
                sourceData.Thumbstick.IsSupported = false;
#else
            sourceData.Thumbstick.IsSupported = interactionSourceState.source.supportsThumbstick;
#endif
            sourceData.Thumbstick.IsAvailable = sourceData.Thumbstick.IsSupported;
            if (sourceData.Thumbstick.IsAvailable)
            {
                AxisButton2D newThumbstick = AxisButton2D.GetThumbstick(interactionSourceState);
                sourceData.ThumbstickPositionUpdated = sourceData.Thumbstick.CurrentReading.Position != newThumbstick.Position;
                sourceData.Thumbstick.CurrentReading = newThumbstick;
            }
            else
            {
                sourceData.Thumbstick.CurrentReading = default(AxisButton2D);
            }

#if UNITY_5
            if (interactionSourceState.source.TryGetController(out ctrl))
                sourceData.Touchpad.IsSupported = ctrl.hasTouchpad;
            else
                sourceData.Touchpad.IsSupported = false;
#else
            sourceData.Touchpad.IsSupported = interactionSourceState.source.supportsTouchpad;
#endif
            sourceData.Touchpad.IsAvailable = sourceData.Touchpad.IsSupported;
            if (sourceData.Touchpad.IsAvailable)
            {
                TouchpadData newTouchpad = TouchpadData.GetTouchpad(interactionSourceState);
                sourceData.TouchpadPositionUpdated = !sourceData.Touchpad.CurrentReading.AxisButton.Position.Equals(newTouchpad.AxisButton.Position);
                sourceData.TouchpadTouchedUpdated = !sourceData.Touchpad.CurrentReading.Touched.Equals(newTouchpad.Touched);
                sourceData.Touchpad.CurrentReading = newTouchpad;
            }
            else
            {
                sourceData.Touchpad.CurrentReading = default(TouchpadData);
            }

            sourceData.Select.IsSupported = true; // All input mechanisms support "select".
            sourceData.Select.IsAvailable = sourceData.Select.IsSupported;
            AxisButton1D newSelect = AxisButton1D.GetSelect(interactionSourceState);
            sourceData.SelectPressedAmountUpdated = !sourceData.Select.CurrentReading.PressedAmount.Equals(newSelect.PressedAmount);
            sourceData.Select.CurrentReading = newSelect;

            sourceData.Grasp.IsSupported = interactionSourceState.source.supportsGrasp;
            sourceData.Grasp.IsAvailable = sourceData.Grasp.IsSupported;
            sourceData.Grasp.CurrentReading = (sourceData.Grasp.IsAvailable && interactionSourceState.grasped);

            sourceData.Menu.IsSupported = interactionSourceState.source.supportsMenu;
            sourceData.Menu.IsAvailable = sourceData.Menu.IsSupported;
            sourceData.Menu.CurrentReading = (sourceData.Menu.IsAvailable && interactionSourceState.menuPressed);
        }

#region Raise GestureRecognizer Events

        // TODO: robertes: Should these also cause source state data to be stored/updated? What about SourceDetected synthesized events?

        protected void GestureRecognizer_Tapped(TappedEventArgs obj)
        {
            InputManager.Instance.RaiseInputClicked(this,
#if UNITY_5
                (uint)obj.sourceId, InteractionPressKind.Select, 
                obj.tapCount);
#else
                obj.source.id, InteractionSourcePressType.Select, 
                obj.tapCount);
#endif
        }

        protected void GestureRecognizer_HoldStarted(HoldStartedEventArgs obj)
        {
            InputManager.Instance.RaiseHoldStarted(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        protected void GestureRecognizer_HoldCanceled(HoldCanceledEventArgs obj)
        {
            InputManager.Instance.RaiseHoldCanceled(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        protected void GestureRecognizer_HoldCompleted(HoldCompletedEventArgs obj)
        {
            InputManager.Instance.RaiseHoldCompleted(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        protected void GestureRecognizer_ManipulationStarted(ManipulationStartedEventArgs obj)
        {
            InputManager.Instance.RaiseManipulationStarted(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        protected void GestureRecognizer_ManipulationUpdated(ManipulationUpdatedEventArgs obj)
        {
            InputManager.Instance.RaiseManipulationUpdated(this,
#if UNITY_5
                (uint)obj.sourceId,
#else
                obj.source.id,
#endif
            obj.cumulativeDelta);
        }

        protected void GestureRecognizer_ManipulationCompleted(ManipulationCompletedEventArgs obj)
        {
            InputManager.Instance.RaiseManipulationCompleted(this,
#if UNITY_5
                (uint)obj.sourceId,
#else
                obj.source.id,
#endif
                obj.cumulativeDelta);
        }

        protected void GestureRecognizer_ManipulationCanceled(ManipulationCanceledEventArgs obj)
        {
            InputManager.Instance.RaiseManipulationCanceled(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        protected void NavigationGestureRecognizer_NavigationStarted(NavigationStartedEventArgs obj)
        {
            InputManager.Instance.RaiseNavigationStarted(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        protected void NavigationGestureRecognizer_NavigationUpdated(NavigationUpdatedEventArgs obj)
        {
            InputManager.Instance.RaiseNavigationUpdated(this,
#if UNITY_5
                (uint)obj.sourceId,
#else
                obj.source.id,
#endif
                obj.normalizedOffset);
        }

        protected void NavigationGestureRecognizer_NavigationCompleted(NavigationCompletedEventArgs obj)
        {
            InputManager.Instance.RaiseNavigationCompleted(this,
#if UNITY_5
                (uint)obj.sourceId,
#else
                obj.source.id,
#endif
                obj.normalizedOffset);
        }

        protected void NavigationGestureRecognizer_NavigationCanceled(NavigationCanceledEventArgs obj)
        {
            InputManager.Instance.RaiseNavigationCanceled(this,
#if UNITY_5
                (uint)obj.sourceId);
#else
                obj.source.id);
#endif
        }

        #endregion
    }
}