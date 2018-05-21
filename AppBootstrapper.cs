using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using PowerBaseWpf.ViewModels;
using PowerBaseWpf.Views;
using PowerMVVM;

namespace PowerBaseWpf
{
    public class AppBootstrapper : BootstrapperBase<IShell>
    {
        public AppBootstrapper()
        {
            //CurrentUser.Initialize();
            //PowerConfig.Initialize();
            //PowerLog.Initialize();
            //InitializeLogging();
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            base.ConfigureContainer(builder);

            builder.RegisterType<MainViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MainView>().SingleInstance();

           

        }
    }
}