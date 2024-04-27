using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Squircle.Blazor;

public static class SquirclePathGenerator {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetSquirclePath(double w, double h, double r1, double r2) {
        r1 = Math.Min(r1, r2);
        var path = $"""
         M 0,{r2}
         C 0,{r1} {r1},0 {r2},0
         L {w - r2},0
         C {w - r1},0 {w},{r1} {w},{r2}
         L {w},{h - r2}
         C {w},{h - r1} {w - r1},{h} {w - r2},{h}
         L {r2},{h}
         C {r1},{h} 0,{h - r1} 0,{h - r2}
         L 0,{r2}
         """;

        return path.Trim().Replace('\n', ' ');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetSquirclePathAsDataUri(double w, double h, double r1, double r2) {
        var id = $"squircle-{w}-{h}-{r1}-{r2}";
        var path = GetSquirclePath(w, h, r1, r2);
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">
                <defs>
                    <clipPath id="{id}"><path fill="#000" d="{path}"/></clipPath>
                </defs>
                <g clip-path="url(#{id})">
                    <rect width="{w}" height="{h}" fill="#000"/>
                </g>
            </svg>
            """;

        svg = svg.Trim().Replace("\n", "").Replace(" {2,}", "");

        var data1 = svg.Replace('"', '\'')
                   .Replace(">\\s+<", "><")
                   .Replace("\\s{2,}", " ");

        // Encode special characters
        var data2 = Regex.Replace(data1, @"[\r\n%#()<>?[\\\]^\`{|}]", m => Uri.EscapeDataString(m.Value));


        return $"data:image/svg+xml,{data2}";
    }
}