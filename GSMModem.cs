using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.IO.Ports;

namespace Vultrue.Communication
{
    public partial class GSMModem : Component
    {
        #region 变量
        private const int BUFFERSIZE = 1024;
        private byte[] data = new byte[BUFFERSIZE];
        private Thread readThread;

        private const int MAXNOTELETLENTH = 280;
        private bool isopen;
        private List<byte> buffer = new List<byte>(127);

        private ITask currentTask = null;
        private AutoResetEvent taskToken = new AutoResetEvent(true);
        private AutoResetEvent taskReadWriteLock = new AutoResetEvent(false);
        private bool istaskruning = false;
        #endregion

        #region 构造

        public GSMModem()
        {
            InitializeComponent();
        }

        public GSMModem(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 读取或设置端口名称
        /// </summary>
        [DefaultValue("COM1")]
        public string PortName
        {
            get { return serialPort.PortName; }
            set { serialPort.PortName = value; }
        }

        /// <summary>
        /// 读取或设置波特率
        /// </summary>
        [DefaultValue(19200)]
        public int BaudRate
        {
            get { return serialPort.BaudRate; }
            set { serialPort.BaudRate = value; }
        }

        /// <summary>
        /// 读取或设置奇偶校验位
        /// </summary>
        [DefaultValue(Parity.None)]
        public Parity Parity
        {
            get { return serialPort.Parity; }
            set { serialPort.Parity = value; }
        }

        /// <summary>
        /// 读取或设置数据位
        /// </summary>
        [DefaultValue(8)]
        public int DataBits
        {
            get { return serialPort.DataBits; }
            set { serialPort.DataBits = value; }
        }

        /// <summary>
        /// 读取或设置停止位
        /// </summary>
        [DefaultValue(StopBits.One)]
        public StopBits StopBits
        {
            get { return serialPort.StopBits; }
            set { serialPort.StopBits = value; }
        }

        /// <summary>
        /// 读取或设置握手方式
        /// </summary>
        [DefaultValue(Handshake.RequestToSend)]
        public Handshake Handshake
        {
            get { return serialPort.Handshake; }
            set { serialPort.Handshake = value; }
        }

        /// <summary>
        /// Modem设置
        /// </summary>
        [Browsable(false)]
        public string SettingInfo
        {
            get
            {
                return string.Format("{0},{1},{2},{3},{4},{5}",
                    serialPort.PortName,
                    serialPort.BaudRate,
                    serialPort.DataBits,
                    (int)serialPort.Parity,
                    (int)serialPort.StopBits,
                    (int)serialPort.Handshake);
            }
            set
            {
                string[] setting = value.Split(',');
                if (setting.Length < 6) throw new ArgumentException();
                bool isOpen = serialPort.IsOpen;
                if (isOpen) serialPort.Close();
                try
                {
                    serialPort.PortName = setting[0];
                    serialPort.BaudRate = int.Parse(setting[1]);
                    serialPort.DataBits = int.Parse(setting[2]);
                    serialPort.Parity = (System.IO.Ports.Parity)int.Parse(setting[3]);
                    serialPort.StopBits = (System.IO.Ports.StopBits)int.Parse(setting[4]);
                    serialPort.Handshake = (System.IO.Ports.Handshake)int.Parse(setting[5]);
                }
                catch (Exception ex) { throw new ArgumentException(ex.Message, ex); }
                finally { if (isOpen) serialPort.Open(); }
            }
        }

        /// <summary>
        /// 是否Modem已打开
        /// </summary>
        [Browsable(false)]
        public bool IsOpen { get { return isopen; } }

        /// <summary>
        /// 本机号码
        /// </summary>
        [DefaultValue("")]
        public string LocalNumber { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        [DefaultValue("")]
        [TypeConverter(typeof(StringConverter))]
        public object Tag { get; set; }

        #endregion

        #region 事件

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的打开事件的方法。
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的关闭事件的方法。
        /// </summary>
        public event EventHandler Closing;

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的通信错误事件的方法。
        /// </summary>
        public event EventHandler<ModemErrorEventArgs> ModemError;

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的发送数据事件的方法。
        /// </summary>
        public event EventHandler<DataTransmitEventArgs> DataSended;

        /// <summary>
        /// 表示将处理 Vultrue.Communication.GSMModem 对象的接收数据事件的方法。
        /// </summary>
        public event EventHandler<DataTransmitEventArgs> DataReceived;

        /// <summary>
        /// 引发连接事件
        /// </summary>
        /// <param name="e"></param>
        protected void OnOpened(EventArgs e)
        {
            if (Opened != null) Opened(this, e);
        }

        /// <summary>
        /// 引发关闭事件
        /// </summary>
        /// <param name="e"></param>
        protected void OnClosing(EventArgs e)
        {
            if (Closing != null) Closing(this, e);
        }

        /// <summary>
        /// 引发通信错误事件
        /// </summary>
        /// <param name="e"></param>
        protected void OnModemError(ModemErrorEventArgs e)
        {
            if (ModemError != null) ModemError(this, e);
        }

        /// <summary>
        /// 引发数据发送事件
        /// </summary>
        /// <param name="e"></param>
        protected void OnDataSended(DataTransmitEventArgs e)
        {
            if (DataSended != null) new Thread(() => { DataSended(this, e); }).Start();
        }

        /// <summary>
        /// 引发数据接收事件
        /// </summary>
        /// <param name="e"></param>
        protected void OnDataReceived(DataTransmitEventArgs e)
        {
            if (DataReceived != null) new Thread(() => { DataReceived(this, e); }).Start();
        }

        #endregion

        #region 基本方法

        /// <summary>
        /// 打开
        /// </summary>
        public void Open()
        {
            isopen = true;
            serialPort.Open();
            (readThread = new Thread(readData)).Start();
            OnOpened(EventArgs.Empty);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            isopen = false;
            OnClosing(EventArgs.Empty);
            Handshake handshake = serialPort.Handshake;
            try
            {
                serialPort.Handshake = Handshake.None;
                serialPort.Close();
            }
            finally
            {
                serialPort.Handshake = handshake;
            }
            if (readThread != null)
            {
                readThread.Join();
                readThread = null;
            }
        }

        #endregion

        #region 核心处理

        private void readData()
        {
            while (isopen)
            {
                int c = 0;
                try { c = serialPort.Read(data, 0, BUFFERSIZE); }
                catch { c = 0; }
                if (c == 0) { OnModemError(new ModemErrorEventArgs("连接中断")); break; }
                for (int i = 0; i < c; i++)
                {
                    byte b = data[i];
                    if (buffer.Count == 0) buffer.Add(b);
                    else if ((b == 0x0A) && (buffer[buffer.Count - 1] == 0x0D))
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                        if (buffer.Count > 0) linedeal();
                    }
                    else if ((b == 0x20) && (buffer[buffer.Count - 1] == 0x3E))
                    {
                        buffer.Add(b);
                        linedeal();
                    }
                    else buffer.Add(b);
                }
            }
        }

        private void linedeal()
        {
            string line = Encoding.ASCII.GetString(buffer.ToArray());
            buffer.Clear();
            OnDataReceived(new DataTransmitEventArgs(line));
            if (currentTask == null) unsolicitedDeal(line);
            else switch (currentTask.DealLine(line))
                {
                    case TaskResult.Finished:
                        taskReadWriteLock.Set();
                        break;
                    case TaskResult.Repeat:
                        sendCmd(currentTask.Instruction);
                        break;
                }
        }

        private void sendCmd(string cmd)
        {
            if (!IsOpen) throw new ModemClosedException("Modem 未打开或已关闭");
            byte[] sendata = Encoding.ASCII.GetBytes(cmd);
            serialPort.Write(sendata, 0, sendata.Length);
            OnDataSended(new DataTransmitEventArgs(cmd));
        }

        private void execTask(ITask task)
        {
            try
            {
                taskToken.WaitOne();
                sendCmd((currentTask = task).Instruction);
                istaskruning = true;
                new Thread(wakeup).Start();
                taskReadWriteLock.WaitOne();
                istaskruning = false;
                if (task.ModemException != null) throw task.ModemException;
            }
            finally
            {
                taskToken.Set();
                currentTask = null;
            }
        }

        private void wakeup()
        {
            Thread.Sleep(300000);
            if (!istaskruning) return;
            currentTask.ModemException = new ModemTimeoutException("Task Timeout");
            sendCmd("\u001B");
            taskReadWriteLock.Set();
        }

        #endregion
    }

