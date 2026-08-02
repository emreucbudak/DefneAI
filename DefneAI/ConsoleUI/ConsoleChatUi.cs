using System.Runtime.InteropServices;
using System.Text;

namespace DefneAI.ConsoleUI;

public sealed class ConsoleChatUi : IDisposable
{
    private const int ComposerHeight = 3;
    private const int MinimumWindowHeight = 4;
    private const int MinimumWindowWidth = 3;
    private const int StandardOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;
    private const string ControlSequenceIntroducer = "\u001b[";

    private readonly bool supportsFixedComposer;
    private int windowHeight;
    private int windowWidth;
    private bool isLayoutActive;
    private bool isDisposed;

    public ConsoleChatUi()
    {
        bool isInteractive =
            !Console.IsInputRedirected &&
            !Console.IsOutputRedirected;

        supportsFixedComposer =
            isInteractive &&
            TryEnableVirtualTerminalProcessing();

        if (!supportsFixedComposer)
        {
            return;
        }

        Console.CancelKeyPress += HandleCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;

        if (!CanUseFixedComposer())
        {
            return;
        }

        ConfigureLayoutPreservingCursor();
        isLayoutActive = true;

        SaveCursor();
        DrawComposerFrame();
        RestoreCursor();
        HideCursor();
    }

    public string ReadPrompt()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        if (!CanUseFixedComposer())
        {
            DeactivateLayout();
            return Console.ReadLine() ?? string.Empty;
        }

        if (!isLayoutActive)
        {
            ConfigureLayoutPreservingCursor();
            isLayoutActive = true;
        }
        else
        {
            RefreshLayoutIfNeeded();
        }

        SaveCursor();

        StringBuilder input = new();
        int cursorIndex = 0;
        int viewportStart = 0;

        DrawComposerFrame();
        ShowCursor();
        RenderInput(input, cursorIndex, ref viewportStart);

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (HasWindowSizeChanged() && CanUseFixedComposer())
            {
                UpdateLayout();
                DrawComposerFrame();
            }

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    ClearRow(InputRow);
                    RestoreCursor();
                    HideCursor();
                    return input.ToString();

                case ConsoleKey.Backspace when cursorIndex > 0:
                    input.Remove(cursorIndex - 1, 1);
                    cursorIndex--;
                    break;

                case ConsoleKey.Delete when cursorIndex < input.Length:
                    input.Remove(cursorIndex, 1);
                    break;

                case ConsoleKey.LeftArrow when cursorIndex > 0:
                    cursorIndex--;
                    break;

                case ConsoleKey.RightArrow when cursorIndex < input.Length:
                    cursorIndex++;
                    break;

                case ConsoleKey.Home:
                    cursorIndex = 0;
                    break;

