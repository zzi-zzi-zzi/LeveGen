using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ff14bot.AClasses;
using ff14bot.Managers;
using LeveGen.Models;
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
        public static string PluginName = "Rb LeveGen";
#else
        public static string PluginName = "Rb LeveGen";
#endif

        public override string Author => "ZZI";
        public override Version Version => new Version(2,0,0);
        public override string Name => PluginName;

        public override bool WantButton => true;
        public override string ButtonText => "Build";
        public override string Description => "Generate Leve Profiles. Originally by Choose";

        public string uiPath = Path.Combine(PluginManager.PluginDirectory, "LeveGen", "GUI");

        private Window _window;

        private LeveDatabase _database =
            JsonConvert.DeserializeObject<LeveDatabase>(File.ReadAllText( Path.Combine(PluginManager.PluginDirectory, "LeveGen", "Database.json")) );

        public override void OnButtonPress()
        {
            if (_window == null)
            {
                _window = new LeveWindow
                {
                    DataContext = new WindowModelProvider(_database),
                    Content = LoadWindowContent(uiPath),
                    Title = "Rb Leve Gen v" + Version,
                    ResizeMode = ResizeMode.CanResizeWithGrip,
                    Width = 900,
                    Height = 600,
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
                    _windowContent = WPF.LoadWindowContent(Path.Combine(uiPath, "MainView.xaml"));
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
