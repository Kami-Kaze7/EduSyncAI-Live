using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace EduSyncAI.Helpers
{
    /// <summary>
    /// Factory that generates MeshGeometry3D primitives for 3D shapes.
    /// </summary>
    public static class Shape3DFactory
    {
        // ── Cube ────────────────────────────────────────────────────────────
        public static GeometryModel3D CreateCube(double size = 1.0, Color? color = null)
        {
            var c = color ?? Colors.DodgerBlue;
            double h = size / 2.0;

            var mesh = new MeshGeometry3D();

            // 8 vertices of a cube centred at origin
            Point3D[] v =
            {
                new(-h, -h, -h), // 0 - front-bottom-left
                new( h, -h, -h), // 1 - front-bottom-right
                new( h,  h, -h), // 2 - front-top-right
                new(-h,  h, -h), // 3 - front-top-left
                new(-h, -h,  h), // 4 - back-bottom-left
                new( h, -h,  h), // 5 - back-bottom-right
                new( h,  h,  h), // 6 - back-top-right
                new(-h,  h,  h), // 7 - back-top-left
            };

            // Each face = 2 triangles (12 total), with per-face normals for flat shading
            AddQuad(mesh, v[0], v[1], v[2], v[3], new Vector3D(0, 0, -1)); // front
            AddQuad(mesh, v[5], v[4], v[7], v[6], new Vector3D(0, 0,  1)); // back
            AddQuad(mesh, v[4], v[0], v[3], v[7], new Vector3D(-1, 0, 0)); // left
            AddQuad(mesh, v[1], v[5], v[6], v[2], new Vector3D( 1, 0, 0)); // right
            AddQuad(mesh, v[3], v[2], v[6], v[7], new Vector3D(0,  1, 0)); // top
            AddQuad(mesh, v[4], v[5], v[1], v[0], new Vector3D(0, -1, 0)); // bottom

            return WrapMesh(mesh, c);
        }

        // ── Sphere ──────────────────────────────────────────────────────────
        public static GeometryModel3D CreateSphere(double radius = 0.5, int segments = 24, Color? color = null)
        {
            var c = color ?? Colors.OrangeRed;
            var mesh = new MeshGeometry3D();

            int stacks = segments / 2;
            for (int stack = 0; stack <= stacks; stack++)
            {
                double phi = Math.PI * stack / stacks;
                for (int slice = 0; slice <= segments; slice++)
                {
                    double theta = 2 * Math.PI * slice / segments;
                    double x = radius * Math.Sin(phi) * Math.Cos(theta);
                    double y = radius * Math.Cos(phi);
                    double z = radius * Math.Sin(phi) * Math.Sin(theta);

                    mesh.Positions.Add(new Point3D(x, y, z));
                    mesh.Normals.Add(new Vector3D(x, y, z));
                }
            }

            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < segments; slice++)
                {
                    int i0 = stack * (segments + 1) + slice;
                    int i1 = i0 + 1;
                    int i2 = i0 + segments + 1;
                    int i3 = i2 + 1;

                    mesh.TriangleIndices.Add(i0);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i1);

                    mesh.TriangleIndices.Add(i1);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i3);
                }
            }

            return WrapMesh(mesh, c);
        }

        // ── Cylinder ────────────────────────────────────────────────────────
        public static GeometryModel3D CreateCylinder(double radius = 0.4, double height = 1.0, int segments = 24, Color? color = null)
        {
            var c = color ?? Colors.MediumSeaGreen;
            var mesh = new MeshGeometry3D();
            double hh = height / 2.0;

            // Side vertices
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                var normal = new Vector3D(x, 0, z);
                normal.Normalize();

                mesh.Positions.Add(new Point3D(x, -hh, z));
                mesh.Normals.Add(normal);
                mesh.Positions.Add(new Point3D(x, hh, z));
                mesh.Normals.Add(normal);
            }

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                int b = i * 2;
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(b + 2);
                mesh.TriangleIndices.Add(b + 1);

                mesh.TriangleIndices.Add(b + 1);
                mesh.TriangleIndices.Add(b + 2);
                mesh.TriangleIndices.Add(b + 3);
            }

            // Top cap
            int topCenter = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(0, hh, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                mesh.Positions.Add(new Point3D(radius * Math.Cos(angle), hh, radius * Math.Sin(angle)));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
            }
            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(topCenter);
                mesh.TriangleIndices.Add(topCenter + 1 + i);
                mesh.TriangleIndices.Add(topCenter + 2 + i);
            }

            // Bottom cap
            int botCenter = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(0, -hh, 0));
            mesh.Normals.Add(new Vector3D(0, -1, 0));
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                mesh.Positions.Add(new Point3D(radius * Math.Cos(angle), -hh, radius * Math.Sin(angle)));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
            }
            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(botCenter);
                mesh.TriangleIndices.Add(botCenter + 2 + i);
                mesh.TriangleIndices.Add(botCenter + 1 + i);
            }

            return WrapMesh(mesh, c);
        }

        // ── Pyramid ─────────────────────────────────────────────────────────
        public static GeometryModel3D CreatePyramid(double baseSize = 1.0, double height = 1.2, Color? color = null)
        {
            var c = color ?? Colors.Gold;
            double h = baseSize / 2.0;
            var mesh = new MeshGeometry3D();

            Point3D apex = new(0, height, 0);
            Point3D bl = new(-h, 0, -h);
            Point3D br = new( h, 0, -h);
            Point3D fr = new( h, 0,  h);
            Point3D fl = new(-h, 0,  h);

            // 4 side faces
            AddTriangleWithNormal(mesh, bl, br, apex); // front
            AddTriangleWithNormal(mesh, br, fr, apex); // right
            AddTriangleWithNormal(mesh, fr, fl, apex); // back
            AddTriangleWithNormal(mesh, fl, bl, apex); // left

            // Bottom (2 triangles)
            var downNormal = new Vector3D(0, -1, 0);
            AddQuad(mesh, fl, fr, br, bl, downNormal);

            return WrapMesh(mesh, c);
        }

        // ── Cone ────────────────────────────────────────────────────────────
        public static GeometryModel3D CreateCone(double radius = 0.4, double height = 1.0, int segments = 24, Color? color = null)
        {
            var c = color ?? Colors.MediumPurple;
            var mesh = new MeshGeometry3D();
            Point3D apex = new(0, height, 0);

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                double a1 = 2 * Math.PI * i / segments;
                double a2 = 2 * Math.PI * (i + 1) / segments;

                var p1 = new Point3D(radius * Math.Cos(a1), 0, radius * Math.Sin(a1));
                var p2 = new Point3D(radius * Math.Cos(a2), 0, radius * Math.Sin(a2));

                AddTriangleWithNormal(mesh, p1, p2, apex);
            }

            // Bottom cap
            int botCenter = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(0, 0, 0));
            mesh.Normals.Add(new Vector3D(0, -1, 0));
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                mesh.Positions.Add(new Point3D(radius * Math.Cos(angle), 0, radius * Math.Sin(angle)));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
            }
            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(botCenter);
                mesh.TriangleIndices.Add(botCenter + 2 + i);
                mesh.TriangleIndices.Add(botCenter + 1 + i);
            }

            return WrapMesh(mesh, c);
        }

        // ── Private helpers ─────────────────────────────────────────────────

        private static void AddQuad(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c, Point3D d, Vector3D normal)
        {
            int idx = mesh.Positions.Count;
            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.Positions.Add(d);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TriangleIndices.Add(idx);
            mesh.TriangleIndices.Add(idx + 1);
            mesh.TriangleIndices.Add(idx + 2);

            mesh.TriangleIndices.Add(idx);
            mesh.TriangleIndices.Add(idx + 2);
            mesh.TriangleIndices.Add(idx + 3);
        }

        private static void AddTriangleWithNormal(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c)
        {
            var normal = Vector3D.CrossProduct(b - a, c - a);
            normal.Normalize();

            int idx = mesh.Positions.Count;
            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TriangleIndices.Add(idx);
            mesh.TriangleIndices.Add(idx + 1);
            mesh.TriangleIndices.Add(idx + 2);
        }

        private static GeometryModel3D WrapMesh(MeshGeometry3D mesh, Color color)
        {
            var mat = new MaterialGroup();
            mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            mat.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 40));

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = mat,
                BackMaterial = new DiffuseMaterial(new SolidColorBrush(
                    Color.FromArgb(color.A, (byte)(color.R * 0.6), (byte)(color.G * 0.6), (byte)(color.B * 0.6))))
            };
        }
    }
}
