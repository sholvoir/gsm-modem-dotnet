using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Vultrue.Communication
{
    partial class GSMModem
    {
        private static Regex regexRing = new Regex("^RING");
        private static Regex regexCMTI = new Regex("^\\+CMTI:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");
        private static Regex regexCREG = new Regex("^\\+CREG:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的来电事件的方法。
        /// </summary>
        public event EventHandler Ringing;

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的接收到短信事件的方法。
        /// </summary>
        public event EventHandler<MessageArrivaledEventArgs> MessageArrivaled;

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的网络注册信息已改变的方法。
        /// </summary>
        public event EventHandler<NetworkRegistrationChangedEventArgs> NetworkRegistrationChanged;

        private void unsolicitedDeal(string line)
        {
            Match match;
            if ((match = regexRing.Match(line)).Success)
            {
                if (Ringing != null) Ringing(this, EventArgs.Empty);
            }
            else if ((match = regexCMTI.Match(line)).Success)
            {
                if (MessageArrivaled != null)
                {
                    string[] result = match.Result("${ans}").Replace("\"", "").Split(',');
                    MessageArrivaled(this, new MessageArrivaledEventArgs(result[0], int.Parse(result[1])));
                }
            }
            else if ((match = regexCREG.Match(line)).Success)
            {
                if (NetworkRegistrationChanged != null)
                {
                    string[] result = match.Result("${ans}").Replace("\"", "").Split(',');
                    NetworkState state = (NetworkState)int.Parse(result[0]);
                    string locationAreaCode = result.Length > 1 ? result[1] : "";
                    string cellID = result.Length > 2 ? result[2] : "";
                    NetworkRegistrationChanged(this, new NetworkRegistrationChangedEventArgs(state, locationAreaCode, cellID));
                }
            }
        }
    }
}
