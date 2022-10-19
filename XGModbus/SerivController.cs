using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XGModbus
{
    /// <summary>
    /// 提供伺服电机操作方法，是对modbus的包装
    /// </summary>
    public class ServiController
    {
        XGModbus mb = null;
        /// <summary>
        /// 公开构造函数，获得唯一的modbus
        /// </summary>
        /// <param name="mb"></param>
        public ServiController(XGModbus mb)
        {
            this.mb = mb;
        }
        /// <summary>
        /// 设置力矩，正负代表方向
        /// </summary>
        /// <param name="t"></param>
        public void SetT(double t)
        {
            byte address = 1;
            ushort start = 0x1C28;
            short[] value = new short[1];
            value[0] = (short)((t * 1158.3488) / (200 * 0.25));
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
        /// <summary>
        /// 获得力矩
        /// </summary>
        /// <returns></returns>
        public double GetRealT()
        {
            short[] values = new short[1];
            mb.SendFc3(1, 0x1C6F, 1, ref values);
            return (values[0] * 0.25 * 200) / 1158.3488;
        }
        /// <summary>
        /// 获得工作模式
        /// </summary>
        /// <returns></returns>
        public short GetWorkMod()
        {
            short[] values = new short[1];
            mb.SendFc3(1, 0x1243, 1, ref values);
            return values[0];
        }
        /// <summary>
        /// 设置工作模式
        /// </summary>
        /// <param name="i">
        ///0 转矩控制模式
        ///1 速度控制模式
        ///2 位置控制模式
        ///3 JOG控制模式
        ///4 速度试运行模式
        ///5 自动校正参数模式
        ///6 演示模式
        ///7混合控制模式
        /// </param>
        public void SetWorkMod(short i)
        {
            byte address = 1;
            ushort start = 0x1243;
            short[] value = new short[1];
            value[0] = i;
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
        /// <summary>
        /// 该参数用于选择转矩、速度、位置控制模式下的指令接口类型。参数定义如下：
        ///        参数值 定义
        ///0 同步指令无效， DSP 仅接收传统的 IO 控制方式，不接收同步指令
        ///1 厂家参数，请勿使用
        ///2 同步指令有效， DSP 接收总线同步指令，不再接收传统的 IO 控制方式
        /// </summary>
        /// <returns></returns>
        public bool GetBusSync()
        {
            short[] values = new short[1];
            mb.SendFc3(1, 0x1c27, 1, ref values);
            if (values[0] == 0)
                return false;
            else
                return true;
        }
        /// <summary>
        /// 同步指令有效
        /// </summary>
        public void EnableBusSync()
        {
            byte address = 1;
            ushort start = 0x1c27;
            short[] value = new short[1];
            value[0] = 2;
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
        /// <summary>
        /// 同步指令无效
        /// </summary>
        public void DisableBusSync()
        {
            byte address = 1;
            ushort start = 0x1c27;
            short[] value = new short[1];
            value[0] = 0;
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
        /// <summary>
        /// 设置转速，正负代表方向
        /// </summary>
        /// <param name="rpm"></param>
        public void SetRPM(double rpm)
        {
            byte address = 1;
            ushort start = 0x1c29;
            short[] value = new short[1];
            value[0] = (short)(Mic.ToQ(rpm, 2));
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }

        //public void SetM(double M)
        //{
        //    byte address = 1;
        //    ushort start = 0x1c28;
        //    short[] value = new short[1];

        //    value[0] = (short)(Mic.ToQ(M, 2));
        //    while (!SendFc16(address, start, (ushort)1, value)) ;
        //}

        /// <summary>
        /// 触发同步指令
        /// </summary>
        public void ActiveCommand()
        {
            byte address = 1;
            ushort start = 0x1c55;
            short[] value = new short[1];
            value[0] = 0x55AA;
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
        /// <summary>
        /// 得到地址
        /// </summary>
        /// <returns></returns>
        public int GetLoc()
        {
            short[] highvalues = new short[1];
            mb.SendFc3(1, 0x1C71, 1, ref highvalues);
            short[] lowvalues = new short[1];
            mb.SendFc3(1, 0x1C72, 1, ref lowvalues);
            return (ushort)(highvalues[0] + lowvalues[0]);//BitConverter.ToInt32(values, 0);
        }
        /// <summary>
        /// 启动伺服电机驱动器
        /// </summary>
        public void EnableRun()
        {
            byte address = 1;
            ushort start = 0x1247;
            short[] value = new short[1];
            value[0] = 32;
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
        /// <summary>
        /// 关闭伺服电机驱动器
        /// </summary>
        public void DisableRun()
        {
            byte address = 1;
            ushort start = 0x1247;
            short[] value = new short[1];
            value[0] = 0;
            while (!mb.SendFc16(address, start, (ushort)1, value))
                ;
        }
    }
    
}