                case ConsoleKey.End:
                    cursorIndex = input.Length;
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        input.Insert(cursorIndex, key.KeyChar);
                        cursorIndex++;
                    }

                    break;
            }

            RenderInput(input, cursorIndex, ref viewportStart);
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        if (supportsFixedComposer)
        {
            Console.CancelKeyPress -= HandleCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
        }

        RestoreTerminal();
    }

    private bool CanUseFixedComposer()
    {
        return supportsFixedComposer &&
               Console.WindowHeight >= MinimumWindowHeight &&
               Console.WindowWidth >= MinimumWindowWidth;
    }

    private void ConfigureLayoutPreservingCursor()
    {
        SaveCursor();
        UpdateLayout();
        RestoreCursor();
    }

    private void UpdateLayout()
    {
        windowHeight = Console.WindowHeight;
        windowWidth = Console.WindowWidth;

        int outputBottomRow = windowHeight - ComposerHeight;
        Console.Write(
            $"{ControlSequenceIntroducer}1;{outputBottomRow}r");
    }

    private void RefreshLayoutIfNeeded()
    {
        if (!HasWindowSizeChanged())
        {
            return;
        }

        ConfigureLayoutPreservingCursor();
    }

    private bool HasWindowSizeChanged()
    {
        return Console.WindowHeight != windowHeight ||
               Console.WindowWidth != windowWidth;
    }

    private void DrawComposerFrame()
    {
        DrawSeparator(UpperSeparatorRow);
        ClearRow(InputRow);
        DrawSeparator(LowerSeparatorRow);
    }

    private void DrawSeparator(int row)
    {
        MoveCursor(row, 1);
        ClearCurrentRow();
        Console.Write(
            new string('\u2500', Math.Max(1, windowWidth - 1)));
    }

    private void RenderInput(
        StringBuilder input,
        int cursorIndex,
        ref int viewportStart)
    {
        const int leftPadding = 1;
        int availableWidth =
            Math.Max(1, windowWidth - (leftPadding * 2));

        if (cursorIndex < viewportStart)
        {
            viewportStart = cursorIndex;
        }
        else if (cursorIndex >= viewportStart + availableWidth)
        {
            viewportStart =
                cursorIndex - availableWidth + 1;
        }

        int visibleLength =
            Math.Min(availableWidth, input.Length - viewportStart);
        string visibleInput = visibleLength > 0
            ? input.ToString(viewportStart, visibleLength)
            : string.Empty;

        MoveCursor(InputRow, 1);
        ClearCurrentRow();
        MoveCursor(InputRow, leftPadding + 1);
        Console.Write(visibleInput);

        int cursorColumn =
            leftPadding + (cursorIndex - viewportStart) + 1;
        MoveCursor(InputRow, cursorColumn);
    }

    private void DeactivateLayout()
    {
        if (!isLayoutActive)
        {
            return;
        }

        ResetScrollingRegion();
        ShowCursor();
        isLayoutActive = false;
    }

    private void RestoreTerminal()
    {
        if (!supportsFixedComposer)
        {
            return;
        }

        try
        {
            ResetScrollingRegion();
            ShowCursor();
            isLayoutActive = false;
        }
        catch (IOException)
        {
            // The terminal stream may already be closed during process exit.
        }
    }

    private void HandleCancelKeyPress(
        object? sender,
        ConsoleCancelEventArgs eventArgs)
    {
        RestoreTerminal();
    }

    private void HandleProcessExit(
        object? sender,
        EventArgs eventArgs)
    {
        RestoreTerminal();
    }

    private int UpperSeparatorRow => windowHeight - 2;

    private int InputRow => windowHeight - 1;

    private int LowerSeparatorRow => windowHeight;

    private static void SaveCursor() =>
        Console.Write($"{ControlSequenceIntroducer}s");

    private static void RestoreCursor() =>
        Console.Write($"{ControlSequenceIntroducer}u");

    private static void HideCursor() =>
        Console.Write($"{ControlSequenceIntroducer}?25l");

    private static void ShowCursor() =>
        Console.Write($"{ControlSequenceIntroducer}?25h");

    private static void MoveCursor(int row, int column) =>
        Console.Write(
            $"{ControlSequenceIntroducer}{row};{column}H");

    private static void ClearCurrentRow() =>
        Console.Write($"{ControlSequenceIntroducer}2K");

    private static void ClearRow(int row)
    {
        MoveCursor(row, 1);
        ClearCurrentRow();
    }

    private static void ResetScrollingRegion() =>
        Console.Write($"{ControlSequenceIntroducer}r");

    private static bool TryEnableVirtualTerminalProcessing()
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        nint outputHandle = GetStdHandle(StandardOutputHandle);
        if (outputHandle == nint.Zero ||
            outputHandle == new nint(-1))
        {
            return false;
        }

        return GetConsoleMode(
                   outputHandle,
                   out uint outputMode) &&
               SetConsoleMode(
                   outputHandle,
                   outputMode | EnableVirtualTerminalProcessing);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(
        nint consoleHandle,
        out uint consoleMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(
        nint consoleHandle,
        uint consoleMode);
}
