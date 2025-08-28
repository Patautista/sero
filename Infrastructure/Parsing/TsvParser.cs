using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Parsing
{
    public class TsvReader
    {
        /// <summary>
        /// Reads a TSV file and returns its contents as a 2D string array (string[][]).
        /// </summary>
        /// <param name="filePath">The path to the TSV file.</param>
        /// <returns>A jagged array of strings containing the file's data.</returns>
        public static string[][] ReadFile(string filePath, char divider = '\t')
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' was not found.");

            var rows = new List<string[]>();

            foreach (var line in File.ReadLines(filePath))
            {
                // Split on tab character
                var columns = line.Split(divider);
                rows.Add(columns);
            }

            return rows.ToArray();
        }
    }
}
