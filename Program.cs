using System;
using System.Linq;
using System.Text;
using Raylib_CsLo;
using MeltySynth;

public static class Program
{
    private static readonly int sampleRate = 48000;
    private static readonly int bufferSize = 2048;

    private static readonly int screenWidth = 1600;
    private static readonly int screenHeight = 400;

    private static readonly int listViewTitleX = 10;
    private static readonly int listViewTitleY = 10;
    private static readonly int listViewX = listViewTitleX;
    private static readonly int listViewY = listViewTitleY + 15;
    private static readonly int listViewWidth = 150;
    private static readonly int listViewHeight = screenHeight - listViewY - 10;

    private static readonly int velocityBarTitleX = listViewX + listViewWidth + 30;
    private static readonly int velocityBarTitleY = 10;
    private static readonly int velocityBarX = velocityBarTitleX;
    private static readonly int velocityBarY = velocityBarTitleY + 15;
    private static readonly int velocityBarWidth = 200;
    private static readonly int velocityBarHeight = 20;

    private static readonly int vibratoBarTitleX = listViewX + listViewWidth + 30;
    private static readonly int vibratoBarTitleY = velocityBarY + velocityBarHeight + 10;
    private static readonly int vibratoBarX = vibratoBarTitleX;
    private static readonly int vibratoBarY = vibratoBarTitleY + 15;
    private static readonly int vibratoBarWidth = 200;
    private static readonly int vibratoBarHeight = 20;

    private static readonly int reverbBarTitleX = listViewX + listViewWidth + 30;
    private static readonly int reverbBarTitleY = vibratoBarY + vibratoBarHeight + 10;
    private static readonly int reverbBarX = reverbBarTitleX;
    private static readonly int reverbBarY = reverbBarTitleY + 15;
    private static readonly int reverbBarWidth = 200;
    private static readonly int reverbBarHeight = 20;

    private static readonly int chorusBarTitleX = listViewX + listViewWidth + 30;
    private static readonly int chorusBarTitleY = reverbBarY + reverbBarHeight + 10;
    private static readonly int chorusBarX = chorusBarTitleX;
    private static readonly int chorusBarY = chorusBarTitleY + 15;
    private static readonly int chorusBarWidth = 200;
    private static readonly int chorusBarHeight = 20;

    private static readonly int keyboardX = listViewX + listViewWidth + 10;
    private static readonly int keyboardY = chorusBarY + chorusBarHeight + 20;
    private static readonly int keyboardWidth = screenWidth - keyboardX - 10;
    private static readonly int keyboardHeight = screenHeight - keyboardY - 10;

    private static readonly int firstOctave = 2;
    private static readonly int octaveCount = 6;

    unsafe static void Main()
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Virtual Keyboard");

        Raylib.InitAudioDevice();
        Raylib.SetAudioStreamBufferSizeDefault(bufferSize);

        var stream = Raylib.LoadAudioStream((uint)sampleRate, 16, 2);
        var buffer = new short[2 * bufferSize];

        Raylib.PlayAudioStream(stream);

        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        Raylib.SetTargetFPS(60);

        var presets = synthesizer.SoundFont.Presets.OrderBy(x => x.BankNumber).ThenBy(x => x.PatchNumber).ToArray();
        var listViewData = GetListViewData(presets);
        var listViewScroll = 0;
        var previousPresetIndex = 0;
        var currentPresetIndex = 0;

        var velocity = 100;
        var vibrato = 0;
        var reverb = 40;
        var chorus = 0;

