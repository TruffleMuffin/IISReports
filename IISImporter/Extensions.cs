using System.Collections.Generic;

namespace IISImporter
{
    static class Extensions
    {
        public static IEnumerable<IList<T>> ChunksOf<T>(this IEnumerable<T> sequence, int size)
        {
            var chunk = new List<T>(size);

            foreach (T element in sequence)
            {
                chunk.Add(element);
                if (chunk.Count == size)
                {
                    yield return chunk;
                    chunk = new List<T>(size);
                }
            }

            yield return chunk;
        }
    }
}