using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Drawing of the graphs of the Graph key (v3.4), into the 144 x 144 image of KeyRenderer (called under its lock).
    /// Coordinates in pixels of the 72-pixel key. The graphs leave the top of the key to the text (position "en haut").
    /// </summary>
    public static class GraphRenderer
    {
        private const float S = KeyRenderer.Scale;

        public static void Draw(Graphics g, GraphType type, double fraction, string color, List<double?> points, double? min, double? max)
        {
            var fill = KeyRenderer.ParseColor(color, KeyRenderer.ParseColor(GraphConfig.DefaultColor, Color.LightBlue));
            var track = KeyRenderer.ParseColor(GraphConfig.TrackColor, Color.DimGray);
            fraction = Math.Max(0, Math.Min(1, fraction));

            switch (type)
            {
                case GraphType.VerticalBar:
                    Bar(g, new RectangleF(26, 22, 20, 44), fraction, fill, track, vertical: true);
                    break;
                case GraphType.Dial:
                    Dial(g, fraction, fill, track);
                    break;
                case GraphType.Curve:
                    Curve(g, points ?? new List<double?>(), min, max, fill, track);
                    break;
                default:
                    Bar(g, new RectangleF(6, 44, 60, 18), fraction, fill, track, vertical: false);
                    break;
            }
        }

        private static void Bar(Graphics g, RectangleF area, double fraction, Color fill, Color track, bool vertical)
        {
            var box = Scaled(area);
            using (var trackBrush = new SolidBrush(track))
            using (var path = Rounded(box, 3 * S))
                g.FillPath(trackBrush, path);

            if (fraction <= 0)
                return;

            var part = vertical
                ? new RectangleF(box.X, box.Bottom - box.Height * (float)fraction, box.Width, box.Height * (float)fraction)
                : new RectangleF(box.X, box.Y, box.Width * (float)fraction, box.Height);
            using (var fillBrush = new SolidBrush(fill))
            using (var path = Rounded(part, 3 * S))
                g.FillPath(fillBrush, path);
        }

        private static void Dial(Graphics g, double fraction, Color fill, Color track)
        {
            const float start = 150, sweep = 240;
            var box = Scaled(new RectangleF(14, 24, 44, 44));
            using (var trackPen = new Pen(track, 7 * S) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(trackPen, box, start, sweep);

            if (fraction <= 0)
                return;
            using (var fillPen = new Pen(fill, 7 * S) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(fillPen, box, start, Math.Max(1f, sweep * (float)fraction));
        }

        /// <summary>History of the key, oldest on the left; the bounds, or the extent of the values when a bound is empty.</summary>
        private static void Curve(Graphics g, List<double?> points, double? min, double? max, Color fill, Color track)
        {
            var area = Scaled(new RectangleF(4, 24, 64, 42));
            using (var axis = new Pen(track, 1 * S))
                g.DrawLine(axis, area.Left, area.Bottom, area.Right, area.Bottom);

            var values = points.Where(p => p.HasValue).Select(p => p.Value).ToList();
            if (values.Count == 0)
                return;

            double low = min ?? values.Min();
            double high = max ?? values.Max();
            if (high <= low)
            {
                low -= 1;
                high += 1;
            }

            float step = points.Count > 1 ? area.Width / (points.Count - 1) : 0;
            var segment = new List<PointF>();
            using (var pen = new Pen(fill, 2 * S) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (!points[i].HasValue)
                    {
                        DrawSegment(g, pen, segment);
                        segment.Clear();
                        continue;
                    }

                    var ratio = (points[i].Value - low) / (high - low);
                    ratio = Math.Max(0, Math.Min(1, ratio));
                    segment.Add(new PointF(area.Left + i * step, area.Bottom - area.Height * (float)ratio));
                }
                DrawSegment(g, pen, segment);
            }
        }

        private static void DrawSegment(Graphics g, Pen pen, List<PointF> segment)
        {
            if (segment.Count >= 2)
                g.DrawLines(pen, segment.ToArray());
            else if (segment.Count == 1)
            {
                using (var dot = new SolidBrush(pen.Color))
                    g.FillEllipse(dot, segment[0].X - pen.Width / 2, segment[0].Y - pen.Width / 2, pen.Width, pen.Width);
            }
        }

        private static RectangleF Scaled(RectangleF area)
        {
            return new RectangleF(area.X * S, area.Y * S, area.Width * S, area.Height * S);
        }

        private static GraphicsPath Rounded(RectangleF box, float radius)
        {
            var path = new GraphicsPath();
            radius = Math.Min(radius, Math.Min(box.Width, box.Height) / 2);
            if (radius <= 0.5f)
            {
                path.AddRectangle(box);
                return path;
            }

            float d = radius * 2;
            path.AddArc(box.X, box.Y, d, d, 180, 90);
            path.AddArc(box.Right - d, box.Y, d, d, 270, 90);
            path.AddArc(box.Right - d, box.Bottom - d, d, d, 0, 90);
            path.AddArc(box.X, box.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
