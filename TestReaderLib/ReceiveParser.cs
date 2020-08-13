using RFID_Reader_Com;
using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
namespace RFID_Reader_Cmds
{
    public class ReceiveParser
    {
        private static bool frameBeginFlag = false;

        private static bool frameEndFlag = true;

        private static long frameLength;

        private static long strNum;

        private static string[] strBuff = new string[4096];
        private IPAddress ServerIP;
        private IPEndPoint serverFullAddr;
        private Socket sock;

        public event EventHandler<StrArrEventArgs> PacketReceived;

        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int i = 10;
            try
            {
                Sp.GetInstance().Listening = true;
             
                ServerIP = IPAddress.Parse("192.168.31.200");
                serverFullAddr = new IPEndPoint(ServerIP, 2000);
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(serverFullAddr);
                byte[] DataCom = new byte[i];
                sock.Receive(DataCom);
                string[] DataRX = new string[DataCom.Length];
             
                for (int j = 0; j < DataCom.Length; j++)
                {
                    DataRX[j] = DataCom[j].ToString("X2").ToUpper();
                }
                if (i != 0)
                {
                    for (int k = 0; k < i; k++)
                    {
                        if (ReceiveParser.frameBeginFlag)
                        {
                            ReceiveParser.strBuff[(int)(checked((IntPtr)ReceiveParser.strNum))] = DataRX[k];
                            if (ReceiveParser.strNum == 4L)
                            {
                                ReceiveParser.frameLength = (long)(256 * Convert.ToInt32(ReceiveParser.strBuff[3], 16) + Convert.ToInt32(ReceiveParser.strBuff[4], 16));
                                if (ReceiveParser.frameLength > 3072L)
                                {
                                    ReceiveParser.frameBeginFlag = false;
                                    goto IL_27C;
                                }
                            }
                            else if (ReceiveParser.strNum == ReceiveParser.frameLength + 6L && ReceiveParser.strBuff[(int)(checked((IntPtr)ReceiveParser.strNum))] == "7E")
                            {
                                int checksum = 0;
                                int l = 1;
                                while ((long)l < ReceiveParser.strNum - 1L)
                                {
                                    checksum += Convert.ToInt32(ReceiveParser.strBuff[l], 16);
                                    l++;
                                }
                                checksum %= 256;
                                if (checksum != Convert.ToInt32(ReceiveParser.strBuff[(int)(checked((IntPtr)(unchecked(ReceiveParser.strNum - 1L))))], 16))
                                {
                                    MessageBox.Show("ERROR FRAME, checksum is not right!");
                                    ReceiveParser.frameBeginFlag = false;
                                    ReceiveParser.frameEndFlag = true;
                                    goto IL_27C;
                                }
                                ReceiveParser.frameBeginFlag = false;
                                ReceiveParser.frameEndFlag = true;
                                if (this.PacketReceived != null)
                                {
                                    string[] packet = new string[ReceiveParser.strNum + 1L];
                                    int m = 0;
                                    while ((long)m <= ReceiveParser.strNum)
                                    {
                                        packet[m] = ReceiveParser.strBuff[m];
                                        m++;
                                    }
                                    this.PacketReceived(this, new StrArrEventArgs(packet));
                                }
                            }
                            else if (ReceiveParser.strNum == ReceiveParser.frameLength + 6L && ReceiveParser.strBuff[(int)(checked((IntPtr)ReceiveParser.strNum))] != "7E")
                            {
                                MessageBox.Show("ERROR FRAME, cannot get FRAME_END when extends frameLength");
                                ReceiveParser.frameBeginFlag = false;
                                ReceiveParser.frameEndFlag = true;
                                goto IL_27C;
                            }
                            ReceiveParser.strNum += 1L;
                        }
                        else if (DataRX[k] == "BB" && !ReceiveParser.frameBeginFlag)
                        {
                            ReceiveParser.strNum = 0L;
                            ReceiveParser.strBuff[(int)(checked((IntPtr)ReceiveParser.strNum))] = DataRX[k];
                            ReceiveParser.frameBeginFlag = true;
                            ReceiveParser.frameEndFlag = false;
                            ReceiveParser.strNum = 1L;
                        }
                    IL_27C:;
                    }
                }
            }
            finally
            {
                Sp.GetInstance().Listening = false;
            }
        }
    }
}
