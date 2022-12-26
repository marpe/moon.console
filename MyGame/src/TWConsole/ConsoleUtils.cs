using System.Globalization;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MyGame.TWConsole;

public static class ConsoleUtils
{
    private static readonly Dictionary<string, bool> _boolLookup = new(StringComparer.InvariantCultureIgnoreCase)
    {
        { "true", true },
        { "1", true },
        { "false", false },
        { "0", false },
    };

    private static readonly Dictionary<Type, string> _typeDisplayNames = new()
    {
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(string), "string" },
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(uint), "uint" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(long), "decimal" },
        { typeof(ulong), "ulong" },
        { typeof(char), "char" },
        { typeof(object), "object" },
        { typeof(Color), "color" },
        { typeof(Point), "point" },
        { typeof(Vector2), "vector2" },
    };

    private static bool ParseBool(ReadOnlySpan<char> str)
    {
        var key = str.ToString();
        if (!_boolLookup.ContainsKey(key))
            throw new InvalidOperationException($"Cannot parse '{key}' to a bool.");
        return _boolLookup[key];
    }

    public static string GetDisplayName(Type type)
    {
        if (_typeDisplayNames.ContainsKey(type))
        {
            return _typeDisplayNames[type];
        }

        if (type.DeclaringType != null)
        {
            return type.DeclaringType.Name + "." + type.Name;
        }

        return type.Name;
    }

    public static object ParseArg(Type t, ReadOnlySpan<char> strValue)
    {
        var underlyingType = Nullable.GetUnderlyingType(t);
        if (underlyingType != null)
        {
            var parsedArg = ParseArg(underlyingType, strValue);
            return Activator.CreateInstance(t, parsedArg)!;
        }
        if (t == typeof(string)) return strValue.ToString();
        if (t.IsEnum) return Enum.Parse(t, strValue, true);
        if (t == typeof(int)) return int.Parse(strValue);
        if (t == typeof(float)) return float.Parse(strValue);
        if (t == typeof(decimal)) return decimal.Parse(strValue);
        if (t == typeof(double)) return double.Parse(strValue);
        if (t == typeof(byte)) return byte.Parse(strValue);
        if (t == typeof(sbyte)) return sbyte.Parse(strValue);
        if (t == typeof(uint)) return uint.Parse(strValue);
        if (t == typeof(short)) return short.Parse(strValue);
        if (t == typeof(ushort)) return ushort.Parse(strValue);
        if (t == typeof(long)) return long.Parse(strValue);
        if (t == typeof(ulong)) return ulong.Parse(strValue);
        if (t == typeof(char)) return strValue[0];
        if (t == typeof(bool)) return ParseBool(strValue);
        if (t == typeof(Color)) return ParseColor(strValue);
        if (t == typeof(Vector2)) return ParseVector2(strValue);
        if (t == typeof(Point)) return ParsePoint(strValue);

        throw new InvalidOperationException($"Cannot parse {GetDisplayName(t)}.");
    }

    private static Point ParsePoint(string strValue)
    {
        var splitBy = strValue.Contains(',') ? ',' : ' ';
        var xy = strValue.Split(splitBy);
        var parsed = new[] { 0, 0 };
        for (var i = 0; i < xy.Length && i < 2; i++)
        {
            parsed[i] = int.Parse(xy[i]);
        }

        return new Point(parsed[0], parsed[1]);
    }

    private static Vector2 ParseVector2(ReadOnlySpan<char> strValues)
    {
        var splitBy = strValues.Contains(',') ? ',' : ' ';
        var splitAt = strValues.IndexOf(splitBy);

        var xSpan = strValues.Slice(0, splitAt);
        var ySpan = strValues.Slice(splitAt + 1);

        var x = float.Parse(xSpan);
        var y = float.Parse(ySpan);

        return new Vector2(x, y);
    }

    public static Point ParsePoint(ReadOnlySpan<char> strValue)
    {
        var splitBy = strValue.Contains(',') ? ',' : ' ';
        var splitAt = strValue.IndexOf(splitBy);

        var xSpan = strValue.Slice(0, splitAt);
        var ySpan = strValue.Slice(splitAt + 1);

        var x = int.Parse(xSpan);
        var y = int.Parse(ySpan);

        return new Point(x, y);
    }

    private static Color ParseColor(ReadOnlySpan<char> strValue)
    {
        Span<float> parsed = stackalloc[] { 1.0f, 1.0f, 1.0f, 1.0f };

        for (var i = 0; i < parsed.Length; i++)
        {
            var splitAt = strValue.IndexOfAny(' ', ',');
            if (splitAt == -1)
            {
                parsed[i] = float.Parse(strValue);
                break;
            }

            parsed[i] = float.Parse(strValue.Slice(0, splitAt));
            strValue = strValue.Slice(splitAt + 1);
        }

        return new Color(parsed[0], parsed[1], parsed[2], parsed[3]);
    }

    private static T ParsePrimitive<T>(string strValue)
    {
        return (T)Convert.ChangeType(strValue, typeof(T), CultureInfo.InvariantCulture);
    }

    private static object ParsePrimitive(Type type, string strValue)
    {
        return Convert.ChangeType(strValue, type, CultureInfo.InvariantCulture);
    }

    private static List<string> _tmpArgs = new();
    public static string[] SplitArgs(ReadOnlySpan<char> text)
    {
        _tmpArgs.Clear();
        var inQuotes = false;
        var splitStart = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (text[i] == ' ')
            {
                if (!inQuotes && i > 0 && text[i - 1] == ' ')
                {
                    // ignore whitespace between values
                    splitStart = i + 1;
                }
                else if (!inQuotes)
                {
                    // we ended an argument
                    var length = i - splitStart;
                    var arg = text.Slice(splitStart, length).ToString();
                    _tmpArgs.Add(arg);
                    splitStart = i + 1;
                }
            }
        }

        _tmpArgs.Add(text[splitStart..].ToString());

        for (var i = 0; i < _tmpArgs.Count; i++)
        {
            _tmpArgs[i] = _tmpArgs[i].Trim('"');
        }

        return _tmpArgs.ToArray();
    }

    public static string Colorize(object? value)
    {
        if (value == null)
            return "^8null";
        if (value is bool boolValue)
            return boolValue ? "^7true" : "^4false";
        if (value is string strValue)
            return $"\"{strValue}\"";

        return ConvertToString(value);
    }

    public static string ConvertToString<T>(T value)
    {
        if (value is null)
            return "null";

        if (value is string strValue)
            return strValue;

        if (value is Color c)
        {
            var r = c.R / 255f;
            var g = c.G / 255f;
            var b = c.B / 255f;
            var a = c.A / 255f;
            return $"{r:0.##} {g:0.##} {b:0.##} {a:0.##}";
        }

        if (value is Point p)
            return $"{p.X}, {p.Y}";

        if (value is Vector2 v)
            return $"{v.X}, {v.Y}";

        return Convert.ToString(value, CultureInfo.InvariantCulture)?.ToLower() ?? string.Empty;
    }
}
