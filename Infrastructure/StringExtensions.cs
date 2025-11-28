using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return $"{char.ToUpper(input[0])}{input[1..]}";
        }

        /// <summary>
        /// Removes common punctuation marks from the string
        /// </summary>
        /// <param name="input">The string to clean</param>
        /// <returns>String without punctuation marks (?, !, ., ,, ;, :, etc.)</returns>
        public static string RemovePunctuation(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            
            // Remove common punctuation marks
            return Regex.Replace(input, @"[?!.,;:\-\(\)\[\]\{\}""'`]", string.Empty);
        }
    }
}
