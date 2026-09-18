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
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Data;
using FUT404DS;

namespace FUT404DSWPF.DS4Forms.ViewModels
{
    public class LogViewModel
    {
        //private object _colLockobj = new object();
        private ReaderWriterLockSlim _logListLocker = new ReaderWriterLockSlim();
        private ObservableCollection<LogItem> logItems = new ObservableCollection<LogItem>();

        public ObservableCollection<LogItem> LogItems => logItems;

        public ReaderWriterLockSlim LogListLocker => _logListLocker;

        public LogViewModel(FUT404DS.ControlService service)
        {
            string version = FUT404DS.Global.exeDisplayVersion;
            logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"FUT404DS version {version}" });
            logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"FUT404DS Assembly Architecture: {(Environment.Is64BitProcess ? "x64" : "x86")}" });
            logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"OS Version: {Environment.OSVersion}" });
            logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"OS Product Name: {FUT404DS.Util.GetOSProductName()}" });
            logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"OS Release ID: {FUT404DS.Util.GetOSReleaseId()}" });
            logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"System Architecture: {(Environment.Is64BitOperatingSystem ? "x64" : "x32")}" });

            //logItems.Add(new LogItem { Datetime = DateTime.Now, Message = "FUT404DS version 2.0" });
            //BindingOperations.EnableCollectionSynchronization(logItems, _colLockobj);
            BindingOperations.EnableCollectionSynchronization(logItems, _logListLocker, LogLockCallback);
            service.Debug += AddLogMessage;
            FUT404DS.AppLogger.GuiLog += AddLogMessage;
        }

        private void LogLockCallback(IEnumerable collection, object context, Action accessMethod, bool writeAccess)
        {
            if (writeAccess)
            {
                using (WriteLocker locker = new WriteLocker(_logListLocker))
                {
                    accessMethod?.Invoke();
                }
            }
            else
            {
                using (ReadLocker locker = new ReadLocker(_logListLocker))
                {
                    accessMethod?.Invoke();
                }
            }
        }

        private void AddLogMessage(object sender, FUT404DS.DebugEventArgs e)
        {
            LogItem item = new LogItem { Datetime = e.Time, Message = e.Data, Warning = e.Warning };
            _logListLocker.EnterWriteLock();
            logItems.Add(item);
            _logListLocker.ExitWriteLock();
            //lock (_colLockobj)
            //{
            //    logItems.Add(item);
            //}
        }
    }
}
