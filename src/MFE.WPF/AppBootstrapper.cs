using Caliburn.Micro;
using MFE.WPF.ViewModels;
using System;
using System.Windows;

namespace MFE.WPF
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<MainViewModel>();
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            AudioFileManager.CloseAll();
        }
    }
}
