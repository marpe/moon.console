namespace MyGame.TWConsole;

public class InputField
{
    public readonly char[] Buffer;

    public InputField(int size, int maxWidth)
    {
        Buffer = new char[size];
        MaxWidth = maxWidth;
    }

    public int CursorX { get; private set; }
    public int CursorY { get; private set; }
    public int CursorIndex => CursorY * MaxWidth + CursorX;
    public int MaxWidth { get; private set; }
    public int Length { get; private set; }
    public int Height => Length / MaxWidth;

    public void ClearInput()
    {
        for (var i = 0; i < Buffer.Length; i++)
        {
            Buffer[i] = default;
        }

        CursorX = 0;
        CursorY = 0;
        Length = 0;
    }

    public void SetCursor(int i)
    {
        var x = i % MaxWidth;
        var y = i / MaxWidth;

        var newIndex = y * MaxWidth + x;
        var clamped = Math.Clamp(newIndex, 0, Length);

        CursorX = clamped % MaxWidth;
        CursorY = clamped / MaxWidth;
    }

    public ReadOnlySpan<char> GetBuffer()
    {
        return Buffer.AsSpan(0, Length);
    }

    public void RemoveChar()
    {
        if (CursorIndex <= 0)
        {
            return;
        }

        Array.Copy(Buffer, CursorIndex, Buffer, CursorIndex - 1, Buffer.Length - (CursorIndex + 1));
        Buffer[Length] = default;
        Length--;
        CursorLeft();
    }

    public void Delete()
    {
        if (CursorIndex >= Length)
        {
            return;
        }

        var length = Length - CursorIndex;
        Array.Copy(Buffer, CursorIndex + 1, Buffer, CursorIndex, Buffer.Length - (CursorIndex + 1));
        Buffer[Length] = default;
        Length--;
    }

    public void AddChar(char c)
    {
        if (Length >= Buffer.Length - 1)
        {
            return;
        }

        Array.Copy(Buffer, CursorIndex, Buffer, CursorIndex + 1, Buffer.Length - (CursorIndex + 1));
        Buffer[CursorIndex] = c;
        Length++;
        CursorRight();
    }

    public void CursorLeft()
    {
        if (CursorY > 0 && CursorX == 0)
        {
            CursorX = MaxWidth - 1;
            CursorY--;
        }
        else if (CursorX > 0)
        {
            CursorX--;
        }
    }

    public void CursorRight()
    {
        if (CursorIndex < Length)
        {
            CursorX++;
        }

        if (CursorX >= MaxWidth)
        {
            CursorX = 0;
            CursorY++;
        }
    }

    public void SetInput(ReadOnlySpan<char> input)
    {
        if (input.Length > Buffer.Length - 1)
        {
            input = input.Slice(0, Buffer.Length - 1);
        }

        input.CopyTo(Buffer);

        for (var i = input.Length; i < Buffer.Length; i++)
        {
            Buffer[i] = default;
        }

        Length = input.Length;
        SetCursor(Length);
    }

    public void SetMaxWidth(int maxWidth)
    {
        MaxWidth = maxWidth;
        SetCursor(Length);
    }
}
