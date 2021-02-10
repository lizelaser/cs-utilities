using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Lizelaser0310.Utilities
{
    public class MeiliResponse<T> where T : class
    {
        public List<T> Hits { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int NbHits { get; set; }
        public bool ExhaustiveNbHits { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string Query { get; set; }
    }

    public class MeiliBody
    {
        public string Q { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
    }
}