using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;
using System.Net;

namespace YetAnotherRelogger.Helpers
{
    public class ConnectionCheck
    {
        
        #region IP / Host check
        // Get external IP from: checkip.dyndns.org
        private static DateTime _lastcheck;
        private static bool _laststate;
        public static bool ValidConnection
        {
            get
            {
                if (!Settings.Default.ConnectionCheckIpHostCloseBots) return true;

                // Check internet every 60 seconds
                if (General.DateSubtract(_lastcheck) > 60)
                {
                    _lastcheck = DateTime.Now;
                    if (!CheckValidConnection(true))
                    {
                        _laststate = false;
                        Logger.Instance.Write("Invalid external IP or Hostname!");
                        foreach (var bot in BotSettings.Instance.Bots.Where(bot => bot != null && bot.IsRunning))
                        {
                            if (bot.Diablo.IsRunning || bot.Demonbuddy.IsRunning)
                            {
                                Logger.Instance.Write(bot, "Stopping bot (No Internet Connection!)");
                                bot.Diablo.Stop();
                                bot.Demonbuddy.Stop();
                            }
                            bot.Status = "Waiting on internet connection";
                        }
                    }
                    else
                    {
                        _laststate = true;
                    }
                }
                return _laststate;
            }
        }
        public static bool CheckValidConnection(bool silent = false)
        {
            try
            {
                var wc = new WebClient();
                wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                var data = wc.OpenRead("http://checkip.dyndns.org");

                var ip = string.Empty;
                var hostname = string.Empty;
                if (data != null)
                {
                    using (var reader = new StreamReader(data))
                    {
                        var s = reader.ReadToEnd();
                        var m = new Regex(@".*Current IP Address: ([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}).*").Match(s);
                        if (m.Success)
                        {
                            ip = m.Groups[1].Value;
                            try
                            {
                                hostname = Dns.GetHostEntry(ip).HostName;
                            }
                            catch (Exception)
                            {
                                //Logger.Instance.WriteGlobal("ValidConnection: {0}", ex.Message);
                            }
                            if (!silent) Logger.Instance.WriteGlobal("ValidConnection: IP {0}{1}", ip, !string.IsNullOrEmpty(hostname) ? " HostName: " + hostname : "");

                            if (!validIp(ip, silent)) return false;
                            if (!string.IsNullOrEmpty(hostname) && !validHost(hostname, silent)) return false;

                        }
                        else
                        {
                            throw new Exception("No IP found!");
                        }
                    }
                   // data.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("ValidConnection: {0}",ex.Message);
                return false;
            }
            return true;
        }

        private static bool validIp(string ip, bool silent)
        {
            // Always return true when IP Check is disabled
            if (!Settings.Default.ConnectionCheckIpCheck) return true;
            foreach (var line in Settings.Default.ConnectionCheckIpHostList.Split('\n'))
            {
                var test = line.Replace(" ", string.Empty);

                var m = new Regex(@"([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})-([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})").Match(test);
                if (m.Success)
                {
                    var lowerip = IPAddress.Parse(m.Groups[1].Value);
                    var higherip = IPAddress.Parse(m.Groups[2].Value);
                    var inrange = new IPAddressRange(lowerip, higherip).IsInRange(IPAddress.Parse(ip));
                    if (inrange)
                    {
                        Logger.Instance.WriteGlobal("ValidConnection: IP {0} in range -> {1}-{2}", ip, lowerip,higherip);
                        return false;
                    }
                    continue;
                }

                m = new Regex(@"([0-9*]{1,3}\.[0-9*]{1,3}\.[0-9*]{1,3}\.[0-9*]{1,3})").Match(test);
                if (!m.Success) continue;

                test = m.Groups[1].Value;
                if (General.WildcardMatch(test, ip))
                {
                    Logger.Instance.WriteGlobal("ValidConnection: IP match {0} -> {1}", ip,test);
                    return false;
                }
            }
            return true;
        }
        private static bool validHost(string hostname, bool silent)
        {
            // Always return true when Host Check is disabled
            if (!Settings.Default.ConnectionCheckHostCheck) return true;
            foreach (var line in Settings.Default.ConnectionCheckIpHostList.Split('\n'))
            {
                var test = line.Replace(" ", string.Empty);
                var m = new Regex(@"([0-9*]{1,3}\.[0-9*]{1,3}\.[0-9*]{1,3}\.[0-9*]{1,3})").Match(test);

                if (m.Success) continue;

                if (General.WildcardMatch(test.ToLower(), hostname.ToLower()))
                {
                    Logger.Instance.WriteGlobal("ValidConnection: Host match {0} -> {1}", hostname, test);
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region PingCheck & IsConnected
        private static DateTime _lastpingcheck;
        private static bool _lastpingstate;
        public static bool IsConnected
        {
            get
            {
                if (!Settings.Default.ConnectionCheckCloseBots) return true;
                // Check internet every 60 seconds
                if (General.DateSubtract(_lastpingcheck) > 60)
                {
                    _lastpingcheck = DateTime.Now;
                    if (!PingCheck(true))
                    {
                        _lastpingstate = false;
                        foreach (var bot in BotSettings.Instance.Bots.Where(bot => bot != null && bot.IsRunning))
                        {
                            if (bot.Diablo.IsRunning || bot.Demonbuddy.IsRunning)
                            {
                                Logger.Instance.Write(bot, "Stopping bot (No Internet Connection!)");
                                bot.Diablo.Stop();
                                bot.Demonbuddy.Stop();
                            }
                            bot.Status = "Waiting on internet connection";
                        }
                    }
                    else
                    {
                        _lastpingstate = true;
                    }
                }
                return _lastpingstate;
            }
        }

        public static bool PingCheck(bool silent = false)
        {
            var ping = new Ping();
            try
            {
                // Ping host 1
                if (!silent) Logger.Instance.WriteGlobal("PingCheck: Ping -> {0}", Settings.Default.ConnectionCheckPingHost1);
                var reply = ping.Send(Settings.Default.ConnectionCheckPingHost1, 3000);
                if (reply == null)
                {
                    if (!silent) Logger.Instance.WriteGlobal("PingCheck: reply = NULL");
                }
                else if (reply.Status != IPStatus.Success)
                {
                    if (!silent) Logger.Instance.WriteGlobal("PingCheck: {0} -> {1}", reply.Address, reply.Status);
                }
                else
                {
                    if (!silent) Logger.Instance.WriteGlobal("PingCheck: {0} -> {1}ms", reply.Address, reply.RoundtripTime);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("PingCheck: Failed with message: " + ex.Message);
            }

            try
            {
                // Ping host 2
                if (!silent) Logger.Instance.WriteGlobal("PingCheck: Ping -> {0}", Settings.Default.ConnectionCheckPingHost2);
                var reply = ping.Send(Settings.Default.ConnectionCheckPingHost2, 3000);
                if (reply == null)
                {
                    if (!silent) Logger.Instance.WriteGlobal("PingCheck: reply = NULL");
                }
                else if (reply.Status != IPStatus.Success)
                {
                    if (!silent) Logger.Instance.WriteGlobal("PingCheck: {0} -> {1}", reply.Address, reply.Status);
                }
                else
                {
                    if (!silent) Logger.Instance.WriteGlobal("PingCheck: {0} -> {1}ms", reply.Address, reply.RoundtripTime);
                    return true;
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("PingCheck: Failed with message: " + ex.Message);
            }

            return false;
        }
        #endregion
    }

    #region IPAdressRange Check
    public class IPAddressRange
    {
        readonly AddressFamily addressFamily;
        readonly byte[] lowerBytes;
        readonly byte[] upperBytes;

        public IPAddressRange(IPAddress lower, IPAddress upper)
        {
            // Assert that lower.AddressFamily == upper.AddressFamily

            this.addressFamily = lower.AddressFamily;
            this.lowerBytes = lower.GetAddressBytes();
            this.upperBytes = upper.GetAddressBytes();
        }

        public bool IsInRange(IPAddress address)
        {
            if (address.AddressFamily != addressFamily)
            {
                return false;
            }

            byte[] addressBytes = address.GetAddressBytes();

            bool lowerBoundary = true, upperBoundary = true;

            for (int i = 0; i < this.lowerBytes.Length &&
                (lowerBoundary || upperBoundary); i++)
            {
                if ((lowerBoundary && addressBytes[i] < lowerBytes[i]) ||
                    (upperBoundary && addressBytes[i] > upperBytes[i]))
                {
                    return false;
                }

                lowerBoundary &= (addressBytes[i] == lowerBytes[i]);
                upperBoundary &= (addressBytes[i] == upperBytes[i]);
            }

            return true;
        }
    }
#endregion
}
