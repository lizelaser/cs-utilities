﻿using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Lizelaser0310.Utilities
{
    public class Paginator<T> where T : class
    {
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; }
    }
}
