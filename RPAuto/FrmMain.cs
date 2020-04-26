using RPAuto.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPAuto
{
    public partial class FrmMain : Form
    {
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

            Interpret();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            FillProcessCombo();
        }

        private void FillProcessCombo()
        {
            cbbProcess.Items.Clear();

            cbbProcess.Items.AddRange(SystemHelper.GetApplicationProcessNames().ToArray());

            //Adds an empty line
            cbbProcess.Items.Insert(0, "");

            cbbProcess.SelectedIndex = 0;
        }

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

                new InterpretHelper().Interpret(rchCommands.Lines);
            }
            catch (Exception exc)
            {
                File.WriteAllText($"Error {DateTime.Now:yyMMdd HHmmSS}.txt", exc.Message);                
            }
            finally
            {
                WindowState = FormWindowState.Normal;
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            FillProcessCombo();
        }
    }
}
