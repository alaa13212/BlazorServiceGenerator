namespace Remal.BlazorServiceGenerator.Extensions;

public static class StringExtensions
{
    public static string LowerFirstLetter(this string value) => char.ToLowerInvariant(value[0]) + value.Substring(1);
    public static string UpperFirstLetter(this string value) => char.ToUpperInvariant(value[0]) + value.Substring(1);
}