using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace YetAnotherRelogger.Helpers
{
    public sealed class Logger
    {

        #region singleton
        static readonly Logger instance = new Logger();

        static Logger()
        {
        }

        Logger()
        {
            Buffer = new List<string>();
            Initialize();
        }

        public static Logger Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion


        private string _logfile;
        private bool _canLog;
        private void Initialize()
        {
            var filename = string.Format("{0:yyyy-MM-dd HH.mm}", DateTime.Now);
            _logfile = string.Format(@"{0}\Logs\{1}.txt", Path.GetDirectoryName(Application.ExecutablePath), filename);
            Debug.WriteLine(_logfile);
            
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_logfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_logfile));
                _canLog = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _canLog = false;
            }
        }

        public void Write(string format, params object[] args)
        {
            string output;
            if (Relogger.Instance.CurrentBot != null)
                output = string.Format("[{0}] <{1}> {2}", DateTime.Now, Relogger.Instance.CurrentBot.Name, string.Format(format, args));
            else
                output = string.Format("[{0}] {1}", DateTime.Now, string.Format(format, args));
            instance.AddBuffer(output);
            addToRTB(output);
        }
        public void Write(Bot.BotClass bot, string format, params object[] args)
        {
            if (bot == null)
            {
                WriteGlobal(format, args);
                return;
            }
            var output = string.Format("[{0}] <{1}> {2}", DateTime.Now, bot.Name, string.Format(format, args));
            instance.AddBuffer(output);
            addToRTB(output);
        }

        public void WriteGlobal(string format, params object[] args)
        {
            format = string.Format("[{0}] {1}", DateTime.Now, string.Format(format, args));
            instance.AddBuffer(format);
            addToRTB(format);
        }

        private void addToRTB(string text)
        {
            if (Program.Mainform != null && Program.Mainform.richTextBox1 != null && Process.GetCurrentProcess().MainWindowHandle != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(
                                delegate()
                                {
                                    Program.Mainform.richTextBox1.AppendText(text + Environment.NewLine);
                                }
                    ));
                }
                catch
                {
                    // Failed! do nothing
                }
            }
        }

        private List<string> Buffer;
        private void AddBuffer(string value)
        {
            Buffer.Add(value);
            if (Buffer.Count > 3)
                ClearBuffer();
        }

        public void ClearBuffer()
        {
            if (!_canLog) return;
            _canLog = false;

            // Write buffer to file
            using (var writer = new StreamWriter(_logfile, true))
            {
                foreach (var line in Buffer)
                    writer.WriteLine(line);
            }
            Buffer.Clear();
            _canLog = true;
        }
    }
}
