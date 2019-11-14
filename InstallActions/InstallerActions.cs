using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections;
using System.IO;
using Microsoft.Win32;

namespace InstallActions
{
    [RunInstaller(true)]
    public partial class InstallActions : System.Configuration.Install.Installer
    {
        public InstallActions()
        {
            
        }

        private static string RegistryPathBase = "Software\\Wow6432Node\\Silicon Nervous System\\Live Electrochemistry";          //if changed, update in SNS_Post_Install_Scripting

        public override void Install(IDictionary stateSaver)
        {
            //WriteLoadsLines("################################################--------------------####################################", 1000);


            //FileStream stream;
            //StreamWriter writer;

            //try
            //{
            //    stream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SNS install log.txt"), FileMode.OpenOrCreate, FileAccess.Write);
            //    writer = new StreamWriter(stream);
            //    WriteLoadsLines("Log File created and opened OK", 1000);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Cannot open output file for writing");
            //    Console.WriteLine(ex.Message);
            //    WriteLoadsLines("Cannot open output file for writing", 1000);

            //    return;
            //}
            //Console.SetOut(writer);

            Console.WriteLine("___________________________________________________________________");
            Console.WriteLine("Updating Registry");

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryPathBase, true))
                {
                    if (key != null)
                    {
                        key.SetValue("InstallationDate", DateTime.Now.ToString());
                        Console.WriteLine("key.SetValue completed OK");
                        WriteLoadsLines("key.SetValue completed OK", 1000);
                    }
                    else
                    {
                        Console.WriteLine("Key returned = NULL");
                        WriteLoadsLines("Key returned = NULL", 5000);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to update Reigstry");
                Console.WriteLine(e.Message);
                WriteLoadsLines("Unable to update Reigstry", 5000);
            }

            Console.WriteLine("___________________________________________________________________");

            //writer.Close();
            //stream.Close();
            base.Install(stateSaver);
        }

        private static void WriteLoadsLines(string Message, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                Console.WriteLine("{0}\t\t{1}", Count.ToString(), Message);
            }
        }

    }

   
}
