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
        private const string WwwPathSlice = "wwwroot";
        private const string ImagePathSlice = "imagen";

        public static string GetPath(HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}/{ImagePathSlice}/";
        }

        /// <exception cref="ImageUtilityException" />
        public static string SaveImage(string leftPath, string base64String)
        {
            try
            {
                using MemoryStream ms = new MemoryStream(Convert.FromBase64String(base64String));
                using Bitmap bm2 = new Bitmap(ms);

                Guid uuid = System.Guid.NewGuid();
                string filePath = uuid.ToString() + ".jpg";

                string wwwPath = Path.Join(leftPath.AsSpan(), WwwPathSlice.AsSpan(), ImagePathSlice.AsSpan());
                string dirPath = Path.Join(wwwPath, filePath.AsSpan());

                bm2.Save(dirPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                return filePath;
            }
            catch (Exception e)
            {
                throw new ImageUtilityException(e.Message);
            }
        }

        public static void CreateImageUrl<T>(T item, HttpRequest request, string propName = "Imagen")
        {
            PropertyInfo prop = item.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

            if (prop is null || !prop.CanWrite) return;

            string imagen = (string)prop.GetValue(item);
            string url = $"{request.Scheme}://{request.Host}/{ImagePathSlice}/{imagen}";

            prop.SetValue(item, url, null);
        }

        public static void CreateImageUrls<T>(IEnumerable<T> items, HttpRequest request)
        {
            string left = $"{request.Scheme}://{request.Host}/{ImagePathSlice}/";

            foreach (T item in items)
            {
                PropertyInfo prop = item.GetType().GetProperty("Imagen", BindingFlags.Public | BindingFlags.Instance);

                if (prop is null || !prop.CanWrite) continue;

                string imagen = (string)prop.GetValue(item);
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
