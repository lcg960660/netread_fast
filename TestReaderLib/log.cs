using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RFID_Reader_Csharp
{
    class log
    {

        static string logfile = string.Empty;
        public void creatlog(string filename)
        {
            
            logfile = System.AppDomain.CurrentDomain.BaseDirectory + @"\log\";
            if (!Directory.Exists(logfile))//如果不存在就创建 dir 文件夹  
                Directory.CreateDirectory(logfile);
            logfile += @filename;
            File.WriteAllText(logfile, "start:\r\n");
                FileStream fs = new FileStream(logfile, FileMode.Truncate, FileAccess.ReadWrite);
                fs.Close();

        }
        //static string logfile = System.AppDomain.CurrentDomain.BaseDirectory + @"log.txt";
        public void writelog(string s)
        {
            DateTime a = DateTime.Now;
            if (!File.Exists(logfile))
                File.WriteAllText(logfile,a.ToString()+"****" +s+"\r\n");
            else
                File.AppendAllText(logfile, a.ToString() + "****" + s + "\r\n");
        }
        public  void DeleteDirectory(string fileName)
        {
            string destinationFile = System.AppDomain.CurrentDomain.BaseDirectory + @"\log\" + @fileName;

            if (File.Exists(destinationFile))

            {

                FileInfo fi = new FileInfo(destinationFile);

                if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)

                    fi.Attributes = FileAttributes.Normal;

                File.Delete(destinationFile);

            }
        }
    }
}
