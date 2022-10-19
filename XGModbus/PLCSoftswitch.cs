using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XGModbus
{
    /// <summary>
    /// 通用的软元类
    /// </summary>
    public abstract class PLCSoftSwitch
    {
        public PLCSoftSwitch(string classname,int no)
        {
            this.classname = classname;
            this.no = no;
        }
        string classname;
        int no;
        int value;
        //int address;
        /// <summary>
        /// PLC软元类型
        /// </summary>
        public string Classname { get => classname; set => classname = value; }
        /// <summary>
        /// PLC软元编号
        /// </summary>
        public int No { get => no; set => no = value; }
        /// <summary>
        /// PLC软元数据
        /// </summary>
        public int Value { get => value; set => this.value = value; }
        /// <summary>
        /// PLC软元地址,由实际实现的类进行明确。
        /// </summary>
        public abstract int Address { get; }

        
    }
    public class FXDSoftSwitch:PLCSoftSwitch
    {
        public FXDSoftSwitch(int no):base("D",no)
        {

        }
        /// <summary>
        /// PLC软元地址,由实际实现的类进行明确。
        /// </summary>

        public override int Address
        {
            get
            {
                return 4096 + No * 2;
            }
        }
    }
    /// <summary>
    /// M读，一次读出16个M位
    /// </summary>
    public class FXMGroupSoftSwitch : PLCSoftSwitch
    {
        public FXMGroupSoftSwitch(int no):base("MG",no)
        {

        }
        /// <summary>
        /// PLC软元地址,由实际实现的类进行明确。
        /// </summary>

        public override int Address
        {
            get
            {
                return 256 + No;
            }
        }
    }
    public class FXDSpecialSoftSwitch : PLCSoftSwitch
    {
        public FXDSpecialSoftSwitch(int no):base("DS",no)
        {

        }
        /// <summary>
        /// PLC软元地址,由实际实现的类进行明确。
        /// </summary>

        public override int Address
        {
            get
            {
                return 3584 + No * 2;
            }
        }
    }
    /// <summary>
    /// M位写
    /// </summary>
    public class FXMSingleSoftSwitch : PLCSoftSwitch
    {
        public FXMSingleSoftSwitch(int no):base("MS",no)
        {

        }
        /// <summary>
        /// PLC软元地址,由实际实现的类进行明确。
        /// </summary>

        public override int Address
        {
            get
            {
                return 2048 + No;
            }
        }
    }
    public class Plc
    {
        XGFXPlc mb = null;
        public Plc(XGFXPlc mb)
        {
            this.mb = mb;
        }
        public PLCSoftSwitch GetValue(PLCSoftSwitch pss)
        {
            if(mb.Read(ref pss))
            {
                return pss;
            }
            return null;
        }
    }
}
