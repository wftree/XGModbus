using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace XGModbus
{
    [Serializable]
    public class 串口
    {
        string portName;
        int baudRate;
        Parity parity;
        int dataBits;
        StopBits stopBits;

        public string PortName
        {
            get
            {
                return portName;
            }

            set
            {
                this.portName = value;
            }
        }

        public int BaudRate
        {
            get
            {
                return baudRate;
            }

            set
            {
                this.baudRate = value;
            }
        }

        public Parity Parity
        {
            get
            {
                return parity;
            }

            set
            {
                this.parity = value;
            }
        }

        public int DataBits
        {
            get
            {
                return dataBits;
            }

            set
            {
                this.dataBits = value;
            }
        }

        public StopBits StopBits
        {
            get
            {
                return stopBits;
            }

            set
            {
                this.stopBits = value;
            }
        }
        public 串口()
        {

        }
        public 串口(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;

        }
    }
}
