using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace StartTA_TCFile
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ProgramData thisProg = new ProgramData();
            TrueCryptSWObj tcSoftware = new TrueCryptSWObj(thisProg);
            TC_Data_File tcData = new TC_Data_File(thisProg);
            tcData.OpenTcFile(tcSoftware, thisProg);
            MessageBox.Show("tcopen done");
        }
    }
}
