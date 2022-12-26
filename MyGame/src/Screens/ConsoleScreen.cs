using System.Diagnostics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using MyGame.TWConsole;
using MyGame.Utils;
using SDL2;

namespace MyGame.Screens;

public enum ConsoleScreenState
{
    TransitionOn,
    Active,
    TransitionOff,
    Hidden,
}

public class ConsoleScreen
{
    private static readonly HashSet<char> AllowedSymbols = new()
    {
        ' ', '.', ',', '\\', '/', '_', '-', '+', '=', '"', '\'',
        '!', '?', '@', '#', '$', '%', '^', '&', '*', '(', ')',
        '[', ']', '>', '<', ':', ';',
    };

    public static readonly Vector2 CharSize = new(10, 18);

    private readonly List<string> _autoCompleteHits = new();
    private readonly Game _game;
    private int _autoCompleteIndex = -1;
    private readonly KeyCode[] _autoCompleteKeys = { KeyCode.Tab, KeyCode.LeftShift };

    private Rectangle _backgroundRect;
    private float _caretBlinkTimer;
    private int _charsDrawn;
    private int _commandHistoryIndex = -1;
    private Easing.Function.Float _easeFunc = Easing.Function.Float.InOutQuad;
    private readonly Easing.Function.Float[] _easeFuncs = Enum.GetValues<Easing.Function.Float>();

    private readonly InputField _inputField = new(1024, TWConsole.TWConsole.BUFFER_WIDTH);

    private readonly KeyCode[] _pageUpAndDown = { KeyCode.PageUp, KeyCode.PageDown };

    private readonly char[] _tmpArr = new char[1];

    private float _transitionPercentage;
    public float TransitionPercentage => _transitionPercentage;
    private readonly KeyCode[] _upAndDownArrows = { KeyCode.Up, KeyCode.Down };

    public bool IsHidden
    {
        get => ConsoleScreenState is ConsoleScreenState.Hidden or ConsoleScreenState.TransitionOff;
        set => ConsoleScreenState = value ? ConsoleScreenState.TransitionOff : ConsoleScreenState.TransitionOn;
    }

    public ConsoleScreenState ConsoleScreenState { get; private set; } = ConsoleScreenState.Hidden;

    private Point _lastRenderSize;
    private Stopwatch _renderStopwatch = new();
    private float _renderDurationMs;
    private uint _drawCalls;
    private float _peakRenderDurationMs;

    private TWConsole.TWConsole TwConsole => Shared.Console;

    private List<char> _textInputChars = new();
    public bool HasBeenDrawn;

    public ConsoleScreen(Game game)
    {
        _game = game;
        _lastRenderSize = new Point(1920, 1080);

        // TODO (marpe): Ugh
        Inputs.TextInput += OnTextInput;
    }

    private void OnTextInput(char c)
    {
        _textInputChars.Add(c);
    }

    [ConsoleHandler("console", "Toggles the console")]
    public static void ToggleConsole()
    {
        Shared.Game.ConsoleScreen.IsHidden = !Shared.Game.ConsoleScreen.IsHidden;
    }

    public void Unload()
    {
    }

    public void Update(float deltaSeconds)
    {
        var inputs = _game.Inputs;

        UpdateTransition(deltaSeconds, _lastRenderSize.X, _lastRenderSize.Y);

        if (IsHidden)
            return;

        CheckResize(_lastRenderSize.X, _lastRenderSize.Y);

        _caretBlinkTimer += deltaSeconds;

        HandleKeyPressed(inputs);

        if (inputs.Mouse.Wheel != 0)
        {
            if (Math.Sign(inputs.Mouse.Wheel) < 0)
            {
                ScrollDown();
            }
            else
            {
                ScrollUp();
            }
        }

        for (var i = 0; i < _textInputChars.Count; i++)
        {
            HandleTextInput(_textInputChars[i]);
        }

        _textInputChars.Clear();

        // disable input for the next screen // TODO (marpe): not in this example app
        // inputs.MouseEnabled = inputs.KeyboardEnabled = false;
    }

