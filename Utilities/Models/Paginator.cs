using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Lizelaser0310.Utilities
{
    public class Paginator<T> where T : class
    {
        public Paginator<dynamic> ToDynamic()
        {
            return new Paginator<dynamic>()
            {
                CurrentPage = CurrentPage,
                ItemsPerPage = ItemsPerPage,
                TotalItems = TotalItems,
                TotalPages = TotalPages,
                Items = Items as List<dynamic>
            };
        }
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; }
    }
}
