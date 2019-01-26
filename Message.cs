using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Vultrue.Communication
{
    /// <summary>
    /// 短信
    /// </summary>
    public class Message
    {
        #region 属性

        /// <summary>
        /// 短信在Sim卡中的位置
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 短信的状态
        /// </summary>
        public MessageState State { get; private set; }

        /// <summary>
        /// 目的/源地址
        /// </summary>
        public string TerminalAddress { get; private set; }

        /// <summary>
        /// 短信编码方式
        /// </summary>
        public DataCodingScheme DCS { get; private set; }

        /// <summary>
        /// 服务中心时间戳
        /// </summary>
        public string ServiceCenterTimeStamp { get; private set; }

        /// <summary>
        /// 短信内容
        /// </summary>
        public string UserData { get; private set; }

        /// <summary>
        /// 获取SMS-SUBMIT短信TPDU
        /// </summary>
        public string Tpdu
        {
            get
            {
                return string.Format("1100{0}00{1}A9{2}{3}", //TP-TOP TP-MR 协议标识TP-PID(00为普通GSM点到点方式) 短信有效期信息TP-VP(A9为3天)
                    GSMAddress.GetGSMAddress(TerminalAddress), //目标电话号码
                    ((int)DCS).ToString("X2"), //用户信息编码方式TP-DCS, 04为8bit编码, 08为UCS2(16bit)编码
                    (UserData.Length >> 1).ToString("X2"), //用户数据字节数, 最大140
                    UserData); //用户数据 最大140字节
            }
            set
            {
                int point = 0;
                int tptop = int.Parse(value.Substring(point, 2), NumberStyles.HexNumber);
                point += 2;
                int oalen = int.Parse(value.Substring(point, 2), NumberStyles.HexNumber);
                if (oalen % 2 != 0) oalen++;
                oalen += 4;
                TerminalAddress = GSMAddress.GetPhoneNumber(value.Substring(point, oalen));
                point += oalen;
                int tppid = int.Parse(value.Substring(point, 2), NumberStyles.HexNumber);
                point += 2;
                DCS = (DataCodingScheme)int.Parse(value.Substring(point, 2), NumberStyles.HexNumber);
                point += 2;
                StringBuilder scts = new StringBuilder(value.Substring(point, 14));
                for (int i = 0; i < 14; i += 2)
                {
                    char ch = scts[i];
                    scts[i] = scts[i + 1];
                    scts[i + 1] = ch;
                }
                int zone = int.Parse(scts.ToString(12, 1), NumberStyles.HexNumber);
                if (zone > 7) scts[12] = (zone - 8).ToString("X1")[0];
                scts.Insert(12, (zone > 7) ? "-" : "+").Insert(10, ':').Insert(8, ':');
                scts.Insert(6, ',').Insert(4, '/').Insert(2, '/').Insert(0, "20");
                ServiceCenterTimeStamp = scts.ToString();
                point += 14;
                int tpudl = int.Parse(value.Substring(point, 2), NumberStyles.HexNumber);
                point += 2;
                UserData = value.Substring(point, tpudl * 2);
            }
        }

        #endregion

        #region 构造

        /// <summary>
        /// 文本模式构造短信
        /// </summary>
        /// <param name="index">存储位置</param>
        /// <param name="messageState">短信状态</param>
        /// <param name="terminalAddress">目的/源地址</param>
        /// <param name="serviceCenterTimeStamp">服务中心时间戳</param>
        /// <param name="userData">短信内容</param>
        public Message(int index, MessageState messageState, string terminalAddress, string serviceCenterTimeStamp, string userData)
        {
            this.Index = index;
            this.State = messageState;
            this.TerminalAddress = terminalAddress;
            this.ServiceCenterTimeStamp = serviceCenterTimeStamp;
            this.UserData = userData;
        }

        /// <summary>
        /// PDU模式构造短信
        /// </summary>
        /// <param name="index">存储位置</param>
        /// <param name="messageState">短信状态</param>
        /// <param name="lenth">TPDU长度</param>
        /// <param name="pdustr">pdu短信内容</param>
        public Message(int index, MessageState messageState, int lenth, string pdustr)
        {
            this.Index = index;
            this.State = messageState;
            Tpdu = pdustr.Substring(pdustr.Length - lenth * 2, lenth * 2);
        }

        /// <summary>
        /// 发送模式构造短信
        /// </summary>
        /// <param name="mobileNum">目标号码</param>
        /// <param name="userData">短信内容(PDU)</param>
        /// <param name="encode">编码方式</param>
        public Message(string mobileNum, string userData, DataCodingScheme encode)
        {
            if (userData.Length > 280) throw new ArgumentException("用户数据过长");
            this.TerminalAddress = mobileNum;
            this.UserData = userData;
            this.DCS = encode;
        }

        #endregion
    }
}