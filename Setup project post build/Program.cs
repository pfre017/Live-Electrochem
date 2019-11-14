using System;
using WindowsInstaller;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Post_Setup_Scripting
{
    class Program
    {

        //$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        //      This script runs at the time of building the Setup application.
        //      But all it does it write into the setup files the appropriate Registry values that will be
        //      added target computer when installed. Therefore, this script does not run when Installed, but
        //      does prepare the Setup application for Install






        static void Main(string[] args)
        {
            Console.WriteLine("___________________________________________________________________");
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().FullName);
            Console.WriteLine("{0} args passed in", args.Length);

            Console.WriteLine("___________________________________________________________________");

            int count = 0;
            foreach (string arg in args)
            {
                Console.WriteLine("{0}: {1}", count, arg);
                    count++;
            }


            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect args.  {0}", DateTime.Now.ToString());
                Console.ReadLine();
                return;
            }

            //arg 1 - path to MSI
            string PathToMSI = args[0];
            //arg 2 - path to assembly
            string PathToAssembly = args[1];


            string InstallerVersion = GetMsiProperty(PathToMSI, "ProductVersion");





            Type InstallerType;
            WindowsInstaller.Installer Installer;
            InstallerType = Type.GetTypeFromProgID("WindowsInstaller.Installer");
            Installer = (WindowsInstaller.Installer)Activator.CreateInstance(InstallerType);

            Assembly Assembly = Assembly.LoadFrom(PathToAssembly);
            string AssemblyStrongName = Assembly.GetName().FullName;
            string AssemblyVersion = Assembly.GetName().Version.ToString();
            string Date = DateTime.Now.ToString();

            Console.WriteLine("PathToAssembly: {0}", PathToAssembly);
            Console.WriteLine("PathToMSI: {0}", PathToMSI);
            Console.WriteLine("AssemblyStrongName: {0}", AssemblyStrongName);
            Console.WriteLine("AssemblyVersion: {0}", AssemblyVersion);
            Console.WriteLine("InstallerVersion: {0}", InstallerVersion);
            Console.WriteLine("InstallerBuildDate: {0}", Date);

            Console.WriteLine();



            string SQL = "SELECT `Key`, `Name`, `Value` FROM `Registry`";
            WindowsInstaller.Database Db = Installer.OpenDatabase(PathToMSI, WindowsInstaller.MsiOpenDatabaseMode.msiOpenDatabaseModeDirect);

            WindowsInstaller.View View = Db.OpenView(SQL);
            View.Execute();

            WindowsInstaller.Record Rec = View.Fetch();
            while (Rec != null)
            {
                for (int c = 0; c <= Rec.FieldCount; c++)
                {
                    string Column = Rec.get_StringData(c);
                    Column = Column.Replace("[AssemblyVersion]", AssemblyVersion);
                    Column = Column.Replace("[AssemblyStrongName]", AssemblyStrongName);
                    Column = Column.Replace("[InstallerBuildDate]", Date);
                    Column = Column.Replace("[InstallerVersion]", InstallerVersion);

                    Rec.set_StringData(c, Column);
                    View.Modify(MsiViewModify.msiViewModifyReplace, Rec);
                    Console.Write("{0}\t", Column);
                    Db.Commit();
                }
                Console.WriteLine();
                Rec = View.Fetch();
            }
            View.Close();

            GC.Collect();
            Marshal.FinalReleaseComObject(Installer);

            Console.ReadLine();
        }

        static string GetMsiProperty(string msiFile, string property)
        {
            string retVal = string.Empty;

            // Create an Installer instance  
            Type classType = Type.GetTypeFromProgID("WindowsInstaller.Installer");
            Object installerObj = Activator.CreateInstance(classType);
            Installer installer = installerObj as Installer;

            // Open the msi file for reading  
            // 0 - Read, 1 - Read/Write  
            Database database = installer.OpenDatabase(msiFile, WindowsInstaller.MsiOpenDatabaseMode.msiOpenDatabaseModeDirect);

            // Fetch the requested property  
            string sql = String.Format(
                "SELECT Value FROM Property WHERE Property='{0}'", property);
            View view = database.OpenView(sql);
            view.Execute(null);

            // Read in the fetched record  
            Record record = view.Fetch();
            if (record != null)
            {
                retVal = record.get_StringData(1);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(record);
            }
            view.Close();
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(view);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(database);

            return retVal;
        }
    }
}