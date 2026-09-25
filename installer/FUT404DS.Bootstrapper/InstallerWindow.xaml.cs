using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WixToolset.BootstrapperApplicationApi;

namespace FUT404DS.Bootstrapper
{
    public partial class InstallerWindow : Window
    {
        private readonly InstallerApplication application;
        private InstallerMode mode;
        private bool applying;

        internal InstallerWindow(InstallerApplication application)
        {
            this.application = application;
            InitializeComponent();
            ApplyWindowsTheme();
            Closing += (_, e) =>
            {
                if (applying) e.Cancel = true;
            };
            Closed += (_, __) => application.OnWindowClosed();
        }

        internal void ShowConfirmation(InstallerMode detectedMode, IReadOnlyDictionary<string, PackageState> packages, bool infrastructureHealthy)
        {
            mode = detectedMode;
            HidePages();
            ConfirmationPage.Visibility = Visibility.Visible;
            OptionsCard.Visibility = Visibility.Visible;
            applying = false;

            switch (mode)
            {
                case InstallerMode.Update:
                    ModeTitle.Text = "Update FUT404DS";
                    ModeDescription.Text = "A managed FUT404DS installation was found. Only package-owned files will be replaced.";
                    ActionButton.Content = "Update";
                    break;
                case InstallerMode.Repair:
                    ModeTitle.Text = "Repair FUT404DS";
                    ModeDescription.Text = "This version is already installed. Setup will verify and repair its managed components.";
                    ActionButton.Content = "Repair";
                    break;
                case InstallerMode.Uninstall:
                    ModeTitle.Text = "Uninstall FUT404DS";
                    ModeDescription.Text = "FUT404DS and its managed VIIPER installation will be removed. Profiles, settings, and shared system drivers are preserved.";
                    ActionButton.Content = "Uninstall";
                    OptionsCard.Visibility = Visibility.Collapsed;
                    break;
                default:
                    ModeTitle.Text = "Install FUT404DS";
                    ModeDescription.Text = "Everything needed for a standard x64 installation is included and works offline.";
                    ActionButton.Content = "Install";
                    break;
            }

            Ds4Status.Text = PackageStatus(packages, "FUT404DSMsi");
            ViiperStatus.Text = infrastructureHealthy ? "Ready" : "Will install or repair";
            UsbipStatus.Text = infrastructureHealthy ? "Ready" : "Will verify before changing";
        }

        internal void ShowPlanning()
        {
            HidePages();
            ProgressPage.Visibility = Visibility.Visible;
            ProgressTitle.Text = "Preparing installation…";
            ProgressDetail.Text = "Building a safe installation plan";
            OverallProgress.IsIndeterminate = true;
            applying = true;
        }

        internal void ShowApplying()
        {
            OverallProgress.IsIndeterminate = false;
            ProgressTitle.Text = mode == InstallerMode.Uninstall ? "Removing FUT404DS…" : "Installing FUT404DS…";
            ProgressDetail.Text = "Administrator permission is requested once";
        }

        internal void SetCurrentPackage(string packageId)
        {
            switch (packageId)
            {
                case "CloseRunningApplications": ProgressDetail.Text = "Closing running FUT404DS and VIIPER processes"; break;
                case "FUT404DSMsi": ProgressDetail.Text = "Installing FUT404DS"; break;
                case "ViiperUsbipSetup": ProgressDetail.Text = "Verifying VIIPER and USB-IP"; break;
                case "HidHide": ProgressDetail.Text = "Installing optional HidHide"; break;
                case "FakerInput": ProgressDetail.Text = "Installing optional FakerInput"; break;
                default: ProgressDetail.Text = "Verifying installation"; break;
            }
        }

        internal void ShowInstallerBusyRetry(int attempt, int maximumRetries)
        {
            ProgressDetail.Text = "Another setup or repair is busy; " +
                "retrying safely (" + attempt + " of " + maximumRetries + ")";
        }

        internal void SetProgress(int percent)
        {
            OverallProgress.Value = Math.Max(0, Math.Min(100, percent));
            ProgressPercent.Text = percent + "%";
        }

