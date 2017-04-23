using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VoxDraw
{
    public class PathDrawingContext
    {
        private List<PathPoint3> localPoints = new List<PathPoint3>(200);
        private readonly Transform root;
        private Vector3 cumulativeLocation = new Vector3();
        private PathPoint3 tail = default(PathPoint3);

        const float VelocityFilterWeight = 0.7f;

        public PathDrawingContext(Transform root)
        {
            this.root = root;
            this.StartTime = DateTime.UtcNow;
            this.AddPoint(root);
        }

        public Transform Root
        {
            get
            {
                return root;
            }
        }

        public DateTime StartTime
        {
            get;
            private set;
        }

        public PathPoint3 Tail
        {
            get
            {
                return this.tail;
            }
        }

        public Bounds LocalBounds
        {
            get
            {
                var b = new Bounds(this.LocalCenter, Vector3.zero);

                foreach (var p in this.Positions)
                {
                    b.Encapsulate(p);
                }

                return b;
            }
        }

        public Vector3 LocalCenter
        {
            get
            {
                return cumulativeLocation / this.localPoints.Count;
            }
        }

        public Vector3 WorldCenter
        {
            get
            {
                return this.root.TransformPoint(this.LocalCenter);
            }
        }

        public Bounds WorldBounds
        {
            get
            {
                return new Bounds(this.WorldCenter, this.LocalBounds.size);
            }
        }

        public void AddPoint(Vector3 position, Quaternion rotation, float pressure, bool isWorldSpace = true)
        {
            if (isWorldSpace)
            {
                // Transform to local space.
                position = this.root.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(rotation) * this.root.rotation;
            }

            if (position == this.tail.localPosition)
            {
                return; // Exactly on top
            }

            Debug.LogFormat("Add point: {0} {1}", position, this.localPoints.Count);

            long timeMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            var item = new PathPoint3(position, rotation, timeMs, pressure);


            item.SetVelocity(this.tail);
            // Low pass filter for velocity 
            item.velocity = VelocityFilterWeight * item.velocity + (1f - VelocityFilterWeight) * this.tail.velocity;

            localPoints.Add(item);

            cumulativeLocation += position;
            tail = item;
        }

        public void AddPoint(Transform source, float pressure = 1f)
        {
            this.AddPoint(source.position, source.rotation, pressure, true);
        }

        public IEnumerable<PathPoint3> Points
        {
            get
            {
                return localPoints;
            }
        }

        public IEnumerable<Vector3> Positions
        {
            get
            {
                return this.localPoints.Select(p => p.localPosition);
            }
        }

        public IEnumerable<Vector3> WorldPositions
        {
            get
            {
                return this.localPoints.Select(p => this.Root.TransformPoint(p.localPosition));
            }
        }

        public IEnumerable<PathPoint2> ConvertTo2d(/* TODO: take in some root rotation */)
        {
            return this.Points.Select(p => p.To2d());
        }

        public int NumberPoints
        {
            get { return this.localPoints.Count; }
        }
    }

    public struct PathPoint3
    {
        public PathPoint3(Vector3 position, Quaternion rotation, long timestamp, float pressure)
        {
            this.localPosition = position;
            this.rotation = rotation;
            this.pressure = pressure;
            this.timestamp = timestamp;
            this.velocity = 0f;
        }

        public Vector3 localPosition;
        public Quaternion rotation;
        public long timestamp; // TODO: Could likely get away with a short 
        public float pressure;
        public float velocity;

        public float VelocityTo(Vector3 other, long timeDelta)
        {
            return Vector3.Distance(this.localPosition, other) / (float)(timeDelta - this.timestamp);
        }

        public float VelocityFrom(Vector3 other, long timeDelta)
        {
            return Vector3.Distance(this.localPosition, other) / (float)(this.timestamp - timeDelta);
        }

        public void SetVelocity(PathPoint3 prior)
        {
            this.velocity = prior.VelocityTo(prior.localPosition, this.timestamp);
        }

        public PathPoint2 To2d()
        {
            return new PathPoint2()
            {
                timestamp = timestamp,
                localPosition = new Vector2(this.localPosition.x, this.localPosition.y),
                pressure = this.pressure,
                velocity = this.velocity // TODO: Not correct
            };
        }
    }

    public struct PathPoint2
    {

        public Vector2 localPosition;
        public long timestamp; // TODO: Could likely get away with a short 
        public float pressure;
        public float velocity;

        internal float Distance(PathPoint2 p2)
        {
            return Dist(this, p2);
        }

        private float Dist(PathPoint2 p1, PathPoint2 p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public float X
        {
            get { return localPosition.x; }
        }

        public float Y
        {
            get { return localPosition.y; }
        }

        public PathPoint2 Clone(Vector2? position = null, long? timestamp = null, float? pressure = null, float? velocity = null)
        {
            var newItem = new PathPoint2();

            newItem.localPosition = position ?? this.localPosition;
            newItem.timestamp = timestamp ?? this.timestamp;
            newItem.pressure = pressure ?? this.pressure;
            newItem.velocity = velocity ?? this.velocity;

            return newItem;
        }

        public PathPoint2 Clone(float? x = null, float? y = null, long? timestamp = null, float? pressure = null, float? velocity = null)
        {
            Vector2 p;
            if(x != null && y != null)
            {
                p.x = x.Value;
                p.y = y.Value;

                return Clone(p, timestamp, pressure, velocity);
            }

            return Clone(null, timestamp, pressure, velocity);
        }
    }
}
