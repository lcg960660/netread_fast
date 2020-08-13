using System;

namespace RFID_Reader_Com
{
    public class byteArrEventArgs : EventArgs
    {
        private readonly byte[] mData;

        public byte[] Data
        {
            get
            {
                return this.mData;
            }
        }

        public byteArrEventArgs(byte[] byteArr)
        {
            this.mData = byteArr;
        }
    }
}

