using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace VoxDraw.Outputs
{
    class RecognitionOutput : IPathOutputService
    {
        private static readonly float Phi = 0.5f * (-1.0f + Mathf.Sqrt(5.0f)); // Golden Ratio
        private List<Unistroke> gestures = new List<Unistroke>();
        public const float DX = 250f;
        public static readonly float Diagonal = Mathf.Sqrt(DX * DX + DX * DX);
        public static readonly float HalfDiagonal = 0.5f * Diagonal;

        public RecognitionOutput()
        {
            var path = Path.Combine(Application.dataPath, "Gestures\\medium");

            foreach(var f in Directory.GetFiles(path, "*.xml"))
            {
                Debug.LogWarning("loaded " + f);
                using (var stream = File.OpenRead(f)) {
                    var reader = new XmlTextReader(f);
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    reader.MoveToContent();
                    this.gestures.Add(this.ReadGesture(reader));
                    reader.Close();
                }
            }
        }

        public void Process(PathDrawingContext context)
        {

            var prefab = Resources.Load<GameObject>("PhysicsLine");
            var instance = GameObject.Instantiate<GameObject>(prefab, context.Root.position, context.Root.rotation);

            var lineRenderer = instance.GetComponent<VRLineRenderer>();

            var avgDepth = context.Positions.Average(p => p.z);



            Vector3[] points = ApplyDepth(Flatten(context.Positions), avgDepth).ToArray();

            lineRenderer.SetPositions(points, true);

            var collider = instance.GetComponent<BoxCollider>();
            collider.size = context.LocalBounds.size;
            collider.center = context.LocalCenter;
            collider.isTrigger = false;

            var r = Recognize(context);
            Debug.LogWarningFormat("{0} - {1}", r.match.name, r.score);
        }

        private MatchResult Recognize(PathDrawingContext context)
        {
            var normalizedInput = ContextToNormalized(context);

			Debug.Log (DebugOutputPath (normalizedInput)); 

            List<MatchResult> matches = new List<MatchResult>();

            foreach(var u in this.gestures)
            {
                float[] best = GoldenSectionSearch(normalizedInput, u.points, -45f * Mathf.Deg2Rad, +45f * Mathf.Deg2Rad, 2.0f * Mathf.Deg2Rad);

                float score = 1.0f - best[0] / HalfDiagonal;
                matches.Add(new MatchResult()
                {
                    match = u,
                    score = score,
                    distance = best[0],
                    angle = best[1]
                });
            }

            

            var ordered = matches.OrderByDescending(b => b.score);

			Debug.Log ("MATCH!!!!");
			Debug.Log (DebugOutputPath (ordered.First ().match.points));

            return ordered.First();
        }

		private string DebugOutputPath( PathPoint2[] data) { 
			StringBuilder sb = new StringBuilder (); 

			foreach (var point in data) { 
				sb.AppendFormat ("{0:0.000} {1:0.000}", point.X, point.Y);
				sb.AppendLine ();
			}

			sb.AppendLine("=====");

			return sb.ToString ();
		}

        private Unistroke ReadGesture(XmlTextReader reader)
        {
            //Debug.Assert(reader.LocalName == "Gesture");
            string name = reader.GetAttribute("Name");

            List<PathPoint2> points = new List<PathPoint2>(XmlConvert.ToInt32(reader.GetAttribute("NumPts")));

            reader.Read(); // advance to the first Point
            Debug.Assert(reader.LocalName == "Point");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                PathPoint2 p = new PathPoint2();
                p.localPosition = new Vector2(XmlConvert.ToSingle(reader.GetAttribute("X")), XmlConvert.ToSingle(reader.GetAttribute("Y")));
                p.timestamp = XmlConvert.ToInt64(reader.GetAttribute("T"));
                points.Add(p);
                reader.ReadStartElement("Point");
            }

            var normalized = this.NormalizePoints(points.ToArray());

            return new Unistroke(name, normalized);
        }

        private float[] GoldenSectionSearch(PathPoint2[] input, PathPoint2[] candidate, float a, float b, float threshold)
        {

            float x1 = Phi * a + (1 - Phi) * b;
            var newPoints = RotatePoints(input, x1).ToArray();
            float distance1 = PathDistance(newPoints, candidate);

            float x2 = (1 - Phi) * a + Phi * b;
            newPoints = RotatePoints(input, x2).ToArray();
            float distance2 = PathDistance(newPoints, candidate);


            float i = 2.0f; // calls to pathdist
            while (Math.Abs(b - a) > threshold)
            {
                if (distance1 < distance2)
                {
                    b = x2;
                    x2 = x1;
                    distance2 = distance1;
                    x1 = Phi * a + (1 - Phi) * b;
                    newPoints = RotatePoints(input, x1).ToArray();
                    distance1 = PathDistance(newPoints, candidate);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    distance1 = distance2;
                    x2 = (1 - Phi) * a + Phi * b;
                    newPoints = RotatePoints(input, x2).ToArray();
                    distance2 = PathDistance(newPoints, candidate);
                }
                i++;
            }
            return new float[3] { Mathf.Min(distance1, distance2), Mathf.Rad2Deg * ((b + a) / 2.0f), i }; // distance, angle, calls to pathdist
        }

        private float PathDistance(PathPoint2[] path1, PathPoint2[] path2)
        {
            float distance = 0;
            for (int i = 0; i < Math.Min(path1.Length, path2.Length); i++)
            {
                distance += path1[i].Distance(path2[i]);
            }
            return distance / path1.Length;
        }

        private PathPoint2[] ContextToNormalized(PathDrawingContext context)
        {
            var context2d = context.ConvertTo2d();
            return NormalizePoints(context2d.ToArray(), true);
        }

        private PathPoint2[] NormalizePoints(PathPoint2[] context2d, bool vectorOrigin = false)
        {
            float averageDistance = PathLength(context2d) / (context2d.Length - 1);
			var resampled = Resample(context2d, averageDistance).ToArray();

            var angleToStart = Mathf.Deg2Rad * Vector2.Angle(GetCentroid(resampled), resampled.First().localPosition);
			resampled = RotatePoints(resampled, angleToStart).ToArray();
            //if (vectorOrigin)
            //{
            //    resampled = ScalePoints(resampled, new Vector2(DX / 2, DX / 2));
            //    resampled = TranslatePoints(resampled, new Vector2(DX / 2, DX / 2));
            //    resampled = TranslatePoints(resampled, Vector2.zero);
            //}
            //else
            //{
			resampled = ScalePoints(resampled, new Vector2(DX, DX)).ToArray();
			resampled = TranslatePoints(resampled, Vector2.zero).ToArray();
            // }

			return resampled;
        }

        private IEnumerable<PathPoint2> TranslatePoints(IEnumerable<PathPoint2> points, Vector2 toPoint)
        {
            var centroid = GetCentroid(points);
            foreach (var p in points)
            {
                var deltaX = toPoint.x - centroid.x;
                var deltaY = toPoint.y - centroid.y;

                yield return p.Clone(x: p.X + deltaX, y: p.Y + deltaY);
            }
        }

        private IEnumerable<PathPoint2> ScalePoints(IEnumerable<PathPoint2> points, Vector2 size)
        {
            var bounds = GetBoundsSize(points);

            foreach (var p in points)
            {
                var newX = p.X;
                var newY = p.Y;

                if (bounds.x != 0.0f)
                    newX *= (size.x / bounds.x);
                if (bounds.y != 0.0f)
                    newY *= (size.y / bounds.y);

                yield return p.Clone(x: newX, y: newY);
            }
        }

        private Vector2 GetBoundsSize(IEnumerable<PathPoint2> points)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var p in points)
            {
                if (p.X < minX)
                    minX = p.X;
                if (p.X > maxX)
                    maxX = p.X;

                if (p.Y < minY)
                    minY = p.Y;
                if (p.Y > maxY)
                    maxY = p.Y;
            }

            return new Vector2(maxX - minX, maxY - minY);
        }

        private IEnumerable<PathPoint2> RotatePoints(IEnumerable<PathPoint2> points, float radians)
        {
            Vector2 centroid = GetCentroid(points);
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            foreach (var p in points)
            {
                float deltaX = p.X - centroid.x;
                float deltaY = p.Y - centroid.y;
                float newX = deltaX * cos - deltaY * sin + centroid.x;
                float newY = deltaX * sin + deltaY * cos + centroid.y;

                yield return p.Clone(x: newX, y: newY);
            }
        }

        Vector2 GetCentroid(IEnumerable<PathPoint2> path)
        {
            float xSum = 0;
            float ySum = 0;
            int c = 0;

            foreach (var p in path)
            {
                xSum += p.X;
                ySum += p.Y;
                c++;
            }

            return new Vector2(xSum / c, ySum / c);
        }

        float PathLength(IEnumerable<PathPoint2> path)
        {
            float length = 0;
            PathPoint2? prior = null;

            foreach (var point in path)
            {
                if (prior == null)
                {
                    prior = point;
                    continue;
                }

                length += prior.Value.Distance(point);
                prior = point;
            }

            return length;
        }

        IEnumerable<PathPoint2> Resample(IEnumerable<PathPoint2> point, float minDistance)
        {
            var pointArray = point.ToArray();
            yield return pointArray[0];

            float runningDistance = 0.0f;

            for (var i = 1; i < pointArray.Length; i++)
            {
                var p1 = pointArray[i - 1];
                var p2 = pointArray[i];
                
                float distanceBetweenCurrentPoints = p1.Distance(p2);

                if ((runningDistance + distanceBetweenCurrentPoints) >= minDistance)
                {
                    float distanceRatio = (minDistance - runningDistance) / distanceBetweenCurrentPoints;

                    float newX = p1.X + distanceRatio * (p2.X - p1.X);
                    float newY = p1.Y + distanceRatio * (p2.Y - p1.Y);

                    float newTime = p1.timestamp + distanceRatio * (p2.timestamp - p1.timestamp);
                    float pressure = p1.pressure + distanceRatio * (p2.pressure - p1.pressure);
                    float velocity = p1.velocity + distanceRatio * (p2.velocity - p1.velocity);

                    var updated = new PathPoint2()
                    {
                        localPosition = new Vector2(newX, newY),
                        timestamp = (long)newTime,
                        pressure = pressure,
                        velocity = velocity
                    };

                    pointArray[i] = updated;

                    yield return updated;
                    runningDistance = 0;
                    continue;
                }

                runningDistance += distanceBetweenCurrentPoints;
            }

            if (runningDistance > 0)
            {
                yield return pointArray[pointArray.Length - 1];
            }
        }

        private IEnumerable<Vector2> Flatten(IEnumerable<Vector3> points)
        {
            // Remove z component
            return points.Select(p => new Vector2(p.x, p.y));
        }

        private IEnumerable<Vector3> ApplyDepth(IEnumerable<Vector2> points, float depth)
        {
            return points.Select(p => new Vector3(p.x, p.y, depth));
        }
    }

    class Unistroke
    {
        public string name;
        public PathPoint2[] points;

        public Unistroke(string name, PathPoint2[] points)
        {
            this.name = name;
            this.points = points;
        }
    }

    class MatchResult
    {
        public Unistroke match;
        public float angle;
        public float score;
        public float distance; 
    }
}
