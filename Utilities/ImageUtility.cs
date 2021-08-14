using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Lizelaser0310.Utilities
{
    public static class ImageUtility
    {

        public static string GetPath(HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}/";
        }

        /// <exception cref="ImageUtilityException" />
        public static string SaveImage(string basePath, string base64String, string fileName=null, string imagePathSlice = "Images")
        {
            if (basePath==null || base64String==null)
            {
                return null;
            }
            
            try
            {
                using var ms = new MemoryStream(Convert.FromBase64String(base64String));
                using var bm2 = new Bitmap(ms);

                var filePath = $"{fileName??Guid.NewGuid().ToString()}.jpg";

                var dirPath = Path.Join(basePath.AsSpan(), imagePathSlice.AsSpan(), filePath.AsSpan());

                new FileInfo(dirPath).Directory?.Create();

                bm2.Save(dirPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                return filePath;
            }
            catch (Exception e)
            {
                throw new ImageUtilityException(e.Message);
            }
        }

        public static void CreateImageUrl<T>(T item, HttpRequest request, string propName = "Imagen", string imagePathSlice="Images")
        {
            PropertyInfo prop = item.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

            if (prop is null || !prop.CanWrite) return;

            string imagen = (string)prop.GetValue(item);

            if (imagen == null)
            {
                return;
            }
            
            string url = $"{request.Scheme}://{request.Host}/{imagePathSlice}/{imagen}";

            prop.SetValue(item, url, null);
        }

        public static void CreateImageUrls<T>(IEnumerable<T> items, HttpRequest request, string imagePathSlice = "Images")
        {
            string left = $"{request.Scheme}://{request.Host}/{imagePathSlice}/";

            foreach (T item in items)
            {
                PropertyInfo prop = item.GetType().GetProperty("Imagen", BindingFlags.Public | BindingFlags.Instance);

                if (prop is null || !prop.CanWrite) continue;

                string imagen = (string)prop.GetValue(item);

                if (imagen == null)
                {
                    continue;
                }
                string url = $"{left}{imagen}";

                prop.SetValue(item, url, null);
            }
        }
    }

    [Serializable()]
    public class ImageUtilityException : Exception
    {
        public ImageUtilityException() : base() { }
        public ImageUtilityException(string message) : base(message) { }
        public ImageUtilityException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected ImageUtilityException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
