using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ControlzEx.Standard;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using PowerBaseWpf.Views;
using PowerMVVM;
using PowerMVVM.Logging;

namespace PowerBaseWpf.ViewModels
{
    public class MainViewModel : PropertyChangedBase, IShell, IOnLoadedHandler, ICancelableOnClosingHandler
    {
        #region Secondary Objects

        #endregion

        #region Primary Objects

        #region Dependency References

        public static IDialogManager DialogManager;

        public IFlyoutManager Flyouts { get; }

        #endregion

        #endregion

        #region Initialization

        public MainViewModel(IDialogManager dialogManager,  IFlyoutManager flyoutManager)
        {
            DialogManager = dialogManager;
            Flyouts = flyoutManager;
        }

        public Task OnLoadedAsync()
        {
            return Task.FromResult(0);
        }

        public bool OnClosing()
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Close",
                NegativeButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = false,

            };

            DialogManager.ShowMessageBox("Close",
                    "Are you sure you want to close?",
                    MessageDialogStyle.AffirmativeAndNegative, mySettings)
                .ContinueWith(t =>
                {
                    if (t.Result == MessageDialogResult.Affirmative)
                    {
                        Application.Current.Shutdown();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());

            return true;
        }

        #endregion
    }
}











