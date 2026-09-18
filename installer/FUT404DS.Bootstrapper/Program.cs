using WixToolset.BootstrapperApplicationApi;

namespace FUT404DS.Bootstrapper
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                ManagedBootstrapperApplication.Run(new InstallerApplication());
                return 0;
            }
            catch (System.Exception ex)
            {
                try
                {
                    var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FUT404DS.Bootstrapper.failure.log");
                    System.IO.File.WriteAllText(path, ex.ToString());
                }
                catch { }
                return ex.HResult;
            }
        }
    }
}
