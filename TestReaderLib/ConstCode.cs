using System;

namespace RFID_Reader_Cmds
{
    public class ConstCode
    {
        public enum NVconfig
        {
            NVenable = 1,
            NVdisable = 0
        }

        public const string FRAME_BEGIN_HEX = "BB";

        public const string FRAME_END_HEX = "7E";

        public const byte FRAME_BEGIN_BYTE = 187;

        public const byte FRAME_END_BYTE = 126;

        public const string FRAME_TYPE_CMD = "00";

        public const string FRAME_TYPE_ANS = "01";

        public const string FRAME_TYPE_INFO = "02";

        public const string CMD_GET_MODULE_INFO = "03";

        public const string CMD_SET_QUERY = "0E";

        public const string CMD_GET_QUERY = "0D";

        public const string CMD_INVENTORY = "22";

        public const string CMD_READ_MULTI = "27";

        public const string CMD_STOP_MULTI = "28";

        public const string CMD_READ_DATA = "39";

        public const string CMD_WRITE_DATA = "49";

        public const string CMD_LOCK_UNLOCK = "82";

        public const string CMD_KILL = "65";

        public const string CMD_SET_REGION = "07";

        public const string CMD_SET_RF_CHANNEL = "AB";

        public const string CMD_GET_RF_CHANNEL = "AA";

        public const string CMD_SET_POWER = "B6";

        public const string CMD_GET_POWER = "B7";

        public const string CMD_SET_FHSS = "AD";

        public const string CMD_SET_CW = "B0";

        public const string CMD_SET_MODEM_PARA = "F0";

        public const string CMD_READ_MODEM_PARA = "F1";

        public const string CMD_SET_SELECT_PARA = "0C";

        public const string CMD_GET_SELECT_PARA = "0B";

        public const string CMD_SET_INVENTORY_MODE = "12";

        public const string CMD_SCAN_JAMMER = "F2";

        public const string CMD_SCAN_RSSI = "F3";

        public const string CMD_IO_CONTROL = "1A";

        public const string CMD_RESTART = "19";

        public const string CMD_SET_READER_ENV_MODE = "F5";

        public const string CMD_INSERT_FHSS_CHANNEL = "A9";

        public const string CMD_SLEEP_MODE = "17";

        public const string CMD_SET_SLEEP_TIME = "1D";

        public const string CMD_LOAD_NV_CONFIG = "0A";

        public const string CMD_SAVE_NV_CONFIG = "09";

        public const string CMD_NXP_CHANGE_CONFIG = "E0";

        public const string CMD_NXP_READPROTECT = "E1";

        public const string CMD_NXP_RESET_READPROTECT = "E2";

        public const string CMD_NXP_CHANGE_EAS = "E3";

        public const string CMD_NXP_EAS_ALARM = "E4";

        public const string CMD_EXE_FAILED = "FF";

        public const string FAIL_INVALID_PARA = "0E";

        public const string FAIL_INVENTORY_TAG_TIMEOUT = "15";

        public const string FAIL_INVALID_CMD = "17";

        public const string FAIL_FHSS_FAIL = "20";

        public const string FAIL_ACCESS_PWD_ERROR = "16";

        public const string FAIL_READ_MEMORY_NO_TAG = "09";

        public const string FAIL_READ_ERROR_CODE_BASE = "A0";

        public const string FAIL_WRITE_MEMORY_NO_TAG = "10";

        public const string FAIL_WRITE_ERROR_CODE_BASE = "B0";

        public const string FAIL_LOCK_NO_TAG = "13";

        public const string FAIL_LOCK_ERROR_CODE_BASE = "C0";

        public const string FAIL_KILL_NO_TAG = "12";

        public const string FAIL_KILL_ERROR_CODE_BASE = "D0";

        public const string FAIL_NXP_CHANGE_CONFIG_NO_TAG = "1A";

        public const string FAIL_NXP_READPROTECT_NO_TAG = "2A";

        public const string FAIL_NXP_RESET_READPROTECT_NO_TAG = "2B";

        public const string FAIL_NXP_CHANGE_EAS_NO_TAG = "1B";

        public const string FAIL_NXP_CHANGE_EAS_NOT_SECURE = "1C";

        public const string FAIL_NXP_EAS_ALARM_NO_TAG = "1D";

        public const string FAIL_CUSTOM_CMD_BASE = "E0";

        public const int ERROR_CODE_OTHER_ERROR = 0;

        public const int ERROR_CODE_MEM_OVERRUN = 3;

        public const int ERROR_CODE_MEM_LOCKED = 4;

        public const int ERROR_CODE_INSUFFICIENT_POWER = 11;

        public const int ERROR_CODE_NON_SPEC_ERROR = 15;

        public const string SUCCESS_MSG_DATA = "00";

        public const string REGION_CODE_CHN2 = "01";

        public const string REGION_CODE_US = "02";

        public const string REGION_CODE_EUR = "03";

        public const string REGION_CODE_CHN1 = "04";

        public const string REGION_CODE_JAPAN = "05";

        public const string REGION_CODE_KOREA = "06";

        public const string SET_ON = "FF";

        public const string SET_OFF = "00";

        public const string INVENTORY_MODE0 = "00";

        public const string INVENTORY_MODE1 = "01";

        public const string INVENTORY_MODE2 = "02";

        public const string MODULE_HARDWARE_VERSION_FIELD = "00";

        public const string MODULE_SOFTWARE_VERSION_FIELD = "01";

        public const string MODULE_MANUFACTURE_INFO_FIELD = "02";
    }
}
