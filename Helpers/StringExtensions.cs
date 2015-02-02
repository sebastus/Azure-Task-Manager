using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a UTF-8 string to a Base-64 version of the string.
        /// </summary>
        /// <param name="s">The string to convert to Base-64.</param>
        /// <returns>The Base-64 converted string.</returns>
        public static string ToBase64(this string s)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Converts a Base-64 encoded string to UTF-8.
        /// </summary>
        /// <param name="s">The string to convert from Base-64.</param>
        /// <returns>The converted UTF-8 string.</returns>
        public static string FromBase64(this string s)
        {
            byte[] bytes = Convert.FromBase64String(s);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
