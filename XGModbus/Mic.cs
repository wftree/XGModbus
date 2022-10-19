using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XGModbus
{
    /// <summary>
    /// 辅助类
    /// </summary>
    public class Mic
    {
        /// <summary>
        /// 力矩到转速
        /// </summary>
        /// <param name="F"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static int ToQ(double F, int q)
        {
            return (int)(F * Math.Pow(2, q));
        }
        /// <summary>
        /// 力矩到力量
        /// </summary>
        /// <param name="Q"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static double ToF(int Q, int q)
        {
            return (double)(Q * Math.Pow(2, -q));
        }
    }
    /// <summary>
    /// 处理数据变化的代理
    /// </summary>
    /// <param name="from">原始值</param>
    /// <param name="to">变化后的数</param>
    public delegate void ValueChanged(float from, float to);
    /// <summary>
    /// 处理接受数据的代理
    /// </summary>
    /// <param name="no">量具编号</param>
    /// <param name="value">测量值</param>
    public delegate void ValueReceive(string no, float value);
}
