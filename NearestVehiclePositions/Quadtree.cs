using System;
using System.Collections.Generic;

namespace NearestVehiclePositions
{
    internal class Quadtree
    {
        public class Rectangle
        {
            public float MinX { get; }
            public float MinY { get; }
            public float MaxX { get; }
            public float MaxY { get; }

            public Rectangle(float minX, float minY, float maxX, float maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public bool Contains(Point point)
            {
                return point.Latitude >= MinX && point.Latitude <= MaxX &&
                       point.Longitude >= MinY && point.Longitude <= MaxY;
            }
        }

        public class Point
        {
            public float Latitude { get; }
            public float Longitude { get; }

            public Point(float latitude, float longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }
        }

        public class VehiclePosition
        {
            public int VehicleId { get; set; }
            public string VehicleRegistration { get; set; }
            public Point Position { get; set; }
            public ulong RecordedTimeUTC { get; set; }
        }

        // Quadtree implementation
        private readonly int _nodeCapacity;
        private readonly Rectangle _bounds;
        private readonly List<VehiclePosition> _positions;
        private Quadtree _northWest;
        private Quadtree _northEast;
        private Quadtree _southWest;
        private Quadtree _southEast;

        public Quadtree(Rectangle bounds, int nodeCapacity = 4)
        {
            _bounds = bounds;
            _nodeCapacity = nodeCapacity;
            _positions = new List<VehiclePosition>();
            _northWest = null;
            _northEast = null;
            _southWest = null;
            _southEast = null;
        }

        public void Insert(VehiclePosition vehiclePosition)
        {
            // If the point is not within the Quadtree bounds, ignore it
            if (!_bounds.Contains(vehiclePosition.Position))
                return;

            // If the node has capacity, add the position to the current node
            if (_positions.Count < _nodeCapacity)
            {
                _positions.Add(vehiclePosition);
            }
            else
            {
                // Subdivide the current node if not already divided
                if (_northWest == null)
                    Subdivide();

                // Insert the position into the appropriate sub-node
                if (_northWest._bounds.Contains(vehiclePosition.Position))
                    _northWest.Insert(vehiclePosition);
                else if (_northEast._bounds.Contains(vehiclePosition.Position))
                    _northEast.Insert(vehiclePosition);
                else if (_southWest._bounds.Contains(vehiclePosition.Position))
                    _southWest.Insert(vehiclePosition);
                else if (_southEast._bounds.Contains(vehiclePosition.Position))
                    _southEast.Insert(vehiclePosition);
            }
        }

        public VehiclePosition FindNearest(Point targetPosition)
        {
            float nearestDistanceSquared = float.MaxValue;
            VehiclePosition nearestVehicle = null;

            // Start with the root node
            FindNearestRecursive(this, targetPosition, ref nearestDistanceSquared, ref nearestVehicle);

            return nearestVehicle;
        }

        private void FindNearestRecursive(Quadtree node, Point targetPosition, ref float nearestDistanceSquared, ref VehiclePosition nearestVehicle)
        {
            foreach (var vehicle in node._positions)
            {
                float distanceSquared = CalculateDistanceSquared(vehicle.Position, targetPosition);
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestVehicle = vehicle;
                }
            }

            // Calculate the distance from the target point to the bounds of each sub-node
            var distanceToNorthWest = CalculateDistanceSquared(node._northWest?._bounds, targetPosition);
            var distanceToNorthEast = CalculateDistanceSquared(node._northEast?._bounds, targetPosition);
            var distanceToSouthWest = CalculateDistanceSquared(node._southWest?._bounds, targetPosition);
            var distanceToSouthEast = CalculateDistanceSquared(node._southEast?._bounds, targetPosition);

            // Recursively search the sub-nodes that are closer to the target point first
            if (node._northWest != null && distanceToNorthWest < nearestDistanceSquared)
                FindNearestRecursive(node._northWest, targetPosition, ref nearestDistanceSquared, ref nearestVehicle);

            if (node._northEast != null && distanceToNorthEast < nearestDistanceSquared)
                FindNearestRecursive(node._northEast, targetPosition, ref nearestDistanceSquared, ref nearestVehicle);

            if (node._southWest != null && distanceToSouthWest < nearestDistanceSquared)
                FindNearestRecursive(node._southWest, targetPosition, ref nearestDistanceSquared, ref nearestVehicle);

            if (node._southEast != null && distanceToSouthEast < nearestDistanceSquared)
                FindNearestRecursive(node._southEast, targetPosition, ref nearestDistanceSquared, ref nearestVehicle);
        }

        private void Subdivide()
        {
            float xMid = (_bounds.MinX + _bounds.MaxX) / 2f;
            float yMid = (_bounds.MinY + _bounds.MaxY) / 2f;

            _northWest = new Quadtree(new Rectangle(_bounds.MinX, _bounds.MinY, xMid, yMid), _nodeCapacity);
            _northEast = new Quadtree(new Rectangle(xMid, _bounds.MinY, _bounds.MaxX, yMid), _nodeCapacity);
            _southWest = new Quadtree(new Rectangle(_bounds.MinX, yMid, xMid, _bounds.MaxY), _nodeCapacity);
            _southEast = new Quadtree(new Rectangle(xMid, yMid, _bounds.MaxX, _bounds.MaxY), _nodeCapacity);
        }

        private float CalculateDistanceSquared(Point p1, Point p2)
        {
            float latDiff = p2.Latitude - p1.Latitude;
            float lonDiff = p2.Longitude - p1.Longitude;
            return (latDiff * latDiff) + (lonDiff * lonDiff);
        }

        private float CalculateDistanceSquared(Rectangle bounds, Point point)
        {
            float dx = Math.Max(Math.Abs(point.Latitude - bounds.MinX), Math.Abs(point.Latitude - bounds.MaxX));
            float dy = Math.Max(Math.Abs(point.Longitude - bounds.MinY), Math.Abs(point.Longitude - bounds.MaxY));
            return (dx * dx) + (dy * dy);
        }
    }
}
