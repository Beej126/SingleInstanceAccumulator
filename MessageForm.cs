using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SingleInstanceArgAggregator
{
    public delegate void NewValueDelegate(string Value);

    public partial class SingleInstanceMessageForm : Form
    {
        private readonly System.Timers.Timer Timer;
        private readonly int TimeoutMillisecs;
        private readonly Action<string> OnNewValue;

        public SingleInstanceMessageForm(string windowTitle, Action<string> onNewValue, int timeoutMillisecs)
        {
            Text = windowTitle;
            OnNewValue = onNewValue;
            TimeoutMillisecs = timeoutMillisecs;
            Timer = new System.Timers.Timer(TimeoutMillisecs);

            //Application.Run does an unavoidable Show(), these properties essentially hide the window
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
        }

        //private void Form1_Load(object sender, EventArgs e)
        protected override void OnLoad(EventArgs e)
        {
            Win32.CHANGEFILTERSTRUCT changeFilter = new();
            changeFilter.size = (uint)Marshal.SizeOf(changeFilter);
            changeFilter.info = 0;
            if (!Win32.ChangeWindowMessageFilterEx(this.Handle, Win32.WM_COPYDATA, Win32.ChangeWindowMessageFilterExAction.Allow, ref changeFilter))
            {
                int error = Marshal.GetLastWin32Error();
                MessageBox.Show(String.Format("The error {0} occured.", error));
                return;
            }

            Timer.Elapsed += Timer_Elapsed;
            Timer.Start();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Timer.Stop();
            // https://stackoverflow.com/questions/10084691/close-a-form-from-an-external-thread-using-the-invoke-method
            Invoke((MethodInvoker)delegate () { this.Close(); });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_COPYDATA)
            {
                // Extract the file name
                Win32.COPYDATASTRUCT copyData = (Win32.COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(Win32.COPYDATASTRUCT));
                int dataType = (int)copyData.dwData;
                if (dataType == 2)
                {
                    string fileName = Marshal.PtrToStringAnsi(copyData.lpData);
                    Timer.Interval = TimeoutMillisecs; //this is a timer "reset"
                    OnNewValue(fileName);
                    //MessageBox.Show($"received: {fileName}", "SingleInstanceArgAggregator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show(String.Format("Unrecognized data type = {0}.", dataType), "SingleInstanceArgAggregator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        const int retryMaxSeconds = 2;
        const int sleepMilliseconds = 100;

        public static bool SendStringToWindow(string windowTitle, string data)
        {
            //wait & retry while initial window is spinning up
            var retries = retryMaxSeconds * 1000 / sleepMilliseconds;

            IntPtr ptrWnd = IntPtr.Zero;
            while ((ptrWnd = Win32.FindWindow(null, windowTitle)) == IntPtr.Zero && --retries > 0)
                System.Threading.Thread.Sleep(sleepMilliseconds);

            if (retries == 0)
            {
                MessageBox.Show($"Couldn't find window named: {windowTitle}", "SingleInstanceArgAggregator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var ptrCopyData = IntPtr.Zero;
            try
            {
                // Create the data structure and fill with data
                Win32.COPYDATASTRUCT copyData = new Win32.COPYDATASTRUCT
                {
                    dwData = new IntPtr(2),    // Just a number to identify the data type
                    cbData = data.Length + 1,  // One extra byte for the \0 character
                    lpData = Marshal.StringToHGlobalAnsi(data)
                };

                // Allocate memory for the data and copy
                ptrCopyData = Marshal.AllocCoTaskMem(Marshal.SizeOf(copyData));
                Marshal.StructureToPtr(copyData, ptrCopyData, false);

                // Send the message
                Win32.SendMessage(ptrWnd, Win32.WM_COPYDATA, IntPtr.Zero, ptrCopyData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), windowTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                // Free the allocated memory after the contol has been returned
                if (ptrCopyData != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(ptrCopyData);
            }

            return true;

        }

    }
}
