using System;

namespace RFID_Reader_Cmds
{
    public class StrArrEventArgs : EventArgs
    {
        private readonly string[] mData;

        public string[] Data
        {
            get
            {
                return this.mData;
            }
        }

        public StrArrEventArgs(string[] strArr)
        {
            this.mData = strArr;
        }
    }
}