    #region 枚举与结构

    /// <summary>
    /// 为 Vultrue.Communication.GSMModem 对象选择活动状态
    /// </summary>
    public enum ActivityStatus
    {
        /// <summary>
        /// Ready
        /// </summary>
        Ready = 0,
        /// <summary>
        /// Unavailable
        /// </summary>
        Unavailable = 1,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 2,
        /// <summary>
        /// Ringing
        /// </summary>
        Ringing = 3,
        /// <summary>
        /// CallInProgress
        /// </summary>
        CallInProgress = 4,
        /// <summary>
        /// Asleep
        /// </summary>
        Asleep = 5
    }

    /// <summary>
    /// 为 Vultrue.Communication.GSMModem 对象网络注册状态
    /// </summary>
    public enum NetworkState
    {
        /// <summary>
        /// NotRegisteredNotSearching
        /// </summary>
        NotRegisteredNotSearching = 0,

        /// <summary>
        /// RegisteredHomeNetwork
        /// </summary>
        RegisteredHomeNetwork = 1,

        /// <summary>
        /// NotRegisteredSearching
        /// </summary>
        NotRegisteredSearching = 2,

        /// <summary>
        /// RegistrationDenied
        /// </summary>
        RegistrationDenied = 3,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 4,

        /// <summary>
        /// RegisteredRoaming
        /// </summary>
        RegisteredRoaming = 5
    }

