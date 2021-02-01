using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Lizelaser0310.Utilities
{
    public class ImagenUtilidad
    {
        private const string WwwPathSlice = "wwwroot";
        private const string ImagePathSlice = "imagen";

        public static string ObtenerRuta(HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}/{ImagePathSlice}/";
        }

        /// <exception cref="ImagenUtilidadException" />
        public static string GuardarImagen(string leftPath, string base64String)
        {
            try
            {
                using MemoryStream ms = new MemoryStream(Convert.FromBase64String(base64String));
                using Bitmap bm2 = new Bitmap(ms);

                Guid uuid = System.Guid.NewGuid();
                string filePath = uuid.ToString() + ".jpg";

                string _dirPath = Path.Join(leftPath.AsSpan(), WwwPathSlice.AsSpan(), ImagePathSlice.AsSpan());
                string dirPath = Path.Join(_dirPath, filePath.AsSpan());

                bm2.Save(dirPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                return filePath;
            }
            catch (Exception e)
            {
                throw new ImagenUtilidadException(e.Message);
            }
        }

        public static void CrearImagenUrl<T>(T item, HttpRequest request, string propName = "Imagen")
        {
            PropertyInfo prop = item.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

            if (prop is null || !prop.CanWrite) return;

            string imagen = (string)prop.GetValue(item);
            string url = $"{request.Scheme}://{request.Host}/{ImagePathSlice}/{imagen}";

            prop.SetValue(item, url, null);
        }

        public static void CrearImagenUrls<T>(IEnumerable<T> items, HttpRequest request)
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
    public class ImagenUtilidadException : System.Exception
    {
        public ImagenUtilidadException() : base() { }
        public ImagenUtilidadException(string message) : base(message) { }
        public ImagenUtilidadException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected ImagenUtilidadException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