    private void CheckResize(int windowWidth, int windowHeight)
    {
        var availWidthInPixels = windowWidth - ConsoleSettings.HorizontalPadding * 2f;
        var minWidth = 60;
        var width = Math.Max((int)(availWidthInPixels / CharSize.X), minWidth);
        if (TwConsole.ScreenBuffer.Width != width)
        {
            var height = TwConsole.ScreenBuffer.Height * TwConsole.ScreenBuffer.Width / width; // windowSize.Y / charSize.Y;
            TwConsole.ScreenBuffer.Resize(width, height);
            _inputField.SetMaxWidth(width);
            TwConsole.Print($"Console size set to: {width}, {height}");
        }
    }

    private void UpdateTransition(float deltaSeconds, int windowWidth, int windowHeight)
    {
        var speed = 1.0f / Math.Clamp(ConsoleSettings.TransitionDuration, float.Epsilon, float.MaxValue);
        if (ConsoleScreenState == ConsoleScreenState.TransitionOn)
        {
            _transitionPercentage = Math.Clamp(_transitionPercentage + deltaSeconds * speed, 0, 1);
            if (_transitionPercentage >= 1.0f)
            {
                ConsoleScreenState = ConsoleScreenState.Active;
            }
        }
        else if (ConsoleScreenState == ConsoleScreenState.TransitionOff)
        {
            _transitionPercentage = Math.Clamp(_transitionPercentage - deltaSeconds * speed, 0, 1);
            if (_transitionPercentage <= 0)
            {
                ConsoleScreenState = ConsoleScreenState.Hidden;
            }
        }

        var height = windowHeight * ConsoleSettings.RelativeConsoleHeight;
        var t = Easing.Function.Get(_easeFunc).Invoke(0f, 1f, _transitionPercentage, 1f);
        _backgroundRect.Y = (int)(-height + height * t);
        _backgroundRect.Height = (int)height;
        _backgroundRect.Width = (int)windowWidth;
    }

    private static bool IsAllowedCharacter(char c)
    {
        return char.IsLetter(c) ||
               char.IsNumber(c) ||
               AllowedSymbols.Contains(c);
    }

    private void HandleTextInput(char c)
    {
        if (c == (char)22) // CTRL + V
        {
            PasteFromClipboard();
        }
        else
        {
            if (!IsAllowedCharacter(c))
            {
                return;
            }

            _inputField.AddChar(c);
        }

        _caretBlinkTimer = 0;
    }

    private void PasteFromClipboard()
    {
        var clipboard = SDL.SDL_GetClipboardText();
        for (var i = 0; i < clipboard.Length; i++)
        {
            HandleTextInput(clipboard[i]);
        }
    }

    private void Execute()
    {
        var trimmedInput = _inputField.GetBuffer();
        TwConsole.Execute(trimmedInput);
        _commandHistoryIndex = -1;
        EndAutocomplete();
        _inputField.ClearInput();
    }

