/*
FUT404DS
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using FUT404DS;
using static FUT404DS.Util;
using Microsoft.Win32;

namespace FUT404DSWPF.DS4Forms.ViewModels
{
    public class SettingsViewModel
    {
        // Re-Enable Ex Mode
        public bool HideDS4Controller
        {
            get => FUT404DS.Global.UseExclusiveMode;
            set => FUT404DS.Global.UseExclusiveMode = value;
        }

        public bool ReclaimSteamInput
        {
            get => FUT404DS.Global.ReclaimSteamInput;
            set => FUT404DS.Global.ReclaimSteamInput = value;
        }

        public bool SwipeTouchSwitchProfile { get => FUT404DS.Global.SwipeProfiles;
            set => FUT404DS.Global.SwipeProfiles = value; }

        public bool AutomaticJoyConPairing
        {
            get => FUT404DS.Global.DeviceOptions.JoyConDeviceOpts.
                AutomaticPairing;
            set
            {
                if (FUT404DS.Global.DeviceOptions.JoyConDeviceOpts.
                        AutomaticPairing == value)
                {
                    return;
                }
                FUT404DS.Global.DeviceOptions.JoyConDeviceOpts.
                    AutomaticPairing = value;
                FUT404DS.Global.Save();
            }
        }

        private bool runAtStartup;
        private bool changingStartupRegistration;
        private StartupRegistrationState startupRegistration;
        private readonly Func<StartupRegistrationState> readStartupRegistration;
        private readonly Action<StartupRegistrationMode> changeStartupRegistration;
        private readonly Action refreshViiperStartup;
        private readonly Action<string> reportStartupError;
        private readonly Action<string> logStartupDiagnostic;
        public bool RunAtStartup
        {
            get => runAtStartup;
            set
            {
                if (!CanChangeStartupPreference || changingStartupRegistration || runAtStartup == value) return;
                ApplyStartupRegistration(value ? StartupRegistrationMode.Program : StartupRegistrationMode.Disabled);
            }
        }
        public event EventHandler RunAtStartupChanged;

        private bool runStartProg;
        public bool RunStartProg
        {
            get => runStartProg;
            set
            {
                if (!CanChangeStartupMode || changingStartupRegistration || !runAtStartup || !value || runStartProg) return;
                ApplyStartupRegistration(StartupRegistrationMode.Program);
            }
        }
        public event EventHandler RunStartProgChanged;

        private bool runStartTask;
        public bool RunStartTask
        {
            get => runStartTask;
            set
            {
                if (!CanChangeStartupMode || changingStartupRegistration || !runAtStartup || !value || runStartTask) return;
                ApplyStartupRegistration(StartupRegistrationMode.Task);
            }
        }
        public event EventHandler RunStartTaskChanged;

        private bool canWriteTask;
        public bool CanWriteTask { get => canWriteTask && SystemIntegrationEnabled; }
        public bool SystemIntegrationEnabled => !PortableLabContext.IsActive;
        public bool CanChangeStartupPreference => SystemIntegrationEnabled && startupRegistration.ReadError == null;
        public event EventHandler CanChangeStartupPreferenceChanged;
        public bool CanChangeStartupMode => CanChangeStartupPreference && !startupRegistration.SetupDeferred;
        public event EventHandler CanChangeStartupModeChanged;
        public string StartupStatusText => startupRegistration.StatusText;
        public event EventHandler StartupStatusTextChanged;
        public Visibility StartupStatusVisibility => string.IsNullOrEmpty(StartupStatusText)
            ? Visibility.Collapsed : Visibility.Visible;
        public event EventHandler StartupStatusVisibilityChanged;

        public ImageSource uacSource;
        public ImageSource UACSource { get => uacSource; }

        public ImageSource questionMarkSource;
        public ImageSource QuestionMarkSource { get => questionMarkSource; }

        private Visibility showRunStartPanel = Visibility.Collapsed;
        public Visibility ShowRunStartPanel {
            get => showRunStartPanel;
            set
            {
                if (showRunStartPanel == value) return;
                showRunStartPanel = value;
                ShowRunStartPanelChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ShowRunStartPanelChanged;

        private Visibility _isProfileChangedCheckVisible;

        public Visibility IsProfileChangedCheckVisible
        {
            get => _isProfileChangedCheckVisible;
            private set
            {
                _isProfileChangedCheckVisible = value;
                IsProfileChangedCheckVisibleChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler IsProfileChangedCheckVisibleChanged;

        public bool ProfileChangedNotification
        {
            get => Global.ProfileChangedNotification;
            set => Global.ProfileChangedNotification = value;
        }

        public int ShowNotificationsIndex
        {
            get => FUT404DS.Global.Notifications;
            set
            {
                Global.Notifications = value;
                // display only when all notifications are on
                IsProfileChangedCheckVisible = value == 2 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool DisconnectBTStop { get => FUT404DS.Global.DCBTatStop; set => FUT404DS.Global.DCBTatStop = value; }
        public bool FlashHighLatency { get => FUT404DS.Global.FlashWhenLate; set => FUT404DS.Global.FlashWhenLate = value; }
        public int FlashHighLatencyAt { get => FUT404DS.Global.FlashWhenLateAt; set => FUT404DS.Global.FlashWhenLateAt = value; }
        public bool StartMinimize { get => FUT404DS.Global.StartMinimized; set => FUT404DS.Global.StartMinimized = value; }
        public bool MinimizeToTaskbar { get => FUT404DS.Global.MinToTaskbar; set => FUT404DS.Global.MinToTaskbar = value; }
        public bool CloseMinimizes { get => FUT404DS.Global.CloseMini; set => FUT404DS.Global.CloseMini = value; }
        public bool QuickCharge { get => FUT404DS.Global.QuickCharge; set => FUT404DS.Global.QuickCharge = value; }
        public bool VerboseStartupLogging
        {
            get => FUT404DS.Global.VerboseStartupLogging;
            set
            {
                if (FUT404DS.Global.VerboseStartupLogging == value)
                {
                    return;
                }

                FUT404DS.Global.VerboseStartupLogging = value;
                FUT404DS.Global.Save();
            }
        }

        public bool PromptForViiperSetup
        {
            get => !FUT404DS.Global.SuppressViiperSetupPrompt;
            set
            {
                bool suppress = !value;
                if (FUT404DS.Global.SuppressViiperSetupPrompt == suppress)
                {
                    return;
                }

                FUT404DS.Global.SuppressViiperSetupPrompt = suppress;
                FUT404DS.Global.Save();
            }
        }

        public int IconChoiceIndex
        {
            get => (int)FUT404DS.Global.UseIconChoice;
            set
            {
                int temp = (int)FUT404DS.Global.UseIconChoice;
                if (temp == value) return;
                FUT404DS.Global.UseIconChoice = (FUT404DS.TrayIconChoice)value;
                IconChoiceIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler IconChoiceIndexChanged;

        public int AppChoiceIndex
        {
            get => (int)FUT404DS.Global.UseCurrentTheme;
            set
            {
                int temp = (int)FUT404DS.Global.UseCurrentTheme;
                if (temp == value) return;
                FUT404DS.Global.UseCurrentTheme = (FUT404DS.AppThemeChoice)value;
                AppChoiceIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler AppChoiceIndexChanged;

        public bool CheckForUpdates
        {
            get => FUT404DS.Global.CheckWhen > 0;
            set
            {
                FUT404DS.Global.CheckWhen = value ? 24 : 0;
                CheckForNoUpdatesWhen();
            }
        }
        public event EventHandler CheckForUpdatesChanged;

        public int CheckEvery
        {
            get
            {
                int temp = FUT404DS.Global.CheckWhen;
                if (temp > 23)
                {
                    temp = temp / 24;
                }
                return temp;
            }
            set
            {
                int temp;
                if (checkEveryUnitIdx == 0 && value < 24)
                {
                    temp = FUT404DS.Global.CheckWhen;
                    if (temp != value)
                    {
                        FUT404DS.Global.CheckWhen = value;
                        CheckForNoUpdatesWhen();
                    }
                }
                else if (checkEveryUnitIdx == 1)
                {
                    temp = FUT404DS.Global.CheckWhen / 24;
                    if (temp != value)
                    {
                        FUT404DS.Global.CheckWhen = value * 24;
                        CheckForNoUpdatesWhen();
                    }
                }
            }
        }
        public event EventHandler CheckEveryChanged;

        private int checkEveryUnitIdx = 1;
        public int CheckEveryUnit
        {
            get
            {
                return checkEveryUnitIdx;
            }
            set
            {
                if (checkEveryUnitIdx == value) return;
                checkEveryUnitIdx = value;
                CheckEveryUnitChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CheckEveryUnitChanged;

        public bool UseOSCServer
        {
            get => FUT404DS.Global.isUsingOSCServer();
            set
            {
                if (FUT404DS.Global.isUsingOSCServer() == value) return;
                FUT404DS.Global.setUsingOSCServer(value);
                UseOSCServerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseOSCServerChanged;
        public int OscPort { get => FUT404DS.Global.getOSCServerPortNum(); set => FUT404DS.Global.setOSCServerPort(value); }
        
        public bool InterpretingOscMonitoring { get => FUT404DS.Global.isInterpretingOscMonitoring(); set => FUT404DS.Global.setInterpretingOscMonitoring(value); }

        public bool UseOSCSender
        {
            get => FUT404DS.Global.isUsingOSCSender();
            set
            {
                if (FUT404DS.Global.isUsingOSCSender() == value) return;
                FUT404DS.Global.setUsingOSCSender(value);
                UseOSCSenderChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseOSCSenderChanged;
        public int OscSendPort { get => FUT404DS.Global.getOSCSenderPortNum(); set => FUT404DS.Global.setOSCSenderPort(value); }

        public string OscSenderAddress
        {
            get => FUT404DS.Global.getOSCSenderAddress();
            set => FUT404DS.Global.setOSCSenderAddress(value);
        }


        public bool UseUDPServer
        {
            get => FUT404DS.Global.isUsingUDPServer();
            set
            {
                if (FUT404DS.Global.isUsingUDPServer() == value) return;
                FUT404DS.Global.setUsingUDPServer(value);
                UseUDPServerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseUDPServerChanged;

        public string UdpIpAddress { get => FUT404DS.Global.getUDPServerListenAddress();
            set => FUT404DS.Global.setUDPServerListenAddress(value); }
        public int UdpPort { get => FUT404DS.Global.getUDPServerPortNum(); set => FUT404DS.Global.setUDPServerPort(value); }

        // Keep edits local until Apply validates the entire endpoint. A failed
        // bind can retain a valid requested preference, but an invalid draft
        // must not silently enable the previous endpoint on the next Start.
        private bool useDSXUDPServer = FUT404DS.Global.IsUsingDSXUDPServer();
        public bool UseDSXUDPServer
        {
            get => useDSXUDPServer;
            set
            {
                if (useDSXUDPServer == value) return;
                useDSXUDPServer = value;
                UseDSXUDPServerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseDSXUDPServerChanged;

        public string DSXUdpIpAddress { get; set; } = FUT404DS.Global.GetDSXUDPServerListenAddress();

        public int DSXUdpPort { get; set; } = FUT404DS.Global.GetDSXUDPServerPortNum();

        public bool UseUdpSmoothing
        {
            get => FUT404DS.Global.UseUDPSeverSmoothing;
            set
            {
                bool temp = FUT404DS.Global.UseUDPSeverSmoothing;
                if (temp == value) return;
                FUT404DS.Global.UseUDPSeverSmoothing = value;
                UseUdpSmoothingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseUdpSmoothingChanged;

        public Visibility UdpServerOneEuroPanelVisibility
        {
            get => FUT404DS.Global.isUsingUDPServer() && FUT404DS.Global.UseUDPSeverSmoothing ? Visibility.Visible : Visibility.Collapsed;
        }
        public event EventHandler UdpServerOneEuroPanelVisibilityChanged;

        public double UdpSmoothMinCutoff
        {
            get => FUT404DS.Global.UDPServerSmoothingMincutoff;
            set
            {
                double temp = FUT404DS.Global.UDPServerSmoothingMincutoff;
                if (temp == value) return;
                FUT404DS.Global.UDPServerSmoothingMincutoff = value;
                UdpSmoothMinCutoffChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UdpSmoothMinCutoffChanged;

        public double UdpSmoothBeta
        {
            get => FUT404DS.Global.UDPServerSmoothingBeta;
            set
            {
                double temp = FUT404DS.Global.UDPServerSmoothingBeta;
                if (temp == value) return;
                FUT404DS.Global.UDPServerSmoothingBeta = value;
                UdpSmoothBetaChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UdpSmoothBetaChanged;

        public bool UseCustomSteamFolder
        {
            get => FUT404DS.Global.UseCustomSteamFolder;
            set
            {
                FUT404DS.Global.UseCustomSteamFolder = value;
                UseCustomSteamFolderChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseCustomSteamFolderChanged;

        public string CustomSteamFolder
        {
            get => FUT404DS.Global.CustomSteamFolder;
            set
            {
                string temp = FUT404DS.Global.CustomSteamFolder;
                if (temp == value) return;
                if (Directory.Exists(value) || value == string.Empty)
                {
                    FUT404DS.Global.CustomSteamFolder = value;
                }
            }
        }

        private bool viewEnabled = true;
        public bool ViewEnabled
        {
            get => viewEnabled;
            set
            {
                viewEnabled = value;
                ViewEnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ViewEnabledChanged;

        public string FakeExeName
        {
            get => FUT404DS.Global.FakeExeName;
            set
            {
                string temp = FUT404DS.Global.FakeExeName;
                if (temp == value) return;
                try
                {
                    if (!string.IsNullOrEmpty(value)) CreateFakeExe(value);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    AppLogger.LogToGui("Could not create the executable alias: " + ex.Message, true);
                    MessageBox.Show("The executable name was not changed. " + ex.Message,
                        "Executable name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FakeExeNameChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }
                FUT404DS.Global.FakeExeName = value;
                if (!PortableLabContext.IsActive && !string.IsNullOrEmpty(temp))
                {
                    try
                    {
                        if (!ExecutableAliasFiles.RemoveOwned(FUT404DS.Global.exelocation, temp))
                            AppLogger.LogToGui("Previous executable alias was left in place because it is in use or no longer matches this build.", false);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
                    {
                        AppLogger.LogToGui("Previous executable alias was left in place: " + ex.Message, false);
                    }
                }
                FakeExeNameChanged?.Invoke(this, EventArgs.Empty);
                FakeExeNameChangeCompare?.Invoke(this, temp, value);
            }
        }
        public event EventHandler FakeExeNameChanged;
        public event FakeExeNameChangeHandler FakeExeNameChangeCompare;
        public delegate void FakeExeNameChangeHandler(SettingsViewModel sender,
            string oldvalue, string newvalue);


        public bool HidHideClientFound
        {
            get
            {
                bool result = FUT404DS.Global.hidHideInstalled &&
                    !string.IsNullOrEmpty(FUT404DS.Util.GetHidHideClientPath());

                return result;
            }
        }
        public event EventHandler HidHideClientFoundChanged;

        private List<MonitorChoiceListing> absMonitorChoices = new List<MonitorChoiceListing>();
        public List<MonitorChoiceListing> AbsMonitorChoices => absMonitorChoices;
        public event EventHandler AbsMonitorChoicesChanged;

        //private string absMonitorSettingEDID = string.Empty;
        public string AbsMonitorSettingEDID
        {
            get => Global.AbsoluteDisplayEDID;
            set => Global.AbsoluteDisplayEDID = value;
        }

        public int ProcessPriorityIndex
        {
            get => Global.ProcessPriority;
            set
            {
                Global.ProcessPriority = value;
                ProcessPriorityIndexChanged?.Invoke(this, EventArgs.Empty);
            }

        }

        public event EventHandler ProcessPriorityIndexChanged;

        public SettingsViewModel()
        {
            readStartupRegistration = StartupMethods.ReadRegistrationState;
            changeStartupRegistration = StartupMethods.SetRegistrationMode;
            refreshViiperStartup = ViiperSetupManager.RefreshSelectedStartupTaskAfterRunAtStartupChange;
            reportStartupError = ReportStartupChangeFailure;
            logStartupDiagnostic = error => FUT404DS.AppLogger.LogToGui(
                "Could not read Windows startup settings: " + error, true);
            checkEveryUnitIdx = 1;
            IsProfileChangedCheckVisible = Global.Notifications == 2 ? Visibility.Visible : Visibility.Collapsed;

            int checklapse = FUT404DS.Global.CheckWhen;
            if (checklapse < 24 && checklapse > 0)
            {
                checkEveryUnitIdx = 0;
            }

            Icon img = SystemIcons.Shield;
            Bitmap bitmap = img.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap =
                 Imaging.CreateBitmapSourceFromHBitmap(
                      hBitmap, IntPtr.Zero, Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
            uacSource = wpfBitmap;

            img = SystemIcons.Question;
            wpfBitmap =
                 Imaging.CreateBitmapSourceFromHBitmap(
                      img.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
            questionMarkSource = wpfBitmap;

            canWriteTask = FUT404DS.Global.IsAdministrator();
            try
            {
                SetStartupDisplay(readStartupRegistration());
            }
            catch (Exception ex)
            {
                SetStartupDisplay(new(false, false, ReadError: ex.Message));
            }

            RefreshMonitorChoices();

            UseUdpSmoothingChanged += SettingsViewModel_UseUdpSmoothingChanged;
            UseUDPServerChanged += SettingsViewModel_UseUDPServerChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            //CheckForUpdatesChanged += SettingsViewModel_CheckForUpdatesChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            RefreshMonitorChoices();
        }

        private void SettingsViewModel_UseUDPServerChanged(object sender, EventArgs e)
        {
            UdpServerOneEuroPanelVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SettingsViewModel_UseUdpSmoothingChanged(object sender, EventArgs e)
        {
            UdpServerOneEuroPanelVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        // The isolated constructor exercises the same setters with no Windows
        // discovery, monitor enumeration, icons, or real startup registrations.
        internal SettingsViewModel(Func<StartupRegistrationState> read,
            Action<StartupRegistrationMode> change, Action refresh,
            Action<string> reportError, Action<string> logDiagnostic = null)
        {
            readStartupRegistration = read ?? throw new ArgumentNullException(nameof(read));
            changeStartupRegistration = change ?? throw new ArgumentNullException(nameof(change));
            refreshViiperStartup = refresh ?? throw new ArgumentNullException(nameof(refresh));
            reportStartupError = reportError ?? throw new ArgumentNullException(nameof(reportError));
            logStartupDiagnostic = logDiagnostic;
            canWriteTask = true;
            SetStartupDisplay(readStartupRegistration());
        }

        private void ApplyStartupRegistration(StartupRegistrationMode mode)
        {
            changingStartupRegistration = true;
            try
            {
                StartupRegistrationChangeResult result = StartupRegistrationChange.Apply(
                    mode, startupRegistration, readStartupRegistration, changeStartupRegistration);
                SetStartupDisplay(result.State);
                if (!result.Success)
                {
                    reportStartupError(result.Error);
                    return;
                }
                try { refreshViiperStartup(); }
                catch (Exception error) { reportStartupError(error.Message); }
            }
            finally { changingStartupRegistration = false; }
        }

        private void SetStartupDisplay(StartupRegistrationState state)
        {
            if (state.ReadError != null && state.ReadError != startupRegistration.ReadError)
                logStartupDiagnostic?.Invoke(state.ReadError);
            startupRegistration = state;
            runAtStartup = state.RunAtStartupRequested;
            runStartTask = state.Task || state.Pending;
            runStartProg = !runStartTask;
            ShowRunStartPanel = runAtStartup ? Visibility.Visible : Visibility.Collapsed;
            RunAtStartupChanged?.Invoke(this, EventArgs.Empty);
            RunStartProgChanged?.Invoke(this, EventArgs.Empty);
            RunStartTaskChanged?.Invoke(this, EventArgs.Empty);
            CanChangeStartupPreferenceChanged?.Invoke(this, EventArgs.Empty);
            CanChangeStartupModeChanged?.Invoke(this, EventArgs.Empty);
            StartupStatusTextChanged?.Invoke(this, EventArgs.Empty);
            StartupStatusVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private static void ReportStartupChangeFailure(string error)
        {
            FUT404DS.AppLogger.LogToGui("The startup change could not be completed. " + error, true);
            const string message = "The startup change could not be completed. Check the status under Run at Startup in Settings, and see the Log tab for details. You can still open FUT404DS manually.";
            MessageBox.Show(message, "Run at startup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void SettingsViewModel_CheckForUpdatesChanged(object sender, EventArgs e)
        {
            if (!CheckForUpdates)
            {
                CheckEveryChanged?.Invoke(this, EventArgs.Empty);
                CheckEveryUnitChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CheckForNoUpdatesWhen()
        {
            if (FUT404DS.Global.CheckWhen == 0)
            {
                checkEveryUnitIdx = 1;
            }

            CheckForUpdatesChanged?.Invoke(this, EventArgs.Empty);
            CheckEveryChanged?.Invoke(this, EventArgs.Empty);
            CheckEveryUnitChanged?.Invoke(this, EventArgs.Empty);
        }

        public void CreateFakeExe(string filename)
        {
            if (PortableLabContext.IsActive) return;
            ExecutableAliasFiles.Create(FUT404DS.Global.exelocation, filename);
        }

        public void DriverCheckRefresh()
        {
            HidHideClientFoundChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RefreshMonitorChoices()
        {
            absMonitorChoices.Clear();
            absMonitorChoices.Add(new MonitorChoiceListing()
            {
                DisplayName = "All Monitors",
                EDID = string.Empty,
                Index = 0,
            });

            int idx = 1;
            foreach(DISPLAY_DEVICE tempDis in Global.GrabCurrentMonitors())
            {
                absMonitorChoices.Add(new MonitorChoiceListing()
                {
                    DisplayName = tempDis.DeviceString,
                    EDID = tempDis.DeviceID,
                    Index = idx,
                });

                idx++;
            }

            AbsMonitorChoicesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public struct MonitorChoiceListing
    {
        private int idx;
        public int Index
        {
            get => idx;
            set => idx = value;
        }

        private string edid;
        public string EDID
        {
            get => edid;
            set => edid = value;
        }

        private string displayName;
        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public string DisplayItemString
        {
            get => $"{idx}: {displayName}";
        }
    }
}