    /// <summary>
    /// 电话本条目
    /// </summary>
    public struct PhonebookEntry
    {
        /// <summary>
        /// 条目的存储索引
        /// </summary>
        public int Index;

        /// <summary>
        /// 电话号码
        /// </summary>
        public string Number;

        /// <summary>
        /// 号码类型
        /// </summary>
        public int Type;

        /// <summary>
        /// 关联的文本
        /// </summary>
        public string Text;
    }

    /// <summary>
    /// 短信格式
    /// </summary>
    public enum MessageFormat
    {
        /// <summary>
        /// Pdu
        /// </summary>
        Pdu = 0,

        /// <summary>
        /// Text
        /// </summary>
        Text = 1
    }

    /// <summary>
    /// 新信息指示方式
    /// </summary>
    public struct NewMessageIndication
    {
        /// <summary>
        /// 新短信提示模式
        /// </summary>
        public int Mode;

        /// <summary>
        /// 短信处理方式
        /// </summary>
        public int MessageTreat;

        /// <summary>
        /// 小区广播处理方式
        /// </summary>
        public int BroadcaseTreat;

        /// <summary>
        /// 短信状态报告
        /// </summary>
        public int SMSStatusReport;

        /// <summary>
        /// 缓冲区处理方式
        /// </summary>
        public int BufferTreat;
    }

    /// <summary>
    /// 短信状态
    /// </summary>
    public enum MessageState
    {
        /// <summary>
        /// REC_UNREAD
        /// </summary>
        REC_UNREAD = 0,

        /// <summary>
        /// REC_READ
        /// </summary>
        REC_READ = 1,

        /// <summary>
        /// STO_UNSENT
        /// </summary>
        STO_UNSENT = 2,

        /// <summary>
        /// STO_SENT
        /// </summary>
        STO_SENT = 3,

        /// <summary>
        /// ALL
        /// </summary>
        ALL = 4
    }

    /// <summary>
    /// Pdu短信编码格式
    /// </summary>
    public enum DataCodingScheme
    {
        /// <summary>
        /// Data
        /// </summary>
        Data = 0x04,

        /// <summary>
        /// USC2
        /// </summary>
        USC2 = 0x08
    }

    /// <summary>
    /// 短信删除标志
    /// </summary>
    public enum DeleteFlag
    {
        /// <summary>
        /// DeleteIndex
        /// </summary>
        DeleteIndex = 0,

        /// <summary>
        /// AllRead
        /// </summary>
        AllRead = 1,

        /// <summary>
        /// AllReadSent
        /// </summary>
        AllReadSent = 2,

        /// <summary>
        /// AllReadSentUnsent
        /// </summary>
        AllReadSentUnsent = 3,

        /// <summary>
        /// All
        /// </summary>
        All = 4
    }

    /// <summary>
    /// 电话本移动模式
    /// </summary>
    public enum MoveMode
    {
        /// <summary>
        /// First
        /// </summary>
        First = 0,

        /// <summary>
        /// Last
        /// </summary>
        Last = 1,

        /// <summary>
        /// Next
        /// </summary>
        Next = 2,

        /// <summary>
        /// Previous
        /// </summary>
        Previous = 3,

        /// <summary>
        /// LastRead
        /// </summary>
        LastRead = 4,

        /// <summary>
        /// LastWriten
        /// </summary>
        LastWriten = 5
    }