        var previousKey = -1;
        var currentKey = -1;

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsAudioStreamProcessed(stream))
            {
                synthesizer.RenderInterleavedInt16(buffer);
                fixed (void* p = buffer)
                {
                    Raylib.UpdateAudioStream(stream, p, bufferSize);
                }
            }

            Raylib.BeginDrawing();

            Raylib.ClearBackground(Raylib.LIGHTGRAY);

            Raylib.DrawText("Instruments", listViewTitleX, listViewTitleY, 10, Raylib.DARKGRAY);
            RayGui.GuiSetStyle((int)GuiControl.DEFAULT, (int)GuiControlProperty.TEXT_ALIGNMENT, (int)GuiTextAlignment.GUI_TEXT_ALIGN_LEFT);
            previousPresetIndex = currentPresetIndex;
            currentPresetIndex = RayGui.GuiListView(
                new Rectangle(listViewX, listViewY, listViewWidth, listViewHeight),
                listViewData, &listViewScroll, currentPresetIndex);
            if (currentPresetIndex == -1)
            {
                currentPresetIndex = previousPresetIndex;
            }
            else
            {
                synthesizer.ProcessMidiMessage(0, 0xC0, presets[currentPresetIndex].PatchNumber, 0);
            }

            Raylib.DrawText("Velocity", velocityBarTitleX, velocityBarTitleY, 10, Raylib.DARKGRAY);
            velocity = (int)RayGui.GuiSlider(
                new Rectangle(velocityBarX, velocityBarY, velocityBarWidth, velocityBarHeight),
                "Min", "Max", velocity, 0, 127);

            Raylib.DrawText("Vibrato", vibratoBarTitleX, vibratoBarTitleY, 10, Raylib.DARKGRAY);
            vibrato = (int)RayGui.GuiSlider(
                new Rectangle(vibratoBarX, vibratoBarY, vibratoBarWidth, vibratoBarHeight),
                "Min", "Max", vibrato, 0, 127);
            synthesizer.ProcessMidiMessage(0, 0xB0, 0x01, vibrato);

            Raylib.DrawText("Reverb", reverbBarTitleX, reverbBarTitleY, 10, Raylib.DARKGRAY);
            reverb = (int)RayGui.GuiSlider(
                new Rectangle(reverbBarX, reverbBarY, reverbBarWidth, reverbBarHeight),
                "Min", "Max", reverb, 0, 127);
            synthesizer.ProcessMidiMessage(0, 0xB0, 0x5B, reverb);

            Raylib.DrawText("Chorus", chorusBarTitleX, chorusBarTitleY, 10, Raylib.DARKGRAY);
            chorus = (int)RayGui.GuiSlider(
                new Rectangle(chorusBarX, chorusBarY, chorusBarWidth, chorusBarHeight),
                "Min", "Max", chorus, 0, 127);
            synthesizer.ProcessMidiMessage(0, 0xB0, 0x5D, chorus);

            previousKey = currentKey;
            currentKey = GetCurrentKey(keyboardX, keyboardY, keyboardWidth, keyboardHeight, firstOctave, octaveCount);
            var keyColor = Raylib.SKYBLUE;
            if (currentKey != -1)
            {
                keyColor = Raylib.BLUE;

                if ((currentKey != previousKey && Raylib.IsMouseButtonDown(0)) || Raylib.IsMouseButtonPressed(0))
                {
                    synthesizer.NoteOffAll(0, false);
                    synthesizer.NoteOn(0, currentKey, velocity);
                }
            }
            if (currentKey == -1 || !Raylib.IsMouseButtonDown(0))
            {
                synthesizer.NoteOffAll(0, false);
            }
            DrawKeyboard(keyboardX, keyboardY, keyboardWidth, keyboardHeight, firstOctave, octaveCount, currentKey, keyColor);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private static string GetListViewData(Preset[] presets)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];

            sb.Append(preset.Name);

            if (i < presets.Length - 1)
            {
                sb.Append(";");
            }
        }

        return sb.ToString();
    }

    private static int GetCurrentKey(int x, int y, int width, int height, int firstOctave, int octaveCount)
    {
        var mouse = Raylib.GetMousePosition();

        if (mouse.X < x || x + width < mouse.X)
        {
            return -1;
        }

        if (mouse.Y < y || y + height < mouse.Y)
        {
            return -1;
        }

        var blackHeight = height * 95 / 150;

        if (mouse.Y < y + blackHeight)
        {
            for (var octave = 0; octave < octaveCount; octave++)
            {
                var keyBase = 60 + 12 * (firstOctave - 4 + octave);

                for (var leftBlack = 0; leftBlack < 2; leftBlack++)
                {
                    var x1 = (int)(x + (7 * octave + 3 * (double)(2 * leftBlack + 1) / 5) * ((double)width / octaveCount / 7));
                    var x2 = (int)(x + (7 * octave + 3 * (double)(2 * leftBlack + 2) / 5) * ((double)width / octaveCount / 7));

                    if (x1 < mouse.X && mouse.X < x2)
                    {
                        return keyBase + LeftBlackToKeyNumber(leftBlack);
                    }
                }

                for (var rightBlack = 0; rightBlack < 3; rightBlack++)
                {
                    var x1 = (int)(x + (7 * octave + 3 + 4 * (double)(2 * rightBlack + 1) / 7) * ((double)width / octaveCount / 7));
                    var x2 = (int)(x + (7 * octave + 3 + 4 * (double)(2 * rightBlack + 2) / 7) * ((double)width / octaveCount / 7));

                    if (x1 < mouse.X && mouse.X < x2)
                    {
                        return keyBase + RightBlackToKeyNumber(rightBlack);
                    }
                }
            }
        }

        for (var octave = 0; octave < octaveCount; octave++)
        {
            var keyBase = 60 + 12 * (firstOctave - 4 + octave);

            for (var white = 0; white < 7; white++)
            {
                var x1 = (int)(x + (7 * octave + white) * ((double)width / octaveCount / 7)) + 1;
                var x2 = (int)(x + (7 * octave + white + 1) * ((double)width / octaveCount / 7));

                if (x1 < mouse.X && mouse.X < x2)
                {
                    return keyBase + WhiteToKeyNumber(white);
                }
            }
        }

        return -1;
    }

    private static void DrawKeyboard(int x, int y, int width, int height, int firstOctave, int octaveCount, int coloredKey, Color coloredKeyColor)
    {
        Raylib.DrawRectangle(x, y, width, height, Raylib.WHITE);

        var blackHeight = height * 95 / 150;

        for (var octave = 0; octave < octaveCount; octave++)
        {
            var keyBase = 60 + 12 * (firstOctave - 4 + octave);

            for (var white = 0; white < 7; white++)
            {
                var x1 = (int)(x + (7 * octave + white) * ((double)width / octaveCount / 7)) + 1;

                var key = keyBase + WhiteToKeyNumber(white);
                if (key == coloredKey)
                {
                    var x2 = (int)(x + (7 * octave + white + 1) * ((double)width / octaveCount / 7));
                    Raylib.DrawRectangle(x1, y + 1, x2 - x1, height - 2, coloredKeyColor);
                }

                Raylib.DrawLine(x1, y, x1, y + height, Raylib.GRAY);
            }

            for (var leftBlack = 0; leftBlack < 2; leftBlack++)
            {
                var x1 = (int)(x + (7 * octave + 3 * (double)(2 * leftBlack + 1) / 5) * ((double)width / octaveCount / 7));
                var x2 = (int)(x + (7 * octave + 3 * (double)(2 * leftBlack + 2) / 5) * ((double)width / octaveCount / 7));

                var key = keyBase + LeftBlackToKeyNumber(leftBlack);
                if (key == coloredKey)
                {
                    Raylib.DrawRectangle(x1, y + 1, x2 - x1, blackHeight - 1, coloredKeyColor);
                }
                else
                {
                    Raylib.DrawRectangle(x1, y, x2 - x1, blackHeight, Raylib.DARKGRAY);
                }
            }

            for (var rightBlack = 0; rightBlack < 3; rightBlack++)
            {
                var x1 = (int)(x + (7 * octave + 3 + 4 * (double)(2 * rightBlack + 1) / 7) * ((double)width / octaveCount / 7));
                var x2 = (int)(x + (7 * octave + 3 + 4 * (double)(2 * rightBlack + 2) / 7) * ((double)width / octaveCount / 7));

                var key = keyBase + RightBlackToKeyNumber(rightBlack);
                if (key == coloredKey)
                {
                    Raylib.DrawRectangle(x1, y + 1, x2 - x1, blackHeight - 1, coloredKeyColor);
                }
                else
                {
                    Raylib.DrawRectangle(x1, y, x2 - x1, blackHeight, Raylib.DARKGRAY);
                }
            }
        }

        Raylib.DrawRectangleLines(x, y, width, height, Raylib.GRAY);
    }

    private static int WhiteToKeyNumber(int white)
    {
        switch (white)
        {
            case 0:
                return 0;
            case 1:
                return 2;
            case 2:
                return 4;
            case 3:
                return 5;
            case 4:
                return 7;
            case 5:
                return 9;
            case 6:
                return 11;
            default:
                throw new Exception();
        }
    }

    private static int LeftBlackToKeyNumber(int leftBlack)
    {
        switch (leftBlack)
        {
            case 0:
                return 1;
            case 1:
                return 3;
            default:
                throw new Exception();
        }
    }

    private static int RightBlackToKeyNumber(int rightBlack)
    {
        switch (rightBlack)
        {
            case 0:
                return 6;
            case 1:
                return 8;
            case 2:
                return 10;
            default:
                throw new Exception();
        }
    }
}
