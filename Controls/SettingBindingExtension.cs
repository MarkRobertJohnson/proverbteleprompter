using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ProverbTeleprompter.Controls
{
    public class SettingBindingExtension : Binding 
    { 
        public SettingBindingExtension() 
        { 
            Initialize(); 
        } 

        public SettingBindingExtension(string path) 
        :base(path) 
        { 
            Initialize(); 
        } 

        private void Initialize() 
        { 
                this.Source = Properties.Settings.Default; 
        this.Mode = BindingMode.TwoWay; 
        } 
    } 

}
