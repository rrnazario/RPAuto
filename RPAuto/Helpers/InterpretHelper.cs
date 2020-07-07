using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace RPAuto.Helpers
{
    public class InterpretHelper
    {
        private InputSimulator inputter;
        private IEnumerable<VirtualKeyCode> keyCodes = Enum.GetValues(typeof(VirtualKeyCode)).Cast<VirtualKeyCode>();
        public InterpretHelper()
        {
            var key = new KeyboardSimulator(new InputSimulator());
            var mouse = new MouseSimulator(new InputSimulator());

            inputter = new InputSimulator(key, mouse, null);
        }

        public void Interpret(IEnumerable<string> textList)
        {
            var keyword = "";
            foreach (var line in textList)
            {
                try
                {
                    keyword = "";
                    for (int index = 0; index < line.Length; index++)
                    {
                        keyword += line[index];

                        if (keyword.StartsWith("{"))
                        {
                            index++;
                            while (line[index] != '}')
                            {
                                keyword += line[index];
                                index++;
                            }
                            keyword += line[index];

                            if (keyword.Contains(":")) //Complex keywords                        
                                interpretComplexKeys(keyword);
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
                }
                catch (Exception exc)
                {
                    throw new Exception($"Erro em {line}, key = {keyword}.\n\n{exc.Message}\n\n\n{exc.StackTrace}");
                }
            }
        }

        private void typeFreeText(string text)
        {
            inputter.Keyboard.TextEntry(text);
            inputter.Keyboard.Sleep(100);
        }
        private void interpretComplexKeys(string text)
        {
            var values = text.Split(':');

            string keyStr = Clean(values[0]),
                   secondStatement = Clean(values[1]);

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
                default:
                    typeFreeText(text);
                    break;
            }
        }

        private void interpretModifiers(string[] values)
        {
            string keyStr = Clean(values[0]),
                   secondStatement = Clean(values[1]);

            var modifiers = keyStr.Split(',').Select(s => keyCodes.First(f => f.ToString().Equals($"{s}")));

            VirtualKeyCode key = secondStatement.Length == 1
            ? keyCodes.First(f => f.ToString().Equals($"VK_{secondStatement}"))
            : keyCodes.First(f => f.ToString().Equals($"{secondStatement}"));

            inputter.Keyboard.ModifiedKeyStroke(modifiers, key);

        }
        private void interpretSimpleKeys(string text)
        {
            text = Translate(Clean(text));

            var modifier = keyCodes.FirstOrDefault(f => f.ToString().Equals($"{text}"));

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
                default:
                    return input;
            }
        }

        #endregion
    }
}