        internal void ShowComplete(LaunchAction action, bool startupWarning = false)
        {
            HidePages();
            CompletePage.Visibility = Visibility.Visible;
            LaunchCheckBox.Visibility = Visibility.Visible;
            applying = false;
            CompleteTitle.Text = "FUT404DS is ready";
            CompleteDescription.Text = startupWarning
                ? "Installation completed. Automatic startup could not be configured. You can launch FUT404DS now; run Repair to retry startup setup."
                : "Installation and verification completed successfully.";
            if (action == LaunchAction.Uninstall)
            {
                CompleteTitle.Text = "FUT404DS was removed";
                CompleteDescription.Text = "Profiles, settings, and shared system drivers were preserved.";
                LaunchCheckBox.Visibility = Visibility.Collapsed;
            }
        }

        internal void ShowRestart(bool startupWarning = false)
        {
            HidePages();
            RestartPage.Visibility = Visibility.Visible;
            RestartDescription.Text = "Windows must restart before setup can safely continue. Setup will resume after you sign in.";
            if (startupWarning)
                RestartDescription.Text += " Automatic startup could not be configured. Run Repair after restarting to retry it.";
            RestartNowButton.IsEnabled = true;
            applying = false;
        }

        internal void ShowFailure(string message)
        {
            HidePages();
            FailurePage.Visibility = Visibility.Visible;
            FailureMessage.Text = message;
            applying = false;
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {
            var action = mode == InstallerMode.Uninstall ? LaunchAction.Uninstall :
                         mode == InstallerMode.Repair ? LaunchAction.Repair : LaunchAction.Install;
            application.Begin(action, DesktopShortcutCheckBox.IsChecked == true,
                HidHideCheckBox.IsChecked == true, FakerInputCheckBox.IsChecked == true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => application.Close(1223);
        private void CloseFailure_Click(object sender, RoutedEventArgs e) => application.CloseWithCurrentResult();
        private void Retry_Click(object sender, RoutedEventArgs e) { HidePages(); DetectingPage.Visibility = Visibility.Visible; application.Retry(); }
        private void OpenLog_Click(object sender, RoutedEventArgs e) => application.OpenLog();
        private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try { Clipboard.SetText(application.Diagnostics()); }
            catch
            {
                FailureMessage.Text += "\r\n\r\nWindows could not access the clipboard. Use Open log instead.";
            }
        }
        private void RestartLater_Click(object sender, RoutedEventArgs e) => application.Close(3010);
        private void RestartNow_Click(object sender, RoutedEventArgs e)
        {
            if (application.RestartWindows())
            {
                application.Close(3010);
            }
            else
            {
                RestartDescription.Text = "Windows could not start the restart automatically. Restart manually; setup will resume after you sign in.";
                RestartNowButton.IsEnabled = false;
            }
        }
        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (LaunchCheckBox.Visibility == Visibility.Visible && LaunchCheckBox.IsChecked == true) application.LaunchDs4Windows();
            application.Close();
        }

        private void HidePages()
        {
            DetectingPage.Visibility = Visibility.Collapsed;
            ConfirmationPage.Visibility = Visibility.Collapsed;
            ProgressPage.Visibility = Visibility.Collapsed;
            CompletePage.Visibility = Visibility.Collapsed;
            RestartPage.Visibility = Visibility.Collapsed;
            FailurePage.Visibility = Visibility.Collapsed;
        }

        private static string PackageStatus(IReadOnlyDictionary<string, PackageState> packages, string id)
        {
            return packages.TryGetValue(id, out var state) && state == PackageState.Present ? "Installed" : "Will install";
        }

        private void ApplyWindowsTheme()
        {
            var light = true;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    light = Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) != 0;
                }
            }
            catch { }

            Resources["WindowBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#F4F7FB" : "#08121F"));
            Resources["CardBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#FFFFFF" : "#0E1B2A"));
            Resources["HoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#EAF2FC" : "#17283B"));
            Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#D8E2EE" : "#22354A"));
            Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#101D2D" : "#F4F8FC"));
            Resources["MutedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#56708D" : "#9FB8D3"));
        }
    }
}
