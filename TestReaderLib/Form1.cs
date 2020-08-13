using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using RFID_Reader_Cmds;
using RFID_Reader_Com;
using BarChart;
using System.Globalization;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace RFID_Reader_Csharp
{

    public partial class Form1 : Form
    {
        private bool bAutoSend = false;
        private int LoopNum_cnt = 0;
        private bool change_q_1st = true;
        private bool change_q_message = true;
        private bool start_count_rfid = false;
        public ReceiveParser rp;

        DataTable basic_table = new DataTable();
        DataTable advanced_table = new DataTable();
        DataSet ds_basic = null;
        DataSet ds_advanced = null;
        string pc = string.Empty;
        string epc = string.Empty;
        string crc = string.Empty;
        string rssi = string.Empty;

        int FailEPCNum = 0;
        int SucessEPCNum = 0;
        double errnum = 0;
        double db_errEPCNum = 0;
        double db_LoopNum_cnt = 0;
        string per = "0.000";

        private String timeFormat = "yyyy/MM/dd HH:mm:ss.ff";
        //private String timeFormat = System.Globalization.DateTimeFormatInfo.CurrentInfo.ShortDatePattern.ToString() + " HH:mm:ss.ff";

        static string[] strBuff = new String[4096];

        int rowIndex = 0;
        int initDataTableLen = 1;  //初始化Datatable的行数

        private static int[] mixerGainTable = {0, 3, 6, 9, 12, 15, 16};

        private static int[] IFAmpGainTable = { 12, 18, 21, 24, 27, 30, 36, 40 };

        private string sn;
        private List<string> epc_no_tid, EPC_No;
        public Form1()
        {
            logfile = new log();
            EPC_No = new List<string>();
            logfile.creatlog("debug.txt");
            Readconfig();
            try
            {
                ServerIP = IPAddress.Parse(Ip);
                serverFullAddr = new IPEndPoint(ServerIP, 2000);
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(serverFullAddr);
            }
           catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                logfile.writelog(ex.Message);
                System.Environment.Exit(0);
            }
            InitializeComponent();
            setTip();
            this.groupBox3.Text = Ip;
            this.tabPage3.Parent = null;
            this.cbxRegion.SelectedIndex = 0;
            this.cbxChannel.SelectedIndex = 0;
            this.cbxDR.SelectedIndex = 0;
            this.cbxM.SelectedIndex = 0;
            this.cbxTRext.SelectedIndex = 1;
            this.cbxSel.SelectedIndex = 0;
            this.cbxSession.SelectedIndex = 0;
            this.cbxTarget.SelectedIndex = 0;
            this.cbxQBasic.SelectedIndex = 4;
            this.cbxQAdv.SelectedIndex = 4;
            this.cbxMemBank.SelectedIndex = 3;
            this.cbxSelTarget.SelectedIndex = 0;
            this.cbxAction.SelectedIndex = 0;
            this.cbxSelMemBank.SelectedIndex = 1;
            this.cbxPaPower.SelectedIndex = 0;
            this.cbxMixerGain.SelectedIndex = 3;
            this.cbxIFAmpGain.SelectedIndex = 6;
            this.cbxMode.SelectedIndex = 2;
            this.cbxIO.SelectedIndex = 0;
            this.cbxIoLevel.SelectedIndex = 0;
            this.cbxIoDircetion.SelectedIndex = 0;
            this.cbxLockKillAction.SelectedIndex = 0;
            this.cbxLockAccessAction.SelectedIndex = 0;
            this.cbxLockEPCAction.SelectedIndex = 0;
            this.cbxLockTIDAction.SelectedIndex = 0;
            this.cbxLockUserAction.SelectedIndex = 0;
            

        }
        private void Readconfig()
        {

                connect = AccessAppSettings("mysql");
                password = AccessAppSettings("pwd");
                command = AccessAppSettings("comm");
                begintxt = AccessAppSettings("begin");
                strFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @begintxt);
                sntxt = AccessAppSettings("sn");
                timeout = int.Parse(AccessAppSettings("timeout"));
                allepccount = int.Parse(AccessAppSettings("count"));
               times = int.Parse(AccessAppSettings("times"));
                Ip= AccessAppSettings("ip");
                model = AccessAppSettings("model");
                performance= AccessAppSettings("performance");
        }


        /// <summary>
        /// 从汉字转换到16进制
        /// </summary>
        /// <param name="s"></param>
        /// <param name="charset">编码,如"utf-8","gb2312"</param>
        /// <param name="fenge">是否每字符用逗号分隔</param>
        /// <returns></returns>
        public static string ToHex(string s, string charset, bool fenge)
{
    if ((s.Length % 2) != 0)
    {
        s += " ";//空格
                 //throw new ArgumentException("s is not valid chinese string!");
    }
    System.Text.Encoding chs = System.Text.Encoding.GetEncoding(charset);
    byte[] bytes = chs.GetBytes(s);
    string str = "";
    for (int i = 0; i < bytes.Length; i++)
    {
        str += string.Format("{0:X}", bytes[i]);
        if (fenge && (i != bytes.Length - 1))
        {
            str += string.Format("{0}", ",");
        }
    }
    return str.ToLower();
}

///<summary>
/// 从16进制转换成汉字
/// </summary>
/// <param name="hex"></param>
/// <param name="charset">编码,如"utf-8","gb2312"</param>
/// <returns></returns>
public static string UnHex(string hex, string charset)
{
    if (hex == null)
        throw new ArgumentNullException("hex");
    hex = hex.Replace(",", "");
    hex = hex.Replace("\n", "");
    hex = hex.Replace("\\", "");
    hex = hex.Replace(" ", "");
    if (hex.Length % 2 != 0)
    {
        hex += "20";//空格
    }
    // 需要将 hex 转换成 byte 数组。 
    byte[] bytes = new byte[hex.Length / 2];


    for (int i = 0; i < bytes.Length; i++)
    {
        try
        {
            // 每两个字符是一个 byte。 
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2),
            System.Globalization.NumberStyles.HexNumber);
        }
        catch
        {
            // Rethrow an exception with custom message. 
            throw new ArgumentException("hex is not a valid hex number!", "hex");
        }
    }
    System.Text.Encoding chs = System.Text.Encoding.GetEncoding(charset);
    return chs.GetString(bytes);
}
private string AccessAppSettings(string key)
{
            try
            {
                //获取Configuration对象
                Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                //根据Key读取元素的Value
                string name = config.AppSettings.Settings[key].Value;
                ////写入元素的Value
                //config.AppSettings.Settings["TemplatePATH"].Value = "xieyc";
                ////增加元素
                //config.AppSettings.Settings.Add("url", "http://www.myhack58.com");
                ////删除元素
                //config.AppSettings.Settings.Remove("name");
                //一定要记得保存，写不带参数的config.Save()也可以
                config.Save(ConfigurationSaveMode.Modified);
                //刷新，否则程序读取的还是之前的值（可能已装入内存）
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                return name;
            }
            catch(Exception ex)
            {
                MessageBox.Show("读取关键词 "+key+" 出错");
                logfile.writelog("读取关键词 " + key + " 出错");
                System.Environment.Exit(0);
                return "";
            }
        }
