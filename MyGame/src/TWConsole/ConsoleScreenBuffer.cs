using System.Runtime.CompilerServices;

namespace MyGame.TWConsole;

public class ConsoleScreenBuffer
{
    private short[] _buffer;
    private int _cursorX = 0;

    private int _displayY = 0;

    public ConsoleScreenBuffer(int width, int height)
    {
        Height = height;
        Width = width;
        _buffer = new short[width * height];
    }

    private int _wrappedY => CursorY % Height * Width;

    public int DisplayY
    {
        get => _displayY;
        set
        {
            var minValue = Math.Max(0, CursorY - Height + 1); // show at least one line
            _displayY = Math.Clamp(value, minValue, CursorY);
        }
    }

    public int Height { get; private set; }

    public int Width { get; private set; }

    public int CursorY { get; private set; } = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetChar(int x, int y, out char c, out byte color)
    {
        var line = (Height + y) % Height;
        var i = line * Width + x;
        Unpack(_buffer[i], out c, out color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static short Pack(char c, byte color)
    {
        return (short)((color << 8) | c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Unpack(short s, out char c, out byte color)
    {
        color = (byte)((s >> 8) & 0xff);
        c = (char)(s & 0xff);
    }

    public void Clear()
    {
        for (var i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = Pack(' ', 0);
        }

        _cursorX = 0;
        CursorY = 0;
        _displayY = 0;
    }

    private void Linefeed()
    {
        var shouldScrollDisplay = _displayY == CursorY;
        CursorY++;
        if (shouldScrollDisplay)
        {
            _displayY = CursorY;
        }

        _cursorX = 0;
        ClearLine(CursorY);
    }

    private void ClearLine(int y)
    {
        for (var i = 0; i < Width; i++)
        {
            _buffer[_wrappedY + i] = Pack('\0', 0);
        }
    }

    public void AddLine(ReadOnlySpan<char> line)
    {
        var lastY = 0;
        foreach (var run in line.SplitLines(true, Width))
        {
            while (run.Y - lastY > 0)
            {
                Linefeed();
                lastY++;
            }

            for (var i = 0; i < run.Text.Length; i++)
            {
                if (_cursorX >= Width)
                {
                    Linefeed();
                }

                _buffer[_wrappedY + _cursorX] = Pack(run.Text[i], run.Color);
                _cursorX++;
            }
        }

        Linefeed();
    }

    public void Resize(int width, int height)
    {
        var (oldWidth, oldHeight) = (Width, Height);
        Width = width;
        Height = height;

        var newBuffer = new short[width * height];

        var numLines = Height < oldHeight ? Height : oldHeight;
        var numChars = Width < oldWidth ? Width : oldWidth;
        for (var y = 0; y < numLines; y++)
        {
            for (var x = 0; x < numChars; x++)
            {
                newBuffer[(height - 1 - y) * width + x] = _buffer[(CursorY - y + oldHeight) % oldHeight * oldWidth + x];
            }
        }

        _buffer = newBuffer;
        CursorY = Height - 1;
        _displayY = CursorY;
    }
}
