using MoonWorks.Input;

namespace MyGame.Utils;

public static class InputsExt
{
    public static readonly KeyCode[] ModifierKeys =
    {
        KeyCode.LeftControl,
        KeyCode.RightControl,
        KeyCode.LeftShift,
        KeyCode.RightShift,
        KeyCode.LeftAlt,
        KeyCode.RightAlt,
        KeyCode.LeftMeta,
        KeyCode.RightMeta,
    };
    
    public static readonly KeyCode[] ControlKeys = { KeyCode.LeftControl, KeyCode.RightControl };
    public static readonly KeyCode[] ShiftKeys = { KeyCode.LeftShift, KeyCode.RightShift };

    public static bool IsAnyModifierKeyDown(this Keyboard kb)
    {
        return kb.IsAnyKeyDown(ModifierKeys);
    }

    public static bool IsAnyKeyDown(this Keyboard kb, KeyCode[] keyCodes)
    {
        for (var i = 0; i < keyCodes.Length; i++)
        {
            var isDown = kb.IsDown(keyCodes[i]);
            if (isDown)
                return true;
        }

        return false;
    }
}
