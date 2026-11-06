using System;

namespace Bascule.Core
{
    /// <summary>
    /// Forme définie par une fonction de distance signée (négative à l'intérieur), avec sa boîte englobante.
    /// Les formes se combinent (union douce, intersection, soustraction) pour dessiner des volumes organiques.
    /// </summary>
    public sealed class Shape
    {
        public readonly Func<float, float, float> D;
        public readonly float X0, Y0, X1, Y1;

        public Shape(Func<float, float, float> d, float x0, float y0, float x1, float y1)
        {
            D = d; X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
        }

        static float Len(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        public static Shape Circle(float cx, float cy, float r) =>
            new Shape((x, y) => Len(x - cx, y - cy) - r, cx - r, cy - r, cx + r, cy + r);

        public static Shape Ellipse(float cx, float cy, float rx, float ry, float rotDeg = 0f)
        {
            float m = Math.Min(rx, ry), ext = Math.Max(rx, ry);
            float c = (float)Math.Cos(-rotDeg * Math.PI / 180), s = (float)Math.Sin(-rotDeg * Math.PI / 180);
            return new Shape((x, y) =>
            {
                float px = x - cx, py = y - cy;
                float lx = px * c - py * s, ly = px * s + py * c;
                return (Len(lx / rx, ly / ry) - 1f) * m;
            }, cx - ext, cy - ext, cx + ext, cy + ext);
        }

        /// <summary>Rectangle centré à coins arrondis, éventuellement tourné.</summary>
        public static Shape Box(float cx, float cy, float hw, float hh, float r = 0f, float rotDeg = 0f)
        {
            r = Math.Min(r, Math.Min(hw, hh));
            float c = (float)Math.Cos(-rotDeg * Math.PI / 180), s = (float)Math.Sin(-rotDeg * Math.PI / 180);
            float ext = rotDeg == 0f ? 0f : Len(hw, hh);
            return new Shape((x, y) =>
            {
                float px = x - cx, py = y - cy;
                float lx = px * c - py * s, ly = px * s + py * c;
                float qx = Math.Abs(lx) - (hw - r), qy = Math.Abs(ly) - (hh - r);
                return Len(Math.Max(qx, 0), Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - r;
            }, cx - (ext > 0 ? ext : hw), cy - (ext > 0 ? ext : hh), cx + (ext > 0 ? ext : hw), cy + (ext > 0 ? ext : hh));
        }

        public static Shape Capsule(float ax, float ay, float bx, float by, float r) => Tapered(ax, ay, r, bx, by, r);

        /// <summary>Segment épais dont le rayon varie de ra à rb (membres, doigts, mèches).</summary>
        public static Shape Tapered(float ax, float ay, float ra, float bx, float by, float rb)
        {
            float m = Math.Max(ra, rb);
            return new Shape((x, y) =>
            {
                float abx = bx - ax, aby = by - ay;
                float l2 = abx * abx + aby * aby;
                float t = l2 > 0 ? ((x - ax) * abx + (y - ay) * aby) / l2 : 0;
                t = t < 0 ? 0 : t > 1 ? 1 : t;
                return Len(x - (ax + t * abx), y - (ay + t * aby)) - (ra + (rb - ra) * t);
            }, Math.Min(ax, bx) - m, Math.Min(ay, by) - m, Math.Max(ax, bx) + m, Math.Max(ay, by) + m);
        }

        /// <summary>Polygone quelconque (distance exacte, méthode d'Inigo Quilez). Sommets x0,y0,x1,y1…</summary>
        public static Shape Poly(params float[] v)
        {
            int n = v.Length / 2;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                x0 = Math.Min(x0, v[2 * i]); x1 = Math.Max(x1, v[2 * i]);
                y0 = Math.Min(y0, v[2 * i + 1]); y1 = Math.Max(y1, v[2 * i + 1]);
            }
            return new Shape((x, y) =>
            {
                float d = (x - v[0]) * (x - v[0]) + (y - v[1]) * (y - v[1]);
                float s = 1f;
                for (int i = 0, j = n - 1; i < n; j = i, i++)
                {
                    float ex = v[2 * j] - v[2 * i], ey = v[2 * j + 1] - v[2 * i + 1];
                    float wx = x - v[2 * i], wy = y - v[2 * i + 1];
                    float t = (wx * ex + wy * ey) / (ex * ex + ey * ey);
                    t = t < 0 ? 0 : t > 1 ? 1 : t;
                    float bx = wx - ex * t, by = wy - ey * t;
                    d = Math.Min(d, bx * bx + by * by);
                    bool c1 = y >= v[2 * i + 1], c2 = y < v[2 * j + 1], c3 = ex * wy > ey * wx;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * (float)Math.Sqrt(d);
            }, x0, y0, x1, y1);
        }

        /// <summary>Arc épais. Angles en degrés (0 = droite, sens trigonométrique).</summary>
        public static Shape Arc(float cx, float cy, float radius, float startDeg, float endDeg, float thickness)
        {
            float a0 = (float)(startDeg * Math.PI / 180), a1 = (float)(endDeg * Math.PI / 180);
            if (a1 < a0) { var t = a0; a0 = a1; a1 = t; }
            float half = thickness * 0.5f;
            float e0x = cx + radius * (float)Math.Cos(a0), e0y = cy + radius * (float)Math.Sin(a0);
            float e1x = cx + radius * (float)Math.Cos(a1), e1y = cy + radius * (float)Math.Sin(a1);
            float ext = radius + thickness;
            return new Shape((x, y) =>
            {
                float ang = (float)Math.Atan2(y - cy, x - cx);
                while (ang < a0) ang += (float)(2 * Math.PI);
                if (ang <= a1) return Math.Abs(Len(x - cx, y - cy) - radius) - half;
                return Math.Min(Len(x - e0x, y - e0y), Len(x - e1x, y - e1y)) - half;
            }, cx - ext, cy - ext, cx + ext, cy + ext);
        }

        /// <summary>Demi-plan : intérieur du côté où (x,y)·n &lt; offset. Utile pour couper une forme.</summary>
        public static Shape HalfPlane(float nx, float ny, float offset)
        {
            float l = Len(nx, ny); nx /= l; ny /= l; offset /= l;
            const float big = 1e5f;
            return new Shape((x, y) => x * nx + y * ny - offset, -big, -big, big, big);
        }

        /// <summary>Complément (tout sauf la forme).</summary>
        public Shape Not()
        {
            var d = D;
            const float big = 1e5f;
            return new Shape((x, y) => -d(x, y), -big, -big, big, big);
        }

        public Shape Grow(float w) => new Shape((x, y) => D(x, y) - w, X0 - w, Y0 - w, X1 + w, Y1 + w);

        public Shape Move(float dx, float dy)
        {
            var d = D;
            return new Shape((x, y) => d(x - dx, y - dy), X0 + dx, Y0 + dy, X1 + dx, Y1 + dy);
        }

        /// <summary>Miroir horizontal autour de x = axis.</summary>
        public Shape MirrorX(float axis)
        {
            var d = D;
            return new Shape((x, y) => d(2 * axis - x, y), 2 * axis - X1, Y0, 2 * axis - X0, Y1);
        }

        public static Shape Union(Shape a, Shape b) =>
            new Shape((x, y) => Math.Min(a.D(x, y), b.D(x, y)), Math.Min(a.X0, b.X0), Math.Min(a.Y0, b.Y0), Math.Max(a.X1, b.X1), Math.Max(a.Y1, b.Y1));

        public static Shape Union(params Shape[] shapes)
        {
            var s = shapes[0];
            for (int i = 1; i < shapes.Length; i++) s = Union(s, shapes[i]);
            return s;
        }

        /// <summary>Union douce : les formes se fondent l'une dans l'autre sur une largeur k (aspect organique).</summary>
        public static Shape Smooth(Shape a, Shape b, float k) =>
            new Shape((x, y) =>
            {
                float da = a.D(x, y), db = b.D(x, y);
                float h = Math.Max(k - Math.Abs(da - db), 0f) / k;
                return Math.Min(da, db) - h * h * k * 0.25f;
            }, Math.Min(a.X0, b.X0) - k, Math.Min(a.Y0, b.Y0) - k, Math.Max(a.X1, b.X1) + k, Math.Max(a.Y1, b.Y1) + k);

        public static Shape Intersect(Shape a, Shape b) =>
            new Shape((x, y) => Math.Max(a.D(x, y), b.D(x, y)), Math.Max(a.X0, b.X0), Math.Max(a.Y0, b.Y0), Math.Min(a.X1, b.X1), Math.Min(a.Y1, b.Y1));

        public static Shape Subtract(Shape a, Shape b) =>
            new Shape((x, y) => Math.Max(a.D(x, y), -b.D(x, y)), a.X0, a.Y0, a.X1, a.Y1);
    }

    /// <summary>Remplissage : couleur unie, dégradé linéaire ou radial.</summary>
    public struct Paint
    {
        enum Kind { Solid, Linear, Radial, Custom }
        Kind _kind;
        Func<float, float, Rgba> _fn;

        /// <summary>Couleur calculée pixel par pixel (textures : grain du bois, herbe…).</summary>
        public static Paint Custom(Func<float, float, Rgba> fn) => new Paint { _kind = Kind.Custom, _fn = fn };
        Rgba _a, _b;
        float _x0, _y0, _x1, _y1, _r;

        public static implicit operator Paint(Rgba c) => new Paint { _kind = Kind.Solid, _a = c };

        public static Paint Linear(Rgba from, float x0, float y0, Rgba to, float x1, float y1) =>
            new Paint { _kind = Kind.Linear, _a = from, _b = to, _x0 = x0, _y0 = y0, _x1 = x1, _y1 = y1 };

        public static Paint Radial(Rgba center, float cx, float cy, Rgba edge, float radius) =>
            new Paint { _kind = Kind.Radial, _a = center, _b = edge, _x0 = cx, _y0 = cy, _r = radius };

        public Rgba At(float x, float y)
        {
            switch (_kind)
            {
                case Kind.Linear:
                {
                    float dx = _x1 - _x0, dy = _y1 - _y0;
                    float t = ((x - _x0) * dx + (y - _y0) * dy) / (dx * dx + dy * dy);
                    return Rgba.Lerp(_a, _b, t);
                }
                case Kind.Radial:
                {
                    float t = (float)Math.Sqrt((x - _x0) * (x - _x0) + (y - _y0) * (y - _y0)) / _r;
                    return Rgba.Lerp(_a, _b, t);
                }
                case Kind.Custom: return _fn(x, y);
                default: return _a;
            }
        }
    }

    /// <summary>Bruit lisse déterministe (grain du bois, texture).</summary>
    public static class Noise
    {
        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        public static float Value(float x, float y)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(ix, iy), b = Hash(ix + 1, iy), c = Hash(ix, iy + 1), d = Hash(ix + 1, iy + 1);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        public static float Fbm(float x, float y, int octaves = 4)
        {
            float sum = 0, amp = 0.5f, f = 1;
            for (int i = 0; i < octaves; i++) { sum += Value(x * f, y * f) * amp; f *= 2; amp *= 0.5f; }
            return sum;
        }
    }
}
