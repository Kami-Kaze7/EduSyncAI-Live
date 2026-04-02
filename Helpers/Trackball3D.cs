using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace EduSyncAI.Helpers
{
    /// <summary>
    /// Projects mouse drag to arcball rotation on a 3D model.
    /// Usage: call OnMouseDown, OnMouseMove (returns rotation delta), OnMouseUp.
    /// </summary>
    public class Trackball3D
    {
        private Point _lastPoint;
        private bool _isDragging;
        private readonly double _sensitivity;

        public bool IsDragging => _isDragging;

        public Trackball3D(double sensitivity = 1.0)
        {
            _sensitivity = sensitivity;
        }

        /// <summary>Record starting position.</summary>
        public void OnMouseDown(Point screenPoint)
        {
            _lastPoint = screenPoint;
            _isDragging = true;
        }

        /// <summary>
        /// Calculate incremental rotation from last position to current position.
        /// Returns a <see cref="QuaternionRotation3D"/> to apply, or null if no meaningful movement.
        /// </summary>
        public QuaternionRotation3D? OnMouseMove(Point screenPoint, Size viewportSize)
        {
            if (!_isDragging) return null;

            double dx = screenPoint.X - _lastPoint.X;
            double dy = screenPoint.Y - _lastPoint.Y;

            if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1)
                return null;

            // Normalise to [-1,1] range based on viewport
            double ndx = dx / viewportSize.Width * 2.0 * _sensitivity;
            double ndy = dy / viewportSize.Height * 2.0 * _sensitivity;

            // Rotation axis is perpendicular to mouse drag direction
            // Horizontal drag → rotate around Y axis
            // Vertical drag   → rotate around X axis
            var axis = new Vector3D(-ndy, ndx, 0);
            double angle = axis.Length * 180.0; // scale to degrees
            if (angle < 0.001) return null;

            axis.Normalize();
            _lastPoint = screenPoint;

            return new QuaternionRotation3D(
                new Quaternion(axis, angle));
        }

        /// <summary>End drag.</summary>
        public void OnMouseUp()
        {
            _isDragging = false;
        }

        /// <summary>
        /// Compose two quaternion rotations: existing * delta.
        /// </summary>
        public static Quaternion Compose(Quaternion existing, Quaternion delta)
        {
            return delta * existing;
        }
    }
}
