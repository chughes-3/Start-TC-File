using System;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Management;
using System.Collections.Generic;
using System.Diagnostics;

using shelllink; // the shelllink namespace for creating shortcuts

namespace StartTA_TCFile
{
    class TC_Data_File
    {
        string dataFileName = "TPDATA.TC";
        string tCryptDriveName = "P";
        internal string path = string.Empty;
        #region TC_Data_File initialization
        internal TC_Data_File(ProgramData thisProg)
        {
            if (Directory.Exists(tCryptDriveName + ":\\"))
            {
                MessageBox.Show("The P Drive exists. Please close it and restart this program", thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            if (!Environment.GetEnvironmentVariable("SystemDrive").StartsWith(thisProg.drvLetter, StringComparison.CurrentCultureIgnoreCase) | Environment.OSVersion.Version.Major < 6)
            {
                path = thisProg.drvLetter + ":\\" + dataFileName;
            }
            else
            {
                path = Environment.GetEnvironmentVariable("Public") + "\\" + dataFileName;
            }
            if (!File.Exists(path))
            {
                MessageBox.Show("we have a problem no tpdatafile exists on " + path);
                Environment.Exit(1);
            }
        } 
        #endregion
        internal void OpenTcFile(TrueCryptSWObj tcSwObj,ProgramData thisProg)
        {
            Process tcproc = new Process();
            tcproc.StartInfo.FileName = tcSwObj.tcProgramFQN;
            tcproc.StartInfo.Arguments = " /q /v " + path + " /l" + tCryptDriveName;
            try
            {
                tcproc.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception on starting TrueCrypt \r\n" + e.ToString(), thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            tcproc.WaitForExit();
            if (tcproc.ExitCode != 0)
            {
                MessageBox.Show("Drive Opening failed in some way, Exiting", thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
    }
    class TrueCryptSWObj
    {
        readonly string TAtcSetupProgramName = "Tax-Aide TrueCrypt Utility.exe";    
        readonly string tcSetupVersion = "7.0"; // this is version below which TC software upgrade on trav of hd will happen
        readonly string tcProgramName = "TrueCrypt.exe";
        readonly string travDir = "Tax-Aide_Traveler\\";
        readonly string stopTravDrv = "\\Stop_Tax-Aide_Drive.exe";
        public string tcProgramFQN = string.Empty;
        public string tcProgramDirectory;
        string tCryptRegEntry;

        #region TrueCryptSWObj Initialization
        public TrueCryptSWObj(ProgramData thisProg) // initialize all static entries
        {

            tCryptRegEntry = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\TrueCryptVolume\Shell\open\command", "", "");
            if (tCryptRegEntry != null)
            {
                tcProgramFQN = tCryptRegEntry.Substring(1, tCryptRegEntry.Length - 10); //registry entry has a leading quote that needs to go
                tcProgramDirectory = tcProgramFQN.Substring(0, tcProgramFQN.Length - 14);
                if (!File.Exists(tcProgramFQN))
                {
                    MessageBox.Show("Windows has registry entries for the TrueCrypt Program but no program exists. Please reinstall", thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
                if (thisProg.removable == true && string.Compare(FileVersionInfo.GetVersionInfo(tcProgramFQN).FileVersion, tcSetupVersion) < 0)
                {   // we have host version that needs upgrading
                    MessageBox.Show("The TrueCrypt software version on this Flash Drive will not work with the version of TrueCrypt on this host system. The Tax-Aide TrueCrypt Utility will be started so that the Host's TrueCrypt can be upgraded. Then restart this Start Tax-Aide drive program", thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    try
                    {
                        Process proc = Process.Start(thisProg.drvLetter + ":\\" + travDir + TAtcSetupProgramName);
                        Environment.Exit(1);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Exception on starting Tax-Aide TrueCrypt Utility\r\n" + e.ToString(), thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                if (thisProg.removable == true)
                {
                    tcProgramFQN = thisProg.drvLetter + ":\\" + travDir + tcProgramName;
                    tcProgramDirectory = thisProg.drvLetter + ":\\" + travDir;
                    if (!File.Exists(tcProgramFQN))
                    {
                        MessageBox.Show("The TrueCrypt Program does not exist on the Traveler drive. Please reinistall" + "\r" + tcProgramFQN, thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    MessageBox.Show("The TrueCrypt program does not exist. Please run the Tax-Aide TrueCrypt Installer", thisProg.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
            }
            if (thisProg.removable == true)
            {
                string tempFolder = Environment.GetEnvironmentVariable("temp");
                File.Copy(thisProg.scriptExePath + stopTravDrv, tempFolder + stopTravDrv, true);
                File.Copy(thisProg.scriptExePath + "\\decryption.ico", tempFolder + "\\decryption.ico", true);
                ShellLink desktopShortcut = new ShellLink();
                desktopShortcut.ShortCutFile = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Stop Traveler.lnk";
                desktopShortcut.Target = tempFolder + stopTravDrv;
                desktopShortcut.IconPath = tempFolder + "\\" + "decryption.ico";
                desktopShortcut.Save();
                desktopShortcut.Dispose();
            }
        } 
        #endregion
    }
    class ProgramData
    {
        public readonly string mbCaption = "AARP Tax-Aide TrueCrypt Open Data File";
        public string drvLetter;
        public bool removable = false;  //will be set later if program running from usb drive
        public string scriptExePath = Assembly.GetEntryAssembly().CodeBase;    // format is, file:///D:/blah/blah.exe so 3 slashes then next 2 to get drive
        string patternPath = "(?<=///).+(?=/.*$)";  //matches d:/trav in file:///d:/trav/abc.exe
        //string pattern = "(?<=//)[a-zA-Z](?=:)";    //matches // followed by a letter followed by :
        public ProgramData()
        {
            Regex r = new Regex(patternPath);
            Match m = r.Match(scriptExePath);
            scriptExePath = m.Value.Replace("/","\\");  //sets path to script directory
            drvLetter = scriptExePath.Substring(0,1);
            GetUSBDrivesSetRemovable();
        }

        private void GetUSBDrivesSetRemovable() // Minor modes to GetUSBDrives from install
        {
            ManagementClass logicalToPartition = new ManagementClass("Win32_LogicalDiskToPartition");
            ManagementClass partitionToDiskDrv = new ManagementClass("Win32_DiskDriveToDiskPartition");
            ManagementClass diskDrvs = new ManagementClass("Win32_DiskDrive");
            List<ManagementObject> usbDrvs = new List<ManagementObject>();
            List<ManagementObject> partitions = new List<ManagementObject>();
            //List<ManagementObject> logicalDrvs = new List<ManagementObject>();
            foreach (ManagementObject udrv in diskDrvs.GetInstances())
            {
                if (udrv.GetPropertyValue("PNPDeviceID").ToString().StartsWith("USBSTOR"))
                {
                    usbDrvs.Add(udrv);
                }
            }
            foreach (ManagementObject ud in usbDrvs)
            {
                foreach (ManagementObject parti in partitionToDiskDrv.GetInstances())
                {
                    if (parti.GetPropertyValue("Antecedent").ToString().Contains(ud.GetPropertyValue("DeviceID").ToString().Replace(@"\", @"\\")))
                    {
                        partitions.Add(parti);
                        break; //make sure only get one partition not 2
                    }
                }
            }
            foreach (ManagementObject partit in partitions)
            {
                foreach (ManagementObject logDrv in logicalToPartition.GetInstances())
                {
                    if (partit.GetPropertyValue("Dependent").ToString() == logDrv.GetPropertyValue("Antecedent").ToString())
                    {
                        //logicalDrvs.Add(logDrv);
                        //DrvInfo mydrive = new DrvInfo();
                        if (drvLetter == logDrv.GetPropertyValue("Dependent").ToString().Substring(logDrv.GetPropertyValue("Dependent").ToString().Length - 3, 1))
                        {
                            removable = true;
                        }
                        //DriveInfo drvInf = new DriveInfo(mydrive.drvName);
                        //mydrive.volName = drvInf.VolumeLabel;
                        //mydrive.combo = mydrive.drvName + " (" + mydrive.volName + ")";
                        //mydrive.tcFilePoss = string.Empty;
                        //travUSBDrv.Add(mydrive);
                    }
                }
            }
        }
    }
}
