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
                if (copyData.dwData == (Win32.WM_USER + 1))
                {
                    Timer.Interval = TimeoutMillisecs; //this is a timer "reset"
                    OnNewValue(copyData.lpData);
                    //MessageBox.Show($"received: {fileName}", "SingleInstanceArgAggregator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Unrecognized message id, expecting {Win32.WM_USER + 1}, got {copyData.dwData}.");
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
                MessageBox.Show($"Couldn't find window named: {windowTitle}");
                return false;
            }

            //good example of COPYDATA + SendMessage: https://forum.strokesplus.net/posts/t6052-Using-Windows-Messages--WM-COPYDATA--to-Execute-Scripts
            try
            {
                // Create the data structure and fill with data
                Win32.COPYDATASTRUCT copyData = new Win32.COPYDATASTRUCT
                {
                    dwData = new IntPtr(Win32.WM_USER + 1),    // Just an arbitrary number to make sure we're getting our specific message on the other side
                    cbData = System.Text.Encoding.Unicode.GetBytes(data).Length + 1,  // One extra byte for the \0 character
                    lpData = data
                };

                // Send the message
                Win32.SendMessage(ptrWnd, Win32.WM_COPYDATA, IntPtr.Zero, ref copyData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), windowTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;

        }

    }
}
