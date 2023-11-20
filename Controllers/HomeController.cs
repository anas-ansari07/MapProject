using DotSpatial.Data;
using DotSpatial.Projections;
using MapProject.Models;
using Microsoft.AspNetCore.Mvc;
using OSGeo.OSR;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using ExifLib;
using Microsoft.AspNetCore.Identity;




//using JsonResult = Microsoft.AspNetCore.Mvc.JsonResult;

namespace MapProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        List<CoordinatesModel> ls = new List<CoordinatesModel>();
        List<CoordinatesModel> ls1 = new List<CoordinatesModel>();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult ArcGIS()
        {
            return View();
        }

        public IActionResult Index()
        {
            var points = new List<CoordinatesModel>
            {
                new CoordinatesModel { Name = "abc", UserId = "user1", CId ="0", Latitude = 529558.0, Longitude = 173511.0},
                new CoordinatesModel { Name = "def", UserId = "0", CId ="user1", Latitude = 389168.0, Longitude = 807306.0},
                new CoordinatesModel { Name = "efg", UserId = "0", CId ="user1", Latitude = 264444.0, Longitude = 869745.0}
            };

            for(int i=0;i<points.Count; i++)
            {
                ls1 = ConversionCoords(points[i].Latitude , points[i].Longitude, points[i].UserId, points[i].CId, points[i].Name);
            }

            AddLatLngToImage();
            GetCoordinatesImage();
           // GeoreferenceMinMax(28.514,77.377,1280,640);
            return View(ls1);
        }


        public List<CoordinatesModel> ConversionCoords(double sourceX, double sourceY,string userId, string cId,string name)
        {

            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();
            SpatialReference srs = new SpatialReference(null);
            srs.ImportFromEPSG(27700);
            SpatialReference targetSRS = new SpatialReference("");
            targetSRS.ImportFromEPSG(4326);
            CoordinateTransformation transform = new CoordinateTransformation(srs, targetSRS);

            double[] transformedCoords = new double[3];
            transform.TransformPoint(transformedCoords, sourceX, sourceY, 0);

            double targetX = transformedCoords[0];
            double targetY = transformedCoords[1];
            ls.Add(new CoordinatesModel { Latitude = targetX, Longitude = targetY, UserId = userId, CId = cId,Name = name});
            return ls;
        }

        public IActionResult Dashboard()
        {
            var points = new List<CoordinatesModel>
            {
                new CoordinatesModel { UserId = "user1", Latitude = 51.445777, Longitude = -0.137096},
                new CoordinatesModel { UserId = "user2", Latitude = 57.156493, Longitude = -2.180694},
                new CoordinatesModel { UserId = "user1", Latitude = 57.697023, Longitude = -4.275991}
            };

           // GeoreferenceMinMax(28.514390, 77.377583, 1280, 720);
            return View(points);          
        }

        // Finding the miny minx maxx maxy coordinate
        public void GeoreferenceMinMax(double lat, double lng, double height, double width)
        {
            lat = 28.514; lng = 77.377; height = 1280; width = 720;
            double halfWidth = width / 2.0;
            double halfHeight = height / 2.0;
            //double PixelLatitude = 0.00001;
            double radius = 6371.0;
            //double cos = Math.Cos(lat * Math.PI / 180.0);
            //double PixelLongitude = 360.0 / (2 * Math.PI * radius * cos) * (1.0 / width);

            double latitudeCircumference = 2.0 * Math.PI * radius * Math.Cos(lat * Math.PI / 180.0);

            // Calculate the pixel width in degrees (longitude)
            double PixelLongitude = 360.0 / width;

            // Calculate the pixel height in degrees (latitude)
            double PixelLatitude = latitudeCircumference / height;

            double minX = lng - (halfWidth * PixelLongitude);
            double minY = lat - (halfHeight * PixelLatitude);
            double maxX = lng + (halfWidth * PixelLongitude);
            double maxY = lat + (halfHeight * PixelLatitude);

            Console.WriteLine(minY);
            Console.WriteLine(maxY);
            Console.WriteLine(minX);
            Console.WriteLine(maxX);
        }

        // adding latitude and longitude to an image 
        public void AddLatLngToImage()
        {
            string path = @"C:\Users\anasp\OneDrive\Pictures\Camera Roll\image.jpg";
            double lat = 28.514;
            double lng = 77.377;
        
            try
            {
                using (Bitmap bmp = new Bitmap(path))
                {
                    // Create PropertyItems for latitude and longitude
                    PropertyItem latitudeProperty = bmp.PropertyItems[0x0002]; // 0x0002 latitude
                    PropertyItem longitudeProperty = bmp.PropertyItems[0x0004]; // 0x0004 longitude

                    // Set latitude and longitude properties
                    latitudeProperty.Type = 5;
                    latitudeProperty.Value = BitConverter.GetBytes(Convert.ToDouble(lat));

                    longitudeProperty.Type = 5;
                    longitudeProperty.Value = BitConverter.GetBytes(Convert.ToDouble(lng));

                    // Save 
                    bmp.SetPropertyItem(latitudeProperty);
                    bmp.SetPropertyItem(longitudeProperty);

                    bmp.Save(@"C:\Users\anasp\OneDrive\Pictures\Camera Roll\updatedimage.jpg", ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static PropertyItem CreatePropertyItem(byte[] img, int id, double value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value.ToString());

            PropertyItem property = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            property.Id = id;
            property.Type = 2;
            property.Len = data.Length;
            property.Value = new byte[data.Length];
            data.CopyTo(property.Value, 0);
            return property;
        }


        // Getting coordinates for the image 
        public void GetCoordinatesImage()
        {
            //Using ExifLib - Initiating reader
            using (ExifReader reader = new ExifReader(@"C:\Users\anasp\OneDrive\Pictures\Camera Roll\image2.jpg"))
            {
                // Creating array to store data
                double[] GpsLongArray;
                double[] GpsLatArray;
                double GpsLongDouble;
                double GpsLatDouble;

                //Checking for image existing Lat & Long
            if (reader.GetTagValue(ExifTags.GPSLatitude, out GpsLatArray) &&
                reader.GetTagValue(ExifTags.GPSLongitude, out GpsLongArray))
                {
                    // Fetching Latitude and Longitude
                    GpsLongDouble = GpsLongArray[0] + GpsLongArray[1] / 60.0 + GpsLongArray[2] / 3600.0;
                    GpsLatDouble = GpsLatArray[0] + GpsLatArray[1] / 60.0 + GpsLatArray[2] / 3600.0;
                    ViewBag.latitude = GpsLatDouble;
                    ViewBag.longitude = GpsLongDouble;
                } 
            }           
        }

        public IActionResult Yahoo()
        {
            return View();
        }

        public IActionResult Here()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}