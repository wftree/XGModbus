using System;
using System.Timers;

namespace XGModbus
{
    
    /// <summary>
    /// 台湾EEE数字百分表
    /// </summary>
    public class CheckHub:XGModbus
    {
        public event ValueChanged Value0Changed;
        public event ValueChanged Value1Changed;
        public event ValueChanged Value2Changed;
        public event ValueChanged Value3Changed;
        Timer timer = new Timer(500);
        public CheckHub():base()
        {
            timer.Stop();
            timer.Elapsed += Timer_Elapsed;
        }
        ~CheckHub()
        {
            timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                
                Get4Values();
                timer.Start();
            }
            catch (Exception ex)
            {
                DeviceStatus = ex.Message;
                
                Close();
                throw ex;
            }
            
        }

        byte start =0;
        /// <summary>
        /// HUB的起始位
        /// </summary>
        public byte Start { get => start; set => start = value; }
        /// <summary>
        /// HUB的地址
        /// </summary>
        public byte Address { get => address; set => address = value; }
        public float Value0 { get => value0; }
        public float Value1 { get => value1; }
        public float Value2 { get => value2;  }
        public float Value3 { get => value3;  }

        byte address =128;
        float value0;
        float value1;
        float value2;
        float value3;
        /// <summary>
        /// 单独得到一次测量数据
        /// </summary>
        /// <returns>4个端口的数据</returns>
        protected float[] Get4Values()
        {
            lock (this)
            {
                byte[] values = new byte[16];
                float[] result = new float[4];
                if (SendFc3(Address, Start, 8, ref values))
                {
                    
                    for (int i = 0; i < 3; i++)
                    {
                        byte[] v = new byte[2] { values[3 + 4 * i], values[2 + 4 * i] };
                        //Return requested register values:
                        result[i] = Convert.ToSingle(BitConverter.ToInt16(v, 0)) / 1000;
                        if (values[4 * i] > 0)
                        {
                            result[i] = result[i] * -1;
                        }
                    }
                    if (value0 != result[0])
                    {
                        Value0Changed(value0, result[0]);
                        value0 = result[0];
                    }
                    if (value1 != result[1])
                    {
                        Value1Changed(value1, result[1]);
                        value1 = result[1];
                    }
                    if (value2 != result[2])
                    {
                        Value2Changed(value2, result[2]);
                        value2 = result[2];
                    }
                    if (value3 != result[3])
                    {
                        Value3Changed(value3, result[3]);
                        value3 = result[3];
                    }
                }
                return result;
            }
            


        }
        public bool Open(string portName, int baudRate, int databits)
        {
            if (Open(portName, baudRate, databits, System.IO.Ports.Parity.Even, System.IO.Ports.StopBits.One))
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
            return timer.Enabled;
        }

        new bool Close()
        {
            timer.Stop();
            return base.Close();
            
        }
        
    }
}
