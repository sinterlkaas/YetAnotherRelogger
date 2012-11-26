using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Mime;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers;

namespace YetAnotherRelogger.Properties
{


    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {

        public Settings()
        {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
            this.SettingsLoaded += new System.Configuration.SettingsLoadedEventHandler(Settings_SettingsLoaded);
        }

        private void Settings_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            
        }

        private void Settings_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            // this.Save();
        }

        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Add code to handle the SettingsSaving event here.
        }

        [UserScopedSettingAttribute]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        [DefaultSettingValueAttribute(null)]
        public List<AutoPosition.ScreensClass> AutoPosScreens
        {
            get
            {
                return ((List<AutoPosition.ScreensClass>)this["AutoPosScreens"]);
            }
            set
            {
                this["AutoPosScreens"] = value;
            }
        }

        [UserScopedSettingAttribute]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        [DefaultSettingValueAttribute(null)]
        public List<Hotkey> HotKeys
        {
            get
            {
                return ((List<Hotkey>)this["HotKeys"]);
            }
            set
            {
                this["HotKeys"] = value;
            }
        }
    }

  
}