    private void HandleKeyPressed(Inputs input)
    {
        if (input.Keyboard.AnyPressed)
        {
            _caretBlinkTimer = 0;
        }

        if (input.Keyboard.AnyPressed && !input.Keyboard.IsAnyModifierKeyDown())
        {
            if (!input.Keyboard.IsAnyKeyDown(_autoCompleteKeys))
            {
                EndAutocomplete();
            }

            if (!input.Keyboard.IsAnyKeyDown(_pageUpAndDown))
            {
                TwConsole.ScreenBuffer.DisplayY = TwConsole.ScreenBuffer.CursorY;
            }

            if (!input.Keyboard.IsAnyKeyDown(_upAndDownArrows))
            {
                _commandHistoryIndex = -1;
            }
        }

        if (input.Keyboard.IsPressed(KeyCode.Tab))
        {
            if (_autoCompleteIndex == -1) // new auto complete
            {
                _autoCompleteHits.Clear();

                if (_inputField.Length > 0)
                {
                    foreach (var key in TwConsole.Commands.Keys)
                    {
                        for (var i = 0; i < _inputField.Length && i < key.Length; i++)
                        {
                            if (key[i] != _inputField.Buffer[i])
                            {
                                break;
                            }

                            if (i == _inputField.Length - 1)
                            {
                                _autoCompleteHits.Add(key);
                            }
                        }
                    }

                    TwConsole.Print($"{_autoCompleteHits.Count} matches:\n{string.Join("\n", _autoCompleteHits)}");
                }
            }

            if (_autoCompleteHits.Count == 0)
            {
                EndAutocomplete();
            }
            else if (_autoCompleteHits.Count == 1)
            {
                _inputField.SetInput(_autoCompleteHits[0]);
            }
            else
            {
                var direction = input.Keyboard.IsDown(KeyCode.LeftShift) ? -1 : 1;
                _autoCompleteIndex += direction;

                if (_autoCompleteIndex < 0)
                {
                    _autoCompleteIndex = _autoCompleteHits.Count - 1;
                }
                else if (_autoCompleteIndex >= _autoCompleteHits.Count)
                {
                    _autoCompleteIndex = 0;
                }

                _inputField.SetInput(_autoCompleteHits[_autoCompleteIndex]);
            }
        }
        else if
            (input.Keyboard.IsPressed(KeyCode
                .Left)) // TODO (marpe): Removed code which allowed the key to be repeated for this example app, now these keys have to be pressed multiple times instead of just being held
        {
            _inputField.CursorLeft();
        }
        else if (input.Keyboard.IsPressed(KeyCode.Right))
        {
            _inputField.CursorRight();
        }
        else if (input.Keyboard.IsPressed(KeyCode.Up))
        {
            _commandHistoryIndex++;
            if (_commandHistoryIndex > TwConsole.CommandHistory.Count - 1)
            {
                _commandHistoryIndex = TwConsole.CommandHistory.Count - 1;
            }

            if (_commandHistoryIndex != -1)
            {
                _inputField.SetInput(TwConsole.CommandHistory[TwConsole.CommandHistory.Count - 1 - _commandHistoryIndex]);
            }
        }
        else if (input.Keyboard.IsPressed(KeyCode.Down))
        {
            _commandHistoryIndex--;
            if (_commandHistoryIndex <= -1)
            {
                _commandHistoryIndex = -1;
                _inputField.ClearInput();
            }

            if (_commandHistoryIndex != -1)
            {
                _inputField.SetInput(TwConsole.CommandHistory[TwConsole.CommandHistory.Count - 1 - _commandHistoryIndex]);
            }
        }
        else if (input.Keyboard.IsPressed(KeyCode.PageDown))
        {
            if (input.Keyboard.IsAnyKeyDown(InputsExt.ControlKeys))
            {
                ScrollBottom();
            }
            else
            {
                ScrollDown();
            }
        }
        else if (input.Keyboard.IsPressed(KeyCode.PageUp))
        {
            if (input.Keyboard.IsAnyKeyDown(InputsExt.ControlKeys))
            {
                ScrollTop();
            }
            else
            {
                ScrollUp();
            }
        }

        if (input.Keyboard.IsAnyKeyDown(InputsExt.ControlKeys))
        {
            if (input.Keyboard.IsPressed(KeyCode.C))
            {
                _inputField.ClearInput();
            }
        }

        if (input.Keyboard.IsAnyKeyDown(InputsExt.ShiftKeys))
        {
            if (input.Keyboard.IsPressed(KeyCode.Insert))
            {
                PasteFromClipboard();
            }
        }

        if (input.Keyboard.IsPressed(KeyCode.Backspace))
        {
            _inputField.RemoveChar();
        }

        if (input.Keyboard.IsPressed(KeyCode.Return))
        {
            Execute();
        }

        if (input.Keyboard.IsPressed(KeyCode.Delete))
        {
            _inputField.Delete();
        }

        if (input.Keyboard.IsPressed(KeyCode.End))
        {
            _inputField.SetCursor(_inputField.Length);
        }

        if (input.Keyboard.IsPressed(KeyCode.Home))
        {
            _inputField.SetCursor(0);
        }

        if (input.Keyboard.IsPressed(KeyCode.F12))
        {
            _easeFunc = (Easing.Function.Float)((_easeFuncs.Length + (int)_easeFunc + 1) % _easeFuncs.Length);
            TwConsole.Print($"EaseFunc: {_easeFunc}");
        }
        else if (input.Keyboard.IsPressed(KeyCode.F11))
        {
            _easeFunc = (Easing.Function.Float)((_easeFuncs.Length + (int)_easeFunc - 1) % _easeFuncs.Length);
            TwConsole.Print($"EaseFunc: {_easeFunc}");
        }
    }

