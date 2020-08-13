using System;

namespace RFID_Reader_Cmds
{
    public class Commands
    {
        public struct lock_payload_type
        {
            public byte byte0;

            public byte byte1;

            public byte byte2;
        }

        public static string CalcCheckSum(string data)
        {
            if (data == null)
            {
                return "";
            }
            int checksum = 0;
            string dataNoSpace = data.Replace(" ", "");
            try
            {
                for (int i = 0; i < dataNoSpace.Length; i += 2)
                {
                    checksum += Convert.ToInt32(dataNoSpace.Substring(i, 2), 16);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("do checksum error" + ex);
            }
            checksum %= 256;
            return checksum.ToString("X2");
        }

        public static string BuildFrame(string data)
        {
            if (data == null)
            {
                return "";
            }
            string frame = data.Replace(" ", "");
            string checkSum = Commands.CalcCheckSum(frame);
            return "BB" + frame + checkSum + "7E";
        }

        public static string BuildFrame(string msgType, string cmdCode)
        {
            if (msgType == null || cmdCode == null)
            {
                return "";
            }
            msgType = msgType.Replace(" ", "");
            if (msgType.Length == 1)
            {
                msgType = "0" + msgType;
            }
            cmdCode = cmdCode.Replace(" ", "");
            if (cmdCode.Length == 1)
            {
                cmdCode = "0" + cmdCode;
            }
            string frame = msgType + cmdCode + "0000";
            frame = "BB" + frame + cmdCode + "7E";
            return Commands.AutoAddSpace(frame);
        }

        public static string BuildFrame(string msgType, string cmdCode, string data)
        {
            if (msgType == null || cmdCode == null)
            {
                return "";
            }
            msgType = msgType.Replace(" ", "");
            if (msgType.Length == 1)
            {
                msgType = "0" + msgType;
            }
            cmdCode = cmdCode.Replace(" ", "");
            if (cmdCode.Length == 1)
            {
                cmdCode = "0" + cmdCode;
            }
             int dataHexLen = 0;
            if (data != null)
            {
                data = data.Replace(" ", "");
                if (data.Length == 1)
                {
                    data = "0" + data;
                }
                dataHexLen = data.Length / 2; //25 data = "000000000100000008A06C3400123456789000123466799000"
                data = data.Substring(0, dataHexLen * 2); //data = "000000000100000008A06C3400123456789000123466799000"
            }
            string frame = msgType + cmdCode + dataHexLen.ToString("X4") + data;//004919000000000100000008A06C3400123456789000123466799000
            string checkSum = Commands.CalcCheckSum(frame);//04
            frame = "BB" + frame + checkSum + "7E";//frame = "BB00490019000000000100000008A06C3400123456789000123466799000047E"
            return Commands.AutoAddSpace(frame);
        }

        public static string BuildFrame(string msgType, string cmdCode, string[] dataArr)
        {
            if (msgType == null || cmdCode == null)
            {
                return "";
            }
            msgType = msgType.Replace(" ", "");
            if (msgType.Length == 1)
            {
                msgType = "0" + msgType;
            }
            cmdCode = cmdCode.Replace(" ", "");
            if (cmdCode.Length == 1)
            {
                cmdCode = "0" + cmdCode;
            }
            int dataHexLen = 0;
            if (dataArr != null)
            {
                dataHexLen = dataArr.Length;
            }
            string frame = "BB" + msgType + cmdCode + dataHexLen.ToString("X4");
            int checksum = 0;
            checksum += 313;
            try
            {
                for (int i = 0; i < dataHexLen; i++)
                {
                    dataArr[i] = dataArr[i].Replace(" ", "");
                    if (dataArr[i].Length == 1)
                    {
                        frame = frame + "0" + dataArr[i];
                    }
                    else
                    {
                        frame += dataArr[i];
                    }
                    checksum += Convert.ToInt32(dataArr[i], 16);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("convert error " + ex);
            }
            frame = frame + (checksum % 256).ToString("X2") + "7E";
            return frame;
        }

        public static string AutoAddSpace(string Str)
        {
            string StrDone = string.Empty;
            if (Str == null || Str.Length == 0)
            {
                return StrDone;
            }
            int i;
            for (i = 0; i <= Str.Length - 2; i += 2)
            {
                StrDone = string.Concat(new object[]
                {
                    StrDone,
                    Str[i],
                    Str[i + 1],
                    " "
                });
            }
            if (Str.Length % 2 != 0)
            {
                StrDone = StrDone + "0" + Str[i].ToString();
            }
            return StrDone;
        }

        public static string BuildGetModuleInfoFrame(string infoType)
        {
            return Commands.BuildFrame("00", "03", infoType);
        }

        public static string BuildSetRegionFrame(string region)
        {
            if (region == null || region.Length == 0)
            {
                return "";
            }
            return Commands.BuildFrame("00", "07", region);
        }

        public static string BuildSetRfChannelFrame(string ch)
        {
            if (ch == null || ch.Length == 0)
            {
                return "";
            }
            return Commands.BuildFrame("00", "AB", ch);
        }

        public static string BuildGetRfChannelFrame()
        {
            return Commands.BuildFrame("00", "AA");
        }

        public static string BuildSetFhssFrame(string OnOff)
        {
            if (OnOff == null || OnOff.Replace(" ", "").Length == 0)
            {
                return "";
            }
            return Commands.BuildFrame("00", "AD", OnOff);
        }

        public static string BuildSetPaPowerFrame(short powerdBm)
        {
            string strPower = powerdBm.ToString("X4");
            return Commands.BuildFrame("00", "B6", strPower);
        }

        public static string BuildGetPaPowerFrame()
        {
            return Commands.BuildFrame("00", "B7");
        }

        public static string BuildSetCWFrame(string OnOff)
        {
            if (OnOff == null || OnOff.Replace(" ", "").Length == 0)
            {
                return "";
            }
            return Commands.BuildFrame("00", "B0", OnOff);
        }

        public static string BuildReadSingleFrame()
        {
            return Commands.BuildFrame("00", "22");
        }

        public static string BuildReadMultiFrame(int loopNum)
        {
            if (loopNum <= 0 || loopNum > 65536)
            {
                return "";
            }
            return Commands.BuildFrame("00", "27", "22" + loopNum.ToString("X4"));
        }

        public static string BuildStopReadFrame()
        {
            return Commands.BuildFrame("00", "28");
        }

        public static string BuildSetQueryFrame(int dr, int m, int TRext, int sel, int session, int target, int q)
        {
            int msb = dr << 7 | m << 5 | TRext << 4 | sel << 2 | session;
            int lsb = target << 7 | q << 3;
            string dataField = msb.ToString("X2") + lsb.ToString("X2");
            return Commands.BuildFrame("00", "0E", dataField);
        }

        public static string BuildGetQueryFrame()
        {
            return Commands.BuildFrame("00", "0D");
        }

        public static string BuildSetSelectFrame(int target, int action, int memBank, int pointer, int len, int truncated, string mask)
        {
            string dataField = string.Empty;
            dataField = (target << 5 | action << 2 | memBank).ToString("X2");
            dataField = dataField + pointer.ToString("X8") + len.ToString("X2");
            if (truncated == 128 || truncated == 1)
            {
                dataField += "80";
            }
            else
            {
                dataField += "00";
            }
            dataField += mask.Replace(" ", "");
            return Commands.BuildFrame("00", "0C", dataField);
        }

        public static string BuildGetSelectFrame()
        {
            return Commands.BuildFrame("00", "0B");
        }

        public static string BuildSetInventoryModeFrame(string mode)
        {
            return Commands.BuildFrame("00", "12", mode);
        }

        public static string BuildReadDataFrame(string accessPwd, int memBank, int sa, int dl)
        {
            if (accessPwd.Replace(" ", "").Length != 8)
            {
                return "";
            }
            string dataField = accessPwd.Replace(" ", "");
            dataField = dataField + memBank.ToString("X2") + sa.ToString("X4") + dl.ToString("X4");
            return Commands.BuildFrame("00", "39", dataField);
        }

        public static string BuildWriteDataFrame(string accessPwd, int memBank, int sa, int dl, string dt)
        {
            if (accessPwd.Replace(" ", "").Length != 8)
            {
                return "";
            }
            string dataField = accessPwd.Replace(" ", "");
            string text = dataField;
            dataField = string.Concat(new string[]
            {
                text,
                memBank.ToString("X2"),
                sa.ToString("X4"),
                dl.ToString("X4"),
                dt.Replace(" ", "")
            });
          
            return Commands.BuildFrame("00", "49", dataField);
        }

        public static Commands.lock_payload_type genLockPayload(byte lockOpt, byte memSpace)
        {
            Commands.lock_payload_type payload;
            payload.byte0 = 0;
            payload.byte1 = 0;
            payload.byte2 = 0;
            switch (memSpace)
            {
                case 0:
                    if (lockOpt == 0)
                    {
                        payload.byte0 |= 8;
                        payload.byte1 = payload.byte1;
                    }
                    else if (lockOpt == 1)
                    {
                        payload.byte0 |= 8;
                        payload.byte1 |= 2;
                    }
                    else if (lockOpt == 2)
                    {
                        payload.byte0 |= 12;
                        payload.byte1 |= 1;
                    }
                    else if (lockOpt == 3)
                    {
                        payload.byte0 |= 12;
                        payload.byte1 |= 3;
                    }
                    break;
                case 1:
                    if (lockOpt == 0)
                    {
                        payload.byte0 |= 2;
                        payload.byte2 = payload.byte2;
                    }
                    else if (lockOpt == 1)
                    {
                        payload.byte0 |= 2;
                        payload.byte2 |= 128;
                    }
                    else if (lockOpt == 2)
                    {
                        payload.byte0 |= 3;
                        payload.byte2 |= 64;
                    }
                    else if (lockOpt == 3)
                    {
                        payload.byte0 |= 3;
                        payload.byte2 |= 192;
                    }
                    break;
                case 2:
                    if (lockOpt == 0)
                    {
                        payload.byte1 |= 128;
                        payload.byte2 = payload.byte2;
                    }
                    else if (lockOpt == 1)
                    {
                        payload.byte1 |= 128;
                        payload.byte2 |= 32;
                    }
                    else if (lockOpt == 2)
                    {
                        payload.byte1 |= 192;
                        payload.byte2 |= 16;
                    }
                    else if (lockOpt == 3)
                    {
                        payload.byte1 |= 192;
                        payload.byte2 |= 48;
                    }
                    break;
                case 3:
                    if (lockOpt == 0)
                    {
                        payload.byte1 |= 32;
                        payload.byte2 = payload.byte2;
                    }
                    else if (lockOpt == 1)
                    {
                        payload.byte1 |= 32;
                        payload.byte2 |= 8;
                    }
                    else if (lockOpt == 2)
                    {
                        payload.byte1 |= 48;
                        payload.byte2 |= 4;
                    }
                    else if (lockOpt == 3)
                    {
                        payload.byte1 |= 48;
                        payload.byte2 |= 12;
                    }
                    break;
                case 4:
                    if (lockOpt == 0)
                    {
                        payload.byte1 |= 8;
                        payload.byte2 = payload.byte2;
                    }
                    else if (lockOpt == 1)
                    {
                        payload.byte1 |= 8;
                        payload.byte2 |= 2;
                    }
                    else if (lockOpt == 2)
                    {
                        payload.byte1 |= 12;
                        payload.byte2 |= 1;
                    }
                    else if (lockOpt == 3)
                    {
                        payload.byte1 |= 12;
                        payload.byte2 |= 3;
                    }
                    break;
            }
            return payload;
        }

        public static string BuildLockFrame(string accessPwd, int ld)
        {
            accessPwd = accessPwd.Replace(" ", "");
            if (accessPwd.Length != 8)
            {
                return "";
            }
            string dataField = accessPwd.Replace(" ", "");
            dataField += ld.ToString("X6");
            return Commands.BuildFrame("00", "82", dataField);
        }

        public static string BuildKillFrame(string killPwd, int rfu = 0)
        {
            killPwd = killPwd.Replace(" ", "");
            string dataField = killPwd;
            if (rfu != 0)
            {
                try
                {
                    dataField += rfu.ToString("X2");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Convert RFU Error! " + ex.Message);
                }
            }
            return Commands.BuildFrame("00", "65", dataField);
        }

        public static string BuildSetModemParaFrame(int mixerGain, int IFAmpGain, int signalThreshold)
        {
            string dataField = mixerGain.ToString("X2") + IFAmpGain.ToString("X2") + signalThreshold.ToString("X4");
            return Commands.BuildFrame("00", "F0", dataField);
        }

        public static string BuildReadModemParaFrame()
        {
            return Commands.BuildFrame("00", "F1");
        }

        public static string BuildScanJammerFrame()
        {
            return Commands.BuildFrame("00", "F2");
        }

        public static string BuildScanRssiFrame()
        {
            return Commands.BuildFrame("00", "F3");
        }

        public static string BuildIoControlFrame(byte optType, byte ioPort, byte modeOrLevel)
        {
            string strParam0 = optType.ToString("X2");
            string strParam = ioPort.ToString("X2");
            string strParam2 = modeOrLevel.ToString("X2");
            return Commands.BuildFrame("00", "1A", strParam0 + strParam + strParam2);
        }

        public static string BuildSetReaderEnvModeFrame(byte mode)
        {
            return Commands.BuildFrame("00", "F5", mode.ToString("X2"));
        }

        public static string BuildSaveConfigToNvFrame(byte NVenable)
        {
            return Commands.BuildFrame("00", "09", NVenable.ToString("X2"));
        }

        public static string BuildLoadConfigFromNvFrame()
        {
            return Commands.BuildFrame("00", "0A");
        }

        public static string BuildSetModuleSleepFrame()
        {
            return Commands.BuildFrame("00", "17");
        }

        public static string BuildSetSleepTimeFrame(byte time)
        {
            return Commands.BuildFrame("00", "1D", time.ToString("X2"));
        }

        public static string BuildInsertRfChFrame(int channelNum, byte[] channelList)
        {
            string param = channelNum.ToString("X2");
            if (channelList == null || channelList.Length == 0)
            {
                return "";
            }
            for (int i = 0; i < channelNum; i++)
            {
                param += channelList[i].ToString("X2");
            }
            return Commands.BuildFrame("00", "A9", param);
        }

        public static string BuildNXPChangeConfigFrame(string accessPwd, int ConfigData)
        {
            accessPwd = accessPwd.Replace(" ", "");
            if (accessPwd.Length != 8)
            {
                return "";
            }
            string dataField = accessPwd;
            dataField += ConfigData.ToString("X4");
            return Commands.BuildFrame("00", "E0", dataField);
        }

        public static string BuildNXPReadProtectFrame(string accessPwd, bool isReset)
        {
            accessPwd = accessPwd.Replace(" ", "");
            if (accessPwd.Length != 8)
            {
                return "";
            }
            string dataField = accessPwd;
            dataField += (isReset ? "01" : "00");
            return Commands.BuildFrame("00", "E1", dataField);
        }

        public static string BuildNXPChangeEasFrame(string accessPwd, bool isSet)
        {
            accessPwd = accessPwd.Replace(" ", "");
            if (accessPwd.Length != 8)
            {
                return "";
            }
            string dataField = accessPwd;
            dataField += (isSet ? "01" : "00");
            return Commands.BuildFrame("00", "E3", dataField);
        }

        public static string BuildNXPEasAlarmFrame()
        {
            return Commands.BuildFrame("00", "E4");
        }
    }
}
