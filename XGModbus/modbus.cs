using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace XGModbus
{
    /// <summary>
    /// 串口类型基础类
    /// </summary>
    public class SerialDevice
    {
        public string PortName { get => sp.PortName; set => sp.PortName = value; }
        public int BaudRate { get => sp.BaudRate; set => sp.BaudRate = value; }
        public int DataBits { get => sp.DataBits; set => sp.DataBits = value; }
        public Parity Parity { get => sp.Parity; set => sp.Parity = value; }
        public StopBits StopBits { get => sp.StopBits; set => sp.StopBits = value; }
        public bool IsOpen { get => sp.IsOpen; }
        ~SerialDevice()
        {
            Close();
        }
        private SerialPort sp = new SerialPort();
        /// <summary>
        /// 串口状态文字
        /// </summary>
        public string DeviceStatus;

        protected SerialPort Sp { get => sp; set => sp = value; }

        #region Open / Close Procedures
        public bool Open(串口 ssp)
        {
            return Open(ssp.PortName, ssp.BaudRate, ssp.DataBits, ssp.Parity, ssp.StopBits);
        }
        /// <summary>
        /// 打开串口的通用方法，不打开RTS和DTS。
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="databits"></param>
        /// <param name="parity"></param>
        /// <param name="stopBits"></param>
        /// <returns></returns>
        public bool Open(string portName, int baudRate, int databits, Parity parity, StopBits stopBits)
        {
            return Open(portName, baudRate, databits, parity, stopBits, false, false);
        }
        /// <summary>
        /// 打开串口的通用方法。
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="databits"></param>
        /// <param name="parity"></param>
        /// <param name="stopBits"></param>
        /// <param name="rts"></param>
        /// <param name="dtr"></param>
        /// <returns></returns>
        public bool Open(string portName, int baudRate, int databits, Parity parity, StopBits stopBits, bool rts, bool dtr)
        {
            //Ensure port isn't already opened:
            if (!Sp.IsOpen)
            {
                //Assign desired settings to the serial port:
                Sp.PortName = portName;
                Sp.BaudRate = baudRate;
                Sp.DataBits = databits;
                Sp.Parity = parity;
                Sp.StopBits = stopBits;
                //These timeouts are default and cannot be editted through the class at this point:
                Sp.ReadTimeout = 1000;
                Sp.WriteTimeout = 1000;
                Sp.RtsEnable = rts;
                Sp.DtrEnable = dtr;
                try
                {
                    Sp.Open();
                }
                catch (Exception err)
                {
                    DeviceStatus = "Error opening " + portName + ": " + err.Message;
                    return false;
                }
                DeviceStatus = portName + " opened successfully";
                return true;
            }
            else
            {
                DeviceStatus = portName + " already opened";
                return false;
            }
        }
        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <returns></returns>
        public bool Close()
        {
            //Ensure port is opened before attempting to close:
            if (Sp.IsOpen)
            {
                try
                {
                    Sp.Close();
                }
                catch (Exception err)
                {
                    DeviceStatus = "Error closing " + Sp.PortName + ": " + err.Message;
                    return false;
                }
                DeviceStatus = Sp.PortName + " closed successfully";
                return true;
            }
            else
            {
                DeviceStatus = Sp.PortName + " is not open";
                return false;
            }
        }
        #endregion

    }
    /// <summary>
    /// 成量zigbee串口量具类
    /// 9600波特率，8数据位，1结束位，无校验位。一帧数据由11字节组成，’#’开始，2、3字节为发射器地址，4-10字节为测量数据，11字节为0xd
    /// 实际接收数据帧为10字节组成，前两个字节是发射器地址，第3字节为正负，后面7个字节是数字
    /// </summary>
    public class MeasuringTool: SerialDevice
    {
        //static SerialDevice serialDevice;
        //public static SerialDevice SerialDevice { get => SerialDevice; set => SerialDevice = value; }
        Timer timer = new Timer(500);
        public MeasuringTool():base()
        {

            
            Sp.DataReceived += Sp_DataReceived;
        }
        ~MeasuringTool()
        {

        }

        public bool Open(string portName, int baudRate, int databits)
        {
            Sp.ReceivedBytesThreshold = 10;
            //为了触发接收事件，后面的DTS必须设定为true。在modbus中不能打开，但是这种情况必须打开。
            return Open(portName, baudRate, databits, Parity.None, StopBits.One,true,true);
        }

        public event ValueReceive ValueReceive;


        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            bool isready = false;
            byte[] response = new byte[10];
            lock (this)
            {
                isready = false;
                for (int t = 0; t < response.Length; t++)
                {
                    response[0] = (byte)(Sp.ReadByte());
                    if (true)//#开始response[0] == 0x23
                    {
                        for (int i = 1; i < response.Length; i++)
                        {
                            response[i] = (byte)(Sp.ReadByte());
                        }
                        isready = true;
                        break;
                    }
                }
                if(isready)
                {
                    string no = Convert.ToString(response[0])+ Convert.ToString(response[1]); 
                    float value = Convert.ToSingle(Encoding.ASCII.GetString(response, 2, 8));
                    ValueReceive(no, value);
                }
            }
        }
    }
    /// <summary>
    /// 实现基本的modbus功能
    /// </summary>
    public class ModbusBase:SerialDevice
    {
        #region CRC Computation
        protected void GetCRC(byte[] message, ref byte[] CRC)
        {
            //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
            //return the CRC values:

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }
        #endregion

        #region Check Response
        protected bool CheckResponse(byte[] response)
        {
            //Perform a basic CRC check:
            byte[] CRC = new byte[2];
            GetCRC(response, ref CRC);
            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                return true;
            else
                return false;
        }
        #endregion

        #region Get Response
        protected void GetResponse(ref byte[] response)
        {
            //sp.Read(response, 0, response.Length);
            //response =Convert.ToByte( sp.ReadLine()
            //There is a bug in .Net 2.0 DataReceived Event that prevents people from using this
            //event as an interrupt to handle data (it doesn't fire all of the time).  Therefore
            //we have to use the ReadByte command for a fixed length as it's been shown to be reliable.

            //这里出现了一个错误，有些modbus设备并不是用80H作为开始的，必须要处理的前面的多余字符
            for (int t = 0; t < response.Length; t++)
            {
                response[0] = (byte)(Sp.ReadByte());
                if (response[0] == 128)
                {
                    for (int i = 1; i < response.Length; i++)
                    {
                        response[i] = (byte)(Sp.ReadByte());
                    }
                    break;
                }
            }


        }
        /// <summary>
        /// ACSII下读取换行符号算是结束
        /// </summary>
        /// <param name="response"></param>
        protected void GetResponse(ref List<byte> response)
        {
            while (true)
            {
                response.Add((byte)Sp.ReadByte());
                if (response.Last() == 0x0A)
                {
                    break;
                }
            }
        }
        #endregion
    }
    /// <summary>
    /// 实现ModBus协议的串口通讯类，目前还没有实现线程安全，务必注意多下位机的情况下可能出现串口错误。
    /// 需要对每一个读写操作原子化
    /// </summary>
    public class XGModbus : ModbusBase
    {

        #region Constructor / Deconstructor
        public XGModbus()
        {
        }
        ~XGModbus()
        {
        }
        #endregion

        #region Open / Close Procedures
        /// <summary>
        /// 按照博美德驱动器协议打开串口
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <returns>是否成功</returns>
        public bool OpenBMServi(string portName)
        {
            return Open(portName, 115200, 8, Parity.Odd, StopBits.One);
        }
        #endregion

        #region Build Message

        private void BuildMessage(byte address, byte type, ushort start, ushort registers, ref byte[] message)
        {
            //Array to receive CRC bytes:
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = type;
            message[2] = (byte)(start >> 8);
            message[3] = (byte)start;
            message[4] = (byte)(registers >> 8);
            message[5] = (byte)registers;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }
        #endregion

        #region Function 16 - Write Multiple Registers
        /// <summary>
        /// Function 16 - Write Multiple Registers
        /// </summary>
        /// <param name="address"></param>
        /// <param name="start"></param>
        /// <param name="registers"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool SendFc16(byte address, ushort start, ushort registers, short[] values)
        {
            //Ensure port is open:
            if (Sp.IsOpen)
            {
                //Clear in/out buffers:
                Sp.DiscardOutBuffer();
                Sp.DiscardInBuffer();
                //Message is 1 addr + 1 fcn + 2 start + 2 reg + 1 count + 2 * reg vals + 2 CRC
                byte[] message = new byte[9 + 2 * registers];
                //Function 16 response is fixed at 8 bytes
                byte[] response = new byte[8];

                //Add bytecount to message:
                message[6] = (byte)(registers * 2);
                //Put write values into message prior to sending:
                for (int i = 0; i < registers; i++)
                {
                    message[7 + 2 * i] = (byte)(values[i] >> 8);
                    message[8 + 2 * i] = (byte)(values[i]);
                }
                //Build outgoing message:
                BuildMessage(address, (byte)16, start, registers, ref message);

                //Send Modbus message to Serial Port:
                try
                {
                    Sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    DeviceStatus = "Error in write event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    DeviceStatus = "Write successful";
                    return true;
                }
                else
                {
                    DeviceStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                DeviceStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

        #region Function 3 - Read Registers
        /// <summary>
        /// 读取寄存器
        /// </summary>
        /// <param name="address">设备地址0~255</param>
        /// <param name="start">寄存器起始地址</param>
        /// <param name="registers">寄存器个数</param>
        /// <param name="values">读取结果</param>
        /// <returns></returns>
        public bool SendFc3(byte address, ushort start, ushort registers, ref short[] values)
        {
            //Ensure port is open:
            if (Sp.IsOpen)
            {
                //Clear in/out buffers:
                Sp.DiscardOutBuffer();
                Sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];
                //Function 3 response buffer:
                byte[] response = new byte[5 + 2 * registers];
                //Build outgoing modbus message:
                BuildMessage(address, (byte)3, start, registers, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    Sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    DeviceStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    //Return requested register values:
                    for (int i = 0; i < (response.Length - 5) / 2; i++)
                    {
                        values[i] = response[2 * i + 3];
                        values[i] <<= 8;
                        values[i] += response[2 * i + 4];
                    }
                    DeviceStatus = "Read successful";
                    return true;
                }
                else
                {
                    DeviceStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                DeviceStatus = "Serial port not open";
                return false;
            }

        }
        /// <summary>
        /// 读取寄存器,直接返回字节
        /// </summary>
        /// <param name="address">设备地址0~255</param>
        /// <param name="start">寄存器起始地址</param>
        /// <param name="registers">寄存器个数</param>
        /// <param name="values">读取结果</param>
        /// <returns></returns>
        public bool SendFc3(byte address, ushort start, ushort registers, ref byte[] values)
        {
            //Ensure port is open:
            if (Sp.IsOpen)
            {
                //Clear in/out buffers:
                Sp.DiscardOutBuffer();
                Sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];
                //Function 3 response buffer:
                byte[] response = new byte[5 + 2 * registers];
                //Build outgoing modbus message:
                BuildMessage(address, (byte)3, start, registers, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    Sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    DeviceStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    //Return requested register values:
                    for (int i = 0; i < (response.Length - 5); i++)
                    {
                        values[i] = response[i + 3];
                    }
                    DeviceStatus = "Read successful";
                    return true;
                }
                else
                {
                    DeviceStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                DeviceStatus = "Serial port not open";
                return false;
            }

        }
        /// <summary>
        /// 获取电子秤ieee754数据
        /// </summary>
        /// <param name="address"></param>
        /// <param name="start"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool SendFc3(byte address, ushort start, ref float values)
        {
            ushort registers = 2;
            //Ensure port is open:
            if (Sp.IsOpen)
            {
                //Clear in/out buffers:
                Sp.DiscardOutBuffer();
                Sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];
                //Function 3 response buffer:
                byte[] response = new byte[5 + 2 * registers];
                //Build outgoing modbus message:
                BuildMessage(address, (byte)3, start, registers, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    Sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    DeviceStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    byte[] v = new byte[4] { response[6], response[5], response[4], response[3] };
                    //Return requested register values:
                    values = BitConverter.ToSingle(v, 0);
                    DeviceStatus = "Read successful";
                    return true;
                }
                else
                {
                    DeviceStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                DeviceStatus = "Serial port not open";
                return false;
            }

        }
        #endregion

    }
    /// <summary>
    /// 三菱PLC的通讯类
    /// </summary>
    public class XGFXPlc : ModbusBase
    {
        #region CRC Computation
        internal new void GetCRC(byte[] message, ref byte[] CRC)
        {
            int temp = 0;
            for (int i = 1; i < message.Length - 2; i++)
            {
                temp += message[i];
            }
            string tempstr = Convert.ToString(temp, 16).PadLeft(4, '0').ToUpper();
            CRC[0] = (byte)tempstr[2];
            CRC[1] = (byte)tempstr[3];
        }
        #endregion

        #region Check Response
        internal new bool CheckResponse(byte[] response)
        {
            //Perform a basic CRC check:
            byte[] CRC = new byte[2];
            GetCRC(response, ref CRC);
            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                return true;
            else
                return false;
        }
        #endregion

        #region Open / Close Procedures
        /// <summary>
        /// 按照三菱FXPLC协议打开串口
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <returns>是否成功</returns>
        public bool OpenFXPlc(string portName)
        {
            return Open(portName, 9600, 7, Parity.Even, StopBits.One);
        }
        #endregion

        #region Build Message

        private void BuildReadMessage(PLCSoftSwitch pss, ref byte[] message)
        {

            //Array to receive CRC bytes:
            byte[] CRC = new byte[2];
            //02 30 31 30 46 36 30 34 03 37 34 

            string tempstring = Convert.ToString(pss.Address, 16).ToUpper().PadLeft(4, '0');
            message[0] = 0x02;
            message[1] = 0x30;
            message[2] = (byte)tempstring[0];
            message[3] = (byte)tempstring[1];
            message[4] = (byte)tempstring[2];
            message[5] = (byte)tempstring[3];
            message[6] = 0x30;
            message[7] = 0x34;
            message[8] = 0x03;

            GetCRC(message, ref CRC);
            message[9] = CRC[0];
            message[10] = CRC[1];
        }
        private void BuildWriteMessage(PLCSoftSwitch pss, ref byte[] message)
        {

            //Array to receive CRC bytes:
            byte[] CRC = new byte[2];
            //02 30 31 30 46 36 30 34 03 37 34 

            string tempstring = Convert.ToString(pss.Address, 16).ToUpper().PadLeft(4, '0');
            string tempvalue = Convert.ToString(pss.Value, 16).ToUpper().PadLeft(4, '0');
            message[0] = 0x02;
            message[1] = 0x31;
            message[2] = (byte)tempstring[0];
            message[3] = (byte)tempstring[1];
            message[4] = (byte)tempstring[2];
            message[5] = (byte)tempstring[3];
            message[6] = 0x30;
            message[7] = 0x32;
            message[8] = (byte)tempvalue[2];
            message[9] = (byte)tempvalue[3];
            message[10] = (byte)tempvalue[0];
            message[11] = (byte)tempvalue[1];
            message[12] = 0x03;

            GetCRC(message, ref CRC);
            message[13] = CRC[0];
            message[14] = CRC[1];
        }
        #endregion

        #region 软元读取
        /// <summary>
        /// 读取特定软元的数值，如果M就是0，1
        /// </summary>
        /// <param name="pss"></param>
        /// <returns>是否成功读取数据</returns>
        public bool Read(ref PLCSoftSwitch pss)
        {
            lock (this)
            {
                if (Sp.IsOpen)
                {
                    //Clear in/out buffers:
                    Sp.DiscardOutBuffer();
                    Sp.DiscardInBuffer();
                    //Function 3 request is always 8 bytes:
                    byte[] message = new byte[11];
                    //Function 3 response buffer:
                    byte[] response = new byte[12];
                    //Build outgoing modbus message:
                    BuildReadMessage(pss, ref message);
                    //Send modbus message to Serial Port:
                    try
                    {
                        Sp.Write(message, 0, message.Length);
                        GetResponse(ref response);
                    }
                    catch (Exception err)
                    {
                        DeviceStatus = "Error in read event: " + err.Message;
                        return false;
                    }
                    //Evaluate message:
                    if (CheckResponse(response))
                    {
                        string temp = "";
                        temp += (char)response[3];
                        temp += (char)response[4];
                        temp += (char)response[1];
                        temp += (char)response[2];
                        pss.Value = Int32.Parse(temp, System.Globalization.NumberStyles.HexNumber);
                        DeviceStatus = "Read successful";
                        return true;
                    }
                    else
                    {
                        DeviceStatus = "CRC error";
                        return false;
                    }
                }
                else
                {
                    DeviceStatus = "Serial port not open";
                    return false;
                }
            }
            
        }
        #endregion

        #region 软元写入
        public bool Write(PLCSoftSwitch pss)
        {
            lock (this)
            {
                if (Sp.IsOpen)
                {
                    //Clear in/out buffers:
                    Sp.DiscardOutBuffer();
                    Sp.DiscardInBuffer();
                    //Function 3 request is always 8 bytes:
                    byte[] message = new byte[15];
                    //Function 3 response buffer:
                    byte[] response = new byte[1];
                    //Build outgoing modbus message:
                    BuildWriteMessage(pss, ref message);
                    //Send modbus message to Serial Port:
                    try
                    {
                        Sp.Write(message, 0, message.Length);
                        GetResponse(ref response);
                    }
                    catch (Exception err)
                    {
                        DeviceStatus = "Error in write event: " + err.Message;
                        return false;
                    }
                    //Evaluate message:
                    if (response[0] == 0x6)
                    {
                        DeviceStatus = "Write successful";
                        return true;
                    }
                    else
                    {
                        DeviceStatus = "Write error";
                        return false;
                    }
                }
                else
                {
                    DeviceStatus = "Serial port not open";
                    return false;
                }
            }
            
        }
        #endregion
    }
    
}
