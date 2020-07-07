using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace RPAuto.Helpers
{
    public class InterpretHelper
    {
        private InputSimulator inputter;
        private IEnumerable<VirtualKeyCode> enumList = Enum.GetValues(typeof(VirtualKeyCode)).Cast<VirtualKeyCode>();

        public List<System.Timers.Timer> timers;
        public InterpretHelper()
        {
            var key = new KeyboardSimulator(new InputSimulator());
            var mouse = new MouseSimulator(new InputSimulator());

            inputter = new InputSimulator(key, mouse, null);

            timers = new List<System.Timers.Timer>();
        }

        public void Interpret(IEnumerable<string> textList)
        {
            Interpret(string.Join("\n", textList));
        }
        public void Interpret(string fullText)
        {
            var keyword = "";

            for (int index = 0; index < fullText.Length; index++)
            {
                keyword += fullText[index];

                if (keyword.StartsWith("{"))
                {
                    index++;
                    while (fullText[index] != '}' && index < fullText.Length - 1)
                    {
                        keyword += fullText[index];
                        index++;
                    }
                    keyword += fullText[index];

                    if (keyword.Contains(":"))
                    {
                        if (keyword.ToUpper().StartsWith("{TIMER:"))
                        {
                            index = GenerateTimer(keyword, fullText, index);
                            keyword = "";
                            continue;
                        }
                        else
                        if (keyword.ToUpper().StartsWith("{REPEAT:"))
                        {
                            index = GenerateRepetition(keyword, fullText, index);
                            keyword = "";
                            continue;
                        }
                        else
                            interpretComplexKeys(keyword);
                    }
                    else
                        interpretSimpleKeys(keyword);

                    inputter.Keyboard.Sleep(150);
                    keyword = "";
                }
                else
                {
                    typeFreeText(keyword);
                    keyword = "";
                }
            }

            timers.ForEach(timer => timer.Start());
        }

        /// <summary>
        /// Replace the commands inside repetition statement and return it.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="fullText"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int GenerateRepetition(string keyword, string fullText, int index)
        {
            var endBlockWord = "{REPEAT}";
            var endBlockIndex = fullText.ToUpper().IndexOf(endBlockWord, index);
            var repeatInstructions = fullText.Substring(index + 1, endBlockIndex - index - 1);

            var times = int.Parse(Clean(keyword.Split(':').Last()));

            for (int i = 1; i <= times; i++)
            {
                Interpret(repeatInstructions.Replace("{%}", i.ToString()).Split('\n'));
                Thread.Sleep(1000);
            }

            return endBlockIndex + endBlockWord.Length;
        }

        /// <summary>
        /// Generates a timer to timers set with the instructions inside of statement.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="fullString"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int GenerateTimer(string keyword, string fullString, int index)
        {
            var endTimerWord = "{TIMER}";
            var endTimerIndex = fullString.ToUpper().IndexOf(endTimerWord, index);
            var timerInstructions = fullString.Substring(index + 1, endTimerIndex - index - 1);

            var timer = new System.Timers.Timer()
            {
                Interval = double.Parse(Clean(keyword.Split(':').Last())),
                Enabled = false
            };

            int times = 1;
            timer.Elapsed += (s, e) =>
            {
                Interpret(timerInstructions.Replace("{%}", times.ToString()).Split('\n'));
                times++;
            };

            timers.Add(timer);

            return endTimerIndex + endTimerWord.Length;
        }

        private void typeFreeText(string text)
        {
            if (text == "\n") return;

            inputter.Keyboard.TextEntry(text);
            inputter.Keyboard.Sleep(100);
        }
        private void interpretComplexKeys(string text)
        {
            var values = text.Split(':');

            string keyStr = Clean(values[0]),
                   secondStatement = Clean(string.Join(":", values.Skip(1)));

            var modifiers = new string[] { "CONTROL", "ALT", "SHIFT", "LWIN", "RWIN" };

            if (modifiers.Any(a => a.Equals(keyStr.Split(',').First())))
            {
                interpretModifiers(values);
                return;
            }

            switch (keyStr)
            {
                case "WAIT":
                    inputter.Keyboard.Sleep(int.TryParse(secondStatement, out var passed) ? passed : 0);
                    break;
                case "OPEN":
                    try
                    {
                        Process.Start(secondStatement);
                    }
                    catch { }
                    break;                
                default:
                    typeFreeText(text);
                    break;
            }
        }

        private void interpretModifiers(string[] values)
        {
            string keyStr = Clean(values[0]),
                   secondStatement = Clean(values[1]);

            var modifiers = keyStr.Split(',').Select(s => enumList.First(f => f.ToString().Equals($"{Translate(s)}")));

            VirtualKeyCode key = secondStatement.Length == 1
            ? enumList.First(f => f.ToString().Equals($"VK_{secondStatement}"))
            : enumList.First(f => f.ToString().Equals($"{secondStatement}"));

            inputter.Keyboard.ModifiedKeyStroke(modifiers, key);

        }
        private void interpretSimpleKeys(string text)
        {
            text = Translate(Clean(text));

            var modifier = enumList.FirstOrDefault(f => f.ToString().Equals($"{text}"));

            inputter.Keyboard.KeyDown(modifier);
        }

        #region String Methods
        private string Clean(string input) => input.Replace("{", "").Replace("}", "").ToUpper();
        private string Translate(string input)
        {
            switch (input.ToUpper())
            {
                case "ENTER":
                    return "RETURN";
                case "ALT":
                    return "MENU";
                default:
                    return input;
            }
        }

        #endregion
    }
}