using System;
using System.Linq;
using RGB.NET.Core;
using Artemis.Plugins.Devices.LeobogHi75CPro.Protocol;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Mapping;

internal readonly record struct Hi75CProKeyDefinition(
    LedId LedId,
    int RawLedIndex,
    int GridX,
    int GridY);

internal static class Hi75CProLedMap
{
    public static readonly Hi75CProKeyDefinition[] Keys =
    {
        // Row 0
        new(LedId.Keyboard_Escape, 0, 0, 0),
        new(LedId.Keyboard_F1, 12, 2, 0),
        new(LedId.Keyboard_F2, 18, 3, 0),
        new(LedId.Keyboard_F3, 24, 4, 0),
        new(LedId.Keyboard_F4, 30, 5, 0),
        new(LedId.Keyboard_F5, 36, 6, 0),
        new(LedId.Keyboard_F6, 42, 7, 0),
        new(LedId.Keyboard_F7, 48, 8, 0),
        new(LedId.Keyboard_F8, 54, 9, 0),
        new(LedId.Keyboard_F9, 60, 10, 0),
        new(LedId.Keyboard_F10, 66, 11, 0),
        new(LedId.Keyboard_F11, 72, 12, 0),
        new(LedId.Keyboard_F12, 78, 13, 0),

        // Row 1
        new(LedId.Keyboard_GraveAccentAndTilde, 1, 0, 1),
        new(LedId.Keyboard_1, 7, 1, 1),
        new(LedId.Keyboard_2, 13, 2, 1),
        new(LedId.Keyboard_3, 19, 3, 1),
        new(LedId.Keyboard_4, 25, 4, 1),
        new(LedId.Keyboard_5, 31, 5, 1),
        new(LedId.Keyboard_6, 37, 6, 1),
        new(LedId.Keyboard_7, 43, 7, 1),
        new(LedId.Keyboard_8, 49, 8, 1),
        new(LedId.Keyboard_9, 55, 9, 1),
        new(LedId.Keyboard_0, 61, 10, 1),
        new(LedId.Keyboard_MinusAndUnderscore, 67, 11, 1),
        new(LedId.Keyboard_EqualsAndPlus, 73, 12, 1),
        new(LedId.Keyboard_Backspace, 79, 13, 1),
        new(LedId.Keyboard_Delete, 85, 14, 1),

        // Row 2
        new(LedId.Keyboard_Tab, 2, 0, 2),
        new(LedId.Keyboard_Q, 8, 1, 2),
        new(LedId.Keyboard_W, 14, 2, 2),
        new(LedId.Keyboard_E, 20, 3, 2),
        new(LedId.Keyboard_R, 26, 4, 2),
        new(LedId.Keyboard_T, 32, 5, 2),
        new(LedId.Keyboard_Y, 38, 6, 2),
        new(LedId.Keyboard_U, 44, 7, 2),
        new(LedId.Keyboard_I, 50, 8, 2),
        new(LedId.Keyboard_O, 56, 9, 2),
        new(LedId.Keyboard_P, 62, 10, 2),
        new(LedId.Keyboard_BracketLeft, 68, 11, 2),
        new(LedId.Keyboard_BracketRight, 74, 12, 2),
        new(LedId.Keyboard_Backslash, 80, 13, 2),
        new(LedId.Keyboard_PageUp, 86, 14, 2),

        // Row 3
        new(LedId.Keyboard_CapsLock, 3, 0, 3),
        new(LedId.Keyboard_A, 9, 1, 3),
        new(LedId.Keyboard_S, 15, 2, 3),
        new(LedId.Keyboard_D, 21, 3, 3),
        new(LedId.Keyboard_F, 27, 4, 3),
        new(LedId.Keyboard_G, 33, 5, 3),
        new(LedId.Keyboard_H, 39, 6, 3),
        new(LedId.Keyboard_J, 45, 7, 3),
        new(LedId.Keyboard_K, 51, 8, 3),
        new(LedId.Keyboard_L, 57, 9, 3),
        new(LedId.Keyboard_SemicolonAndColon, 63, 10, 3),
        new(LedId.Keyboard_ApostropheAndDoubleQuote, 69, 11, 3),
        new(LedId.Keyboard_Enter, 81, 13, 3),
        new(LedId.Keyboard_PageDown, 87, 14, 3),

        // Row 4
        new(LedId.Keyboard_LeftShift, 4, 0, 4),
        new(LedId.Keyboard_Z, 10, 2, 4),
        new(LedId.Keyboard_X, 16, 3, 4),
        new(LedId.Keyboard_C, 22, 4, 4),
        new(LedId.Keyboard_V, 28, 5, 4),
        new(LedId.Keyboard_B, 34, 6, 4),
        new(LedId.Keyboard_N, 40, 7, 4),
        new(LedId.Keyboard_M, 46, 8, 4),
        new(LedId.Keyboard_CommaAndLessThan, 52, 9, 4),
        new(LedId.Keyboard_PeriodAndBiggerThan, 58, 10, 4),
        new(LedId.Keyboard_SlashAndQuestionMark, 64, 11, 4),
        new(LedId.Keyboard_RightShift, 70, 12, 4),
        new(LedId.Keyboard_ArrowUp, 82, 13, 4),
        new(LedId.Keyboard_End, 88, 14, 4),

        // Row 5
        new(LedId.Keyboard_LeftCtrl, 5, 0, 5),
        new(LedId.Keyboard_LeftGui, 11, 1, 5),
        new(LedId.Keyboard_LeftAlt, 17, 2, 5),
        new(LedId.Keyboard_Space, 35, 6, 5),
        new(LedId.Keyboard_Function, 53, 9, 5),
        new(LedId.Keyboard_RightCtrl, 59, 10, 5),
        new(LedId.Keyboard_ArrowLeft, 77, 12, 5),
        new(LedId.Keyboard_ArrowDown, 83, 13, 5),
        new(LedId.Keyboard_ArrowRight, 89, 14, 5)
    };

    public static int[] RawLedIndices { get; } =
        Keys.Select(k => k.RawLedIndex).ToArray();

    public static void Validate()
    {
        if (Keys.Length != Hi75CProConstants.LogicalLedCount)
            throw new InvalidOperationException(
                $"Expected {Hi75CProConstants.LogicalLedCount} LEDs, got {Keys.Length}.");

        if (Keys.Select(k => k.LedId).Distinct().Count() != Keys.Length)
            throw new InvalidOperationException("Duplicate LedId detected.");

        if (Keys.Select(k => k.RawLedIndex).Distinct().Count() != Keys.Length)
            throw new InvalidOperationException("Duplicate raw LED index detected.");

        if (Keys.Max(k => k.RawLedIndex) != Hi75CProConstants.MaxRawLedIndex)
            throw new InvalidOperationException(
                $"Expected max raw index {Hi75CProConstants.MaxRawLedIndex}.");
    }
}