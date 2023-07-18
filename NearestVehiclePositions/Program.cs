using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace NearestVehiclePositions
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading data and processing... Please wait.");
            Console.WriteLine();
            Console.WriteLine();

            string filePath = "VehiclePositions.dat";

            // Creating the bounds for the Quadtree
            Quadtree.Rectangle quadtreeBounds = new Quadtree.Rectangle(-90, -180, 90, 180);

            // Creating the Quadtree instance with the specified bounds
            Quadtree quadtree = new Quadtree(quadtreeBounds);

            // Loading vehicle positions from the binary file and insert into the Quadtree
            LoadAndInsertVehiclePositions(filePath, quadtree);

            // The 10 coordinates to find the nearest vehicle positions
            List<Quadtree.Point> coordinates = new List<Quadtree.Point>
            {
                new Quadtree.Point(34.544909f, -102.100843f),
                new Quadtree.Point(32.345544f, -99.123124f),
                new Quadtree.Point(33.234235f, -100.214124f),
                new Quadtree.Point(35.195739f, -95.348899f),
                new Quadtree.Point(31.895839f, -97.789573f),
                new Quadtree.Point(32.895839f, -101.789573f),
                new Quadtree.Point(34.115839f, -100.225732f),
                new Quadtree.Point(32.335839f, -99.992232f),
                new Quadtree.Point(33.535339f, -94.792232f),
                new Quadtree.Point(32.234235f, -100.222222f)
            };

            // Finding the nearest vehicle positions for the given coordinates
            foreach (var coordinate in coordinates)
            {
                var nearestPosition = quadtree.FindNearest(coordinate);

                if (nearestPosition != null)
                {
                    Console.WriteLine($"Nearest vehicle position to target point ({coordinate.Latitude}, {coordinate.Longitude}):");
                    Console.WriteLine($"Vehicle ID: {nearestPosition.VehicleId}");
                    Console.WriteLine($"Vehicle Registration: {nearestPosition.VehicleRegistration}");
                    Console.WriteLine($"Latitude: {nearestPosition.Position.Latitude}");
                    Console.WriteLine($"Longitude: {nearestPosition.Position.Longitude}");
                    Console.WriteLine();
                }
            }

            // Await user input before closing the console
            Console.ReadLine();
        }

        static void LoadAndInsertVehiclePositions(string filePath, Quadtree quadtree)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4]; // Buffer for reading 4-byte chunks (Int32)
                byte[] floatBuffer = new byte[4]; // Buffer for reading 4-byte chunks (Float)
                byte[] ulongBuffer = new byte[8]; // Buffer for reading 8-byte chunks (UInt64)

                while (fileStream.Read(buffer, 0, 4) == 4) // Read 4 bytes for VehicleId
                {
                    int vehicleId = BitConverter.ToInt32(buffer, 0);

                    // Read and interpret the VehicleRegistration as a null-terminated ASCII string
                    StringBuilder registrationBuilder = new StringBuilder();
                    byte registrationByte;
                    while ((registrationByte = (byte)fileStream.ReadByte()) != 0)
                    {
                        registrationBuilder.Append((char)registrationByte);
                    }
                    string vehicleRegistration = registrationBuilder.ToString();

                    // Read 4 bytes for Latitude (float)
                    fileStream.Read(floatBuffer, 0, 4);
                    float latitude = BitConverter.ToSingle(floatBuffer, 0);

                    // Read 4 bytes for Longitude (float)
                    fileStream.Read(floatBuffer, 0, 4);
                    float longitude = BitConverter.ToSingle(floatBuffer, 0);

                    // Read 8 bytes for RecordedTimeUTC (ulong)
                    fileStream.Read(ulongBuffer, 0, 8);
                    ulong recordedTimeUTC = BitConverter.ToUInt64(ulongBuffer, 0);

                    Quadtree.Point position = new Quadtree.Point(latitude, longitude);

                    Quadtree.VehiclePosition vehiclePosition = new Quadtree.VehiclePosition
                    {
                        VehicleId = vehicleId,
                        VehicleRegistration = vehicleRegistration,
                        Position = position,
                        RecordedTimeUTC = recordedTimeUTC
                    };

                    quadtree.Insert(vehiclePosition);
                }
            }
        }
    }
}
