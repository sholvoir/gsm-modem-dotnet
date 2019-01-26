using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Vultrue.Communication
{
    /// <summary>
    /// ����
    /// </summary>
    public class Message
    {
        #region ����

        /// <summary>
        /// ������Sim���е�λ��
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// ���ŵ�״̬
        /// </summary>
        public MessageState State { get; private set; }

        /// <summary>
        /// Ŀ��/Դ��ַ
        /// </summary>
        public string TerminalAddress { get; private set; }

        /// <summary>
        /// ���ű��뷽ʽ
        /// </summary>
        public DataCodingScheme DCS { get; private set; }

        /// <summary>
        /// ��������ʱ���
        /// </summary>
        public string ServiceCenterTimeStamp { get; private set; }

        /// <summary>
        /// ��������
        /// </summary>
        public string UserData { get; private set; }

        /// <summary>
        /// ��ȡSMS-SUBMIT����TPDU
        /// </summary>
        public string Tpdu
        {
            get
            {
                return string.Format("1100{0}00{1}A9{2}{3}", //TP-TOP TP-MR Э���ʶTP-PID(00Ϊ��ͨGSM�㵽�㷽ʽ) ������Ч����ϢTP-VP(A9Ϊ3��)
                    GSMAddress.GetGSMAddress(TerminalAddress), //Ŀ��绰����
                    ((int)DCS).ToString("X2"), //�û���Ϣ���뷽ʽTP-DCS, 04Ϊ8bit����, 08ΪUCS2(16bit)����
                    (UserData.Length >> 1).ToString("X2"), //�û������ֽ���, ���140
                    UserData); //�û����� ���140�ֽ�
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

        #region ����

        /// <summary>
        /// �ı�ģʽ�������
        /// </summary>
        /// <param name="index">�洢λ��</param>
        /// <param name="messageState">����״̬</param>
        /// <param name="terminalAddress">Ŀ��/Դ��ַ</param>
        /// <param name="serviceCenterTimeStamp">��������ʱ���</param>
        /// <param name="userData">��������</param>
        public Message(int index, MessageState messageState, string terminalAddress, string serviceCenterTimeStamp, string userData)
        {
            this.Index = index;
            this.State = messageState;
            this.TerminalAddress = terminalAddress;
            this.ServiceCenterTimeStamp = serviceCenterTimeStamp;
            this.UserData = userData;
        }

        /// <summary>
        /// PDUģʽ�������
        /// </summary>
        /// <param name="index">�洢λ��</param>
        /// <param name="messageState">����״̬</param>
        /// <param name="lenth">TPDU����</param>
        /// <param name="pdustr">pdu��������</param>
        public Message(int index, MessageState messageState, int lenth, string pdustr)
        {
            this.Index = index;
            this.State = messageState;
            Tpdu = pdustr.Substring(pdustr.Length - lenth * 2, lenth * 2);
        }

        /// <summary>
        /// ����ģʽ�������
        /// </summary>
        /// <param name="mobileNum">Ŀ�����</param>
        /// <param name="userData">��������(PDU)</param>
        /// <param name="encode">���뷽ʽ</param>
        public Message(string mobileNum, string userData, DataCodingScheme encode)
        {
            if (userData.Length > 280) throw new ArgumentException("�û����ݹ���");
            this.TerminalAddress = mobileNum;
            this.UserData = userData;
            this.DCS = encode;
        }

        #endregion
    }
}