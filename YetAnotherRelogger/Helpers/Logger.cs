using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using YetAnotherRelogger.Helpers.Bot;

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
            Buffer = new List<LogMessage>();
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
                MessageBox.Show(ex.ToString(), "Creating log file failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _canLog = false;
            }
        }
        /// <summary>
        /// Write log message for active bot
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Write(string format, params object[] args)
        {
            var message = new LogMessage();
            if (Relogger.Instance.CurrentBot != null)
                message.Message = string.Format("<{0}> {1}", Relogger.Instance.CurrentBot.Name, string.Format(format, args));
            else
                message.Message = string.Format("[{0}] {1}", DateTime.Now, string.Format(format, args));
            instance.AddBuffer(message);
            addToRTB(message);
        }
        /// <summary>
        /// Write Log message for specific bot
        /// </summary>
        /// <param name="bot">BotClass</param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Write(BotClass bot, string format, params object[] args)
        {
            if (bot == null)
            {
                WriteGlobal(format, args);
                return;
            }
            var message = new LogMessage {Message = string.Format("<{0}> {1}", bot.Name, string.Format(format, args))};
            instance.AddBuffer(message);
            addToRTB(message);
        }

        /// <summary>
        /// Write global log message
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteGlobal(string format, params object[] args)
        {
            var message = new LogMessage {Message = string.Format("{0}", string.Format(format, args))};
            instance.AddBuffer(message);
            addToRTB(message);
        }

        /// <summary>
        /// Add custom log message to log buffer
        /// </summary>
        /// <param name="message">Logmessage</param>
        public void AddLogmessage(LogMessage message)
        {
            instance.AddBuffer(message);
            addToRTB(message);
        }

        private void addToRTB(LogMessage message)
        {
            if (Program.Mainform == null || Program.Mainform.richTextBox1 == null) return;

            try
            {
                Program.Mainform.Invoke(new Action(() =>
                    {
                        var rtb = Program.Mainform.richTextBox1;
                        var font = new Font("Tahoma", 8, FontStyle.Regular);
                        rtb.SelectionFont = font;
                        rtb.SelectionColor = message.Color;
                        var text = string.Format("{0} [{1}] {2}", LoglevelChar(message.Loglevel), message.TimeStamp, message.Message);
                        rtb.AppendText(text + Environment.NewLine);
                    }));
            }
            catch
            {
                // Failed! do nothing
            }
        }

        private List<LogMessage> Buffer;
        private void AddBuffer(LogMessage logmessage)
        {
            Buffer.Add(logmessage);
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
                foreach (var message in Buffer)
                {
                    writer.WriteLine("{0} [{1}] {2}", LoglevelChar(message.Loglevel), message.TimeStamp, message.Message);
                }
            }
            Buffer.Clear();
            _canLog = true;
        }
        /// <summary>
        /// Get Loglevel char
        /// </summary>
        /// <param name="loglevel">Loglevel</param>
        /// <returns>char</returns>
        public char LoglevelChar(Loglevel loglevel)
        {
            switch (loglevel)
            {
                case Loglevel.Debug:
                    return 'D';
                case Loglevel.Verbose:
                    return 'V';
                default:
                    return 'N';
            }
        }
    }
    public class LogMessage : IDisposable
    {
        public LogMessage(Color color, Loglevel loglevel, string message, params object[] args)
        {
            TimeStamp = DateTime.Now;
            Loglevel = Loglevel.Normal;
        }
        public LogMessage()
        {
            Color = Color.Black;
            TimeStamp = DateTime.Now;
            Loglevel = Loglevel.Normal;
        }
        public Color Color;
        public Loglevel Loglevel;
        public DateTime TimeStamp { get; private set; }
        public string Message;

        public void Dispose()
        {
            Message = null;
        }
    }
    public enum Loglevel
    {
        Normal,
        Verbose,
        Debug
    }
}
