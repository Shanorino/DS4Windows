using System;
using System.Threading;

namespace FUT404DS.SetupActions
{
    // Keep all infrastructure mutation behind the same nonblocking owner,
    // including cleanup that runs before or after the main install helper.
    internal static class SetupMutationOwnership
    {
        internal const string MutexName = @"Global\FUT404DS-VIIPER-Setup";

        internal static int Run(Func<int> action, Action<string> log,
            string mutexName = MutexName)
        {
            using var setupMutex = new Mutex(false, mutexName);
            var mutexOwned = false;
            try
            {
                try
                {
                    mutexOwned = setupMutex.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    mutexOwned = true;
                }
                if (!mutexOwned)
                {
                    log?.Invoke("Another FUT404DS VIIPER setup owns the global " +
                        "setup mutex; returning Windows Installer busy (1618).");
                    return 1618;
                }
                return action();
            }
            finally
            {
                if (mutexOwned)
                    setupMutex.ReleaseMutex();
            }
        }
    }
}
