using LingYanAutoUpdateServer;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;

namespace LingYanAutoUpdateServerServer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static AutoUpdateModel AutoUpdateModel { get; set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var entityUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LingYanAutoUpdate.Temp");
            if (File.Exists(entityUrl))
            {
                var entityValue = File.ReadAllText(Path.Combine(entityUrl));
                AutoUpdateModel = JsonConvert.DeserializeObject<AutoUpdateModel>(entityValue);
                AutoUpdateHelper.SettingConfig();
            }                    
        }
    }
}
