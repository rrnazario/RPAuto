using RPAuto.Helpers;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsInput.Native;

namespace RPAuto
{
    public partial class FrmMain : Form
    {
        private InterpretHelper interpreter;
        private string[] temporizerCommands = new string[] { "{TIMER:", "{REPEAT:" };
        private GlobalHotkey ghk;
        Thread thread;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            //Validate
            if (string.IsNullOrEmpty(rchCommands.Text))
            {
                MessageBox.Show("No commands found, please, type it.", Text);
                rchCommands.Focus();
                return;
            }

            var temporizer = temporizerCommands.Any(command => rchCommands.Text.ToUpper().Contains(command));

            if (temporizer)
                btnStartStop.Text = btnStartStop.Text == "Start" ? "Stop" : "Start";

            if (btnStartStop.Text == "Stop" || !temporizer)
                Interpret();
            else
                StopTimers();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            ghk = new GlobalHotkey(Constants.SHIFT, Keys.Escape, this);
            ghk.Register();

            FillProcessCombo();
        }

        /// <summary>
        /// Fill the combo with current running processes.
        /// </summary>
        private void FillProcessCombo()
        {
            cbbProcess.Items.Clear();

            cbbProcess.Items.AddRange(SystemHelper.GetApplicationProcessNames().ToArray());

            //Adds an empty line
            cbbProcess.Items.Insert(0, "");

            cbbProcess.SelectedIndex = 0;
        }

        /// <summary>
        /// Performs key interpretation.
        /// </summary>
        private void Interpret()
        {
            try
            {
                WindowState = FormWindowState.Minimized;

                //Perform delay
                if (!string.IsNullOrEmpty(txtDelay.Text))
                    Thread.Sleep(new TimeSpan(0, 0, int.TryParse(txtDelay.Text, out var delay) ? delay : 1));

                //Bring window if it is needed
                if (cbbProcess.SelectedIndex > 0)
                    SystemHelper.BringToFront(SystemHelper.FindProccessByDescription(cbbProcess.SelectedItem.ToString()));

                //Start process
                thread = new Thread(new ParameterizedThreadStart(CallInterpreterThread));
                thread.Start(rchCommands.Lines);
                
            }
            catch (Exception exc)
            {
                File.WriteAllText($"Error {DateTime.Now:yyMMdd HHmmSS}.txt", $"{exc.Message}\n\n{exc.StackTrace}\n\n{exc.InnerException?.Message}");
            }
            finally
            {
                if (!ckbPreventMax.Checked)
                    WindowState = FormWindowState.Normal;
            }
        }

        /// <summary>
        /// Create a method to thread be aborted if needed.
        /// </summary>
        /// <param name="obj"></param>
        private void CallInterpreterThread(object obj)
        {
            interpreter = new InterpretHelper();
            interpreter.Interpret(obj as string[]);
        }

        /// <summary>
        /// Stop all timers that were created on interpret method
        /// </summary>
        private void StopTimers()
        {
            if (interpreter?.timers != null && interpreter?.timers.Count() > 0)
                interpreter?.timers.ForEach(timer =>
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                });
        }

        /// <summary>
        /// Refresh process combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUp_Click(object sender, EventArgs e)
        {
            FillProcessCombo();
        }

        /// <summary>
        /// It mounts a help file and show it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHelp_Click(object sender, EventArgs e)
        {
            var helpFile = "help.txt";

            if (!File.Exists(helpFile))
                File.Delete(helpFile);

            {
                var msg = new StringBuilder("Commands:\n\n");

                msg.Append("To use text free, use it without brackets.\n");
                msg.Append("{WAIT:1000} = Time to wait between commands\n");
                msg.Append("{ENTER} = Break lines\n");
                msg.Append("{OPEN:PATH} = Open a file, if it exists. Example: {Open:notepad}, {Open:Path\\To\\File}\n");
                msg.Append("{TIMER:TIME} ... {COMMANDS} ... {TIMER} = Create a loop with inside commands that repeats every \"TIME\" interval. Example: {TIMER:3000}{OPEN:calc}{TIMER}\n");
                msg.Append("{REPEAT:COUNT} ... {COMMANDS} ... {REPEAT} = Repeats a command block for COUNT times. Example: {REPEAT:5}{OPEN:calc}{REPEAT}\n");
                msg.Append("{CONTROL,SHIFT,ALT,LWIN,RWIN:KEY} = To use modified keys. (Where KEY could be anything. Letters, numbers, etc. Example: {CONTROL,SHIFT:T} or {CONTROL:C})\n");
                msg.Append("Available key names:\n\n");

                Enum.GetValues(typeof(VirtualKeyCode)).Cast<VirtualKeyCode>().ToList().ForEach(f => msg.Append($"\t{f}\n"));

                File.WriteAllText(helpFile, msg.ToString());
            }

            Process.Start(helpFile);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            var result = ofd.ShowDialog();

            if (result == DialogResult.OK)
                try
                {
                    rchCommands.Text = File.ReadAllText(ofd.FileName);
                }
                catch (Exception)
                {
                    MessageBox.Show("It was impossible to read this file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            AbortProcess();
        }

        /// <summary>
        /// Stop possible current executions.
        /// </summary>
        private void AbortProcess()
        {
            thread?.Abort();
            StopTimers();
        }

        #region Methods to handle Windows HotKeys
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                AbortProcess();

            base.WndProc(ref m);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ghk.Unregiser())
                MessageBox.Show("Hotkey failed to unregister!");
        }

        #endregion
    }
}