private void Form1_Load(object sender, EventArgs e)
        {
           
            //ComDevice.DataReceived += new SerialDataReceivedEventHandler(ComDevice_DataReceived);
            rp = new ReceiveParser();
            Sp.GetInstance().ComDevice.DataReceived += new SerialDataReceivedEventHandler(rp.DataReceived);
           // rp.PacketReceived +=new EventHandler<StrArrEventArgs>(rp_PaketReceived);
            Sp.GetInstance().DataSent += new EventHandler<byteArrEventArgs>(ComDataSent);
            this.dgvEpcBasic.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(dgvEpcBasic_DataBindingComplete);
            this.dgv_epc2.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(dgv_epc2_DataBindingComplete);
            //ComDevice.NewLine = "/r/n";
            change_q_1st = false;

            //DataGridView
            ds_basic = new DataSet();
            ds_advanced = new DataSet();
            //dt = new DataTable();

            basic_table = BasicGetEPCHead();
            advanced_table = AdvancedGetEPCHead();
            ds_basic.Tables.Add(basic_table);
            ds_advanced.Tables.Add(advanced_table);

            DataView BasicDataViewEpc = ds_basic.Tables[0].DefaultView;
            DataView AdvancedDataViewEpc = ds_advanced.Tables[0].DefaultView;
            this.dgvEpcBasic.DataSource = BasicDataViewEpc;
            this.dgv_epc2.DataSource = AdvancedDataViewEpc;
            Basic_DGV_ColumnsWidth(this.dgvEpcBasic);
            Advanced_DGV_ColumnsWidth(this.dgv_epc2);
            btnInvtBasic.Focus();

            adjustUIcomponents("M100");
        }

        private void ComDataSent(object sender, byteArrEventArgs e)
        {
            txtCOMTxCnt.Text = (Convert.ToInt32(txtCOMTxCnt.Text) + e.Data.Length).ToString();
            txtCOMTxCnt_adv.Text = txtCOMTxCnt.Text;
        }

        //private void rp_PaketReceived(object sender, StrArrEventArgs e)
        private void rp_PaketReceived(string [] x)
        {
            string[] packetRx =x;
            string strPacket = string.Empty;
            for (int i = 0; i < packetRx.Length; i++)
            {
                strPacket += packetRx[i] + " ";
            }
            this.Invoke((EventHandler)(delegate
            {
                txtCOMRxCnt.Text = (Convert.ToInt32(txtCOMRxCnt.Text) + packetRx.Length).ToString();
                txtCOMRxCnt_adv.Text = txtCOMRxCnt.Text;

                //auto clear received data region
                int txtReceive_len = txtReceive.Lines.Length; //txtReceive.GetLineFromCharIndex(txtReceive.Text.Length + 1);
                if (cbxAutoClear.Checked)
                {
                    if (txtReceive_len > 9)
                    {
                        txtReceive.Text = string.Empty;
                    }
                }
                #region show received packet
                if (cbxRxVisable.Checked == true)
                {
                    //this.txtReceive.Text = this.txtReceive.Text + strPacket + "\r\n";
                    this.txtReceive.Text = strPacket;
                    
                }
                if (packetRx[1] == ConstCode.FRAME_TYPE_INFO && packetRx[2] == ConstCode.CMD_INVENTORY)         //Succeed to Read EPC
                {
                    //Console.Beep();
                    SucessEPCNum = SucessEPCNum + 1;
                    db_errEPCNum = FailEPCNum;
                    db_LoopNum_cnt = db_LoopNum_cnt + 1;
                    errnum = (db_errEPCNum / db_LoopNum_cnt) * 100;
                    per = string.Format("{0:0.000}", errnum);

                    int rssidBm = Convert.ToInt16(packetRx[5], 16); // rssidBm is negative && in bytes
                    if (rssidBm > 127)
                    {
                        rssidBm = -((-rssidBm)&0xFF);
                    }
                    rssidBm -= Convert.ToInt32(tbxCoupling.Text, 10);
                    rssidBm -= Convert.ToInt32(tbxAntennaGain.Text, 10);
                    rssi = rssidBm.ToString();
                    string s="";
                    foreach (string a in packetRx)
                        s += a;
                    string c = packetRx[6];
                    int PCEPCLength = ((Convert.ToInt32((packetRx[6]), 16)) / 8 + 1) * 2;
                    pc = packetRx[6] + " " + packetRx[7];
                    epc = string.Empty;
                    for (int i = 0; i < PCEPCLength - 2; i++)
                    {
                        epc = epc + packetRx[8 + i];
                    }
                    epc = Commands.AutoAddSpace(epc);
                    crc = packetRx[6 + PCEPCLength] + " " + packetRx[7 + PCEPCLength];
                    GetEPC(pc, epc, crc, rssi, per);
                    if (start_count_rfid && epc != "")
                    {
                        try
                        {
                            EPC_No.Add(epc);
                        }
                        catch (Exception ex)
                        {
                            ;
                        }

                    }
                    logfile.writelog("Recived: " + strPacket);
                    if (model!="read_tid"&&EPC_No.Count >= allepccount)
                    {

                        MessageBox.Show("成功发现共计" + allepccount + "只标签！");
                        // EndAction();
                        //Dictionary<string, int>.KeyCollection keyColl = EPC_No.Keys;
                        //foreach(string s in keyColl)
                        //    MessageBox.Show(s);

                        System.Environment.Exit(0);
                    }
                }
                else if (packetRx[1] == ConstCode.FRAME_TYPE_ANS)
                {
                    if (packetRx[2] == ConstCode.CMD_EXE_FAILED)
                    {
                        int failType = Convert.ToInt32(packetRx[5], 16);
                        if (packetRx.Length > 9) // has PC+EPC field
                        {
                            txtOperateEpc.Text = "";
                            int pcEpcLen = Convert.ToInt32(packetRx[6], 16);

                            for (int i = 0; i < pcEpcLen; i++)
                            {
                                txtOperateEpc.Text += packetRx[i + 7] + " ";
                            }
                        }
                        else
                        {
                            txtOperateEpc.Text = "";
                        }
                        if (packetRx[5] == ConstCode.FAIL_INVENTORY_TAG_TIMEOUT)
                        {
                            FailEPCNum = FailEPCNum + 1;
                            db_errEPCNum = FailEPCNum;
                            db_LoopNum_cnt = db_LoopNum_cnt + 1;
                            errnum = (db_errEPCNum / db_LoopNum_cnt) * 100;
                            per = string.Format("{0:0.000}", errnum);
                            //GetEPC(pc, epc, crc, rssi_i, rssi_q, per);
                            pbx_Inv_Indicator.Visible = false;
                        }
                        else if (packetRx[5] == ConstCode.FAIL_FHSS_FAIL)
                        {
                            //MessageBox.Show("FHSS Failed.", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("FHSS Failed", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_ACCESS_PWD_ERROR)
                        {
                            //MessageBox.Show("Access Failed, Please Check the Access Password!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Access Failed, Please Check the Access Password", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_READ_MEMORY_NO_TAG)
                        {
                            setStatus("No Tag Response, Fail to Read Tag Memory", Color.Red);
                        }
                        else if (packetRx[5].Substring(0,1) == ConstCode.FAIL_READ_ERROR_CODE_BASE.Substring(0,1))
                        {
                            //MessageBox.Show("Read Failed. Error Code: " + ParseErrCode(failType), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Read Failed. Error Code: " + ParseErrCode(failType), Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_WRITE_MEMORY_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, Fail to Write Tag Memory", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, Fail to Write Tag Memory", Color.Red);
                        }
                        else if (packetRx[5].Substring(0, 1) == ConstCode.FAIL_WRITE_ERROR_CODE_BASE.Substring(0, 1))
                        {
                            //MessageBox.Show("Write Failed. Error Code: " + ParseErrCode(failType), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Write Failed. Error Code: " + ParseErrCode(failType), Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_LOCK_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, Lock Operation Failed", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, Lock Operation Failed", Color.Red);
                        }
                        else if (packetRx[5].Substring(0, 1) == ConstCode.FAIL_LOCK_ERROR_CODE_BASE.Substring(0, 1))
                        {
                            //MessageBox.Show("Lock Failed. Error Code: " + ParseErrCode(failType), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Lock Failed. Error Code: " + ParseErrCode(failType), Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_KILL_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, Kill Operation Failed", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, Kill Operation Failed", Color.Red);
                        }
                        else if (packetRx[5].Substring(0, 1) == ConstCode.FAIL_KILL_ERROR_CODE_BASE.Substring(0, 1))
                        {
                            //MessageBox.Show("Kill Failed. Error Code: " + ParseErrCode(failType), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Kill Failed. Error Code: " + ParseErrCode(failType), Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_NXP_CHANGE_CONFIG_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, NXP Change Config Failed!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, NXP Change Config Failed", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_NXP_CHANGE_EAS_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, NXP Change EAS Failed!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, NXP Change EAS Failed", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_NXP_CHANGE_EAS_NOT_SECURE)
                        {
                            //MessageBox.Show("Tag is not in Secure State, NXP Change EAS Failed!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Tag is not in Secure State, NXP Change EAS Failed", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_NXP_EAS_ALARM_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, NXP EAS Alarm Operation Failed!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtOperateEpc.Text = "";
                            setStatus("No Tag Response, NXP EAS Alarm Operation Failed", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_NXP_READPROTECT_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, NXP ReadProtect Failed!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, NXP ReadProtect Failed", Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_NXP_RESET_READPROTECT_NO_TAG)
                        {
                            //MessageBox.Show("No Tag Response, NXP Reset ReadProtect Failed!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("No Tag Response, NXP Reset ReadProtect Failed", Color.Red);
                        }
                        else if (packetRx[5].Substring(0, 1) == ConstCode.FAIL_CUSTOM_CMD_BASE.Substring(0, 1))
                        {
                            //MessageBox.Show("Command Executed Failed. Error Code: " + ParseErrCode(failType), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            setStatus("Command Executed Failed. Error Code: " + ParseErrCode(failType), Color.Red);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_INVALID_PARA)
                        {
                            MessageBox.Show("无效参数", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (packetRx[5] == ConstCode.FAIL_INVALID_CMD)
                        {
                            MessageBox.Show("无效命令!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else if (packetRx[2] == ConstCode.CMD_SET_QUERY)            //SetQuery
                    {
                        MessageBox.Show("Query Parameters is Setted up", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (packetRx[2] == ConstCode.CMD_GET_QUERY)            //GetQuery
                    {
                        string infoGetQuery = string.Empty;
                        string[] strMSB = String16toString2(packetRx[5]);
                        string[] strLSB = String16toString2(packetRx[6]);
                        int intQ = Convert.ToInt32(strLSB[6]) * 8 + Convert.ToInt32(strLSB[5]) * 4
                            + Convert.ToInt32(strLSB[4]) * 2 + Convert.ToInt32(strLSB[3]);
                        string strM = string.Empty;
                        if ((strMSB[6] + strMSB[5]) == "00")
                        {
                            strM = "1";
                        }
                        else if ((strMSB[6] + strMSB[5]) == "01")
                        {
                            strM = "2";
                        }
                        else if ((strMSB[6] + strMSB[5]) == "10")
                        {
                            strM = "4";
                        }
                        else if ((strMSB[6] + strMSB[5]) == "11")
                        {
                            strM = "8";
                        }
                        string strTRext = string.Empty;
                        if (strMSB[4] == "0")
                        {
                            strTRext = "NoPilot";
                        }
                        else
                        {
                            strTRext = "UsePilot";
                        }
                        string strTarget = string.Empty;
                        if (strLSB[7] == "0")
                        {
                            strTarget = "A";
                        }
                        else
                        {
                            strTarget = "B";
                        }
                        infoGetQuery = "DR=" + strMSB[7] + ", ";
                        infoGetQuery = infoGetQuery + "M=" + strM + ", ";
                        infoGetQuery = infoGetQuery + "TRext=" + strTRext + ", ";
                        infoGetQuery = infoGetQuery + "Sel=" + strMSB[3] + strMSB[2] + ", ";
                        infoGetQuery = infoGetQuery + "Session=" + strMSB[1] + strMSB[0] + ", ";
                        infoGetQuery = infoGetQuery + "Target=" + strTarget + ", ";
                        infoGetQuery = infoGetQuery + "Q=" + intQ;
                        MessageBox.Show(infoGetQuery, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (packetRx[2] == ConstCode.CMD_READ_DATA)         //Read Tag Memory
                    {
                        string strInvtReadData = "";
                        txtInvtRWData.Text = "";
                        txtOperateEpc.Text = "";
                        int dataLen = Convert.ToInt32(packetRx[3], 16) * 256 + Convert.ToInt32(packetRx[4], 16);
                        int pcEpcLen = Convert.ToInt32(packetRx[5], 16);

                        for (int i = 0; i < pcEpcLen; i++)
                        {
                            txtOperateEpc.Text += packetRx[i + 6] + " ";
                        }

                        for (int i = 0; i < dataLen - pcEpcLen - 1; i++)
                        {
                            strInvtReadData = strInvtReadData + packetRx[i + pcEpcLen + 6];
                        }
                        txtInvtRWData.Text = Commands.AutoAddSpace(strInvtReadData);
                        setStatus("Read Memory Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_WRITE_DATA)
                    {
                        //MessageBox.Show("Write Memory Success!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        getSuccessTagEpc(packetRx);
                        setStatus("Write Memory Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_LOCK_UNLOCK)
                    {
                        //MessageBox.Show("Lock Success!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        getSuccessTagEpc(packetRx);
                        setStatus("Lock Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_KILL)
                    {
                        //MessageBox.Show("Kill Success!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        getSuccessTagEpc(packetRx);
                        setStatus("Kill Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_NXP_CHANGE_CONFIG)
                    {
                        int pcEpcLen = getSuccessTagEpc(packetRx);
                        string configWord = packetRx[pcEpcLen + 6] + packetRx[pcEpcLen + 7];
                        setStatus("NXP Tag Change Config Success, Config Word: 0x" + configWord, Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_NXP_CHANGE_EAS)
                    {
                        getSuccessTagEpc(packetRx);
                        setStatus("NXP Tag Change EAS Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_NXP_READPROTECT)
                    {
                        getSuccessTagEpc(packetRx);
                        setStatus("NXP Tag ReadProtect Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_NXP_RESET_READPROTECT)
                    {
                        getSuccessTagEpc(packetRx);
                        setStatus("NXP Tag Reset ReadProtect Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_NXP_EAS_ALARM)
                    {
                        setStatus("NXP Tag EAS Alarm Success", Color.MediumSeaGreen);
                    }
                    else if (packetRx[2] == ConstCode.CMD_GET_SELECT_PARA)            //GetQuery
                    {
                        string infoGetSelParam = string.Empty;
                        string[] strSelCombParam = String16toString2(packetRx[5]);
                        string strSelTarget = strSelCombParam[7] + strSelCombParam[6] + strSelCombParam[5];
                        string strSelAction = strSelCombParam[4] + strSelCombParam[3] + strSelCombParam[2];
                        string strSelMemBank = strSelCombParam[1] + strSelCombParam[0];

                        string strSelTargetInfo = null;
                        if (strSelTarget == "000")
                        {
                            strSelTargetInfo = "S0";
                        }
                        else if (strSelTarget == "001")
                        {
                            strSelTargetInfo = "S1";
                        }
                        else if (strSelTarget == "010")
                        {
                            strSelTargetInfo = "S2";
                        }
                        else if (strSelTarget == "011")
                        {
                            strSelTargetInfo = "S3";
                        }
                        else if (strSelTarget == "100")
                        {
                            strSelTargetInfo = "SL";
                        }
                        else
                        {
                            strSelTargetInfo = "RFU";
                        }

                        string strSelMemBankInfo = null;
                        if (strSelMemBank == "00")
                        {
                            strSelMemBankInfo = "RFU";
                        }
                        else if (strSelMemBank == "01")
                        {
                            strSelMemBankInfo = "EPC";
                        }
                        else if (strSelMemBank == "10")
                        {
                            strSelMemBankInfo = "TID";
                        }
                        else
                        {
                            strSelMemBankInfo = "User";
                        }
                        infoGetSelParam = "Target=" + strSelTargetInfo + ", Action=" + strSelAction + ", Memory Bank=" + strSelMemBankInfo;
                        infoGetSelParam = infoGetSelParam + ", Pointer=0x" + packetRx[6] + packetRx[7] + packetRx[8] + packetRx[9];
                        infoGetSelParam = infoGetSelParam + ", Length=0x" + packetRx[10];
                        string strTruncate = null;
                        if (packetRx[11] == "00")
                        {
                            strTruncate = "Disable Truncation";
                        }
                        else
                        {
                            strTruncate = "Enable Truncation";
                        }
                        infoGetSelParam = infoGetSelParam + ", " + strTruncate;

                        this.txtGetSelLength.Text = packetRx[10];

                        string strGetSelMask = null;
                        int intGetSelMaskByte = Convert.ToInt32(packetRx[10], 16) / 8;
                        int intGetSelMaskBit = Convert.ToInt32(packetRx[10], 16) - intGetSelMaskByte * 8;
                        if (intGetSelMaskBit == 0)
                        {
                            for (int i = 0; i < intGetSelMaskByte; i++)
                            {
                                strGetSelMask = strGetSelMask + packetRx[12 + i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < intGetSelMaskByte + 1; i++)
                            {
                                strGetSelMask = strGetSelMask + packetRx[12 + i];
                            }
                        }

                        this.txtGetSelMask.Text = Commands.AutoAddSpace(strGetSelMask);
                        MessageBox.Show(infoGetSelParam, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (packetRx[2] == ConstCode.CMD_GET_RF_CHANNEL)
                    {
                        double curRfCh = Convert.ToInt32(packetRx[5],16);
                        switch (curRegion)
                        {
                            case ConstCode.REGION_CODE_CHN2 : // China 2
                                curRfCh = 920.125 + curRfCh * 0.25;
            	                break;
                            case ConstCode.REGION_CODE_CHN1: // China 1
                                curRfCh = 840.125 + curRfCh * 0.25;
                                break;
                            case ConstCode.REGION_CODE_US: // US
                                curRfCh = 902.25 + curRfCh * 0.5;
                                break;
                            case ConstCode.REGION_CODE_EUR: // Europe
                                curRfCh = 865.1 + curRfCh * 0.2;
                                break;
                            case ConstCode.REGION_CODE_KOREA:  // Korea
                                curRfCh = 917.1 + curRfCh * 0.2;
                                break;
                            default :
                                break;
                        }
                        MessageBox.Show("当前RF频道 " + curRfCh + " MHz", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (packetRx[2] == ConstCode.CMD_GET_POWER)
                    {
                        string curPower = packetRx[5] + packetRx[6];
                        MessageBox.Show("当前增益 " + (Convert.ToInt16(curPower, 16) / 100.0) + "dBm", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (packetRx[2] == ConstCode.CMD_READ_MODEM_PARA)
                    {
                        int mixerGain = mixerGainTable[Convert.ToInt32(packetRx[5], 16)];
                        int IFAmpGain = IFAmpGainTable[Convert.ToInt32(packetRx[6], 16)];
                        string signalTh = packetRx[7] + packetRx[8];
                        MessageBox.Show("Mixer Gain is " + mixerGain + "dB, IF AMP Gain is " + IFAmpGain + "dB, Decode Threshold is 0x" + signalTh + ".", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (packetRx[2] == ConstCode.CMD_SCAN_JAMMER)
                    {
                        int startChannel = Convert.ToInt16(packetRx[5], 16);
                        int stopChannel = Convert.ToInt16(packetRx[6], 16);
                        
                        hBarChartJammer.Items.Maximum = 40;
                        hBarChartJammer.Items.Minimum = 0;

                        hBarChartJammer.Items.Clear();

                        int[] allJammer = new int[(stopChannel - startChannel + 1)];
                        int maxJammer = -100;
                        int minJammer = 20;
                        for (int i = 0; i < (stopChannel - startChannel + 1); i++)
                        {
                            int jammer = Convert.ToInt16(packetRx[7 + i], 16);
                            if (jammer > 127)
                            {
                                jammer = -((-jammer) & 0xFF);
                            }
                            allJammer[i] = jammer;
                            if (jammer >= maxJammer)
                            {
                                maxJammer = jammer;
                            }
                            if (jammer <= minJammer)
                            {
                                minJammer = jammer;
                            }
                        }
                        int offset = -minJammer + 3;
                        for (int i = 0; i < (stopChannel - startChannel + 1); i++)
                        {
                            allJammer[i] = allJammer[i] + offset;
                            hBarChartJammer.Items.Add(new HBarItem((double)(allJammer[i]),(double)offset, (i + startChannel).ToString(), Color.FromArgb(255, 190, 200, 255)));
                        }
                        hBarChartJammer.RedrawChart();
                    }
                    else if (packetRx[2] == ConstCode.CMD_SCAN_RSSI)
                    {
                        int startChannel = Convert.ToInt16(packetRx[5], 16);
                        int stopChannel = Convert.ToInt16(packetRx[6], 16);

                        hBarChartRssi.Items.Maximum = 73;
                        hBarChartRssi.Items.Minimum = 0;

                        hBarChartRssi.Items.Clear();

                        int[] allRssi = new int[(stopChannel - startChannel + 1)];
                        int maxRssi = -100;
                        int minRssi = 20;
                        for (int i = 0; i < (stopChannel - startChannel + 1); i++)
                        {
                            int rssi = Convert.ToInt16(packetRx[7 + i], 16);
                            if (rssi > 127)
                            {
                                rssi = -((-rssi) & 0xFF);
                            }
                            allRssi[i] = rssi;
                            if (rssi >= maxRssi)
                            {
                                maxRssi = rssi;
                            }
                            if (rssi <= minRssi)
                            {
                                minRssi = rssi;
                            }
                        }
                        int offset = -minRssi + 3;
                        for (int i = 0; i < (stopChannel - startChannel + 1); i++)
                        {
                            allRssi[i] = allRssi[i] + offset;
                            hBarChartRssi.Items.Add(new HBarItem((double)(allRssi[i]), (double)offset, (i + startChannel).ToString(), Color.FromArgb(255, 190, 200, 255)));
                        }
                        hBarChartRssi.RedrawChart();
                    }
                    else if (packetRx[2] == ConstCode.CMD_GET_MODULE_INFO)
                    {
                        if (checkingReaderAvailable)
                        {
                            hardwareVersion = String.Empty;
                            if (packetRx[5] == ConstCode.MODULE_HARDWARE_VERSION_FIELD)
                            {
                                try
                                {
                                    for (int i = 0; i < Convert.ToInt32(packetRx[4], 16) - 1; i++)
                                    {
                                        hardwareVersion += (char)Convert.ToInt32(packetRx[6 + i], 16);
                                    }
                                    txtHardwareVersion.Text = hardwareVersion;
                                    adjustUIcomponents(hardwareVersion);
                                }
                                catch (System.Exception ex)
                                {
                                    hardwareVersion = packetRx[6].Substring(1, 1) + "." + packetRx[7];
                                    txtHardwareVersion.Text = hardwareVersion;
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }
                    }
                    else if (packetRx[2] == "1A")
                    {
                        if (packetRx[5] == "02")
                        {
                            MessageBox.Show("IO" + packetRx[6].Substring(1) + " is " + (packetRx[7] == "00" ? "Low" : "High"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
#endregion
            }));

#if TRACE
            //Console.WriteLine("a packet received!");
#endif
        }

        private int getSuccessTagEpc(string[] packetRx)
        {
            txtOperateEpc.Text = "";
            if (packetRx.Length < 9)
            {
                return 0;
            }
            int pcEpcLen = Convert.ToInt32(packetRx[5], 16);
            for (int i = 0; i < pcEpcLen; i++)
            {
                txtOperateEpc.Text += packetRx[i + 6] + " ";
            }
            return pcEpcLen;
        }

        private void setStatus(string msg, Color color)
        {
            rtbxStatus.Text = msg;
            rtbxStatus.ForeColor = color;
        }

        private void adjustUIcomponents(string hardwareVersion)
        {
            if (hardwareVersion.Length >= 10 && "M100 26dBm".Equals(hardwareVersion.Substring(0, 10)))
            {
                this.cbxPaPower.Items.Clear();
                for (int i = 26; i >= -9; i--) {
                    this.cbxPaPower.Items.Add(i.ToString() + "dBm");
                }
                this.cbxPaPower.SelectedIndex = 0;
                this.cbxMixerGain.SelectedIndex = 2;
                this.cbxIFAmpGain.SelectedIndex = 6;
                this.tbxSignalThreshold.Text = "00A0";
                this.tbxAntennaGain.Text = "3";
                this.tbxCoupling.Text = "-20";
                this.gbxIoControl.Visible = false;
            }
            else if (hardwareVersion.Length >= 10 && "M100 20dBm".Equals(hardwareVersion.Substring(0, 10)))
            {
                this.cbxPaPower.Items.Clear();
                this.cbxPaPower.Items.AddRange(new object[] {
                                    "26dBm",
                                    "20dBm",
                                    "18.5dBm",
                                    "17dBm",
                                    "15.5dBm",
                                    "14dBm",
                                    "12.5dBm"});
                this.cbxPaPower.SelectedIndex = 0;
                this.cbxMixerGain.SelectedIndex = 3;
                this.cbxIFAmpGain.SelectedIndex = 6;
                this.tbxSignalThreshold.Text = "01B0";
                this.tbxAntennaGain.Text = "1";
                this.tbxCoupling.Text = "-27";
                this.gbxIoControl.Visible = false;
            }
            else if (hardwareVersion.Length >= 10 && "QM100 30dBm".Equals(hardwareVersion.Substring(0, 11)))
            {
                this.cbxPaPower.Items.Clear();
                for (int i = 30; i >= 19; i--)
                {
                    this.cbxPaPower.Items.Add(i.ToString() + "dBm");
                }
                this.cbxPaPower.SelectedIndex = 0;
                this.cbxMixerGain.SelectedIndex = 4;
                this.cbxIFAmpGain.SelectedIndex = 6;
                this.tbxSignalThreshold.Text = "0120";
                this.tbxAntennaGain.Text = "3";
                this.tbxCoupling.Text = "-10";
                this.cbxQBasic.SelectedIndexChanged -= new System.EventHandler(this.cbx_q_basic_SelectedIndexChanged);
                this.cbxQBasic.SelectedIndex = 5;
                this.cbxQBasic.SelectedIndexChanged += new System.EventHandler(this.cbx_q_basic_SelectedIndexChanged);
                this.cbxQAdv.SelectedIndex = 5;
                this.gbxIoControl.Visible = true;
            }
            else if (hardwareVersion.Length >= 5 && "QM100".Equals(hardwareVersion.Substring(0, 5)))
            {
                this.cbxPaPower.Items.Clear();
                this.cbxPaPower.Items.AddRange(new object[] {
                                     "30dBm",
                                     "28.5dBm",
                                     "27dBm",
                                     "25.5dBm",
                                     "24dBm",
                                     "22.5dBm",
                                     "21dBm",
                                     "19.5dBm"});
                this.cbxPaPower.SelectedIndex = 2;
                this.cbxMixerGain.SelectedIndex = 4;
                this.cbxIFAmpGain.SelectedIndex = 6;
                this.tbxSignalThreshold.Text = "0280";
                this.tbxAntennaGain.Text = "4";
                this.tbxCoupling.Text = "-10";
                this.gbxIoControl.Visible = true;
            }
        }
        private void setTip()
        {
            toolTip1.SetToolTip(this.label1, "Available COM Port");
            toolTip1.SetToolTip(this.txtReceive, "Double Click To Select ALL");
        }

        #region Serial Port connection and download Firmware
        /// <summary>
        /// 打开指定端口
        /// </summary>
   
        public void checkReaderAvailable()
        {
            if (Sp.GetInstance().IsOpen())
            {
                hardwareVersion = "";
                checkingReaderAvailable = true;
                readerConnected = false;
                sock.Send(HexStrTobyte(Commands.BuildGetModuleInfoFrame(ConstCode.MODULE_HARDWARE_VERSION_FIELD)));
                
                timerCheckReader.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            }
        }

        #endregion
        private void cbx_dr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbxDR.SelectedIndex == 1)
            {
                MessageBox.Show("Does Not Support DR = 64/3 In this Version", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.cbxDR.SelectedIndex = 0;
            }
        }

        private void cbx_m_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbxM.SelectedIndex == 1 || this.cbxM.SelectedIndex == 2 || this.cbxM.SelectedIndex == 3)
            {
                MessageBox.Show("Does Not Support M = 2/4/8 In this Version", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.cbxM.SelectedIndex = 0;
            }
        }

        private void cbx_trext_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbxTRext.SelectedIndex == 0)
            {
                MessageBox.Show("Does Not Support No Pilot Tone In this Version", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.cbxTRext.SelectedIndex = 1;
            }
        }

        #region send data
        private void btn_Send_Click(object sender, EventArgs e)
        {
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {

                bAutoSend = !bAutoSend;
                if (bAutoSend)
                {
                    timerAutoSend.Interval = Convert.ToInt32(txtSendDelay.Text);
                    timerAutoSend.Enabled = true;
                    txtSend.Text = Commands.BuildReadSingleFrame();
                    btnContinue.Text = "停止";
                    tmrCheckEpc.Enabled = true;
                }
                else
                {
                    timerAutoSend.Interval = Convert.ToInt32(txtSendDelay.Text);
                    timerAutoSend.Enabled = false;
                    btnContinue.Text = "继续";
                    tmrCheckEpc.Enabled = false;
                }


        }
        private void timerAutoSend_Tick(object sender, EventArgs e)
        {
            LoopNum_cnt = LoopNum_cnt + 1;
            try
            {
                if (sock.Send(HexStrTobyte(txtSend.Text)) == 0)
                {
                    bAutoSend = false;
                    timerAutoSend.Enabled = false;
                    btnContinue.Text = "Continue";
                }
            }
            catch (System.Exception ex)
            {
                bAutoSend = false;
                timerAutoSend.Enabled = false;
                btnContinue.Text = "Continue";
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //clear send text region
        private void btnClearS_Click(object sender, EventArgs e)
        {
            txtSend.Text = "";
        }

        private void btnSetFreq_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtSend.Text = Commands.BuildSetRfChannelFrame(cbxChannel.SelectedIndex.ToString("X2"));
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled=true;
        }

        private void btn_invt_Click(object sender, EventArgs e)
        {
            LoopNum_cnt = LoopNum_cnt + 1;
            txtSend.Text = Commands.BuildReadSingleFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void cbx_q_basic_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (change_q_1st == false)
            {
                if (bAutoSend == true)
                {
                    if (change_q_message == true)
                    {
                        MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        change_q_message = false;
                        this.cbxQBasic.SelectedIndex = this.cbxQAdv.SelectedIndex;
                    }
                    else
                    {
                        change_q_message = true;
                    }
                }
                else
                {
                    int intDR = this.cbxDR.SelectedIndex;
                    int intM = this.cbxM.SelectedIndex;
                    int intTRext = this.cbxTRext.SelectedIndex;
                    int intSel = this.cbxSel.SelectedIndex;
                    int intSession = this.cbxSession.SelectedIndex;

                    int intTarget = this.cbxTarget.SelectedIndex;
                    int intQ = this.cbxQBasic.SelectedIndex;

                    txtSend.Text = Commands.BuildSetQueryFrame(intDR, intM, intTRext, intSel, intSession, intTarget, intQ);
                    sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
                    timerrecive.Enabled = true;
                    this.cbxQAdv.SelectedIndex = intQ;
                }
            }
        }

        private void btnSetCW_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (btnSetCW.Text == "CW ON")
            {
                txtSend.Text = Commands.BuildSetCWFrame(ConstCode.SET_ON);
            }
            else
            {
                txtSend.Text = Commands.BuildSetCWFrame(ConstCode.SET_OFF);
            }
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;

            if (btnSetCW.Text == "CW ON")
            {
                btnSetCW.Text = "CW OFF";
            }
            else
            {
                btnSetCW.Text = "CW ON";
            }
        }

        #endregion

        private void btn_clear_rx_Click(object sender, EventArgs e)
        {
            txtReceive.Text = "";
        }
        //clear EPC Table
        private void btn_clear_basictable_Click(object sender, EventArgs e)
        {
            basic_table.Clear();
            advanced_table.Clear();
            LoopNum_cnt = 0;
            FailEPCNum = 0;
            SucessEPCNum = 0;
            db_LoopNum_cnt = 0;
            for (int i = 0; i <= initDataTableLen - 1; i++)
            {
                basic_table.Rows.Add(new object[] { null });
            }
            basic_table.AcceptChanges();
            for (int i = 0; i <= initDataTableLen - 1; i++)
            {
                advanced_table.Rows.Add(new object[] { null });
            }
            advanced_table.AcceptChanges();
            rowIndex = 0;
        }

        #region DataGridView
        private void GetEPC(string pc, string epc, string crc, string rssi , string per)
        {
            this.dgv_epc2.ClearSelection();
            bool isFoundEpc = false;
            string newEpcItemCnt;
            int indexEpc = 0;

            int EpcItemCnt;
            if (rowIndex <= initDataTableLen)
            {
                EpcItemCnt = rowIndex;
            }
            else
            {
                EpcItemCnt = basic_table.Rows.Count;
                EpcItemCnt = advanced_table.Rows.Count;
            }

            for (int j = 0; j < EpcItemCnt; j++)
            {
                if (basic_table.Rows[j][2].ToString() == epc && basic_table.Rows[j][1].ToString() == pc)
                {
                    indexEpc = j;
                    isFoundEpc = true;
                    break;
                }
            }

            if (EpcItemCnt < initDataTableLen) //basic_table.Rows[EpcItemCnt][0].ToString() == ""
            {
                if (!isFoundEpc || EpcItemCnt == 0)
                {
                    if (EpcItemCnt + 1 < 10)
                    {
                        newEpcItemCnt = "0" + Convert.ToString(EpcItemCnt + 1);
                    }
                    else
                    {
                        newEpcItemCnt = Convert.ToString(EpcItemCnt + 1);
                    }
                    basic_table.Rows[EpcItemCnt][0] = newEpcItemCnt; // EpcItemCnt + 1;
                    basic_table.Rows[EpcItemCnt][1] = pc;
                    basic_table.Rows[EpcItemCnt][2] = epc;
                    basic_table.Rows[EpcItemCnt][3] = crc;
                    basic_table.Rows[EpcItemCnt][4] = rssi;
                    basic_table.Rows[EpcItemCnt][5] = 1;
                    basic_table.Rows[EpcItemCnt][6] = "0.000";
                    basic_table.Rows[EpcItemCnt][7] = System.DateTime.Now.ToString(timeFormat);

                    advanced_table.Rows[EpcItemCnt][0] = newEpcItemCnt; // EpcItemCnt + 1;
                    advanced_table.Rows[EpcItemCnt][1] = pc;
                    advanced_table.Rows[EpcItemCnt][2] = epc;
                    advanced_table.Rows[EpcItemCnt][3] = crc;
                    advanced_table.Rows[EpcItemCnt][4] = 1;

                    rowIndex++;
                }
                else
                {
                    if (indexEpc + 1 < 10)
                    {
                        newEpcItemCnt = "0" + Convert.ToString(indexEpc + 1);
                    }
                    else
                    {
                        newEpcItemCnt = Convert.ToString(indexEpc + 1);
                    }
                    basic_table.Rows[indexEpc][0] = newEpcItemCnt; // indexEpc + 1;
                    basic_table.Rows[indexEpc][4] = rssi;
                    basic_table.Rows[indexEpc][5] = Convert.ToInt32(basic_table.Rows[indexEpc][5].ToString()) + 1;
                    basic_table.Rows[indexEpc][6] = per;
                    basic_table.Rows[indexEpc][7] = System.DateTime.Now.ToString(timeFormat);

                    advanced_table.Rows[indexEpc][0] = newEpcItemCnt; // indexEpc + 1;
                    advanced_table.Rows[indexEpc][4] = Convert.ToInt32(advanced_table.Rows[indexEpc][4].ToString()) + 1;
                }
            }
            else
            {
                if (!isFoundEpc || EpcItemCnt == 0)
                {
                    if (EpcItemCnt + 1 < 10)
                    {
                        newEpcItemCnt = "0" + Convert.ToString(EpcItemCnt + 1);
                    }
                    else
                    {
                        newEpcItemCnt = Convert.ToString(EpcItemCnt + 1);
                    }
                    basic_table.Rows.Add(new object[] { newEpcItemCnt, pc, epc, crc, rssi, "1", "0.000", DateTime.Now.ToString(timeFormat) });
                    advanced_table.Rows.Add(new object[] { newEpcItemCnt, pc, epc, crc, "1" });
                    rowIndex++;
                }
                else
                {
                    if (indexEpc + 1 < 10)
                    {
                        newEpcItemCnt = "0" + Convert.ToString(indexEpc + 1);
                    }
                    else
                    {
                        newEpcItemCnt = Convert.ToString(indexEpc + 1);
                    }
                    basic_table.Rows[indexEpc][0] = newEpcItemCnt; // indexEpc + 1;
                    basic_table.Rows[indexEpc][4] = rssi;
                    basic_table.Rows[indexEpc][5] = Convert.ToInt32(basic_table.Rows[indexEpc][5].ToString()) + 1;
                    basic_table.Rows[indexEpc][6] = per;
                    basic_table.Rows[indexEpc][7] = System.DateTime.Now.ToString(timeFormat);

                    advanced_table.Rows[indexEpc][0] = newEpcItemCnt; // indexEpc + 1;
                    advanced_table.Rows[indexEpc][4] = Convert.ToInt32(advanced_table.Rows[indexEpc][4].ToString()) + 1;
                }
            }
        }
        private void dgvEpcBasic_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            this.dgvEpcBasic.ClearSelection();
            //double totalCnt = 0;
            //if (e.ListChangedType == ListChangedType.ItemChanged || e.ListChangedType == ListChangedType.ItemMoved)
            {
                //for (int i = 0; i < this.dgvEpcBasic.Rows.Count; i++)
                //{
                //    string cnt = this.dgvEpcBasic.Rows[i].Cells[5].Value.ToString();
                //    if (null != cnt && !"".Equals(cnt))
                //    {
                //        totalCnt += Convert.ToInt32(cnt);
                //    }
                //}
                //for (int i = 0; i < this.dgvEpcBasic.Rows.Count; i++)
                //{
                //    string cnt = this.dgvEpcBasic.Rows[i].Cells[5].Value.ToString();
                //    if (null != cnt && !"".Equals(cnt))
                //    {
                //        int sigleCnt = Convert.ToInt32(cnt);
                //        int r = 0xFF & (int)(sigleCnt / totalCnt * 255);
                //        this.dgvEpcBasic.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(0xff,255 - r,255 - r);
                //    }
                //}
                pbx_Inv_Indicator.Visible = true;
            }
        }
        private void dgv_epc2_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            //if (e.ListChangedType == ListChangedType.ItemChanged || e.ListChangedType == ListChangedType.ItemAdded)
            {
                for (int i = 0; i < this.dgv_epc2.Rows.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        this.dgv_epc2.Rows[i].DefaultCellStyle.BackColor = Color.AliceBlue;
                    }
                }
            }
        }
        private DataTable BasicGetEPCHead()
        {
            basic_table.TableName = "EPC";
            basic_table.Columns.Add("No.", typeof(string)); //0
            basic_table.Columns.Add("PC", typeof(string)); //1
            basic_table.Columns.Add("EPC", typeof(string)); //2
            basic_table.Columns.Add("CRC", typeof(string)); //3
            basic_table.Columns.Add("RSSI(dBm)", typeof(string)); //4
            basic_table.Columns.Add("CNT", typeof(string)); //5
            basic_table.Columns.Add("PER(%)", typeof(string)); //6
            basic_table.Columns.Add("Time", typeof(string)); //7

            for (int i = 0; i <= initDataTableLen - 1; i++)
            {
                basic_table.Rows.Add(new object[] { null });
            }
            basic_table.AcceptChanges();

            return basic_table;
        }

        private DataTable AdvancedGetEPCHead()
        {
            advanced_table.TableName = "EPC";
            advanced_table.Columns.Add("No.", typeof(string)); //0
            advanced_table.Columns.Add("PC", typeof(string)); //1
            advanced_table.Columns.Add("EPC", typeof(string)); //2
            advanced_table.Columns.Add("CRC", typeof(string)); //3
            advanced_table.Columns.Add("CNT", typeof(string)); //4

            for (int i = 0; i <= initDataTableLen - 1; i++)
            {
                advanced_table.Rows.Add(new object[] { null });
            }
            advanced_table.AcceptChanges();

            return advanced_table;
        }
        private void Basic_DGV_ColumnsWidth(DataGridView dataGridView1)
        {
            //dataGridView1.Columns[6].SortMode = DataGridViewColumnSortMode.Programmatic;
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.ColumnHeadersHeight = 40;
            //dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[0].Width = 40;
            //dataGridView1.Columns[0].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[0].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[1].Width = 60;
            //dataGridView1.Columns[1].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[1].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[2].Width = 290;
            //dataGridView1.Columns[2].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[2].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[3].Width = 60;
            //dataGridView1.Columns[3].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[3].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[4].Width = 75;
            //dataGridView1.Columns[4].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[4].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //dataGridView1.Columns[5].Width = 70;
            ////dataGridView1.Columns[5].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            //dataGridView1.Columns[5].Resizable = DataGridViewTriState.False;
            //dataGridView1.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[5].Width = 70;
            //dataGridView1.Columns[5].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[5].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[6].Width = 72;
            //dataGridView1.Columns[6].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[6].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[7].Visible = false;
            //dataGridView1.Columns[7].Width = 72;
            ////dataGridView1.Columns[7].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            //dataGridView1.Columns[7].Resizable = DataGridViewTriState.False;
            //dataGridView1.Columns[7].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void Advanced_DGV_ColumnsWidth(DataGridView dataGridView1)
        {
            //dataGridView1.Columns[6].SortMode = DataGridViewColumnSortMode.Programmatic;
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.ColumnHeadersHeight = 40;
            //dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[0].Width = 40;
            //dataGridView1.Columns[0].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[0].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[1].Width = 60;
            //dataGridView1.Columns[1].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[1].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[2].Width = 240;
            //dataGridView1.Columns[2].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[2].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[3].Width = 60;
            //dataGridView1.Columns[3].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[3].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[4].Width = 52;
            //dataGridView1.Columns[6].DefaultCellStyle.Font = new Font("Lucida Console", 10);
            dataGridView1.Columns[4].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        #endregion

        #region others
        private void btn_clear_cnt_Click(object sender, EventArgs e)
        {
            txtCOMRxCnt.Text = "0";
            txtCOMTxCnt.Text = "0";
            txtCOMRxCnt_adv.Text = "0";
            txtCOMTxCnt_adv.Text = "0";
        }

        private void btn_clear_cnt_adv_Click(object sender, EventArgs e)
        {
            txtCOMRxCnt.Text = "0";
            txtCOMTxCnt.Text = "0";
            txtCOMRxCnt_adv.Text = "0";
            txtCOMTxCnt_adv.Text = "0";
        }

        private string[] String16toString2(string S)
        {
            string[] S_array = new string[8];
            int intS = Convert.ToInt32(S, 16);
            for (int i = 7; i >= 0; i--)
            {
                S_array[i] = "0";
                if (intS >= System.Math.Pow(2, i)) S_array[i] = "1";
                intS = intS - Convert.ToInt32(S_array[i]) * Convert.ToInt32(System.Math.Pow(2, i));
            }
            return S_array;
        }

        private string StringToString(string S)
        {
            string Str = null;

            int S_num = Convert.ToInt32(S, 16);
            if (S_num < 16)
            {
                Str = "0" + S;
            }
            else
            {
                Str = S;
            }
            return Str;
        }

        private string[] StringArrayToStringArray(string[] S)
        {
            string[] Str = new string[S.Length];
            for (int i = 0; i < S.Length; i++)
            {
                int S_num = Convert.ToInt32(S[i], 16);
                if (S_num < 16)
                {
                    Str[i] = "0" + S[i];
                }
                else
                {
                    Str[i] = S[i];
                }
            }
            return Str;
        }

        private byte[] StringsToBytes(string[] B)
        {
            byte[] BToInt32 = new byte[B.Length];
            for (int i = 0; i < B.Length; i++)
            {
                BToInt32[i] = StringToByte(B[i]);
            }
            return BToInt32;
        }

        private byte StringToByte(string Str)
        {
            for (int i = Str.Length; i < 2; i++)
            {
                Str += "0";
            }
            return (byte)(Convert.ToInt32(Str, 16));
        }

        private string AutoAddSpace(string Str)
        {
            String StrDone = string.Empty;
            int i;
            for (i = 0; i < (Str.Length - 2); i = i + 2)
            {
                StrDone = StrDone + Str[i] + Str[i + 1] + " ";
            }
            if (Str.Length % 2 == 0 && Str.Length != 0)
            {
                if (Str.Length == i + 1)
                {
                    StrDone = StrDone + Str[i];
                }
                else
                {
                    StrDone = StrDone + Str[i] + Str[i + 1];
                }
            }
            else
            {
                StrDone = StrDone + StringToString(Str[i].ToString());
            }
            return StrDone;
        }

        private void txtReceive_DoubleClick(object sender, EventArgs e)
        {
            txtReceive.SelectAll();
        }

        private void txtSelMask_DoubleClick(object sender, EventArgs e)
        {
            txtSelMask.SelectAll();
        }

        private void txtSend_DoubleClick(object sender, EventArgs e)
        {
            txtSend.SelectAll();
        }


        private void txtInvtReadData_DoubleClick(object sender, EventArgs e)
        {
            txtInvtRWData.SelectAll();
        }

        private void txtGetSelMask_DoubleClick(object sender, EventArgs e)
        {
            txtGetSelMask.SelectAll();
        }
        #endregion

        #region Advanced Tab received data display
        private void btn_clear_epc2_Click(object sender, EventArgs e)
        {
            txtReceive.Text = "";
            basic_table.Clear();
            advanced_table.Clear();
            LoopNum_cnt = 0;
            FailEPCNum = 0;
            SucessEPCNum = 0;
            db_LoopNum_cnt = 0;
            for (int i = 0; i <= initDataTableLen - 1; i++)
            {
                basic_table.Rows.Add(new object[] { null });
            }
            basic_table.AcceptChanges();
            for (int i = 0; i <= initDataTableLen - 1; i++)
            {
                advanced_table.Rows.Add(new object[] { null });
            }
            advanced_table.AcceptChanges();
            rowIndex = 0;

        }

        public void dataGrid_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            int rowIndex = dgv_epc2.CurrentRow.Index;
            if (dgv_epc2.Rows[rowIndex].Cells[2].Value.ToString() != null)
            {
                txtSelMask.Text = dgv_epc2.Rows[rowIndex].Cells[2].Value.ToString();
            }
            txtSelLength.Text = (txtSelMask.Text.Replace(" ", "").Length * 4).ToString("X2");
        }

        private void btn_invt2_Click(object sender, EventArgs e)
        {
            LoopNum_cnt = LoopNum_cnt + 1;
            txtSend.Text = Commands.BuildReadSingleFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }
        #endregion

        #region Advanced Tab send data region

        private void btn_setquery_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int intDR = this.cbxDR.SelectedIndex;
            int intM = this.cbxM.SelectedIndex;
            int intTRext = this.cbxTRext.SelectedIndex;
            int intSel = this.cbxSel.SelectedIndex;
            int intSession = this.cbxSession.SelectedIndex;

            int intTarget = this.cbxTarget.SelectedIndex;
            int intQ = this.cbxQAdv.SelectedIndex;

            txtSend.Text = Commands.BuildSetQueryFrame(intDR, intM, intTRext, intSel, intSession, intTarget, intQ);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
        }

        private void btn_getquery_Click(object sender, EventArgs e)
        {
            txtSend.Text = Commands.BuildGetQueryFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
        }
        #endregion

        private void btn_invt_multi_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int loopCnt = Convert.ToInt32(txtRDMultiNum.Text);
            txtSend.Text = Commands.BuildReadMultiFrame(loopCnt);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
           //tmrCheckEpc.Enabled = true;
        }

        private void btn_stop_rd_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtSend.Text = Commands.BuildStopReadFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
           // tmrCheckEpc.Enabled = false;
        }

        private void select()
        {
            if (Sp.GetInstance().IsOpen() == false)
            {
                return;
            }
            int intSelTarget = this.cbxSelTarget.SelectedIndex;
            int intAction = this.cbxAction.SelectedIndex;
            int intSelMemBank = this.cbxSelMemBank.SelectedIndex;

            int intSelPointer = Convert.ToInt32((txtSelPrt3.Text + txtSelPrt2.Text + txtSelPrt1.Text + txtSelPrt0.Text), 16);
            int intMaskLen = Convert.ToInt32(txtSelLength.Text, 16);
            int intSelDataMSB = intSelMemBank + intAction * 4 + intSelTarget * 32;
            int intTruncate = 0;
            string cc = Commands.BuildSetSelectFrame(intSelTarget, intAction, intSelMemBank, intSelPointer, intMaskLen, intTruncate, txtSelMask.Text);

            sock.Send(HexStrTobyte(cc));
            Thread.Sleep(50);
        }
        private void select2()
        {
            //this.cbxSelTarget.Text = "00";
            this.cbxAction.Text = "000";
            this.cbxSelMemBank.Text = "EPC";
            txtSelPrt3.Text = "00";
            txtSelPrt2.Text = "00";
            txtSelPrt1.Text = "00";
            txtSelPrt0.Text = "20";
            txtSelLength.Text = "60";
            txtSelMask.Text = epc;
            int intSelTarget = this.cbxSelTarget.SelectedIndex;
            int intAction = this.cbxAction.SelectedIndex;
            int intSelMemBank = this.cbxSelMemBank.SelectedIndex;

            int intSelPointer = Convert.ToInt32((txtSelPrt3.Text + txtSelPrt2.Text + txtSelPrt1.Text + txtSelPrt0.Text), 16);
            int intMaskLen = Convert.ToInt32(txtSelLength.Text, 16);
            int intSelDataMSB = intSelMemBank + intAction * 4 + intSelTarget * 32;
            int intTruncate = 1;
            sock.Send(HexStrTobyte(Commands.BuildSetSelectFrame(intSelTarget, intAction, intSelMemBank, intSelPointer, intMaskLen, intTruncate, txtSelMask.Text)));
            timerrecive.Enabled = true;
            Thread.Sleep(50);
        }
        private void select3(string epc)
        {
            //this.cbxSelTarget.Text = "00";
            this.cbxAction.Text = "000";
            this.cbxSelMemBank.Text = "TID";
            txtSelPrt3.Text = "00";
            txtSelPrt2.Text = "00";
            txtSelPrt1.Text = "00";
            txtSelPrt0.Text = "20";
            txtSelLength.Text = "60";
            txtSelMask.Text = epc;
            int intSelTarget = this.cbxSelTarget.SelectedIndex;
            int intAction = this.cbxAction.SelectedIndex;
            int intSelMemBank = this.cbxSelMemBank.SelectedIndex;

            int intSelPointer = Convert.ToInt32((txtSelPrt3.Text + txtSelPrt2.Text + txtSelPrt1.Text + txtSelPrt0.Text), 16);
            int intMaskLen = Convert.ToInt32(txtSelLength.Text, 16);
            int intSelDataMSB = intSelMemBank + intAction * 4 + intSelTarget * 32;
            int intTruncate = 1;
            string x = Commands.BuildSetSelectFrame(intSelTarget, intAction, intSelMemBank, intSelPointer, intMaskLen, intTruncate, txtSelMask.Text);
            sock.Send(HexStrTobyte(x));
            logfile.writelog("Send:"+x);
            timerrecive.Enabled = true;
            Thread.Sleep(50);
   
        }
        private void btn_invtread_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = txtRwAccPassWord.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码设置为2个字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int wordPtr = Convert.ToInt32((txtWordPtr1.Text.Replace(" ","") + txtWordPtr0.Text.Replace(" ","")),16);
            int wordCnt =Convert.ToInt32((txtWordCnt1.Text.Replace(" ","") + txtWordCnt0.Text.Replace(" ","")),16);

            int intMemBank = cbxMemBank.SelectedIndex;

            select();

            txtSend.Text = Commands.BuildReadDataFrame(strAccessPasswd, intMemBank, wordPtr, wordCnt);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;

        }
        private void btn_invtread_Click_tid(string epc)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = txtRwAccPassWord.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码设置为2个字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int wordPtr = Convert.ToInt32((txtWordPtr1.Text.Replace(" ", "") + txtWordPtr0.Text.Replace(" ", "")), 16);
            int wordCnt = Convert.ToInt32((txtWordCnt1.Text.Replace(" ", "") + txtWordCnt0.Text.Replace(" ", "")), 16);
            cbxMemBank.Text = "TID";
            int intMemBank = cbxMemBank.SelectedIndex;

            select3(epc);

            txtSend.Text = Commands.BuildReadDataFrame(strAccessPasswd, intMemBank, wordPtr, wordCnt);
            sock.Send(HexStrTobyte(txtSend.Text)); logfile.writelog("Send: " + txtSend.Text);
            timerrecive.Enabled = true;

        }
        private String int2HexString(int a)
        {
            byte byte_a = Convert.ToByte(a);
            string str = byte_a.ToString("x").ToUpper();
            str = StringToString(str);
            return str;
        }
        private void btnSetSelect_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int intSelTarget = this.cbxSelTarget.SelectedIndex;
            int intAction = this.cbxAction.SelectedIndex;
            int intSelMemBank = this.cbxSelMemBank.SelectedIndex;

            int intSelPointer = Convert.ToInt32((txtSelPrt3.Text + txtSelPrt2.Text + txtSelPrt1.Text + txtSelPrt0.Text),16);
            int intMaskLen = Convert.ToInt32(txtSelLength.Text, 16);
            int intSelDataMSB = intSelMemBank + intAction * 4 + intSelTarget * 32;
            int intTruncate = 0;
            if (this.ckxTruncated.Checked == true)
            {
                intTruncate = 0x80;
            }

            txtSend.Text = Commands.BuildSetSelectFrame(intSelTarget, intAction, intSelMemBank, intSelPointer, intMaskLen, intTruncate, txtSelMask.Text);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
            //inv_mode.Checked = true;
        }

        private void btnGetSelect_Click(object sender, EventArgs e)
        {
            txtSend.Text = Commands.BuildGetSelectFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnInvtWrtie_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = txtRwAccPassWord.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码为二个字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string strDate4Write = txtInvtRWData.Text.Replace(" ", "");

            int intMemBank = cbxMemBank.SelectedIndex;
            int wordPtr = Convert.ToInt32((txtWordPtr1.Text.Replace(" ","") + txtWordPtr0.Text.Replace(" ","")),16);
            int wordCnt = strDate4Write.Length / 4; // in word!

            if (strDate4Write.Length % 4 != 0)
            {
                MessageBox.Show("写入的数据应该为整型的倍数", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (strDate4Write.Length > 16 * 4)
            {
                MessageBox.Show("Write Data Length Limit is 16 Words", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            select();

            txtSend.Text = Commands.BuildWriteDataFrame(strAccessPasswd, intMemBank
                , wordPtr, wordCnt, strDate4Write);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;


        }

        private void buttonLock_Click(object sender, EventArgs e)
        {
            if (textBoxLockAccessPwd.Text.Length == 0) return;
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            select();

            int lockPayload = buildLockPayload();
            txtSend.Text = Commands.BuildLockFrame(textBoxLockAccessPwd.Text, lockPayload);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private int buildLockPayload()
        {
            int ld = 0;
            Commands.lock_payload_type payload;
            if (checkBoxKillPwd.Checked)
            {
                payload = Commands.genLockPayload((byte)cbxLockKillAction.SelectedIndex, 0x00);
                ld |= (payload.byte0 << 16) | (payload.byte1 << 8) | (payload.byte2);
            }
            if (checkBoxAccessPwd.Checked)
            {
                payload = Commands.genLockPayload((byte)cbxLockAccessAction.SelectedIndex, 0x01);
                ld |= (payload.byte0 << 16) | (payload.byte1 << 8) | (payload.byte2);
            }
            if (checkBoxEPC.Checked)
            {
                payload = Commands.genLockPayload((byte)cbxLockEPCAction.SelectedIndex, 0x02);
                ld |= (payload.byte0 << 16) | (payload.byte1 << 8) | (payload.byte2);
            }
            if (checkBoxTID.Checked)
            {
                payload = Commands.genLockPayload((byte)cbxLockTIDAction.SelectedIndex, 0x03);
                ld |= (payload.byte0 << 16) | (payload.byte1 << 8) | (payload.byte2);
            }
            if (checkBoxUser.Checked)
            {
                payload = Commands.genLockPayload((byte)cbxLockUserAction.SelectedIndex, 0x04);
                ld |= (payload.byte0 << 16) | (payload.byte1 << 8) | (payload.byte2);
            }
            return ld;
        }

        private void buttonKill_Click(object sender, EventArgs e)
        {
            if (textBoxKillPwd.Text.Length == 0) return;

            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strKillPasswd = textBoxKillPwd.Text.Replace(" ", "");
            if (strKillPasswd.Length != 8)
            {
                MessageBox.Show("访问密码为二个字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int killRfu = 0;
            string strKillRfu = textBoxKillRFU.Text.Replace(" ", "");
            if (strKillRfu.Length == 0)
            {
                killRfu = 0;
            }
            else if (strKillRfu.Length != 3)
            {
                MessageBox.Show("清除RFU命令应该为 3 bits!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                try
                {
                    killRfu = Convert.ToInt32(strKillRfu, 2);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Convert Kill RFU fail." + ex.Message);
                    MessageBox.Show("清除 RFU 命令应该为 3 bits!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            select();

            txtSend.Text = Commands.BuildKillFrame(strKillPasswd, killRfu);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void inv_mode_CheckedChanged(object sender, EventArgs e)
        {
            if (inv_mode.Checked)
            {
                txtSend.Text = Commands.BuildSetInventoryModeFrame(ConstCode.INVENTORY_MODE0);  //INVENTORY_MODE0
            }
            else
            {
                txtSend.Text = Commands.BuildSetInventoryModeFrame(ConstCode.INVENTORY_MODE1);  //INVENTORY_MODE1
            }
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void ckxTruncated_CheckedChanged(object sender, EventArgs e)
        {
            if (ckxTruncated.Checked)
            {
                int intSelTarget = this.cbxSelTarget.SelectedIndex;
                int intSelMemBank = this.cbxSelMemBank.SelectedIndex;
                if (intSelTarget != 4 || intSelMemBank != 1)
                {
                    MessageBox.Show("Select Target should be 100 and MemBank should be EPC", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ckxTruncated.Checked = false;
                }
            }
        }

        private void btnSetFhss_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (btnSetFhss.Text == "FHSS ON")
            {

                txtSend.Text = Commands.BuildSetFhssFrame(ConstCode.SET_ON);
            }
            else
            {
                txtSend.Text = Commands.BuildSetFhssFrame(ConstCode.SET_OFF);
            }
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;

            if (btnSetFhss.Text == "FHSS ON")
            {
                btnSetFhss.Text = "FHSS OFF";
            }
            else
            {
                btnSetFhss.Text = "FHSS ON";
            }
        }

        private string curRegion = ConstCode.REGION_CODE_CHN2;
        private string hardwareVersion;
        private bool checkingReaderAvailable;
        private bool readerConnected;
        private void btnSetRegion_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string frame = string.Empty;
            if (cbxRegion.SelectedIndex == 0) // China 2
            {
                frame = Commands.BuildSetRegionFrame(ConstCode.REGION_CODE_CHN2);
                curRegion = ConstCode.REGION_CODE_CHN2;
            }
            else if (cbxRegion.SelectedIndex == 1) // China 1
            {
                frame = Commands.BuildSetRegionFrame(ConstCode.REGION_CODE_CHN1);
                curRegion = ConstCode.REGION_CODE_CHN1;
            }
            else if (cbxRegion.SelectedIndex == 2) // US
            {
                frame = Commands.BuildSetRegionFrame(ConstCode.REGION_CODE_US);
                curRegion = ConstCode.REGION_CODE_US;
            }
            else if (cbxRegion.SelectedIndex == 3) // Europe
            {
                frame = Commands.BuildSetRegionFrame(ConstCode.REGION_CODE_EUR);
                curRegion = ConstCode.REGION_CODE_EUR;
            }
            else if (cbxRegion.SelectedIndex == 4) // Korea
            {
                frame = Commands.BuildSetRegionFrame(ConstCode.REGION_CODE_KOREA);
                curRegion = ConstCode.REGION_CODE_KOREA;
            }
            
            txtSend.Text = frame;
            sock.Send(HexStrTobyte(frame));
            timerrecive.Enabled = true;
            cbxChannel.SelectedIndex = 0;
        }

        private void cbxRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbxChannel.Items.Clear();

            switch (cbxRegion.SelectedIndex)
            {
                case 0 : // China 2
                    for (int i = 0; i < 20; i++)
                    {
                        this.cbxChannel.Items.Add((920.125 + i * 0.25).ToString() + "MHz");
                    }
            	    break;
                case 1: // China 1
                    for (int i = 0; i < 20; i++)
                    {
                        this.cbxChannel.Items.Add((840.125 + i * 0.25).ToString() + "MHz");
                    }
                    break;
                case 2: // US
                    for (int i = 0; i < 52; i++)
                    {
                        this.cbxChannel.Items.Add((902.25 + i * 0.5).ToString() + "MHz");
                    }
                    break;
                case 3: // Europe
                    for (int i = 0; i < 15; i++)
                    {
                        this.cbxChannel.Items.Add((865.1 + i * 0.2).ToString() + "MHz");
                    }
                        break;
                case 4:  // Korea
                        for (int i = 0; i < 32; i++)
                        {
                            this.cbxChannel.Items.Add((917.1 + i * 0.2).ToString() + "MHz");
                        }
                        break;
                default :
                        break;
            }
            cbxChannel.SelectedIndex = 0;
        }

        private void btnGetChannel_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtSend.Text = Commands.BuildGetRfChannelFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnSetPaPower_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int powerDBm = 0;
            float powerFloat = 0;
            try
            {
                powerFloat = float.Parse(cbxPaPower.SelectedItem.ToString().Replace("dBm", ""));
              
                powerDBm = (int)(powerFloat * 100);
            }
            catch (Exception formatException)
            {
                Console.WriteLine(formatException.ToString());
                switch (cbxPaPower.SelectedIndex)
                {

                  
                     
                    case 6:
                        powerDBm = 1250;
                        break;
                    case 5:
                        powerDBm = 1400;
                        break;
                    case 4:
                        powerDBm = 1550;
                        break;
                    case 3:
                        powerDBm = 1700;
                        break;
                    case 2:
                        powerDBm = 1850;
                        break;
                    case 1:
                        powerDBm = 2000;
                        break;
                  case 0:
                        powerDBm = 2600;
                        break;
                    default:
                        powerDBm = 2600;
                        break;
                }
            }
            txtSend.Text = Commands.BuildSetPaPowerFrame((Int16)powerDBm);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }
        private void btnSetPaPower_Click0(int select)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int powerDBm = 0;
                switch (select)
                {



                    case 6:
                        powerDBm = 1250;
                        break;
                    case 5:
                        powerDBm = 1400;
                        break;
                    case 4:
                        powerDBm = 1550;
                        break;
                    case 3:
                        powerDBm = 1700;
                        break;
                    case 2:
                        powerDBm = 1850;
                        break;
                    case 1:
                        powerDBm = 2000;
                        break;
                    case 0:
                        powerDBm = 2600;
                        break;
                    default:
                        powerDBm = 2600;
                        break;
                }
            double power = powerDBm / 100;
            logfile.writelog("设置发射功率为：" + power + "dBm");
            txtSend.Text = Commands.BuildSetPaPowerFrame((Int16)powerDBm);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }
        private void SetPaPower(int select)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int powerDBm = 0;
            switch (select)
            {



                case 6:
                    powerDBm = 1250;
                    break;
                case 5:
                    powerDBm = 1400;
                    break;
                case 4:
                    powerDBm = 1550;
                    break;
                case 3:
                    powerDBm = 1700;
                    break;
                case 2:
                    powerDBm = 1850;
                    break;
                case 1:
                    powerDBm = 2000;
                    break;
                case 0:
                    powerDBm = 2600;
                    break;
                default:
                    powerDBm = 2600;
                    break;
            }
            double power = powerDBm / 100;
            logfile.writelog("设置发射功率为：" + power + "dBm");
            txtSend.Text = Commands.BuildSetPaPowerFrame((Int16)powerDBm);
            sock.Send(HexStrTobyte( txtSend.Text)); logfile.writelog("Send: " + txtSend.Text);
            timerrecive.Enabled = true;
        }
        private void SetPaPower2(int select)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int powerDBm = 0;
            switch (select)
            {



                case 6:
                    powerDBm = 1250;
                    break;
                case 5:
                    powerDBm = 1400;
                    break;
                case 4:
                    powerDBm = 1550;
                    break;
                case 3:
                    powerDBm = 1700;
                    break;
                case 2:
                    powerDBm = 1850;
                    break;
                case 1:
                    powerDBm = 2000;
                    break;
                case 0:
                    powerDBm = 2600;
                    break;
                default:
                    powerDBm = 2600;
                    break;
            }
            double power = powerDBm / 100;
            logfile.writelog("设置发射功率为：" + power + "dBm");
            txtSend.Text = Commands.BuildSetPaPowerFrame((Int16)powerDBm);
            sock.Send(HexStrTobyte(txtSend.Text)); logfile.writelog("Send: " + txtSend.Text);
        }
        private void btnGetPaPower_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            txtSend.Text = Commands.BuildGetPaPowerFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnSetModemPara_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            int mixerGain = cbxMixerGain.SelectedIndex;
            int IFAmpGain = cbxIFAmpGain.SelectedIndex;
            int signalTh = Convert.ToInt32(tbxSignalThreshold.Text,16);
            txtSend.Text = Commands.BuildSetModemParaFrame(mixerGain, IFAmpGain, signalTh);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnGetModemPara_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            txtSend.Text = Commands.BuildReadModemParaFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private string ParseErrCode(int errorCode)
        {
            switch (errorCode & 0x0F)
            {
                case ConstCode.ERROR_CODE_OTHER_ERROR :
                    return "Other Error";
                case ConstCode.ERROR_CODE_MEM_OVERRUN:
                    return "Memory Overrun";
                case ConstCode.ERROR_CODE_MEM_LOCKED:
                    return "Memory Locked";
                case ConstCode.ERROR_CODE_INSUFFICIENT_POWER:
                    return "Insufficient Power";
                case ConstCode.ERROR_CODE_NON_SPEC_ERROR:
                    return "Non-specific Error";
                default :
                    return "Non-specific Error";
            }
        }

        private void btnScanJammer_Click(object sender, EventArgs e)
        {
            txtSend.Text = Commands.BuildScanJammerFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void saveAsTxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();

            //File type filter
            save.Filter = "*.csv|*.CSV|*.*|(*.*)";

            if (save.ShowDialog() == DialogResult.OK)
            {
                string name = save.FileName;
                FileInfo info = new FileInfo(name);
                //info.Delete();
                StreamWriter writer = null;
                try
                {
                    writer = info.CreateText();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    writer.Write("No.,PC,EPC,CRC,RSSI(dBm),CNT,PER(%),");
                    writer.WriteLine();
                    for (int i = 0; i < basic_table.Rows.Count; i++)
                    {
                        for(int j = 0; j < basic_table.Columns.Count; j++)
                        {
                            writer.Write(basic_table.Rows[i][j].ToString()+",");
                        }
                        writer.WriteLine();
                        //writer.Write(richTextBox1.Text);
                    }
                    writer.Close();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnScanRssi_Click(object sender, EventArgs e)
        {
            txtSend.Text = Commands.BuildScanRssiFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void timerCheckReader_Tick(object sender, EventArgs e)
        {
            timerCheckReader.Enabled = false;
            readerConnected = true;
            //if (hardwareVersion == "")
            //{
            //   MessageBox.Show("连接读写器失败, 请检查固件是否下载!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    readerConnected = false;
            //}
            //else
            //{
            //    MessageBox.Show("Connect Success! Hardware version: " + hardwareVersion, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    readerConnected = true;
            //}
        }

        private void Reset_FW_Click(object sender, EventArgs e)
        {
            txtSend.Text = "BB 00 55 00 00 55 7E";
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        int lastRecCnt = 0;
        private int timeout;
        private int allepccount;
        private int times;
        private string sninfo;
        private string allepc;
        private log logfile;
        private string connect;
        private string password;
        private string command;
        private string begintxt;
        private string strFilePath;
        private string port;
        private string sntxt;
        private int baud;
        private Dictionary<string, string> ALLTID;
        private IPAddress ServerIP = IPAddress.Parse("127.0.0.1");
        private IPEndPoint serverFullAddr;
        private Socket sock;
        private  byte[] MsgBuffer = new byte[64];
        private bool timerreceive_run=false;
        private string Ip;
        private string model;
        private string performance;
     

        private void tmrCheckEpc_Tick(object sender, EventArgs e)
        {
            //if (lastRecCnt == Convert.ToInt32(txtCOMRxCnt.Text)) // no data received during last Tick, it may mean the Read Continue stoped
            //{
            //    tmrCheckEpc.Enabled = false;
            //    return;
            //}
            lastRecCnt = Convert.ToInt32(txtCOMRxCnt.Text);
            DateTime now = System.DateTime.Now;
            DateTime dt;
            DateTimeFormatInfo dtFormat = new System.Globalization.DateTimeFormatInfo();

            dtFormat.LongDatePattern = timeFormat;

            int timeout = (10 * tmrCheckEpc.Interval);
            for (int i = 0; i < this.dgvEpcBasic.Rows.Count; i++)
            {
                string time = this.dgvEpcBasic.Rows[i].Cells[7].Value.ToString();
                if (null != time && !"".Equals(time))
                {
                    //dt = Convert.ToDateTime(time, dtFormat);
                    //dt = DateTime.ParseExact(time, timeFormat, CultureInfo.InvariantCulture);
                    if (DateTime.TryParse(time,out dt))
                    {
                        TimeSpan sub = now.Subtract(dt);
                        if (sub.TotalMilliseconds > timeout)
                        {
                            this.dgvEpcBasic.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                        }
                        //else if ((sub.TotalMilliseconds > (tmrCheckEpc.Interval + 100)))
                        //{
                        //    this.dgvEpcBasic.Rows[i].DefaultCellStyle.BackColor = Color.Pink;
                        //}
                        else
                        {
                            int r = 0xFF & (int)(sub.TotalMilliseconds / timeout * 255);
                            //this.dgvEpcBasic.Rows[i].DefaultCellStyle.BackColor = Color.White;
                            this.dgvEpcBasic.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(0xff,255 - r ,255 - r);

                        }
                    }

                }
            }


        }

        private void btnSetIO_Click(object sender, EventArgs e)
        {
            byte para0 = 0x01;
            byte para1 = (byte)(cbxIO.SelectedIndex + 1);
            byte para2 = (byte)cbxIoLevel.SelectedIndex;
            txtSend.Text = Commands.BuildIoControlFrame(para0, para1, para2);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnSetIoDirection_Click(object sender, EventArgs e)
        {
            byte para0 = 0x00;
            byte para1 = (byte)(cbxIO.SelectedIndex + 1);
            byte para2 = (byte)cbxIoDircetion.SelectedIndex;
            txtSend.Text = Commands.BuildIoControlFrame(para0, para1, para2);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnGetIO_Click(object sender, EventArgs e)
        {
            byte para0 = 0x02;
            byte para1 = (byte)(cbxIO.SelectedIndex + 1);
            byte para2 = 0x00;
            txtSend.Text = Commands.BuildIoControlFrame(para0, para1, para2);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnSetMode_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            int mixerGain = cbxMode.SelectedIndex;
            int IFAmpGain = 6;
            int signalTh = Convert.ToInt32("00A0", 16);
            txtSend.Text = Commands.BuildSetModemParaFrame(mixerGain, IFAmpGain, signalTh);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
            //txtSend.Text = Commands.BuildSetReaderEnvModeFrame((byte)cbxMode.SelectedIndex);
            //sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
        }

        private void btnSaveConfigToNv_Click(object sender, EventArgs e)
        {
            byte NV_enable = cbxSaveNvConfig.Checked ? (byte)0x01 : (byte)0x00;
            txtSend.Text = Commands.BuildSaveConfigToNvFrame(NV_enable);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnSetModuleSleep_Click(object sender, EventArgs e)
        {
            txtSend.Text = Commands.BuildSetModuleSleepFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnInsertRfCh_Click(object sender, EventArgs e)
        {
            byte[] channelList;
            int chIndexBegin = Convert.ToInt32(txtChIndexBegin.Text);
            int chIndexEnd = Convert.ToInt32(txtChIndexEnd.Text);
            byte channelNum = (byte)(chIndexEnd - chIndexBegin + 1);
            channelList = new byte[channelNum];
            for (int i = chIndexBegin; i <= chIndexEnd; i++)
            {
                channelList[i - chIndexBegin] = (byte)i;
            }
            txtSend.Text = Commands.BuildInsertRfChFrame(channelNum, channelList);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnChangeConfig_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = tbxNxpCmdAccessPwd.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码为二字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            select();

            txtSend.Text = Commands.BuildNXPChangeConfigFrame(strAccessPasswd, Convert.ToInt32(txtConfigData.Text.Replace(" ",""), 16));
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnChangeEas_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = tbxNxpCmdAccessPwd.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码为二字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            select();

            txtSend.Text = Commands.BuildNXPChangeEasFrame(strAccessPasswd, cbxSetEas.Checked);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnEasAlarm_Click(object sender, EventArgs e)
        {
            //txtSend.Text = Commands.BuildFrame(ConstCode.FRAME_TYPE_CMD, "E4");
            txtSend.Text = Commands.BuildNXPEasAlarmFrame();
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }

        private void btnReadProtect_Click(object sender, EventArgs e)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = tbxNxpCmdAccessPwd.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码为二字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            select();

            txtSend.Text = Commands.BuildNXPReadProtectFrame(strAccessPasswd, cbxReadProtectReset.Checked);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }
        private void txtOperateEpc_DoubleClick(object sender, EventArgs e)
        {
            txtOperateEpc.SelectAll();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void label46_Click(object sender, EventArgs e)
        {

        }

        private void pbx_Inv_Indicator_Click(object sender, EventArgs e)
        {

        }

        private void cbxPaPower_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbxAutoClear_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cbxPort_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void TxtReceive_TextChanged(object sender, EventArgs e)
        {

        }

        private void DgvEpcBasic_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void TxtSend_TextChanged(object sender, EventArgs e)
        {

        }

        private void GbxEpcTableBasic_Enter(object sender, EventArgs e)
        {

        }

        private void Timerauto_Tick(object sender, EventArgs e)
        {


            timerauto.Enabled = false ;


            start_count_rfid = true;
            cbxAutoClear.Checked = true;
             cbxRxVisable.Checked = true;

            if(model=="read") ReadModel(null, null);
            else if (model == "write")
            {
                if (File.Exists(strFilePath))
                {

                    string snfile = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @sntxt);
                    if (File.Exists(snfile))
                    {
                        foreach (string line in File.ReadAllLines(snfile))
                        {
                            sn = line;
                            break;
                        }
                        logfile.creatlog(sn + ".txt");
                        String connetStr = connect + password;
                        logfile.writelog(connetStr);
                        // server=127.0.0.1/localhost 代表本机，端口号port默认是3306可以不写
                        MySqlConnection conn = new MySqlConnection(connetStr);
                        try
                        {
                            conn.Open();//打开通道，建立连接，可能出现异常,使用try catch语句
                                        //MessageBox.Show("已经建立连接");
                                        //在这里使用代码对数据库进行增删查改
                            MySqlCommand comm = new MySqlCommand(command + "'" + sn + "'", conn);
                            logfile.writelog(command + "'" + sn + "'");
                            MySqlDataReader sdr = comm.ExecuteReader();
                            int i = 0;
                            while (sdr.Read())
                            {

                                sninfo = sdr[1].ToString();
                                //listView1.Items.Add(sdr[0].ToString());
                                //listView1.Items[i].SubItems.Add(sdr[1].ToString());
                                i++;
                            }

                        }
                        catch (MySqlException ex)
                        {
                            MessageBox.Show(ex.Message);
                            logfile.writelog(ex.Message);
                            System.Environment.Exit(0);
                        }
                        finally
                        {
                            conn.Close();
                        }
                        if(sninfo!="") WriteModel(); ;
                       
                    }
                    //string strContent = File.ReadAllText(strFilePath);
                    //strContent = Regex.Replace(strContent, "将要被修改的内容", "修改后的内容");
                    //File.WriteAllText(strFilePath, strContent);
                }

                timerauto.Enabled = true;
            }
            else if(model == "read_tid")
                ReadTidModel(null, null);
            else
            {
                MessageBox.Show("模式输入错误！");
                System.Environment.Exit(0);
            }

        }
        /// <summary>
        /// 完成写值并检查值
        /// </summary>

        private void WriteModel()
        {
            logfile.DeleteDirectory("debug.txt");
            logfile.creatlog(sn+".txt");
            Delay(100);//单位为毫秒；

            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //单次轮询
            logfile.writelog("单次轮询：");
            DateTime start = DateTime.Now;
            do
            {
                btn_invt2_Click(null, null);
               //WaitReceive();
                         //System.Threading.Thread.Sleep(100);

            }
            while (txtReceive.Text.IndexOf("BB 01 FF") == 0 && Spantime(start) < timeout);
            txtSelMask.Text = epc;
            Delay(100);//单位为毫秒；


            if (Spantime(start) > timeout)
            {
                MessageBox.Show("获取标签超时！");
                logfile.writelog("获取标签超时");
                EndAction();
                System.Environment.Exit(0);
            }
            logfile.writelog("单次轮询：");
            //select标签
            start = DateTime.Now;
            do
            {
                select2();
                Delay(100);//单位为毫秒；

            } while (txtReceive.Text.IndexOf("BB 01 0C") != 0 && Spantime(start) < timeout);

            if (Spantime(start) > timeout)
            {
                MessageBox.Show("select标签超时！");
                logfile.writelog("select标签超时！");
                EndAction();
                return;
            }
            //写值
            logfile.writelog("写值：");
            start = DateTime.Now;
            do
            {
                Delay(100);//单位为毫秒；
                string dd = Decryption(sninfo);
                Writedata(dd); //写info，
                Delay(100);//单位为毫秒；
            } while (txtReceive.Text.IndexOf("BB 01 FF") == 0 && Spantime(start) < timeout);

            if (Spantime(start) > timeout)
            {
               
                MessageBox.Show("写标签超时！");
                logfile.writelog("写标签超时！");
                return;
            }
            btn_clear_epc2_Click(null, null);
            logfile.writelog("单次轮询：");
            //单次轮询
            start = DateTime.Now;
            do
            {
                btn_invt2_Click(null, null);
                Delay(100);//单位为毫秒；
                         //System.Threading.Thread.Sleep(100);

            }
            while (txtReceive.Text.IndexOf("BB 01 FF") == 0 && Spantime(start) < timeout);
            logfile.writelog("select标签:");
            //select标签
            if (Spantime(start) > timeout)
            {
                MessageBox.Show("获取标签超时！");
                logfile.writelog("获取标签超时！");
             
                return;
            }
            start = DateTime.Now;
            do
            {
                select2();
                Delay(100);//单位为毫秒；

            } while ((txtReceive.Text == ""||txtReceive.Text.IndexOf("BB 01 FF") == 0) && Spantime(start) < timeout);

            if (Spantime(start) > timeout )
            {
                MessageBox.Show("select标签超时！");
                logfile.writelog("select标签超时！");
                EndAction();
                return;
            }
            logfile.writelog("读标签:");
            //读标签
            start = DateTime.Now;
            string getinfo = string.Empty;
            do
            {
               getinfo= Readdata(sninfo); //写info，
               Delay(100);//单位为毫秒；
            } while ((getinfo==""|| txtReceive.Text.IndexOf("BB 01 FF") == 0 || txtReceive.Text.IndexOf("BB 01 0C") == 0) && Spantime(start) < timeout);
    
            if (Spantime(start) > timeout)
            {
                MessageBox.Show("写标签超时！");
                logfile.writelog("写标签超时！");

                return;
            }
            logfile.writelog("校验：");
            //得到最终data；
            string s = string.Empty;
            start = DateTime.Now;
            do
            {
                //s = txtReceive.Text.Substring(txtReceive.Text.LastIndexOf(pc) + 6);
                logfile.writelog(txtReceive.Text + "校验：");
                Delay(100);
            } while ((txtReceive.Text.Length < 4 || txtReceive.Text.IndexOf("BB") != 0 || txtReceive.Text.LastIndexOf("7E") != txtReceive.Text.Length - 3) && Spantime(start) < timeout);
            if (Spantime(start) > timeout)
            {
                MessageBox.Show("check标签标签超时！");
                logfile.writelog("check标签标签超时！");

                return;
            }
            s = txtReceive.Text.Substring(txtReceive.Text.LastIndexOf(pc) + 6);
            s = s.Substring(0, s.Length - 7);
            while (s.LastIndexOf("FF") == s.Length-2) //去掉末尾的FF;
            {
                s = s.Remove(s.Length - 3);
            }
           
            if (Decryption(HexToStr(s)) != sninfo)
            {
                string cc = Decryption(HexToStr(s));
                MessageBox.Show("check标签结果：写入失败！");
                logfile.writelog("check标签结果：写入失败！");
                DeleteDirectory(@"D:\test\sn.txt");
                DeleteDirectory(@"D:\test\start.txt");
                return;
            }
            DeleteDirectory( @"D:\test\sn.txt");
            DeleteDirectory( @"D:\test\start.txt");
            txtReceive.Text = "************写入成功！**********\r\n";
            logfile.writelog("************写入成功！**********");
           // EndAction();
            btn_clear_basictable_Click(null, null);
            txtSend.Text = "";
           
        }

       

        public string StrToHex(string mStr) //返回处理后的十六进制字符串 
        {
            return BitConverter.ToString(
            ASCIIEncoding.Default.GetBytes(mStr)).Replace("-", " ");
        } /* StrToHex */
        public string HexToStr(string mHex) // 返回十六进制代表的字符串 
        {
            mHex = mHex.Replace(" ", "");
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return ASCIIEncoding.Default.GetString(vBytes);
        } /* HexToStr */

        /// <summary>
        /// 字符串转换为16进制字符
        /// </summary>
        /// <param name="s"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        private string StringToHexString(string s, Encoding encode)
        {
            byte[] b = encode.GetBytes(s);//按照指定编码将string编程字节数组
            string result = string.Empty;
            for (int i = 0; i < b.Length; i++)//逐字节变为16进制字符
            {
                result += Convert.ToString(b[i], 16);
            }
            return result;
        } /// <summary>
          /// 16进制字符转换为字符串
          /// </summary>
          /// <param name="hs"></param>
          /// <param name="encode"></param>
          /// <returns></returns>
        private string HexStringToString(string hs, Encoding encode)
        {
            hs = hs.Replace(" ", "");
            string strTemp = "";
            byte[] b = new byte[hs.Length / 2];
            for (int i = 0; i < hs.Length / 2; i++)
            {
                strTemp = hs.Substring(i * 2, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encode.GetString(b);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sninfo">信息</param>
        /// <param name="target">判断标识</param>
        private void Writedata(string sninfo)
        {
           // dataGrid_MouseUp(null, null);
            string CRC = (crc+pc).Replace(" ", "");
            string Hex = StrToHex(sninfo);
            string str= HexToStr(Hex);
            string info = AutoAddSpace(Hex);
            Hex = CRC+Hex;
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string strAccessPasswd = txtRwAccPassWord.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码为二个字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string strDate4Write = Hex.Replace(" ", "");

            int intMemBank = 1;
            int wordPtr = 0;//Convert.ToInt32((txtWordPtr1.Text.Replace(" ", "") + txtWordPtr0.Text.Replace(" ", "")), 16)

            if (strDate4Write.Length > 16 * 4)
            {
                MessageBox.Show("Write Data Length Limit is 16 Words", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string a = new string('F', 16*4- strDate4Write.Length);
            strDate4Write = strDate4Write + a;
            allepc = strDate4Write.Substring(8);
            int wordCnt = strDate4Write.Length / 4; // in word!

            //select();
            txtInvtRWData.Text = AutoAddSpace(strDate4Write);
            txtWordCnt0.Text = wordCnt.ToString("X2");
            cbxMemBank.Text = "EPC";
            //wordPtr = "1";
            txtSend.Text = Commands.BuildWriteDataFrame(strAccessPasswd, intMemBank
                , wordPtr, wordCnt, strDate4Write);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
        }
        private string Readdata(string sninfo)
        {
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            string strAccessPasswd = txtRwAccPassWord.Text.Replace(" ", "");
            if (strAccessPasswd.Length != 8)
            {
                MessageBox.Show("访问密码设置为2个字节!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }

            int wordPtr = Convert.ToInt32((txtWordPtr1.Text.Replace(" ", "") + txtWordPtr0.Text.Replace(" ", "")), 16);
            int wordCnt = Convert.ToInt32((txtWordCnt1.Text.Replace(" ", "") + txtWordCnt0.Text.Replace(" ", "")), 16);
            int intMemBank = cbxMemBank.SelectedIndex;

            txtSend.Text = Commands.BuildReadDataFrame(strAccessPasswd, intMemBank, wordPtr, wordCnt);
            sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
            timerrecive.Enabled = true;
            string getinfo = txtReceive.Text;


            return getinfo;
        }
        private void CbxRxVisable_CheckedChanged(object sender, EventArgs e)
        {

        }
        public static bool Delay(double delayTime)
        {
            DateTime now = DateTime.Now;
           double s;
            do
            {
                TimeSpan spand = DateTime.Now - now;
                s = spand.TotalMilliseconds;
                Application.DoEvents();
            }
            while (s < delayTime);
            return true;
        }
        /// <summary>
        /// 计算超时时间
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public static double Spantime(DateTime start)
        {
            DateTime now = DateTime.Now;
            double s;
            TimeSpan spand = DateTime.Now - start;
            s = spand.TotalMilliseconds;;
            Application.DoEvents();
            return s;
        }

        private void Label27_Click(object sender, EventArgs e)
        {

        }

        private void CbxMemBank_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        //加密&&解密
        private string Decryption(string str)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = str.Length - 1; i >= 0; i--)
            {
                //大写转小写
                if (str[i] >= 'A' && str[i] <= 'Z')
                {
                    string newStr = string.Empty;    //用于存放新字符串
                    newStr += str[i].ToString().ToLower();
                    sb.Append(newStr);
                }
                else if (str[i] >= 'a' && str[i] <= 'z')
                {
                    string newStr = string.Empty;    //用于存放新字符串
                    newStr += str[i].ToString().ToUpper();
                    sb.Append(newStr);
                }
                else
                    sb.Append(str[i]);

            }
            string s = sb.ToString();
            return s;
        }
        /// <summary>
        /// 删除文件夹以及文件
        /// </summary>
        /// <param name="directoryPath"> 文件夹路径 </param>
        /// <param name="fileName"> 文件名称 </param>
        public static void DeleteDirectory(string fileName)
        {
            string destinationFile = fileName;

            if (File.Exists(destinationFile))

            {

                FileInfo fi = new FileInfo(destinationFile);

                if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)

                    fi.Attributes = FileAttributes.Normal;

                File.Delete(destinationFile);

            }
        }

        private void ToolTip1_Popup(object sender, PopupEventArgs e)
        {

        }
        /// <summary>
        /// IP
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadModel(object sender, EventArgs e)
        {
           
            txtSelMask.Text = epc;
            Delay(100);//单位为毫秒；
                       //多次轮询

            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //停止轮询；
            logfile.writelog("停止轮询！");
            btn_stop_rd_Click(null, null);
            WaitReceive();
            logfile.writelog("多次轮询开始！");
            //开始轮询
           for(int i = 0; i <= 6; i++)
            {
                //设置功率
                SetPaPower(i);
                WaitReceive();
                txtRDMultiNum.Text = times.ToString();
                int loopCnt = Convert.ToInt32(txtRDMultiNum.Text);
                //btn_invt_multi_Click(null, null);
                btn_invt2_Click(null, null);
                WaitReceive();
                txtSend.Text = Commands.BuildReadMultiFrame(loopCnt);
                sock.Send(HexStrTobyte(txtSend.Text));logfile.writelog("Send: "+txtSend.Text);
                timerrecive.Enabled = true;
                WaitReceive();
            }
            EndAction();
            logfile.writelog("读取标签失败！设定有标签" + allepccount + "个，但只读取到" + EPC_No.Count + "个");
            MessageBox.Show("只读取到" + EPC_No.Count.ToString() + "个标签，还有" + (allepccount - EPC_No.Count).ToString() + "个标签未读出！");
            System.Environment.Exit(0);
            return;
            //  return;


        }
        private void ReadTidModel(object sender, EventArgs e)
        {
            ALLTID = new Dictionary<string, string>();
            EPC_No = new List<string>();
            if (bAutoSend == true)
            {
                MessageBox.Show("请停止连续盘存", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //单次轮询
           

            for (int i = 0; i <= 6; i++)
            {
                DateTime allstart = DateTime.Now;
                DateTime starttime = DateTime.Now;
                //设置功率
                SetPaPower2(i);
                List<string> rec = receivedata();
                while ((rec.Count == 0 || rec[0].IndexOf("BB01B6") != 0) && Spantime(starttime) < timeout)
                {
                    rec = receivedata();
                }

                do
                {
                    
                    logfile.writelog("单次轮询：");

                    //do
                    //{
                    //    btn_invt2_Click(null, null);
                    //    Delay(1000);//单位为毫秒；
                    //                //WaitReceive();
                    //                //System.Threading.Thread.Sleep(100);

                    //}
                    //while (txtReceive.Text.IndexOf("BB 01 FF") == 0 && Spantime(starttime) < timeout);
                    //WaitReceive();
                    ////txtSelMask.Text = EPC_No.Count.ToString();
                    //foreach (string epc in EPC_No)
                    //{
                    //    int flag = 1;//判断，如果epc已读出TID便不再操作该epc
                    //    foreach (var tid in ALLTID)
                    //    {
                    //        if (tid.Key == epc)
                    //        {
                    //            flag = 0;
                    //            break;
                    //        }

                    //    }
                    //    if (flag == 0) continue;
                    //    //logfile.writelog("epc:"+epc);
                    //    starttime = DateTime.Now;
                    //    txtInvtRWData.Text = "";
                    //    while ((txtInvtRWData.Text == "") && Spantime(starttime) < timeout)//每个epc 获取tid直到超时；
                    //    {
                    //        //logfile.writelog(epc);
                       //  btn_invtread_Click_tid(epc);//读tid包括select
                    //        Delay(100);//单位为毫秒；
                    //        if (txtInvtRWData.Text != "")
                    //        {
                    //            try
                    //            {
                    //                ALLTID.Add(epc, txtInvtRWData.Text.Substring(0, txtInvtRWData.Text.Length - 3));
                    //                if (ALLTID.Count >= allepccount)
                    //                {
                    //                    foreach (var tid in ALLTID)
                    //                    {
                    //                        logfile.writelog("EPC:" + tid.Key + " TID:" + tid.Value);
                    //                    }
                    //                    MessageBox.Show("共发现RFID数:   " + ALLTID.Count.ToString() );
                    //                    return;
                    //                    System.Environment.Exit(0);
                    //                }
                    //            }
                    //            catch (Exception x)
                    //            {

                    //            }
                    //        }
                    //    }
                    //    //if (txtInvtRWData.Text == "") epc_no_tid.Add(epc);
                    //    //sock.Send(HexStrTobyte(Commands.BuildSetInventoryModeFrame("01")));//取消选择
                    //}

                    //发送单次循环
                    LoopNum_cnt = LoopNum_cnt + 1;
                    txtSend.Text = Commands.BuildReadSingleFrame();
                    sock.Send(HexStrTobyte(txtSend.Text)); 
                    logfile.writelog("Send: " + txtSend.Text);
                    starttime = DateTime.Now;
                    do
                    {
                        rec = receivedata();
                    }
                    while ((rec.Count == 0 || rec[0].IndexOf("BB01FF") == 0) && Spantime(starttime) < timeout);
                  
                    if (rec.Count>0&&rec[0].IndexOf("BB01FF") != 0)
                    {
                        foreach (string epc in rec)
                        {
                            logfile.writelog("Received   EPC: " +AutoAddSpace(epc));
                        }
                            //处理收到的数据
                         foreach (string epc in rec)
                        {
                         
                            EPC_No.Add(AutoAddSpace(epc.Substring(16,24)));
                            int flag = 1;//判断，如果epc已读出TID便不再操作该epc
                            foreach (var tid in ALLTID)
                            {
                                if (tid.Key ==AutoAddSpace( epc))
                                {
                                    flag = 0;
                                    break;
                                }

                            }
                            if (flag == 0) continue;
                            //select
                            string send = Commands.BuildSetSelectFrame(0, 0, 2, 32, 96, 1, epc);
                            sock.Send(HexStrTobyte(send)); 
                            logfile.writelog("Send: " + send);
                            starttime = DateTime.Now;
                            do
                            {
                                rec = receivedata();
                            }
                            while ((rec.Count == 0 || rec[0].IndexOf("BB010C") != 0) && Spantime(starttime) < timeout);
                            if(rec.Count>0&&rec[0].IndexOf("BB010C") == 0)
                            {
                                logfile.writelog("Received: " +AutoAddSpace( rec[0]));
                                //读tid

                                starttime = DateTime.Now;
                                do
                                {
                                    send = "BB 00 39 00 09 00 00 00 00 02 00 00 00 08 4C 7E";
                                    sock.Send(HexStrTobyte(send)); logfile.writelog("Send: " + send);
                                    rec = receivedata();
                                }
                                while ((rec.Count == 0 || rec[0].IndexOf("BB0139") != 0) && Spantime(starttime) < timeout);
                                if (rec[0].IndexOf("BB0139") == 0)
                                {
                                    try
                                    {
                                        //"BB0139001F0E3000E200001D541102321930CC60E2003412013702000E629F1809140163AE7E"
                                        string cc = AutoAddSpace(epc.Substring(16, 24));
                                        string tidd = AutoAddSpace(rec[0].Substring(40, 30));
                                        //BB 01 39 00 1F 0E 30 00 //E2 00 00 1D 61 15 02 43 09 60 DD A1 //E2 00 34 12 01 33 1B 00 01 4B 9F 78 05 1F 01// 5E// 95 7E 
                                        ALLTID.Add(cc, tidd);
                                        txtReceive.Text += "EPC:" + cc + "**TID:" + tidd+ "\r\n";
                                        if (ALLTID.Count >= allepccount)
                                        {
                                            foreach (var tid in ALLTID)
                                            {
                                                logfile.writelog("EPC:" + tid.Key + " TID:" + tid.Value);
                                            }
                                            MessageBox.Show("共发现RFID数:   " + ALLTID.Count.ToString());
                                            return;
                                            System.Environment.Exit(0);
                                        }
                                    }
                                    catch (Exception x)
                                    {

                                    }
                                }
                            }
                        }
                        //logfile.writelog("Received   EPC: " + AutoAddSpace(epc));
                    }


                    EPC_No.Clear();
                    groupBox3.Text = ALLTID.Count.ToString();

                }
                while (Spantime(allstart) < times);
                if(ALLTID.Count >= allepccount)
                {
                    foreach (string tid in ALLTID.Keys)
                    {
                        logfile.writelog("TID:" + tid);
                    }
                    
                    MessageBox.Show("成功发现RFID数:   " + ALLTID.Count.ToString());
                    return;
                    System.Environment.Exit(0);
                }

                
            }
            logfile.writelog("总共发现TID: " + ALLTID.Count.ToString());
            foreach (string tid in ALLTID.Keys)
            {
                logfile.writelog("TID:" + tid);
            }
            MessageBox.Show("Expect:   " + allepccount + "\nFact: "+ ALLTID.Count.ToString());
            return;
            System.Environment.Exit(0);
            DateTime start = DateTime.Now;
            //txtSelMask.Text = epc;
            Delay(100000);//单位为毫秒；
            //select标签
            start = DateTime.Now;
            do
            {
                select2();
                Delay(100);//单位为毫秒；

            } while (txtReceive.Text.IndexOf("BB 01 0C") != 0 && Spantime(start) < timeout);

            if (Spantime(start) > timeout)
            {
                MessageBox.Show("select标签超时！");
                logfile.writelog("select标签超时！");
                EndAction();
                return;
            }
            //写值
            logfile.writelog("写值：");
            start = DateTime.Now;
            do
            {
                Delay(100);//单位为毫秒；
                string dd = Decryption(sninfo);
                Writedata(dd); //写info，
                Delay(100);//单位为毫秒；
            } while (txtReceive.Text.IndexOf("BB 01 FF") == 0 && Spantime(start) < timeout);

            if (Spantime(start) > timeout)
            {

                MessageBox.Show("写标签超时！");
                logfile.writelog("写标签超时！");
                return;
            }
            btn_clear_epc2_Click(null, null);
            logfile.writelog("单次轮询：");
            //单次轮询
            start = DateTime.Now;
            do
            {
                btn_invt2_Click(null, null);
                Delay(100);//单位为毫秒；
                           //System.Threading.Thread.Sleep(100);

            }
            while (txtReceive.Text.IndexOf("BB 01 FF") == 0 && Spantime(start) < timeout);
            logfile.writelog("select标签:");
            //select标签
            if (Spantime(start) > timeout)
            {
                MessageBox.Show("获取标签超时！");
                logfile.writelog("获取标签超时！");

                return;
            }
            start = DateTime.Now;
            do
            {
                select2();
                Delay(100);//单位为毫秒；

            } while ((txtReceive.Text == "" || txtReceive.Text.IndexOf("BB 01 FF") == 0) && Spantime(start) < timeout);

            if (Spantime(start) > timeout)
            {
                MessageBox.Show("select标签超时！");
                logfile.writelog("select标签超时！");
                EndAction();
                return;
            }
            logfile.writelog("读标签:");
            //读标签
            start = DateTime.Now;
            string getinfo = string.Empty;
            do
            {
                getinfo = Readdata(sninfo); //写info，
                Delay(100);//单位为毫秒；
            } while ((getinfo == "" || txtReceive.Text.IndexOf("BB 01 FF") == 0 || txtReceive.Text.IndexOf("BB 01 0C") == 0) && Spantime(start) < timeout);

            if (Spantime(start) > timeout)
            {
                MessageBox.Show("写标签超时！");
                logfile.writelog("写标签超时！");

                return;
            }
            logfile.writelog("校验：");
            //得到最终data；
            string s = string.Empty;
            start = DateTime.Now;
            do
            {
                //s = txtReceive.Text.Substring(txtReceive.Text.LastIndexOf(pc) + 6);
                logfile.writelog(txtReceive.Text + "校验：");
                Delay(100);
            } while ((txtReceive.Text.Length < 4 || txtReceive.Text.IndexOf("BB") != 0 || txtReceive.Text.LastIndexOf("7E") != txtReceive.Text.Length - 3) && Spantime(start) < timeout);
            if (Spantime(start) > timeout)
            {
                MessageBox.Show("check标签标签超时！");
                logfile.writelog("check标签标签超时！");

                return;
            }
            s = txtReceive.Text.Substring(txtReceive.Text.LastIndexOf(pc) + 6);
            s = s.Substring(0, s.Length - 7);
            while (s.LastIndexOf("FF") == s.Length - 2) //去掉末尾的FF;
            {
                s = s.Remove(s.Length - 3);
            }

            if (Decryption(HexToStr(s)) != sninfo)
            {
                string cc = Decryption(HexToStr(s));
                MessageBox.Show("check标签结果：写入失败！");
                logfile.writelog("check标签结果：写入失败！");
                DeleteDirectory(@"D:\test\sn.txt");
                DeleteDirectory(@"D:\test\start.txt");
                return;
            }
            // EndAction();
            btn_clear_basictable_Click(null, null);
            txtSend.Text = "";

        }


        private List<string> receivedata()
        {
            List<string> kk = new List<string>();
            int count = 0;
            while (true)
            {
                byte[] rec = new byte[1000];
                if (sock.Available != 0)
                {
                    //logfile.writelog(byteToHexStr(rec));
                    count = 0;
                    sock.Receive(rec);

                }

                else
                {
                    Delay(50);
                    count++;
                    label1.Text = count.ToString();
                    if (model == "read_tid" && count >= 3) break;
                    else
                        if (count >= 5) break;
                    continue;
                }


                // txtReceive.Text = byteToHexStr(rec);
                string tt = byteToHexStr(rec);
                // list<string[]> kk=new list<string[]>;
                string ss = string.Empty;
                //logfile.writelog(tt);
                //tt = "BB02220011C53000E200001D381901020270472F2EEF827EBB02220011C63000E200001D38190102
                //BB02220011BB3400300833B2DDD7E4000000000C41E1A7EBB02220011C13400445533B2DDD91147E01112117E
                for (int ii = 0; ii < tt.Length; ii++)
                {
                    int pos1 = tt.IndexOf("BB");
                    int pos2 = tt.IndexOf("7EBB", pos1 + 2);
                    if (pos2 == -1) pos2 = tt.IndexOf("7E", pos1 + 2);
                    if (pos1 == -1) break;
                    if (pos2 == -1) break;
                    ss = tt.Substring(pos1, pos2 + 2);
                    string frame = ss.Substring(2, ss.Length - 6);
                    string checkSum = Commands.CalcCheckSum(frame);
                    if (checkSum != ss.Substring(ss.Length - 4, 2)) break; //保证receive显示的是正确的信息
                    kk.Add(ss);
                    tt = tt.Replace(ss, "");

                }
             
            }
            return kk;
        }
        private static byte[] HexStrTobyte(string hexString)
        {
             hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);
            return returnBytes;
        }
        // 字节数组转16进制字符串   
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");//ToString("X2") 为C#中的字符串格式控制符
                }
            }
            return returnStr;
        }

        private static string[] strtostring(string s)
        {
            List<string> strList = new List<string>();
            //if (s.Length % 2!=0)  s=s.Remove(s.Length-1);
            for (int i = 0; i < s.Length; i = i + 2)
            {
                string ss = s.Substring(i, 2);
                strList.Add(ss);
            }
            string[] astr = strList.ToArray();
            return astr;
        }
        private void Timerrecive_Tick(object sender, EventArgs e)
        {
            //if(performance=="color") tmrCheckEpc.Enabled = true ;
            timerreceive_run =true;
            timerrecive.Enabled = false;
            string recold = "b";
            string recnew = "a";
            bool flag = false;
            int rr = 0;
            int count = 0;
            while(true)
            {
                recold = recnew;
                byte[] rec = new byte[1000];
                if (sock.Available != 0)
                {
                    //logfile.writelog(byteToHexStr(rec));
                    count = 0;
                    sock.Receive(rec);

                }

                else {
                    Delay(50);
                    count++;
                    label1.Text = count.ToString();
                    if (model== "read_tid" && count >= 3) break;
                    else
                        if (count >= 100) break;
                    continue;
                } 

                
                // txtReceive.Text = byteToHexStr(rec);
                string tt = byteToHexStr(rec);
                // list<string[]> kk=new list<string[]>;
                List<string> kk = new List<string>();
                string s = string.Empty;
                //logfile.writelog(tt);
                //tt = "BB02220011C53000E200001D381901020270472F2EEF827EBB02220011C63000E200001D38190102
                //BB02220011BB3400300833B2DDD7E4000000000C41E1A7EBB02220011C13400445533B2DDD91147E01112117E
                for (int i = 0; i < tt.Length; i++)
                {
                    int pos1 = tt.IndexOf("BB");
                    int pos2 = tt.IndexOf("7EBB", pos1 + 2);
                    if(pos2==-1) pos2= tt.IndexOf("7E", pos1 + 2);
                    if (pos1 == -1) break;
                    if (pos2 == -1) break;
                    s = tt.Substring(pos1, pos2 + 2);
                    string frame = s.Substring(2,s.Length-6);
                    string checkSum = Commands.CalcCheckSum(frame);
                    if (checkSum != s.Substring(s.Length - 4, 2)) break; //保证receive显示的是正确的信息
                    kk.Add(s);
                    tt = tt.Replace(s, "");

                }
                foreach (string ss in kk)
                {

                    //if (ss[0] != 'B' || ss[1] != 'B' || ss[ss.Length - 2] != '7' || ss[ss.Length - 1] != 'E') continue;
                   // textBox1.Text =ss;
                   
                  rp_PaketReceived(strtostring(ss));
                   
                }
 
              logfile.writelog("Received:"+txtReceive.Text);
               // Delay(100);
                //rr++;
                recnew = txtCOMRxCnt.Text;
               

               
            }
            timerreceive_run = false;
            logfile.writelog("recieve data end");
        }
        private void WaitReceive()
        {
            timerrecive.Enabled = true;
           if(model!="read_tid")Delay(1000);
            while (timerreceive_run) ;
        }
        /// <summary>
        /// 结束动作
        /// </summary>
        private void EndAction()
        {
            //停止轮询
            if (model == "read")
            {
                logfile.writelog("停止轮询");
                btn_stop_rd_Click(null, null);
                WaitReceive();
                
            }
            //恢复默认功率
           logfile.writelog("恢复默认功率");
            SetPaPower(0);
            WaitReceive();
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //MessageBox.Show("你想要关闭程序？");
            DialogResult result;
            result = MessageBox.Show("确定退出吗？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                btn_stop_rd_Click(null, null);
                Application.ExitThread();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("RFID_Reader_Csharp");
            processes[0].Kill();
        }

        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            MessageBox.Show("yyyyy");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtGetSelMask_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtInvtRWData_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
