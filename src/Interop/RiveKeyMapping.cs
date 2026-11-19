using Stride.Input;

namespace VL.Rive.Interop;

// Key and KeyModifiers are generated from RiveSharpInterop.hpp
static class RiveKeyMapping
{
    /// <summary>Maps a Stride key to a rive key. Returns false for keys rive has no code for.</summary>
    internal static bool TryMap(Keys key, out Key result)
    {
        // Letters, digits, function keys and the numpad are contiguous ranges in both enums
        if (key >= Keys.A && key <= Keys.Z)
            return Set(65 + (key - Keys.A), out result);
        if (key >= Keys.D0 && key <= Keys.D9)
            return Set(48 + (key - Keys.D0), out result);
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            return Set(320 + (key - Keys.NumPad0), out result);
        if (key >= Keys.F1 && key <= Keys.F24)
            return Set((int)Key.f1 + (key - Keys.F1), out result);

        Key? mapped = key switch
        {
            Keys.Space => Key.space,
            Keys.OemQuotes => Key.apostrophe,
            Keys.OemComma => Key.comma,
            Keys.OemMinus => Key.minus,
            Keys.OemPeriod => Key.period,
            Keys.OemQuestion => Key.slash,
            Keys.OemSemicolon => Key.semicolon,
            Keys.OemPlus => Key.equal,
            Keys.OemOpenBrackets => Key.leftBracket,
            Keys.OemPipe or Keys.OemBackslash => Key.backslash,
            Keys.OemCloseBrackets => Key.rightBracket,
            Keys.OemTilde => Key.graveAccent,
            Keys.Escape => Key.escape,
            Keys.Enter => Key.enter,
            Keys.Tab => Key.tab,
            Keys.Back => Key.backspace,
            Keys.Insert => Key.insert,
            Keys.Delete => Key.deleteKey,
            Keys.Right => Key.right,
            Keys.Left => Key.left,
            Keys.Down => Key.down,
            Keys.Up => Key.up,
            Keys.PageUp => Key.pageUp,
            Keys.PageDown => Key.pageDown,
            Keys.Home => Key.home,
            Keys.End => Key.end,
            Keys.CapsLock => Key.capsLock,
            Keys.Scroll => Key.scrollLock,
            Keys.NumLock => Key.numLock,
            Keys.PrintScreen => Key.printScreen,
            Keys.Pause => Key.pause,
            Keys.Decimal => Key.kpDecimal,
            Keys.Divide => Key.kpDivide,
            Keys.Multiply => Key.kpMultiply,
            Keys.Subtract => Key.kpSubtract,
            Keys.Add => Key.kpAdd,
            Keys.NumPadEnter => Key.kpEnter,
            Keys.LeftShift => Key.leftShift,
            Keys.LeftCtrl => Key.leftControl,
            Keys.LeftAlt => Key.leftAlt,
            Keys.LeftWin => Key.leftSuper,
            Keys.RightShift => Key.rightShift,
            Keys.RightCtrl => Key.rightControl,
            Keys.RightAlt => Key.rightAlt,
            Keys.RightWin => Key.rightSuper,
            Keys.Apps => Key.menu,
            _ => null,
        };
        result = mapped ?? default;
        return mapped.HasValue;
    }

    /// <summary>Maps a VL key (a Windows virtual-key code) to a rive key. Returns false for keys rive has no code for.</summary>
    internal static bool TryMap(VL.Lib.IO.Keys key, out Key result)
    {
        var vk = (int)key;
        // Letters and digits are their ASCII codes in both
        if (vk is >= 'A' and <= 'Z' or >= '0' and <= '9')
            return Set(vk, out result);
        if (vk is >= 0x60 and <= 0x69) // NumPad0..9
            return Set((int)Key.kp0 + vk - 0x60, out result);
        if (vk is >= 0x70 and <= 0x87) // F1..F24
            return Set((int)Key.f1 + vk - 0x70, out result);

        Key? mapped = vk switch
        {
            0x20 => Key.space,
            0x08 => Key.backspace,
            0x09 => Key.tab,
            0x0D => Key.enter,
            0x13 => Key.pause,
            0x14 => Key.capsLock,
            0x1B => Key.escape,
            0x21 => Key.pageUp,
            0x22 => Key.pageDown,
            0x23 => Key.end,
            0x24 => Key.home,
            0x25 => Key.left,
            0x26 => Key.up,
            0x27 => Key.right,
            0x28 => Key.down,
            0x2C => Key.printScreen,
            0x2D => Key.insert,
            0x2E => Key.deleteKey,
            0x5B => Key.leftSuper,
            0x5C => Key.rightSuper,
            0x5D => Key.menu,
            0x6A => Key.kpMultiply,
            0x6B => Key.kpAdd,
            0x6D => Key.kpSubtract,
            0x6E => Key.kpDecimal,
            0x6F => Key.kpDivide,
            0x90 => Key.numLock,
            0x91 => Key.scrollLock,
            0xA0 => Key.leftShift,
            0xA1 => Key.rightShift,
            0xA2 => Key.leftControl,
            0xA3 => Key.rightControl,
            0xA4 => Key.leftAlt,
            0xA5 => Key.rightAlt,
            0xBA => Key.semicolon,
            0xBB => Key.equal,
            0xBC => Key.comma,
            0xBD => Key.minus,
            0xBE => Key.period,
            0xBF => Key.slash,
            0xC0 => Key.graveAccent,
            0xDB => Key.leftBracket,
            0xDC or 0xE2 => Key.backslash,
            0xDD => Key.rightBracket,
            0xDE => Key.apostrophe,
            _ => null,
        };
        result = mapped ?? default;
        return mapped.HasValue;
    }

    static bool Set(int value, out Key result)
    {
        result = (Key)value;
        return true;
    }
}
