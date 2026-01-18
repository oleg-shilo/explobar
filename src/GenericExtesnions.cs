using System;
using static System.Environment;
using System.IO;

namespace Explobar
{
    static class GenericExtesnions
    {
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        public static bool HasText(this string text) => !string.IsNullOrEmpty(text);

        public static string GetPath(this SpecialFolder folder) => Environment.GetFolderPath(folder);

        public static string Combine(this SpecialFolder folder, string path, params string[] paths)
            => Path.Combine(Environment.GetFolderPath(folder), path, Path.Combine(paths));
    }
}