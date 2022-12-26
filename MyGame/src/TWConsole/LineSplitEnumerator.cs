namespace MyGame.TWConsole;

public readonly ref struct RefTextRun
{
    public readonly ReadOnlySpan<char> Text;
    public readonly byte Color;
    public readonly int X;
    public readonly int Y;

    public RefTextRun(ReadOnlySpan<char> text, byte color, int x, int y)
    {
        Color = color;
        Text = text;
        X = x;
        Y = y;
    }
}

public ref struct LineSplitEnumerator
{
    private ReadOnlySpan<char> _str;
    private readonly bool _stripColors;
    private readonly int _maxLineWidth;

    public RefTextRun Current { get; private set; }

    private int _cursorX = 0;
    private int _cursorY = 0;

    private byte _currentColor = 0;

    public LineSplitEnumerator(ReadOnlySpan<char> str, bool stripColors, int maxLineWidth)
    {
        _maxLineWidth = maxLineWidth;
        _stripColors = stripColors;
        _str = str;
        Current = default;
    }

    private static bool IsColor(ReadOnlySpan<char> text, out byte color)
    {
        if (text.Length >= 2 && text[0] == '^' && char.IsDigit(text[1]))
        {
            color = (byte)char.GetNumericValue(text[1]);
            return true;
        }

        color = 0;
        return false;
    }

    private static int SkipColors(ReadOnlySpan<char> text, out byte color)
    {
        var foundColor = false;
        var i = 0;
        color = 0;

        while (IsColor(text[i..], out var nextColor))
        {
            foundColor = true;
            color = nextColor;
            i += 2;
        }

        return foundColor ? i : -1;
    }

    private static bool IsLinebreak(ReadOnlySpan<char> text, out int nextLinebreak)
    {
        if ((text.Length > 0 && text[0] == '\r') || text[0] == '\n')
        {
            nextLinebreak =
                text[0] == '\r' && text.Length > 1 && text[1] == '\n' ? 2 : 1; // if this is an \r followed by an \n, return 2 instead of 1
            return true;
        }

        nextLinebreak = -1;
        return false;
    }

    private static int GetWordLength(ReadOnlySpan<char> text, bool stripColors)
    {
        var l = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (stripColors && IsColor(text[i..], out _))
            {
                i += 1;
            }
            else if (text[i] == ' ' || IsLinebreak(text[i..], out _))
            {
                break;
            }
            else
            {
                l++;
            }
        }

        return l;
    }

    private static (int start, int length, byte color, int wrap) GetNextRun(ReadOnlySpan<char> text, byte color, bool stripColors,
        int cursorX, int maxLineLength)
    {
        var start = 0;
        var wrap = 0;

        if (stripColors)
        {
            var lastColorPos = SkipColors(text, out var lastColor);
            if (lastColorPos != -1)
            {
                color = lastColor;
                start += lastColorPos;
            }
        }
        else if (IsColor(text, out var firstColor))
        {
            color = firstColor;
        }

        var length = 0;
        var run = text.Slice(start);

        for (var i = 0; i < run.Length; i++)
        {
            var remaining = run.Slice(i);
            if (IsLinebreak(remaining, out var nextLinebreak))
            {
                length += nextLinebreak;
                wrap = 1;
                break;
            }

            if (i > 0 && IsColor(remaining, out var nextColor))
            {
                if (stripColors || nextColor != color)
                {
                    break;
                }
            }

            var prevWasSpace = i > 0 && run[i - 1] == ' ';
            if (prevWasSpace)
            {
                var l = GetWordLength(remaining, stripColors);
                if (cursorX + length + l >= maxLineLength)
                {
                    wrap = 1;
                    break;
                }
            }
            else if (cursorX + i >= maxLineLength)
            {
                break;
            }

            length++;
        }

        return (start, length, color, wrap);
    }


    public bool MoveNext()
    {
        if (_str.Length == 0)
        {
            return false;
        }

        var (start, length, color, wrap) = GetNextRun(_str, _currentColor, _stripColors, _cursorX, _maxLineWidth);

        if (length == 0)
        {
            return false;
        }

        var run = _str.Slice(start, length);
        _str = _str.Slice(start + length);

        Current = new RefTextRun(run, color, _cursorX, _cursorY);

        _currentColor = color;
        _cursorX += run.Length;
        if (_cursorX >= _maxLineWidth || wrap > 0)
        {
            _cursorX = 0;
            _cursorY++;
        }

        return true;
    }

    public LineSplitEnumerator GetEnumerator()
    {
        return this;
    }
}

public ref struct SplitEnumerator
{
    private ReadOnlySpan<char> _str;
    private ReadOnlySpan<char> _splitBy;

    public ReadOnlySpan<char> Current { get; private set; }

    public SplitEnumerator(ReadOnlySpan<char> str, ReadOnlySpan<char> splitBy)
    {
        _str = str;
        _splitBy = splitBy;
        Current = default;
    }

    public bool MoveNext()
    {
        var span = _str;
        if (span.Length == 0)
            return false;

        var index = span.IndexOf(_splitBy);
        if (index == -1)
        {
            _str = ReadOnlySpan<char>.Empty;
            Current = span;
            return true;
        }

        Current = span.Slice(0, index);
        _str = span.Slice(index + 1);

        return true;
    }

    public ReadOnlySpan<char> Next()
    {
        var result = MoveNext();
        if (!result)
            throw new InvalidOperationException();
        return Current;
    }

    public SplitEnumerator GetEnumerator() => this;
}

public static class StringExtensions
{
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str, bool stripColors, int maxLineWidth)
    {
        return new LineSplitEnumerator(str, stripColors, maxLineWidth);
    }

    public static SplitEnumerator Split(this ReadOnlySpan<char> str, ReadOnlySpan<char> splitBy)
    {
        return new SplitEnumerator(str, splitBy);
    }
}
