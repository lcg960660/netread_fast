using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using RFID_Reader_Csharp;
namespace RFID_Reader_Com
{
    public class Sp
    {
        public SerialPort ComDevice = new SerialPort();

        private static readonly Sp instance = new Sp();

        private int communicatBaudrate = 115200;

        public bool Listening;

        public bool Closing;

        public event EventHandler<byteArrEventArgs> DataSent;

        private Sp()
        {
   
            this.ComDevice.PortName = "COM1";
            this.ComDevice.BaudRate = 9600;
            this.ComDevice.Parity = Parity.None;
            this.ComDevice.DataBits = 8;
            this.ComDevice.StopBits = StopBits.One;
            this.ComDevice.NewLine = "/r/n";
        }

        public static Sp GetInstance()
        {
            return Sp.instance;
        }

        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void Config(string port, int baudrate, Parity p, int databits, StopBits s)
        {
            this.ComDevice.PortName = port;
            this.ComDevice.BaudRate = baudrate;
            this.ComDevice.Parity = p;
            this.ComDevice.DataBits = 8;
            this.ComDevice.StopBits = s;
        }

        public bool Open()
        {
            if (this.ComDevice.IsOpen)
            {
                return true;
            }
            try
            {
                this.ComDevice.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Open Port Fail, " + ex.Message);
                return false;
            }
           // MessageBox.Show("Port Opened, ");
            return true;
        }

        public bool Close()
        {
            this.Closing = true;
            if (!this.ComDevice.IsOpen)
            {
                this.Closing = false;
                return true;
            }
            try
            {
                while (this.Listening)
                {
                    Application.DoEvents();
                }
                this.ComDevice.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Close Port Fail, " + ex.Message);
                this.Closing = false;
                return false;
            }
            this.Closing = false;

           // MessageBox.Show("Closed Port, ");
            return true;
        }

        public bool IsOpen()
        {
            return this.ComDevice.IsOpen;
        }

        public void SetCommunicatBaudRate(int baudrate)
        {
            this.communicatBaudrate = baudrate;
        }

        public int GetCommunicateBaudRate()
        {
            return this.communicatBaudrate;
        }

        private void ComDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
        }

        public int Send(string hexText)
        {
            if (hexText.Length <= 0)
            {
                MessageBox.Show("Please Write Send Data!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return 0;
            }
            if (this.ComDevice.IsOpen)
            {
                byte[] SendBytes = null;
                try
                {
                    string SendData = hexText.Replace(" ", "");
                    if (SendData.Length % 2 == 1)
                    {
                        SendData = SendData.Remove(SendData.Length - 1, 1);
                    }
                    List<string> SendDataList = new List<string>();
                    for (int i = 0; i < SendData.Length; i += 2)
                    {
                        SendDataList.Add(SendData.Substring(i, 2));
                    }
                    SendBytes = new byte[SendDataList.Count];
                    for (int j = 0; j < SendBytes.Length; j++)
                    {
                        SendBytes[j] = (byte)Convert.ToInt32(SendDataList[j], 16);
                        
                       
                    }
                }
                catch
                {
                    MessageBox.Show("Please Use HEX words!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return 0;
                }
                this.ComDevice.Write(SendBytes, 0, SendBytes.Length);
                log logfile=new log();
                logfile.writelog("Send: "+hexText);
                
                if (this.DataSent != null)
                {
                    this.DataSent(this, new byteArrEventArgs(SendBytes));
                }
                return SendBytes.Length;
            }
            MessageBox.Show("Please Connect Serial Port!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return 0;
        }

        public void DownLoadFW(string fileName)
        {
            if (this.ComDevice.IsOpen)
            {
                this.Send("FE");
                Thread.Sleep(10);
                int num = this.communicatBaudrate;
                if (num <= 28800)
                {
                    if (num == 9600)
                    {
                        this.Send("B0");
                        goto IL_CC;
                    }
                    if (num == 19200)
                    {
                        this.Send("B1");
                        goto IL_CC;
                    }
                    if (num == 28800)
                    {
                        this.Send("B2");
                        goto IL_CC;
                    }
                }
                else
                {
                    if (num == 38400)
                    {
                        this.Send("B3");
                        goto IL_CC;
                    }
                    if (num == 57600)
                    {
                        this.Send("B4");
                        goto IL_CC;
                    }
                    if (num == 115200)
                    {
                        this.Send("B5");
                        goto IL_CC;
                    }
                }
                this.Send("B5");
            IL_CC:
                Thread.Sleep(10);
                this.ComDevice.DiscardInBuffer();
                this.ComDevice.DiscardOutBuffer();
                this.Closing = true;
                while (this.Listening)
                {
                    Application.DoEvents();
                }
                this.ComDevice.Close();
                this.Closing = false;
                this.ComDevice.BaudRate = this.communicatBaudrate;
                this.ComDevice.Open();
                this.Send("DB");
                Thread.Sleep(10);
                this.Send("FD");
                Thread.Sleep(10);
                FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                BinaryReader sr = new BinaryReader(file);
                int count = (int)file.Length;
                byte[] buffer = new byte[count];
                sr.Read(buffer, 0, buffer.Length);
                this.ComDevice.Write(buffer, 0, buffer.Length);
                this.Send("D3 D3 D3 D3 D3 D3");
                Thread.Sleep(10);
                sr.Close();
                file.Close();
                return;
            }
            MessageBox.Show("Please Open COM Port First!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}