    private void ScrollTop()
    {
        TwConsole.ScreenBuffer.DisplayY = TwConsole.ScreenBuffer.CursorY - TwConsole.ScreenBuffer.Height;
    }

    private void ScrollUp()
    {
        TwConsole.ScreenBuffer.DisplayY -= ConsoleSettings.ScrollSpeed;
    }

    private void ScrollDown()
    {
        TwConsole.ScreenBuffer.DisplayY += ConsoleSettings.ScrollSpeed;
    }

    private void ScrollBottom()
    {
        TwConsole.ScreenBuffer.DisplayY = TwConsole.ScreenBuffer.CursorY;
    }

    private void EndAutocomplete()
    {
        _autoCompleteIndex = -1;
    }

    private void DrawText(Renderer renderer, ReadOnlySpan<char> text, Vector2 position, float depth, Color color)
    {
        if (MyGameInstance.UseFreeType)
            renderer.DrawFTText(text, position, /*depth,*/ color);
        else
            renderer.DrawText(text, position, depth, color);
    }

    public static bool CanSkipChar(char c) => c < 0x20 || c > 0x7e || c == ' ';

    public void Draw(Renderer renderer, ref CommandBuffer commandBuffer, Texture renderDestination, double alpha)
    {
        if (ConsoleScreenState == ConsoleScreenState.Hidden)
            return;

        HasBeenDrawn = true;
        _renderStopwatch.Restart();
        renderer.DrawRect(_backgroundRect, ConsoleSettings.BackgroundColor.MultiplyAlpha(ConsoleSettings.BackgroundAlpha));

        // draw line start and end
        var textArea = new Rectangle(
            ConsoleSettings.HorizontalPadding,
            _backgroundRect.Top,
            (int)(CharSize.X * TwConsole.ScreenBuffer.Width),
            _backgroundRect.Height
        );

        var marginColor = Color.Orange * 0.5f;
        renderer.DrawLine(textArea.Min(), textArea.BottomLeft(), marginColor, 1f);
        renderer.DrawLine(textArea.TopRight(), textArea.Max(), marginColor, 1f);

        var hasScrolled = TwConsole.ScreenBuffer.DisplayY != TwConsole.ScreenBuffer.CursorY;
        if (hasScrolled)
        {
            var bottomRight = new Vector2(_backgroundRect.Width, _backgroundRect.Height);
            var scrollIndicatorPosition = bottomRight - new Vector2(CharSize.Y, ConsoleSettings.HorizontalPadding);
            var color = ConsoleSettings.ScrollIndicatorColor;
            DrawText(renderer, ConsoleSettings.ScrollIndicatorChar, scrollIndicatorPosition, 0, color);
        }

        var displayPosition = new Vector2(
            ConsoleSettings.HorizontalPadding,
            _backgroundRect.Bottom - CharSize.Y
        );

        var showInput = !hasScrolled;
        if (showInput)
        {
            displayPosition.Y -= _inputField.Height * CharSize.Y;
        }

        // Draw history
        var numLinesToDraw = _backgroundRect.Height / CharSize.Y;

        _charsDrawn = 0;

        for (var i = 0; i < numLinesToDraw; i++)
        {
            var lineIndex = TwConsole.ScreenBuffer.DisplayY - i;
            if (lineIndex < 0)
            {
                break;
            }

            var numDrawnLines = TwConsole.ScreenBuffer.CursorY - lineIndex;
            if (numDrawnLines >= TwConsole.ScreenBuffer.Height) // past scrollback wrap point
            {
                break;
            }

            for (var j = 0; j < TwConsole.ScreenBuffer.Width; j++)
            {
                TwConsole.ScreenBuffer.GetChar(j, lineIndex, out var c, out var color);
                if (c == '\0')
                    break;
                if (CanSkipChar(c))
                    continue;
                var charColor = ConsoleSettings.Colors[color];
                var position = displayPosition + new Vector2(CharSize.X * j, -CharSize.Y * i);
                _tmpArr[0] = c;
                DrawText(renderer, _tmpArr, position, 0, charColor);
                _charsDrawn++;
            }
        }


        if (showInput)
        {
            DrawInput(renderer, textArea, displayPosition);
        }

        if (ConsoleSettings.ShowDebug)
        {
            if (!MyGameInstance.UseFreeType)
                renderer.TextButcher.FlushToSpriteBatch(renderer.SpriteBatch);

            _peakRenderDurationMs = StopwatchExt.SmoothValue(_peakRenderDurationMs, _renderDurationMs);
            var scrolledLinesStr =
                $"CharsDrawn({_charsDrawn}) " +
                $"DrawCalls({_drawCalls}) " +
                $"Elapsed({_peakRenderDurationMs:00.00} ms) ";
            var lineLength = scrolledLinesStr.Length * CharSize.X;
            var scrollLinesPos = new Vector2(
                _backgroundRect.Width - lineLength - ConsoleSettings.HorizontalPadding,
                0
            );

            renderer.DrawRect(new Rectangle((int)scrollLinesPos.X, 0, (int)lineLength, (int)CharSize.Y), Color.Black);
            DrawText(renderer, scrolledLinesStr, scrollLinesPos, 0, Color.Yellow);
        }

        renderer.RunRenderPass(ref commandBuffer, renderDestination, Color.Transparent, null);
        _drawCalls = renderer.SpriteBatch.DrawCalls;
        _renderStopwatch.Stop();
        _renderDurationMs = _renderStopwatch.GetElapsedMilliseconds();
    }