    #endregion

    #region 事件参数

    /// <summary>
    /// 为Vultrue.Communication.GSMModem.DataSended事件和Vultrue.Communication.GSMModem.DataReceived提供参数DataTransmitted
    /// </summary>
    public class DataTransmitEventArgs : EventArgs
    {
        /// <summary>
        /// 指示该数据是否为接受的数据
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// 初始化类Vultrue.Communication.DataTransmitEventArgs的新实例
        /// </summary>
        /// <param name="data">传输的数据行</param>
        public DataTransmitEventArgs(string data) { Data = data; }
    }

    /// <summary>
    /// 为Vultrue.Communication.GSMModem.ModemError事件提供参数
    /// </summary>
    public class ModemErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 初始化类Vultrue.Communication.CommunicationErrorEventArgs的新实例
        /// </summary>
        /// <param name="message">错误信息</param>
        public ModemErrorEventArgs(string message) { Message = message; }
    }

    /// <summary>
    /// 为Vultrue.Communication.GSMModem.Ring事件提供参数
    /// </summary>
    public class RingingEventArgs : EventArgs { }

    /// <summary>
    /// 为Vultrue.Communication.GSMModem.NoteletArrivaled事件提供参数
    /// </summary>
    public class MessageArrivaledEventArgs : EventArgs
    {
        /// <summary>
        /// 短信存储的媒体
        /// </summary>
        public string StoreMedia { get; set; }

        /// <summary>
        /// 短信位置
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 初始化类Vultrue.Communication.NoteletArrivaledEventArgs的新实例
        /// </summary>
        /// <param name="storeMedia">短信存储的媒体</param>
        /// <param name="index">短信位置</param>
        public MessageArrivaledEventArgs(string storeMedia, int index)
        {
            StoreMedia = storeMedia;
            Index = index;
        }
    }

    /// <summary>
    /// 为 Vultrue.Communication.GSMModem.NetworkRegistrationChanged 事件提供参数
    /// </summary>
    public class NetworkRegistrationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 网络注册状态
        /// </summary>
        public NetworkState State { get; private set; }

        /// <summary>
        /// 区域代码
        /// </summary>
        public string LocationAreaCode { get; private set; }

        /// <summary>
        /// 基站ID
        /// </summary>
        public string CellID { get; private set; }

        /// <summary>
        /// 初始化类 Vultrue.Communication.NetworkRegistrationChangedEventArgs 的新实例
        /// </summary>
        /// <param name="state">网络注册状态</param>
        /// <param name="locationAreaCode">区域代码</param>
        /// <param name="cellID">基站ID</param>
        public NetworkRegistrationChangedEventArgs(NetworkState state, string locationAreaCode, string cellID)
        {
            State = state;
            LocationAreaCode = locationAreaCode;
            CellID = cellID;
        }
    }

    #endregion

    #region 异常

    /// <summary>
    /// Modem流关闭时对Modem进行操作引发该异常
    /// </summary>
    public class ModemClosedException : Exception
    {
        /// <summary>
        /// 构造ModemClosedException异常
        /// </summary>
        /// <param name="message"></param>
        public ModemClosedException(string message) : base(message) { }
    }

    /// <summary>
    /// Modem尚未启动完成时或不支持该功能时, 对Modem进行操作引发该异常
    /// </summary>
    public class ModemUnsupportedException : Exception
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorID { get; private set; }

        /// <summary>
        /// 构造ModemUnsupportedException异常
        /// </summary>
        /// <param name="message"></param>
        public ModemUnsupportedException(string message) : base(message) { }

        /// <summary>
        /// 构造ModemUnsupportedException异常
        /// </summary>
        /// <param name="errorid"></param>
        /// <param name="message"></param>
        public ModemUnsupportedException(int errorid, string message)
            : base(message)
        {
            ErrorID = errorid;
        }
    }

    /// <summary>
    /// Modem接受的数据异常
    /// </summary>
    public class ModemDataException : Exception
    {
        /// <summary>
        /// 构造ModemDataException异常
        /// </summary>
        /// <param name="message"></param>
        public ModemDataException(string message) : base(message) { }
    }

    /// <summary>
    /// Modem接受的数据异常
    /// </summary>
    public class ModemTimeoutException : Exception
    {
        /// <summary>
        /// 构造ModemDataException异常
        /// </summary>
        /// <param name="message"></param>
        public ModemTimeoutException(string message) : base(message) { }
    }

    #endregion
}
