using System;
using System.Collections;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text.RegularExpressions;
using YetAnotherRelogger.Helpers.Bot;

namespace YetAnotherRelogger.Helpers.Tools
{
    public static class UserAccount
    {
        /// <summary>
        /// Get Admin group name
        /// </summary>
        public static string AdminGroup
        {
            get
            {
                if (string.IsNullOrEmpty(_admingroup))
                {
                    var builtinAdminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    var grp = builtinAdminSid.Translate(typeof (NTAccount)).ToString();
                    _admingroup = Regex.Match(grp, @".+\\(.+)", RegexOptions.Compiled).Groups[1].Value;
                }
                return _admingroup;
            }
        }
        private static string _admingroup; // Cached AdminGroup string

        /// <summary>
        /// Get user group name
        /// </summary>
        public static string UserGroup
        {
            get
            {
                if (string.IsNullOrEmpty(_usergroup))
                {
                    var builtinAdminSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    var grp = builtinAdminSid.Translate(typeof (NTAccount)).ToString();
                    _usergroup = Regex.Match(grp, @".+\\(.+)", RegexOptions.Compiled).Groups[1].Value;
                }
                return _usergroup;
            }
        }
        private static string _usergroup; // Cached UserGroup string

        /// <summary>
        /// Lookup admin on localhost
        /// </summary>
        /// <param name="username">username to lookup</param>
        /// <returns>returns true if username exists as admin</returns>
        public static bool ExistsAsAdmin(string username)
        {
            
            
            //get machine
            using (var machine = new DirectoryEntry("WinNT://localhost"))
            {
                //get local admin group
                using (var group = machine.Children.Find(AdminGroup, "Group"))
                {
                    //get all members of local admin group
                    var members = group.Invoke("Members", null);
                    if ((from object member in (IEnumerable)members select new DirectoryEntry(member).Name).Any(accountName => accountName.ToLower().Equals(username.ToLower())))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Lookup username on localhost
        /// </summary>
        /// <param name="username">username to lookup</param>
        /// <returns>returns true if user exists</returns>
        public static bool Exists(string username)
        {
            var directoryEntry = new DirectoryEntry("WinNT://localhost");
            return (from DirectoryEntry child in directoryEntry.Children where child.SchemaClassName == "User" let props = child.Properties select child).Any(child => child.Name.ToLower().Equals(username.ToLower()));
        }

        /// <summary>
        /// Creates a new user account
        /// </summary>
        /// <param name="name">User login name</param>
        /// <param name="password">User password</param>
        /// <param name="fullName">User full name</param>
        /// <param name="isAdmin">flag as admin</param>
        /// <returns>returns true when user is successfully created</returns>
        public static bool Create(string name, string password, string fullName = "", bool isAdmin = false)
        {
            try
            {
                var dirEntry = new DirectoryEntry("WinNT://localhost");
                var entries = dirEntry.Children;
                var newUser = entries.Add(name, "user");
                newUser.Properties["FullName"].Add(fullName);
                newUser.Invoke("SetPassword", password);
                newUser.CommitChanges();

                // Remove the if condition along with the else to create user account in "user" group.
                DirectoryEntry grp;
                grp = dirEntry.Children.Find(UserGroup, "group");
                grp.Invoke("Add", new object[] { newUser.Path });

                if (isAdmin)
                {
                    grp = dirEntry.Children.Find(AdminGroup, "group");
                    grp.Invoke("Add", new object[] { newUser.Path });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("Failed to add new user: {0}", name);
                DebugHelper.Exception(ex);
                return false;
            }
            
            return (isAdmin && ExistsAsAdmin(name)) || (Exists(name));
        }

        public static ProcessStartInfo ImpersonateStartInfo(ProcessStartInfo startinfo, BotClass bot)
        {
            if (bot.UseWindowsUser)
            {
                if (!ExistsAsAdmin(bot.WindowsUserName))
                {
                    if (bot.CreateWindowsUser)
                        Create(bot.WindowsUserName, bot.WindowsUserPassword, isAdmin: true);
                    else
                    {
                        Logger.Instance.Write("User Account \"{0}\" does not exist and we are not allowed to create it", bot.WindowsUserName);
                        bot.Stop();
                        return startinfo;
                    }
                }

                startinfo.UserName = bot.WindowsUserName;
                var encPassword = new SecureString();
                foreach (var c in bot.WindowsUserPassword)
                    encPassword.AppendChar(c);
                startinfo.Password = encPassword;
                startinfo.UseShellExecute = false;
            }
            return startinfo;
        }
    }
}
