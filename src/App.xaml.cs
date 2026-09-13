using System.Windows;

using LiveCaptionsTranslator.i18n;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Translator.Setting?.Save();

            Task.Run(() => Translator.SyncLoop());
            Task.Run(() => Translator.TranslateLoop());
            Task.Run(() => Translator.DisplayLoop());
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            LocalizationService.Instance.Initialize(Translator.Setting?.Language);
            base.OnStartup(e);
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (Translator.Window != null)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                LiveCaptionsHandler.KillLiveCaptions(Translator.Window);
            }
        }
    }
}
