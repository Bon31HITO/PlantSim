using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PlantSim;

public class OrbitCameraController
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(OrbitCameraController),
            new PropertyMetadata(false, OnIsEnabledPropertyChanged));

    public static readonly DependencyProperty ResetTriggerProperty =
        DependencyProperty.RegisterAttached(
            "ResetTrigger",
            typeof(object),
            typeof(OrbitCameraController),
            new PropertyMetadata(null, OnResetTriggerPropertyChanged));

    private static readonly DependencyProperty OrbitStateProperty =
        DependencyProperty.RegisterAttached(
            "OrbitState",
            typeof(OrbitState),
            typeof(OrbitCameraController),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject dependencyObject)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        return (bool)dependencyObject.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject dependencyObject, bool value)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        dependencyObject.SetValue(IsEnabledProperty, value);
    }

    public static object? GetResetTrigger(DependencyObject dependencyObject)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        return dependencyObject.GetValue(ResetTriggerProperty);
    }

    public static void SetResetTrigger(DependencyObject dependencyObject, object? value)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        dependencyObject.SetValue(ResetTriggerProperty, value);
    }

    public static void ResetCamera(DependencyObject dependencyObject)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        if (dependencyObject.GetValue(OrbitStateProperty) is OrbitState orbitState)
        {
            orbitState.ResetView();
        }
    }

    private static void OnIsEnabledPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArguments)
    {
        if (dependencyObject is FrameworkElement frameworkElement)
        {
            bool isEnabled = (bool)eventArguments.NewValue;
            if (isEnabled)
            {
                if (frameworkElement.IsLoaded)
                {
                    AttachOrbitState(frameworkElement);
                }
                else
                {
                    RoutedEventHandler? loadedEventHandler = null;
                    loadedEventHandler = (object senderObject, RoutedEventArgs routedEventArgs) =>
                    {
                        frameworkElement.Loaded -= loadedEventHandler;
                        AttachOrbitState(frameworkElement);
                    };
                    frameworkElement.Loaded += loadedEventHandler;
                }
            }
            else
            {
                DetachOrbitState(frameworkElement);
            }
        }
    }

    private static void OnResetTriggerPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArguments)
    {
        if (dependencyObject.GetValue(OrbitStateProperty) is OrbitState orbitState)
        {
            orbitState.ResetView();
        }
    }

    private static void AttachOrbitState(FrameworkElement targetElement)
    {
        DetachOrbitState(targetElement);
        Viewport3D? targetViewport = targetElement as Viewport3D ?? FindVisualChildViewport(targetElement);
        if (targetViewport == null)
        {
            return;
        }

        FrameworkElement inputElement = targetElement is Viewport3D
            ? FindInteractiveParent(targetElement)
            : targetElement;

        OrbitState orbitState = new OrbitState(inputElement, targetViewport);
        targetElement.SetValue(OrbitStateProperty, orbitState);
    }

    private static void DetachOrbitState(FrameworkElement targetElement)
    {
        if (targetElement.GetValue(OrbitStateProperty) is OrbitState existingState)
        {
            existingState.Detach();
            targetElement.ClearValue(OrbitStateProperty);
        }
    }

    private static Viewport3D? FindVisualChildViewport(DependencyObject parentObject)
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(parentObject);
        for (int childIndex = 0; childIndex < childrenCount; childIndex++)
        {
            DependencyObject childObject = VisualTreeHelper.GetChild(parentObject, childIndex);
            if (childObject is Viewport3D viewport)
            {
                return viewport;
            }

            Viewport3D? nestedViewport = FindVisualChildViewport(childObject);
            if (nestedViewport != null)
            {
                return nestedViewport;
            }
        }

        return null;
    }

    private static FrameworkElement FindInteractiveParent(FrameworkElement element)
    {
        DependencyObject? currentObject = VisualTreeHelper.GetParent(element);
        while (currentObject != null)
        {
            if (currentObject is FrameworkElement parentFrameworkElement)
            {
                return parentFrameworkElement;
            }

            currentObject = VisualTreeHelper.GetParent(currentObject);
        }

        return element;
    }

    private class OrbitState
    {
        private const double DefaultAzimuthAngleInDegrees = 45.0;
        private const double DefaultElevationAngleInDegrees = 20.0;
        private const double DefaultCameraDistance = 120.0;
        private static readonly Point3D DefaultTargetCenterPosition = new Point3D(0.0, 30.0, 0.0);

        private readonly FrameworkElement _inputElement;
        private readonly Viewport3D _viewport;
        private readonly PerspectiveCamera _camera;
        private Point _previousMousePosition;
        private double _azimuthAngle = DefaultAzimuthAngleInDegrees;
        private double _elevationAngle = DefaultElevationAngleInDegrees;
        private double _cameraDistance = DefaultCameraDistance;
        private Point3D _targetCenter = DefaultTargetCenterPosition;
        private bool _isTransformDirty = true;
        private bool _isDragging;

        public OrbitState(FrameworkElement inputElement, Viewport3D viewport)
        {
            _inputElement = inputElement;
            _viewport = viewport;

            if (_viewport.Camera is PerspectiveCamera existingCamera && !existingCamera.IsFrozen)
            {
                _camera = existingCamera;
            }
            else
            {
                _camera = new PerspectiveCamera
                {
                    FieldOfView = 45.0,
                    UpDirection = new Vector3D(0.0, 1.0, 0.0)
                };
                _viewport.Camera = _camera;
            }

            _inputElement.MouseDown += HandleMouseDown;
            _inputElement.MouseUp += HandleMouseUp;
            _inputElement.MouseMove += HandleMouseMove;
            _inputElement.MouseWheel += HandleMouseWheel;
            CompositionTarget.Rendering += HandleCompositionTargetRendering;

            ResetView();
        }

        public void ResetView()
        {
            _azimuthAngle = DefaultAzimuthAngleInDegrees;
            _elevationAngle = DefaultElevationAngleInDegrees;
            _cameraDistance = DefaultCameraDistance;
            _targetCenter = DefaultTargetCenterPosition;
            _isTransformDirty = true;
            UpdateCameraTransformDirectly();
        }

        public void Detach()
        {
            if (_isDragging)
            {
                _inputElement.ReleaseMouseCapture();
                _isDragging = false;
            }

            _inputElement.MouseDown -= HandleMouseDown;
            _inputElement.MouseUp -= HandleMouseUp;
            _inputElement.MouseMove -= HandleMouseMove;
            _inputElement.MouseWheel -= HandleMouseWheel;
            CompositionTarget.Rendering -= HandleCompositionTargetRendering;
        }

        private void HandleMouseDown(object senderObject, MouseButtonEventArgs eventArguments)
        {
            if (eventArguments.MiddleButton == MouseButtonState.Pressed ||
                (eventArguments.ClickCount == 2 && eventArguments.LeftButton == MouseButtonState.Pressed))
            {
                ResetView();
                eventArguments.Handled = true;
                return;
            }

            if (eventArguments.LeftButton == MouseButtonState.Pressed ||
                eventArguments.RightButton == MouseButtonState.Pressed)
            {
                _previousMousePosition = eventArguments.GetPosition(_inputElement);
                _isDragging = true;
                _inputElement.CaptureMouse();
                eventArguments.Handled = true;
            }
        }

        private void HandleMouseUp(object senderObject, MouseButtonEventArgs eventArguments)
        {
            if (_isDragging &&
                eventArguments.LeftButton == MouseButtonState.Released &&
                eventArguments.RightButton == MouseButtonState.Released)
            {
                _isDragging = false;
                _inputElement.ReleaseMouseCapture();
                eventArguments.Handled = true;
            }
        }

        private void HandleMouseMove(object senderObject, MouseEventArgs eventArguments)
        {
            if (!_isDragging)
            {
                return;
            }

            Point currentPosition = eventArguments.GetPosition(_inputElement);
            Vector positionDelta = currentPosition - _previousMousePosition;
            _previousMousePosition = currentPosition;

            if (eventArguments.LeftButton == MouseButtonState.Pressed)
            {
                _azimuthAngle -= positionDelta.X * 0.4;
                _elevationAngle = Math.Clamp(_elevationAngle + (positionDelta.Y * 0.4), -85.0, 85.0);
                _isTransformDirty = true;
                eventArguments.Handled = true;
            }
            else if (eventArguments.RightButton == MouseButtonState.Pressed)
            {
                double panningFactor = _cameraDistance * 0.002;
                (Vector3D cameraRightDirection, Vector3D cameraUpDirection) = CalculateCameraViewAxes();

                Vector3D panningDeltaVector =
                    (cameraRightDirection * (-positionDelta.X * panningFactor)) +
                    (cameraUpDirection * (positionDelta.Y * panningFactor));

                _targetCenter += panningDeltaVector;
                _isTransformDirty = true;
                eventArguments.Handled = true;
            }
        }

        private void HandleMouseWheel(object senderObject, MouseWheelEventArgs eventArguments)
        {
            double zoomingFactor = eventArguments.Delta > 0 ? 0.9 : 1.1;
            _cameraDistance = Math.Clamp(_cameraDistance * zoomingFactor, 2.0, 2000.0);
            _isTransformDirty = true;
            eventArguments.Handled = true;
        }

        private void HandleCompositionTargetRendering(object? senderObject, EventArgs eventArguments)
        {
            if (!_isTransformDirty)
            {
                return;
            }

            UpdateCameraTransformDirectly();
            _isTransformDirty = false;
        }

        private (Vector3D RightDirection, Vector3D UpDirection) CalculateCameraViewAxes()
        {
            double azimuthRadians = _azimuthAngle * (Math.PI / 180.0);
            double elevationRadians = _elevationAngle * (Math.PI / 180.0);
            double cosineElevation = Math.Cos(elevationRadians);
            double sineElevation = Math.Sin(elevationRadians);

            Vector3D forwardDirection = new Vector3D(
                -Math.Sin(azimuthRadians) * cosineElevation,
                -sineElevation,
                -Math.Cos(azimuthRadians) * cosineElevation);

            Vector3D worldUpDirection = new Vector3D(0.0, 1.0, 0.0);
            Vector3D rightDirection = Vector3D.CrossProduct(forwardDirection, worldUpDirection);

            if (rightDirection.LengthSquared < 1e-6)
            {
                rightDirection = new Vector3D(Math.Cos(azimuthRadians), 0.0, -Math.Sin(azimuthRadians));
            }
            else
            {
                rightDirection.Normalize();
            }

            Vector3D upDirection = Vector3D.CrossProduct(rightDirection, forwardDirection);
            upDirection.Normalize();

            return (rightDirection, upDirection);
        }

        private void UpdateCameraTransformDirectly()
        {
            double azimuthRadians = _azimuthAngle * (Math.PI / 180.0);
            double elevationRadians = _elevationAngle * (Math.PI / 180.0);
            double horizontalDistance = _cameraDistance * Math.Cos(elevationRadians);

            double cameraX = _targetCenter.X + (horizontalDistance * Math.Sin(azimuthRadians));
            double cameraY = _targetCenter.Y + (_cameraDistance * Math.Sin(elevationRadians));
            double cameraZ = _targetCenter.Z + (horizontalDistance * Math.Cos(azimuthRadians));

            Point3D cameraPosition = new Point3D(cameraX, cameraY, cameraZ);
            Vector3D lookDirection = _targetCenter - cameraPosition;

            _camera.Position = cameraPosition;
            _camera.LookDirection = lookDirection;
        }
    }
}