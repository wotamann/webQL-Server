using System;
    
    public static class Extensions{

        // String Extension Truncate
        public static string Truncate(this string source, int length)
        {
            int l = Math.Max(length / 2 - 1, 2);
            return source.Length > length ? source.Substring(0, l) + " ... " + source.Substring(source.Length - l, l) : source;

        }

    }