    private void DrawInput(Renderer renderer, Rectangle textArea, Vector2 displayPosition)
    {
        // draw input background
        renderer.DrawRect(
            new Rectangle(
                textArea.Left,
                (int)displayPosition.Y,
                textArea.Width,
                (int)(CharSize.Y + _inputField.Height * CharSize.Y)
            ),
            ConsoleSettings.InputBackgroundColor
        );

        // Draw input line indicator
        var inputLineIndicatorPosition = displayPosition - Vector2.UnitX * CharSize.X;

        if (0 <= inputLineIndicatorPosition.Y && inputLineIndicatorPosition.Y <= _backgroundRect.Bottom)
        {
            DrawText(renderer, ConsoleSettings.InputLineChar, inputLineIndicatorPosition, 0, ConsoleSettings.InputLineCharColor);
        }

        if (0 <= displayPosition.Y && displayPosition.Y <= _backgroundRect.Bottom)
        {
            var inputColor = ConsoleSettings.InputTextColor;
            if (_autoCompleteIndex != -1)
            {
                inputColor = ConsoleSettings.AutocompleteSuggestionColor;
            }
            else if (_commandHistoryIndex != -1)
            {
                inputColor = ConsoleSettings.ActiveCommandHistoryColor;
            }

            var inputPosition = displayPosition;
            for (var i = 0; i < _inputField.Length; i++)
            {
                var x = i % _inputField.MaxWidth;
                var y = i / _inputField.MaxWidth;

                _tmpArr[0] = _inputField.Buffer[i];
                DrawText(renderer, _tmpArr, inputPosition + new Vector2(x, y) * CharSize, 0, inputColor);
            }

            // Draw caret
            var caretPosition = new Vector2(
                displayPosition.X + _inputField.CursorX * CharSize.X,
                displayPosition.Y + _inputField.CursorY * CharSize.Y
            );

            var color = ConsoleSettings.CaretColor;
            var blinkDelay = 1.5f;
            if (_caretBlinkTimer >= blinkDelay)
            {
                color = Color.Lerp(color, Color.Transparent, MathF.Sin(ConsoleSettings.CaretBlinkSpeed * (_caretBlinkTimer - blinkDelay)));
            }

            DrawText(renderer, ConsoleSettings.CaretChar, caretPosition, 0, color);
        }
    }
}
