using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Hotkeys;

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
            if (HotKeys == null) HotKeys = new BindingList<Hotkey>();
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
        public BindingList<Hotkey> HotKeys
        {
            get
            {
                return ((BindingList<Hotkey>)this["HotKeys"]);
            }
            set
            {
                this["HotKeys"] = value;
            }
        }
    }

  
}
