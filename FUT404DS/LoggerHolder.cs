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
using System.IO;
using System.Threading;
using FUT404DS;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace FUT404DSWPF
{
    public class LoggerHolder
    {
        private Logger logger;// = LogManager.GetCurrentClassLogger();
        public Logger Logger { get => logger; }
        private ReaderWriterLockSlim logLock = new ReaderWriterLockSlim();

        public LoggerHolder(FUT404DS.ControlService service)
        {
            string dataPath = PortableLabContext.Current?.DataPath ?? FUT404DS.Global.appdatapath;
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                dataPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "FUT404DS");
            }

            string logDirectory = Path.Combine(
                dataPath, "Logs");
            Directory.CreateDirectory(logDirectory);

            LoggingConfiguration configuration =
                LogManager.Configuration ?? new LoggingConfiguration();
            FileTarget fileTarget = FindFileTarget(configuration);
            if (fileTarget == null)
            {
                // A partial extraction, antivirus quarantine, or packaging error
                // can leave NLog.config absent. Logging must never be the reason
                // the application silently fails to start.
                fileTarget = new FileTarget("logfile")
                {
                    Layout = "${longdate}|${level:uppercase=true}|${message}",
                    MaxArchiveFiles = 7,
                };
                configuration.AddRule(LogLevel.Info, LogLevel.Fatal,
                    fileTarget);
            }

            fileTarget.FileName = Path.Combine(logDirectory,
                "fut404ds_log.txt");
            fileTarget.ArchiveFileName = Path.Combine(logDirectory,
                "fut404ds_log_{#}.txt");
            LogManager.Configuration = configuration;
            LogManager.ReconfigExistingLoggers();

            logger = LogManager.GetCurrentClassLogger();

            if (service != null)
            {
                service.Debug += WriteToLog;
            }
            FUT404DS.AppLogger.GuiLog += WriteToLog;
        }

        internal static FileTarget FindFileTarget(
            LoggingConfiguration configuration)
        {
            Target target = configuration?.FindTargetByName("logfile");
            while (target is WrapperTargetBase wrapper)
            {
                target = wrapper.WrappedTarget;
            }

            return target as FileTarget;
        }

        private void WriteToLog(object sender, FUT404DS.DebugEventArgs e)
        {
            if (e.Temporary)
            {
                return;
            }

            using WriteLocker locker = new WriteLocker(logLock);
            if (!e.Warning)
            {
                logger.Info(e.Data);
            }
            else
            {
                logger.Warn(e.Data);
            }
        }
    }
}
