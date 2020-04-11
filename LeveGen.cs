using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ff14bot.AClasses;
using ff14bot.Managers;
using LeveGen.Localization;
using LeveGen.Models;
using LeveGen.Properties;
using LeveGen.Utils;
using Newtonsoft.Json;

namespace LeveGen
{
    public class LeveWindow : Window
    {
        public LeveWindow()
        {
            InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;
        }
    }
    public class LeveGen : BotPlugin
    {
#if RB_CN
        public static string PluginName = "生产职业理符任务生成";
#else
        public static string PluginName = "Rb LeveGen";
#endif

        public override string Author => "ZZI";
        public override Version Version => new Version(2,1,0);
        public override string Name => PluginName;

        public override bool WantButton => true;
        public override string ButtonText => "Build";
        public override string Description => "Generate Leve Profiles. Originally by Choose";

        public string uiPath = Path.Combine(PluginManager.PluginDirectory, "LeveGen", "GUI");

        private Window _window;
        public override void OnInitialize()
        {
            LocalizationInitializer.Initalize();
        }

        private LeveDatabase _database =
            JsonConvert.DeserializeObject<LeveDatabase>(Resources.Database);

        public override void OnButtonPress()
        {
            
            if (_window == null)
            {
                //this if is Just in case, it helps force the correct culture into the xaml parser
#if RB_CN
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN");
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("zh-CN");
#endif
                _window = new LeveWindow
                {
                    DataContext = new WindowModelProvider(_database),
                    Content = LoadWindowContent(uiPath),
                    Title = "Rb Leve Gen v" + Version,
                    ResizeMode = ResizeMode.CanResizeWithGrip,
                    Width = 900,
                    Height = 600,
            };
                
                _window.Loaded += (e, a) =>
                {
                    
                };
                _window.Closed += (e, a) =>
                {
                    _window = null;
                };
            }
            _window.Show();
        }

        private static object ContentLock = new object();
        private UserControl _windowContent;

        /// <summary>
        /// Load up our xaml window
        /// </summary>
        /// <param name="uiPath"></param>
        /// <returns></returns>
        internal UserControl LoadWindowContent(string uiPath)
        {
            try
            {
                lock (ContentLock)
                {
                    _windowContent = WPF.LoadWindowContent(Resources.MainView);
                    //LoadResourceForWindow(Path.Combine(uiPath, "Dictionary.xaml"), _windowContent);
                    return _windowContent;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception loading window content! {0}", ex);
            }
            return null;
        }

        /// <summary>
        /// load our Resource file that contains styles and magic.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="control"></param>
        private void LoadResourceForWindow(string filename, UserControl control)
        {
            try
            {
                ResourceDictionary resource = WPF.LoadAndTransformXamlFile<ResourceDictionary>(filename);
                foreach (System.Collections.DictionaryEntry res in resource)
                {
                    if (!control.Resources.Contains(res.Key))
                        control.Resources.Add(res.Key, res.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading resources {0}", ex);
            }
        }
    }
}
