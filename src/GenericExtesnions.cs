using System.IO;

namespace Explobar
{
    static class GenericExtesnions
    {
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        public static bool HasText(this string text) => !string.IsNullOrEmpty(text);
    }
}