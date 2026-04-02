using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace EduSyncAI.Helpers
{
    /// <summary>
    /// A utility that bisects a 3D mesh along a cutting plane,
    /// producing two independent MeshGeometry3D halves ("Fruit-Ninja" style).
    /// </summary>
    public static class MeshSlicer
    {
        /// <summary>
        /// Represents the result of a slice operation.
        /// </summary>
        public class SliceResult
        {
            public MeshGeometry3D? SideA { get; set; }
            public MeshGeometry3D? SideB { get; set; }
        }

        /// <summary>
        /// Bakes a Transform3D into a MeshGeometry3D, converting its vertices and normals 
        /// to absolute world space before slicing operations.
        /// </summary>
        public static MeshGeometry3D TransformMesh(MeshGeometry3D original, Transform3D transform)
        {
            if (transform == null || transform.Value.IsIdentity)
                return original; // No transform needed

            var mesh = new MeshGeometry3D();
            
            // Transform positions
            var positions = new Point3DCollection(original.Positions.Count);
            foreach (var p in original.Positions)
                positions.Add(transform.Transform(p));
            mesh.Positions = positions;

            // Transform normals (normals are rotated but not translated)
            if (original.Normals != null)
            {
                var normals = new Vector3DCollection(original.Normals.Count);
                foreach (var n in original.Normals)
                    normals.Add(transform.Transform(n)); // Transform handles normals correctly
                mesh.Normals = normals;
            }

            // Copy other properties
            if (original.TextureCoordinates != null)
                mesh.TextureCoordinates = new PointCollection(original.TextureCoordinates);
            mesh.TriangleIndices = new Int32Collection(original.TriangleIndices);

            return mesh;
        }

        /// <summary>
        /// Build a cutting plane from a camera position and two screen-space points.
        /// The plane passes through the two 3D rays and is oriented toward the camera.
        /// </summary>
        public static (Point3D PlanePoint, Vector3D PlaneNormal) BuildCuttingPlane(
            PerspectiveCamera camera,
            Viewport3D viewport,
            Point screenP1,
            Point screenP2)
        {
            // Get 3D rays from the two screen points
            var ray1 = Get3DRay(camera, viewport, screenP1);
            var ray2 = Get3DRay(camera, viewport, screenP2);

            // The cutting plane contains camera position and both ray directions 
            // We want a plane whose normal is perpendicular to both ray directions
            var dir1 = ray1.Direction;
            var dir2 = ray2.Direction;
            dir1.Normalize();
            dir2.Normalize();

            // The plane normal is the cross product of the two ray directions
            var planeNormal = Vector3D.CrossProduct(dir1, dir2);

            // If the two points are nearly collinear, use camera up as fallback
            if (planeNormal.Length < 0.0001)
            {
                // Fallback: create a vertical plane along the drag direction
                var screenDir = screenP2 - screenP1;
                // Map screen direction to a 3D rotation of the camera's right vector
                var camRight = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection);
                camRight.Normalize();
                var camUp = camera.UpDirection;
                camUp.Normalize();

                // Combine into a world-space direction based on screen drag
                var worldDir = camRight * screenDir.X + camUp * (-screenDir.Y);
                worldDir.Normalize();

                planeNormal = Vector3D.CrossProduct(camera.LookDirection, worldDir);
            }

            planeNormal.Normalize();

            // The camera position lies ON this plane (both rays originate there).
            // We MUST apply the camera's Transform if the user has orbited.
            var planePoint = camera.Transform != null ? camera.Transform.Transform(camera.Position) : camera.Position;

            return (planePoint, planeNormal);
        }

        /// <summary>
        /// Slice a mesh into two halves using a cutting plane defined by a point and normal.
        /// Each triangle is assigned to side A or side B based on which side of the plane
        /// its centroid falls on.
        /// </summary>
        public static SliceResult SliceMesh(MeshGeometry3D mesh, Point3D planePoint, Vector3D planeNormal)
        {
            if (mesh == null || mesh.Positions.Count == 0 || mesh.TriangleIndices.Count < 3)
                return new SliceResult();

            planeNormal.Normalize();

            var positionsA = new List<Point3D>();
            var positionsB = new List<Point3D>();
            var normalsA = new List<Vector3D>();
            var normalsB = new List<Vector3D>();
            var texCoordsA = new List<Point>();
            var texCoordsB = new List<Point>();
            var indicesA = new List<int>();
            var indicesB = new List<int>();

            bool hasNormals = mesh.Normals != null && mesh.Normals.Count == mesh.Positions.Count;
            bool hasTexCoords = mesh.TextureCoordinates != null && mesh.TextureCoordinates.Count == mesh.Positions.Count;

            // Process each triangle
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                int i0 = mesh.TriangleIndices[i];
                int i1 = mesh.TriangleIndices[i + 1];
                int i2 = mesh.TriangleIndices[i + 2];

                var p0 = mesh.Positions[i0];
                var p1 = mesh.Positions[i1];
                var p2 = mesh.Positions[i2];

                // Calculate centroid
                var centroid = new Point3D(
                    (p0.X + p1.X + p2.X) / 3.0,
                    (p0.Y + p1.Y + p2.Y) / 3.0,
                    (p0.Z + p1.Z + p2.Z) / 3.0);

                // Signed distance from centroid to the cutting plane
                var toPlane = centroid - planePoint;
                double signedDist = Vector3D.DotProduct(toPlane, planeNormal);

                // Choose which side of the plane this triangle belongs to
                List<Point3D> positions;
                List<Vector3D> normals;
                List<Point> texCoords;
                List<int> indices;

                if (signedDist >= 0)
                {
                    positions = positionsA;
                    normals = normalsA;
                    texCoords = texCoordsA;
                    indices = indicesA;
                }
                else
                {
                    positions = positionsB;
                    normals = normalsB;
                    texCoords = texCoordsB;
                    indices = indicesB;
                }

                // Add the three vertices (duplicated per-side for simplicity)
                int baseIdx = positions.Count;
                positions.Add(p0);
                positions.Add(p1);
                positions.Add(p2);

                if (hasNormals)
                {
                    normals.Add(mesh.Normals[i0]);
                    normals.Add(mesh.Normals[i1]);
                    normals.Add(mesh.Normals[i2]);
                }

                if (hasTexCoords)
                {
                    texCoords.Add(mesh.TextureCoordinates[i0]);
                    texCoords.Add(mesh.TextureCoordinates[i1]);
                    texCoords.Add(mesh.TextureCoordinates[i2]);
                }

                indices.Add(baseIdx);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 2);
            }

            // Construct meshes
            var result = new SliceResult();

            if (positionsA.Count > 0)
            {
                var meshA = new MeshGeometry3D();
                meshA.Positions = new Point3DCollection(positionsA);
                meshA.TriangleIndices = new Int32Collection(indicesA);
                if (normalsA.Count > 0) meshA.Normals = new Vector3DCollection(normalsA);
                if (texCoordsA.Count > 0) meshA.TextureCoordinates = new PointCollection(texCoordsA);
                result.SideA = meshA;
            }

            if (positionsB.Count > 0)
            {
                var meshB = new MeshGeometry3D();
                meshB.Positions = new Point3DCollection(positionsB);
                meshB.TriangleIndices = new Int32Collection(indicesB);
                if (normalsB.Count > 0) meshB.Normals = new Vector3DCollection(normalsB);
                if (texCoordsB.Count > 0) meshB.TextureCoordinates = new PointCollection(texCoordsB);
                result.SideB = meshB;
            }

            return result;
        }

        /// <summary>
        /// Recursively collects all GeometryModel3D instances from a Model3D tree,
        /// along with the accumulated Transform3D leading down to them.
        /// </summary>
        public static List<(GeometryModel3D gm, Transform3D xform)> CollectGeometryModelsWithTransform(Model3D model)
        {
            var result = new List<(GeometryModel3D gm, Transform3D xform)>();
            CollectGeometryModelsRecursive(model, Transform3D.Identity, result);
            return result;
        }

        private static void CollectGeometryModelsRecursive(Model3D model, Transform3D accumulatedTransform, List<(GeometryModel3D gm, Transform3D xform)> list)
        {
            // Add this node's transform to the accumulator
            Transform3D currentAccumulated = accumulatedTransform;
            if (model.Transform != null && !model.Transform.Value.IsIdentity)
            {
                var group = new Transform3DGroup();
                group.Children.Add(accumulatedTransform);
                group.Children.Add(model.Transform);
                currentAccumulated = group;
            }

            if (model is GeometryModel3D gm)
            {
                list.Add((gm, currentAccumulated));
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children)
                    CollectGeometryModelsRecursive(child, currentAccumulated, list);
            }
        }

        /// <summary>
        /// Translates a MeshGeometry3D's vertices by a specified offset.
        /// Used to center a mesh locally and extract its bounds offset.
        /// </summary>
        public static (MeshGeometry3D CenteredMesh, Vector3D Offset) CenterMesh(MeshGeometry3D original)
        {
            if (original.Positions.Count == 0) return (original, new Vector3D(0, 0, 0));

            // Calculate center of mass (bounds center)
            var bounds = original.Bounds;
            var center = new Vector3D(
                bounds.X + bounds.SizeX / 2.0,
                bounds.Y + bounds.SizeY / 2.0,
                bounds.Z + bounds.SizeZ / 2.0);

            // Shift positions
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection(original.Positions.Count);
            foreach (var p in original.Positions)
                positions.Add(new Point3D(p.X - center.X, p.Y - center.Y, p.Z - center.Z));
            
            mesh.Positions = positions;

            // Copy other properties unchanged
            if (original.Normals != null) mesh.Normals = new Vector3DCollection(original.Normals);
            if (original.TextureCoordinates != null) mesh.TextureCoordinates = new PointCollection(original.TextureCoordinates);
            mesh.TriangleIndices = new Int32Collection(original.TriangleIndices);

            return (mesh, center);
        }

        /// <summary>
        /// Gets a 3D ray from screen coords through the camera.
        /// </summary>
        private static Ray3D Get3DRay(PerspectiveCamera camera, Viewport3D viewport, Point screenPoint)
        {
            double w = viewport.ActualWidth;
            double h = viewport.ActualHeight;

            if (w < 1 || h < 1)
                return new Ray3D(camera.Position, camera.LookDirection);

            // Normalized device coordinates (-1 to 1)
            double ndcX = (2.0 * screenPoint.X / w) - 1.0;
            double ndcY = 1.0 - (2.0 * screenPoint.Y / h);

            // Field of view
            double fovRad = camera.FieldOfView * Math.PI / 180.0;
            double aspect = w / h;

            // Camera coordinate system
            var lookDir = camera.LookDirection;
            lookDir.Normalize();
            var upDir = camera.UpDirection;
            upDir.Normalize();
            var rightDir = Vector3D.CrossProduct(lookDir, upDir);
            rightDir.Normalize();
            var trueUp = Vector3D.CrossProduct(rightDir, lookDir);
            trueUp.Normalize();

            // Half-extents at unit distance
            double halfH = Math.Tan(fovRad / 2.0);
            double halfW = halfH * aspect;

            // Ray direction
            var rayDir = lookDir + rightDir * (ndcX * halfW) + trueUp * (ndcY * halfH);
            rayDir.Normalize();

            var origin = camera.Position;
            if (camera.Transform != null)
            {
                origin = camera.Transform.Transform(origin);
                rayDir = camera.Transform.Transform(rayDir);
                rayDir.Normalize();
            }

            return new Ray3D(origin, rayDir);
        }

        public struct Ray3D
        {
            public Point3D Origin;
            public Vector3D Direction;

            public Ray3D(Point3D origin, Vector3D direction)
            {
                Origin = origin;
                Direction = direction;
            }
        }
    }
}
