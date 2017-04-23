using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VoxDraw.Outputs
{
    class PRecoOutput : IPathOutputService
    {
        private Gesture[] trainingSet = new Gesture[0];
        private bool training = false;
        private string traingLabel = "line"; 

        public PRecoOutput()
        {
            if(training)
            {
                return;
            }

            var basePath = Path.Combine(Application.dataPath, "Gestures\\p");

            List<Gesture> gestures = new List<Gesture>(); 
            foreach(var dataFile in Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories))
            {
                var json = File.ReadAllText(dataFile);
                Gesture g = JsonUtility.FromJson<Gesture>(json);
                gestures.Add(g);
            }

            trainingSet = gestures.ToArray();
        }

        public void Process(PathDrawingContext context)
        {
            Point[] strokeToPoints = this.ContextToStroke(context);

            if (training)
            {
                var g = new Gesture(strokeToPoints, traingLabel);
                var outputPath = Path.Combine(Application.dataPath, "Gestures\\p\\" + traingLabel);
                Directory.CreateDirectory(outputPath);


                var data = JsonUtility.ToJson(g, true);
                File.WriteAllText(Path.Combine(outputPath, DateTime.UtcNow.Ticks + ".json"), data); 
            }
            else
            {
                var result = PointCloudRecognizer.Classify(new Gesture(strokeToPoints), this.trainingSet);

                this.GenerateFromResult(result, context);
                
            }
        }

        private void GenerateFromResult(RecognizerResult result, PathDrawingContext context)
        {
            Dictionary<string, Action<PathDrawingContext>> prefabMap = new Dictionary<string, Action<PathDrawingContext>>()
            {
                { "circle", MakeSphere },
                { "square", MakeSquare },
                { "line", MakeLine },
                { "arrow", MakeArrow }
            };

            Action<PathDrawingContext> callback = null;
            if(prefabMap.TryGetValue(result.Class, out callback))
            {
                callback(context);
            }

        }

        private void MakeLine(PathDrawingContext context)
        {
            const float depth = 0.025f;
            const float cylinderFactor = 0.5f;

            var worldX = context.WorldBounds.size.x;
            var worldY = context.WorldBounds.size.y;

            var cyclinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            // Scale to fixed x and z with Y (up) being the largest extent of the line.
            // TODO: We could likely take the aggregate distance of the line assuming limited curves 
            cyclinder.transform.localScale = new Vector3(depth, Math.Max(worldX, worldY) * cylinderFactor, depth);

            // Get angle between start and end
            var p = context.WorldPositions.ToArray();
            var dir = p[p.Length - 1] - p[0];
            var mid = (dir) / 2.0f + p[0];

            // Transform the cylinder to be across the path of the line with the correct rotation 
            cyclinder.transform.SetPositionAndRotation(mid, Quaternion.FromToRotation(Vector3.up, dir));
            
            // Create an empty snappable object and assign the cylinder as the "mesh"  
            const string prefabName = @"Prefabs/StubSnappable";
            var prefab = Resources.Load<GameObject>(prefabName);
            var instance = GameObject.Instantiate(prefab, cyclinder.transform.position, Quaternion.identity);
            var assign = instance.GetComponent<SnappableAssign>();
            
            assign.AssignSnappableChild(cyclinder);

            // Create snap points in *local space" at the start and of the "line" 
            var first = assign.GenerateSnapPoint();
            first.transform.localPosition = new Vector3(0, +1, 0); // Far extent
            first.transform.localScale = Vector3.one * (depth * 5); // Just feels right, TODO: Make more formal 
            first.transform.localRotation = Quaternion.Euler(180, 0, -90);

            var last = assign.GenerateSnapPoint();
            last.transform.localPosition = new Vector3(0, -1, 0); // Near extent 
            last.transform.localScale = Vector3.one * (depth * 5);
            last.transform.localRotation = Quaternion.Euler(180, 0, 90);

        }

        private void MakeArrow(PathDrawingContext context)
        {
            // TODO 
        }

        private void MakeSphere(PathDrawingContext context)
        {
            const string prefabName = @"Prefabs/SnappableSphere";

            var prefab = Resources.Load<GameObject>(prefabName);
            var instance = GameObject.Instantiate(prefab, context.WorldCenter, Quaternion.identity);
            PlaceInstance(context, instance);
        }

        private void MakeSquare(PathDrawingContext context)
        {
            const string prefabName = @"Prefabs/ParentedBlock";

            var prefab = Resources.Load<GameObject>(prefabName);
            var instance = GameObject.Instantiate(prefab, context.WorldCenter, Quaternion.identity);
            PlaceInstance(context, instance);
        }

        private static void PlaceInstance(PathDrawingContext context, GameObject instance, float initialScale = 0.05f, float loss = 0.9f)
        {
            var size = Math.Max(context.WorldBounds.size.x, context.WorldBounds.size.y);
            var globalScale = (Vector3.one / initialScale) * (size * loss); // Hack
            var lossyScale = instance.transform.lossyScale;
            instance.transform.localScale = new Vector3(globalScale.x / lossyScale.x, globalScale.y / lossyScale.y, globalScale.z / lossyScale.z);
        }

        private Point[] ContextToStroke(PathDrawingContext context)
        {
            List<Point> data = new List<Point>(context.NumberPoints);
            foreach(var p in context.ConvertTo2d())
            {
                data.Add(new Point(p.X, p.Y, 0));
            }

            return data.ToArray();
        }
    }
    
    [Serializable]
    public class Point
    {
        [SerializeField]
        public float X, Y;
        [SerializeField]
        public int StrokeID;

        public Point(float x, float y, int strokeId)
        {
            this.X = x;
            this.Y = y;
            this.StrokeID = strokeId;
        }
    }

    [Serializable]
    public class Gesture
    {
        [SerializeField]
        public Point[] Points = null;            // gesture points (normalized)

        [SerializeField]
        public string Name = "";                 // gesture class
        private const int SAMPLING_RESOLUTION = 32;

        public Gesture()
        {

        }

        /// <summary>
        /// Constructs a gesture from an array of points
        /// </summary>
        /// <param name="points"></param>
        public Gesture(Point[] points, string gestureName = "")
        {
            this.Name = gestureName;

            // normalizes the array of points with respect to scale, origin, and number of points
            this.Points = Scale(points);
            this.Points = TranslateTo(Points, Centroid(Points));
            this.Points = Resample(Points, SAMPLING_RESOLUTION);
        }

        #region gesture pre-processing steps: scale normalization, translation to origin, and resampling

        /// <summary>
        /// Performs scale normalization with shape preservation into [0..1]x[0..1]
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private Point[] Scale(Point[] points)
        {
            float minx = float.MaxValue, miny = float.MaxValue, maxx = float.MinValue, maxy = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                if (minx > points[i].X) minx = points[i].X;
                if (miny > points[i].Y) miny = points[i].Y;
                if (maxx < points[i].X) maxx = points[i].X;
                if (maxy < points[i].Y) maxy = points[i].Y;
            }

            Point[] newPoints = new Point[points.Length];
            float scale = Math.Max(maxx - minx, maxy - miny);
            for (int i = 0; i < points.Length; i++)
                newPoints[i] = new Point((points[i].X - minx) / scale, (points[i].Y - miny) / scale, points[i].StrokeID);
            return newPoints;
        }

        /// <summary>
        /// Translates the array of points by p
        /// </summary>
        /// <param name="points"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Point[] TranslateTo(Point[] points, Point p)
        {
            Point[] newPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
                newPoints[i] = new Point(points[i].X - p.X, points[i].Y - p.Y, points[i].StrokeID);
            return newPoints;
        }

        /// <summary>
        /// Computes the centroid for an array of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private Point Centroid(Point[] points)
        {
            float cx = 0, cy = 0;
            for (int i = 0; i < points.Length; i++)
            {
                cx += points[i].X;
                cy += points[i].Y;
            }
            return new Point(cx / points.Length, cy / points.Length, 0);
        }

        /// <summary>
        /// Resamples the array of points into n equally-distanced points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public Point[] Resample(Point[] points, int n)
        {
            Point[] newPoints = new Point[n];
            newPoints[0] = new Point(points[0].X, points[0].Y, points[0].StrokeID);
            int numPoints = 1;

            float I = PathLength(points) / (n - 1); // computes interval length
            float D = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].StrokeID == points[i - 1].StrokeID)
                {
                    float d = Geometry.EuclideanDistance(points[i - 1], points[i]);
                    if (D + d >= I)
                    {
                        Point firstPoint = points[i - 1];
                        while (D + d >= I)
                        {
                            // add interpolated point
                            float t = Math.Min(Math.Max((I - D) / d, 0.0f), 1.0f);
                            if (float.IsNaN(t)) t = 0.5f;
                            newPoints[numPoints++] = new Point(
                                (1.0f - t) * firstPoint.X + t * points[i].X,
                                (1.0f - t) * firstPoint.Y + t * points[i].Y,
                                points[i].StrokeID
                            );

                            // update partial length
                            d = D + d - I;
                            D = 0;
                            firstPoint = newPoints[numPoints - 1];
                        }
                        D = d;
                    }
                    else D += d;
                }
            }

            if (numPoints == n - 1) // sometimes we fall a rounding-error short of adding the last point, so add it if so
                newPoints[numPoints++] = new Point(points[points.Length - 1].X, points[points.Length - 1].Y, points[points.Length - 1].StrokeID);
            return newPoints;
        }

        /// <summary>
        /// Computes the path length for an array of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private float PathLength(Point[] points)
        {
            float length = 0;
            for (int i = 1; i < points.Length; i++)
                if (points[i].StrokeID == points[i - 1].StrokeID)
                    length += Geometry.EuclideanDistance(points[i - 1], points[i]);
            return length;
        }

        #endregion
    }

    public class RecognizerResult
    {
        public string Class;
        public float Distance; 
    }

    public class PointCloudRecognizer
    {
        /// <summary>
        /// Main function of the $P recognizer.
        /// Classifies a candidate gesture against a set of training samples.
        /// Returns the class of the closest neighbor in the training set.
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="trainingSet"></param>
        /// <returns></returns>
        public static RecognizerResult Classify(Gesture candidate, Gesture[] trainingSet)
        {
            float minDistance = float.MaxValue;
            string gestureClass = "";
            foreach (Gesture template in trainingSet)
            {
                float dist = GreedyCloudMatch(candidate.Points, template.Points);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    gestureClass = template.Name;
                }
            }
            return new RecognizerResult() { Class = gestureClass, Distance = minDistance };
        }

        /// <summary>6
        /// Implements greedy search for a minimum-distance matching between two point clouds
        /// </summary>
        /// <param name="points1"></param>
        /// <param name="points2"></param>
        /// <returns></returns>
        private static float GreedyCloudMatch(Point[] points1, Point[] points2)
        {
            int n = points1.Length; // the two clouds should have the same number of points by now
            float eps = 0.5f;       // controls the number of greedy search trials (eps is in [0..1])
            int step = (int)Math.Floor(Math.Pow(n, 1.0f - eps));
            float minDistance = float.MaxValue;
            for (int i = 0; i < n; i += step)
            {
                float dist1 = CloudDistance(points1, points2, i);   // match points1 --> points2 starting with index point i
                float dist2 = CloudDistance(points2, points1, i);   // match points2 --> points1 starting with index point i
                minDistance = Math.Min(minDistance, Math.Min(dist1, dist2));
            }
            return minDistance;
        }

        /// <summary>
        /// Computes the distance between two point clouds by performing a minimum-distance greedy matching
        /// starting with point startIndex
        /// </summary>
        /// <param name="points1"></param>
        /// <param name="points2"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private static float CloudDistance(Point[] points1, Point[] points2, int startIndex)
        {
            int n = points1.Length;       // the two clouds should have the same number of points by now
            bool[] matched = new bool[n]; // matched[i] signals whether point i from the 2nd cloud has been already matched
            Array.Clear(matched, 0, n);   // no points are matched at the beginning

            float sum = 0;  // computes the sum of distances between matched points (i.e., the distance between the two clouds)
            int i = startIndex;
            do
            {
                int index = -1;
                float minDistance = float.MaxValue;
                for (int j = 0; j < n; j++)
                    if (!matched[j])
                    {
                        float dist = Geometry.SqrEuclideanDistance(points1[i], points2[j]);  // use squared Euclidean distance to save some processing time
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            index = j;
                        }
                    }
                matched[index] = true; // point index from the 2nd cloud is matched to point i from the 1st cloud
                float weight = 1.0f - ((i - startIndex + n) % n) / (1.0f * n);
                sum += weight * minDistance; // weight each distance with a confidence coefficient that decreases from 1 to 0
                i = (i + 1) % n;
            } while (i != startIndex);
            return sum;
        }
    }

    public class Geometry
    {
        /// <summary>
        /// Computes the Squared Euclidean Distance between two points in 2D
        /// </summary>
        public static float SqrEuclideanDistance(Point a, Point b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        /// <summary>
        /// Computes the Euclidean Distance between two points in 2D
        /// </summary>
        public static float EuclideanDistance(Point a, Point b)
        {
            return (float)Math.Sqrt(SqrEuclideanDistance(a, b));
        }
    }
}
