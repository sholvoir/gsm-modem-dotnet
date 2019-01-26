using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;

namespace Vultrue.Communication
{
    public partial class GSMModem
    {
        #region GENERAL COMMANDS

        /// <summary>
        /// 直接对 Modem 发送指令
        /// </summary>
        /// <param name="cmd">指令</param>
        public void SendDirect(string cmd)
        {
            ModemTask task = new ModemTask(cmd, "", "");
            execTask(task);
        }

        /// <summary>
        /// 执行AT空指令
        /// </summary>
        public void AT()
        {
            ModemTask task = new ModemTask("AT\r", "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 获取制造商信息
        /// </summary>
        /// <returns>制造商信息</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetManufacturerIdentification()
        {
            ModemTask task = new ModemTask("AT+CGMI\r", "", "^(?<ans>[ \\w]+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 获取支持的频带
        /// </summary>
        /// <returns>支持的频带</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetModelIdentification()
        {
            ModemTask task = new ModemTask("AT+CGMM\r", "", "^(?<ans>[ \\w]+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 获取Modem固件版本
        /// </summary>
        /// <returns>Modem固件版本</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetRevisionIdentification()
        {
            ModemTask task = new ModemTask("AT+CGMR\r", "", "^(?<ans>[ \\w\\p{P}\\p{S}]+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 获取产品序列号
        /// </summary>
        /// <returns>产品序列号</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemUnsupportedException">Modem EEPROM中没有产品序列号时发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetProductSerialNumber()
        {
            ModemTask task = new ModemTask("AT+CGSN\r", "IMEI not found in EEPROM", "^(?<ans>\\d+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 获取支持的TE字符集列表
        /// </summary>
        /// <returns>支持的TE字符集列表</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动尚未完成时发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string[] GetTECharacterSetList()
        {
            ModemTask task = new ModemTask("AT+CSCS=?\r", "Modem未启动", "^\\+CSCS:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}").Trim('(', ')').Replace("\"", "").Split(',');
        }

        /// <summary>
        /// 获取当前TE字符集
        /// </summary>
        /// <returns>当前TE字符集</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动尚未完成时将发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetTECharacterSet()
        {
            ModemTask task = new ModemTask("AT+CSCS?\r", "Modem未启动", "^\\+CSCS:\\s*\"(?<ans>\\w+)\"");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 设置当前TE字符集
        /// </summary>
        /// <param name="characterSet">TE字符集</param>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemUnsupportedException">试图设置Modem不支持的字符集时将发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public void SetTECharacterSet(string characterSet)
        {
            ModemTask task = new ModemTask(string.Format("AT+CSCS=\"{0}\"\r", characterSet),
                string.Format("Modem不支持字符集\"{0}\"", characterSet), "");
            execTask(task);
        }


        /// <summary>
        /// 获取SIM卡国际移动用户标识
        /// </summary>
        /// <returns>SIM卡国际移动用户标识</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动尚未完成或无SIM卡时发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetInternationalMobileSubscriberIdentity()
        {
            ModemTask task = new ModemTask("AT+CIMI\r", "Modem未启动", "^(?<ans>\\d+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 获取SIM卡标识
        /// </summary>
        /// <returns>SIM卡标识</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动尚未完成时发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string GetCardIdentification()
        {
            ModemTask task = new ModemTask("AT+CCID\r", "Modem未启动", "^\\+CCID:\\s*\"(?<ans>\\d+)\"");
            execTask(task);
            return task.Matchs[0].Result("${ans}");
        }

        /// <summary>
        /// 获取Modem功能列表
        /// </summary>
        /// <returns>Modem功能列表</returns>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public string[] GetCapabilitiesList()
        {
            ModemTask task = new ModemTask("AT+GCAP\r", "", "^\\+GCAP:\\s*(?<ans>[ \\w\\p{P}\\p{S}]+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}").Replace(" ", "").Split(',');
        }

        /// <summary>
        /// 关闭Modem ME, 等效于Modem功能等级设置为0
        /// </summary>
        /// <exception cref="ModemClosedException">Modem未打开是发生该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成</exception>
        /// <exception cref="ModemDataException">Modem接受到的数据异常</exception>
        public void PowerOff()
        {
            ModemTask task = new ModemTask("AT+CPOF\r", "", "");
            execTask(task);
        }

        /// <summary>
        /// 设置Modem功能级别(0或1)
        /// </summary>
        /// <returns>Modem功能级别</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public int GetPhoneFunctionalityLevel()
        {
            ModemTask task = new ModemTask("AT+CFUN?\r", "", "^\\+CFUN:\\s*(?<ans>\\d+)");
            execTask(task);
            return int.Parse(task.Matchs[0].Result("${ans}"));
        }

        /// <summary>
        /// 设置Modem功能级别(0或1)
        /// </summary>
        /// <param name="level">Modem功能级别</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">试图设置不被支持的功能级别引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetPhoneFunctionalityLevel(int level)
        {
            ModemTask task = new ModemTask(string.Format("AT+CFUN={0}\r", level), "不支持的功能级别", "");
            execTask(task);
        }

        /// <summary>
        /// 获取Modem活动状态
        /// </summary>
        /// <returns>Modem活动状态</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public ActivityStatus GetPhoneActivityStatus()
        {
            ModemTask task = new ModemTask("AT+CPAS\r", "", "^\\+CPAS:\\s*(?<ans>\\d+)");
            execTask(task);
            return (ActivityStatus)int.Parse(task.Matchs[0].Result("${ans}"));
        }

        /// <summary>
        /// 获取是否报告ME错误
        /// </summary>
        /// <returns>是否报告ME错误</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public bool GetReportMobileEquipmentErrors()
        {
            ModemTask task = new ModemTask("AT+CMEE?\r", "", "^\\+CMEE:\\s*(?<ans>\\d+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}") == "1";
        }

        /// <summary>
        /// 设置是否报告ME错误
        /// </summary>
        /// <param name="isReportMEError">是否报告ME错误</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetReportMobileEquipmentErrors(bool isReportMEError)
        {
            ModemTask task = new ModemTask(string.Format("AT+CMEE={0}\r", (isReportMEError ? "1" : "0")), "", "");
            execTask(task);
        }

        /// <summary>
        /// 设置按键控制Pattern
        /// </summary>
        /// <param name="keypadControl">按键控制Pattern</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">试图设置不被支持的功能级别引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        /// <returns></returns>
        public void SetKeypadControl(string keypadControl)
        {
            ModemTask task = new ModemTask(string.Format("AT+CKPD=\"{0}\"\r", keypadControl),
                "Modem未启动完成或不支持的键盘控制码", "^\\+CCFC:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");
            execTask(task);
        }

        /// <summary>
        /// 获取Modem时间(精确到分)
        /// </summary>
        /// <returns>Modem时间</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public DateTime GetModemClock()
        {
            ModemTask task = new ModemTask("AT+CCLK?\r", "", "^\\+CCLK:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");
            execTask(task);
            return DateTime.Parse("20" + task.Matchs[0].Result("${ans}").Trim('\"'));
        }

        /// <summary>
        /// 设置Modem时钟(精确到分)
        /// </summary>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">日期时间格式错误</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        /// <returns></returns>
        public void SetModemClock(DateTime clock)
        {
            ModemTask task = new ModemTask(string.Format("AT+CCLK=\"{0:yy/MM/dd,HH:mm:ss}\"\r", clock), "日期时间格式错误", "");
            execTask(task);
        }

        /// <summary>
        /// 获取Modem闹钟列表(精确到分,尚未响铃)
        /// </summary>
        /// <returns>尚未响铃的Modem闹钟列表</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public DateTime[] GetModemAlarms()
        {
            ModemTask task = new ModemTask("AT+CALA?\r", "", "^\\+CALA:\\s*\"(?<cala>[/:,\\d]+)\",(?<index>\\d+)");
            execTask(task);
            List<DateTime> alarms = new List<DateTime>();
            foreach (Match match in task.Matchs)
                alarms.Add(DateTime.Parse("20" + match.Result("${cala}")));
            return alarms.ToArray();
        }

        /// <summary>
        /// 设置Modem闹钟列表(精确到分)
        /// </summary>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">ModemAlarm位置已满(最多16个闹钟)</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void AddModemAlarm(DateTime alarm)
        {
            ModemTask task = new ModemTask(string.Format("AT+CALA=\"{0:yy/MM/dd,HH:mm:ss}\"\r", alarm), "ModemAlarm位置已满", "");
            execTask(task);
        }

        /// <summary>
        /// 获取来电音量(0-15, 6为默认值)
        /// </summary>
        /// <returns>音量</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">不支持的音量数值</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public int GetRingerSoundLevel()
        {
            ModemTask task = new ModemTask("AT+CRSL?\r", "", "^\\+CRSL:\\s*(?<ans>\\d+)");
            execTask(task);
            return int.Parse(task.Matchs[0].Result("${ans}"));
        }

        /// <summary>
        /// 设置来电音量(0-15, 6为默认值)
        /// </summary>
        /// <param name="soundLevel">音量</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">不支持的音量数值</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetRingerSoundLevel(int soundLevel)
        {
            ModemTask task = new ModemTask(string.Format("AT+CRSL={0}\r", soundLevel), "不支持的音量数值", "");
            execTask(task);
        }

        #endregion

        #region CALL CONTROL COMMANDS
        #endregion

        #region NETWORK SERVICE COMMANDS

        /// <summary>
        /// 获取Modem信号质量
        /// </summary>
        /// <param name="errorRate">当前误码率 取值范围(0-7) 99表示未知或不可检测</param>
        /// <returns>Modem信号强度 取值范围(0-31) 99表示未知或不可检测</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public int GetSignalQuality(out int errorRate)
        {
            ModemTask task = new ModemTask("AT+CSQ\r", "", "^\\+CSQ:\\s*(?<rssi>\\d+),(?<ber>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            errorRate = int.Parse(match.Result("${ber}"));
            return int.Parse(match.Result("${rssi}"));
        }

        /// <summary>
        /// 设置网络注册信息模式
        /// </summary>
        /// <param name="mode">注册信息模式</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem 不支持该模式时发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetNetworkRegistrationMode(int mode)
        {
            ModemTask task = new ModemTask(string.Format("AT+CREG={0}\r", mode), "不支持的注册信息模式", "");
            execTask(task);
        }

        /// <summary>
        /// 获取Modem网络注册信息
        /// </summary>
        /// <param name="mode">模式, 取值范围(0-2)</param>
        /// <param name="locationAreaCode">区域代码</param>
        /// <param name="cellID">蜂窝ID</param>
        /// <returns>Modem网络注册状态</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public NetworkState GetNetworkRegistration(out int mode, out string locationAreaCode, out string cellID)
        {
            ModemTask task = new ModemTask("AT+CREG?\r", "",
                "^\\+CREG:\\s*(?<mode>\\d+),(?<stat>\\d+)(,\"(?<lac>[0-9a-fA-F]+)\",\"(?<ci>[0-9a-fA-F]+)\")*");
            execTask(task);
            Match match = task.Matchs[0];
            mode = int.Parse(match.Result("${mode}"));
            locationAreaCode = match.Result("${lac}");
            cellID = match.Result("${ci}");
            return (NetworkState)int.Parse(match.Result("${stat}"));
        }

        #endregion

        #region SECURITY COMMANDS
        #endregion

        #region PHONEBOOK COMMANDS

        /// <summary>
        /// 获取Modem支持的电话本存储位置列表
        /// </summary>
        /// <returns>电话本存储位置列表</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public string[] GetPhonebookMemoryStorageList()
        {
            ModemTask task = new ModemTask("AT+CPBS=?\r", "Modem未启动", "^\\+CPBS:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}").Trim('(', ')').Replace("\"", "").Split(',');
        }

        /// <summary>
        /// 获取电话本存储空间信息
        /// </summary>
        /// <param name="usedLocations">已使用的空间</param>
        /// <param name="totalLocations">总可使用的空间</param>
        /// <returns>电话本存储位置</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public string GetPhonebookMemoryStorage(out int usedLocations, out int totalLocations)
        {
            ModemTask task = new ModemTask("AT+CPBS?\r", "Modem未启动",
                "^\\+CPBS:\\s*\"(?<mem>\\d+)\",(?<used>\\d+),(?<total>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            usedLocations = int.Parse(match.Result("${used}"));
            totalLocations = int.Parse(match.Result("${total}"));
            return match.Result("${mem}");
        }

        /// <summary>
        /// 设置电话本存储空间
        /// </summary>
        /// <param name="storage">电话本存储空间</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetPhonebookMemoryStorage(string storage)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBS=\"{0}\"\r", storage), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 获取电话本信息
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <param name="endIndex">结束索引</param>
        /// <param name="maxPhoneLenth">电话号码最大长度</param>
        /// <param name="maxTextLenth">关联文本最大长度</param>
        public void GetPhonebookInfo(out int startIndex, out int endIndex, out int maxPhoneLenth, out int maxTextLenth)
        {
            ModemTask task = new ModemTask("AT+CPBR=?\r", "Modem未启动",
                "^\\+CPBR:\\s*\\((?<si>\\d+)-(?<ei>\\d+)\\),(?<mp>\\d+),(?<mt>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            startIndex = int.Parse(match.Result("${si}"));
            endIndex = int.Parse(match.Result("${ei}"));
            maxPhoneLenth = int.Parse(match.Result("${mp}"));
            maxTextLenth = int.Parse(match.Result("${mt}"));
        }

        /// <summary>
        /// 从电话本读取电话号码
        /// </summary>
        /// <param name="index">起始索引</param>
        /// <returns>读取的电话本条目</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public PhonebookEntry ReadPhonebookEntries(int index)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBR={0}\r", index), "Modem未启动或索引超出范围",
                "^\\+CPBR:\\s*(?<index>\\d+),\"(?<number>[\\d\\+]+)\",(?<type>\\d+),\"(?<text>\\w+)\"");
            execTask(task);
            Match match = task.Matchs[0];
            return new PhonebookEntry()
            {
                Index = int.Parse(match.Result("${index}")),
                Number = match.Result("${number}"),
                Type = int.Parse(match.Result("${type}")),
                Text = match.Result("${text}")
            };
        }

        /// <summary>
        /// 从电话本读取电话号码
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <param name="endIndex">结束索引</param>
        /// <returns>读取的电话本条目</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public PhonebookEntry[] ReadPhonebookEntries(int startIndex, int endIndex)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBR={0},{1}\r", startIndex, endIndex),
                "Modem未启动或索引超出范围",
                "^\\+CPBR:\\s*(?<index>\\d+),\"(?<number>[\\d\\+]+)\",(?<type>\\d+),\"(?<text>\\w+)\"");
            execTask(task);
            List<PhonebookEntry> entries = new List<PhonebookEntry>();
            foreach (Match match in task.Matchs)
                entries.Add(new PhonebookEntry()
                {
                    Index = int.Parse(match.Result("${index}")),
                    Number = match.Result("${number}"),
                    Type = int.Parse(match.Result("${type}")),
                    Text = match.Result("${text}")
                });
            return entries.ToArray();
        }

        /// <summary>
        /// 从电话本查找电话号码
        /// </summary>
        /// <param name="text">搜索文本</param>
        /// <returns>查找到的电话本条目</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public PhonebookEntry[] FindPhonebookEntries(string text)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBF=\"{0}\"\r", text), "Modem未启动或索引超出范围",
                "^\\+CPBF:\\s*(?<index>\\d+),\"(?<number>[\\d\\+]+)\",(?<type>\\d+),\"(?<text>\\w+)\"");
            execTask(task);
            List<PhonebookEntry> entries = new List<PhonebookEntry>();
            foreach (Match match in task.Matchs)
                entries.Add(new PhonebookEntry()
                {
                    Index = int.Parse(match.Result("${index}")),
                    Number = match.Result("${number}"),
                    Type = int.Parse(match.Result("${type}")),
                    Text = match.Result("${text}")
                });
            return entries.ToArray();
        }

        /// <summary>
        /// 从电话本删除电话本条目
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void DeletePhonebookEntry(int index)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBW={0}\r", index), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 向电话本写入电话本条目
        /// </summary>
        /// <param name="index">存储位置</param>
        /// <param name="number">电话号码</param>
        /// <param name="type">号码类型(129:本地号码/145:国际号码)</param>
        /// <param name="text">联系文本</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void WritePhonebookEntry(int index, string number, int type, string text)
        {
            ModemTask task = new ModemTask(
                string.Format("AT+CPBW={0},\"{1}\",{2},\"{3}\"\r", index, number, type, text), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 向电话本写入电话本条目
        /// </summary>
        /// <param name="number">电话号码</param>
        /// <param name="type">号码类型(129:本地号码/145:国际号码)</param>
        /// <param name="text">联系文本</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void WritePhonebookEntry(string number, int type, string text)
        {
            ModemTask task = new ModemTask(
                string.Format("AT+CPBW=,\"{0}\",{1},\"{2}\"\r", number, type, text), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 由电话号码搜索电话本条目
        /// </summary>
        /// <param name="number">电话号码</param>
        /// <returns>电话本条目</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public PhonebookEntry PhonebookPhoneSearch(string number)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBP=\"{0}\"\r", number), "Modem未启动或索引超出范围",
                "^\\+CPBP:\\s*(?<index>\\d+),\"(?<number>[\\d\\+]+)\",(?<type>\\d+),\"(?<text>\\w+)\"");
            execTask(task);
            Match match = task.Matchs[0];
            return new PhonebookEntry()
            {
                Index = int.Parse(match.Result("${index}")),
                Number = match.Result("${number}"),
                Type = int.Parse(match.Result("${type}")),
                Text = match.Result("${text}")
            };
        }

        /// <summary>
        /// 在电话号码本中移动
        /// </summary>
        /// <param name="mode">移动模式</param>
        /// <returns>电话本条目</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或索引超出范围</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public PhonebookEntry MoveActionPhonebook(MoveMode mode)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPBN={0}\r", (int)mode), "Modem未启动或索引超出范围",
                "^\\+CPBN:\\s*(?<index>\\d+),\"(?<number>[\\d\\+]+)\",(?<type>\\d+),\"(?<text>\\w+)\"");
            execTask(task);
            Match match = task.Matchs[0];
            return new PhonebookEntry()
            {
                Index = int.Parse(match.Result("${index}")),
                Number = match.Result("${number}"),
                Type = int.Parse(match.Result("${type}")),
                Text = match.Result("${text}")
            };
        }

        #endregion

        #region SHORT MESSAGES COMMANDS

        /// <summary>
        /// 获取GSMModem短信服务AT命令版本(0或1)
        /// </summary>
        /// <returns>GSMModem短信服务AT命令版本</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem未启动或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public int GetMessageService()
        {
            ModemTask task = new ModemTask("AT+CSMS?\r", "Modem未启动",
                "^\\+CSMS:\\s*(?<service>\\d+),(?<mo>\\d+),(?<mt>\\d+),(?<cb>\\d+)");
            execTask(task);
            return int.Parse(task.Matchs[0].Result("${service}"));
        }

        /// <summary>
        /// 设置GSMModem短信服务AT命令版本(0或1)
        /// </summary>
        /// <param name="messageService">GSMModem短信服务AT命令版本</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem不支持该AT命令版本时发生该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        /// <returns></returns>
        public void SetMessageService(int messageService)
        {
            ModemTask task = new ModemTask(string.Format("AT+CSMS={0}\r", messageService),
                "不支持的短信协议", "^\\+CSMS:\\s*(?<ans>[\\w\\p{P}\\p{S}]+)");
            execTask(task);
        }

        /// <summary>
        /// 新短信接收确认
        /// 文本模式下, 只能进行肯定确认(自动忽略acknowledge参数)
        /// PDU模式下, 可进行肯定和否定确认
        /// 只有短信AT命令版本1下且+CMT和+CDS设置为显示时才允许确认
        /// </summary>
        /// <param name="acknowledge">肯定确认或否定确认</param>
        /// <param name="pdustr">确认信息</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">没有要确认的短信时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void NewMessageAcknowledgement(bool acknowledge, string pdustr)
        {
            if (pdustr == null || pdustr.Length == 0)
            {
                ModemTask task = new ModemTask("AT+CNMA=0\r", "没有要确认的短信", "(?<ans>[ \\w\\p{P}\\p{S}]+)");
                execTask(task);
            }
            else
            {
                ModemTask task1 = new ModemTask(string.Format("AT+CNMA={0},{1}\r", (acknowledge ? "1" : "2"), pdustr.Length / 2), "Modem未启动", "\u003E\u0020");
                ModemTask task2 = new ModemTask(string.Format("{0}\u001A", pdustr), "确认短信未发送成功", "(?<ans>[ \\w\\p{P}\\p{S}]+)");
                task1.IsNonOKCmd = true;
                TaskGroup group = new TaskGroup(new ModemTask[] { task1, task2 });
                execTask(group);
            }
        }

        /// <summary>
        /// 获取短信存储空间列表
        /// </summary>
        /// <param name="rldStorages">读取,列出,删除操作的可选目标空间</param>
        /// <param name="wsStorages">写入,发送的可选目标空间</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void GetMessageStorageList(out string[] rldStorages, out string[] wsStorages)
        {
            ModemTask task = new ModemTask("AT+CPMS=?\r", "Modem未启动",
                "^\\+CPMS:\\s*\\(\\((?<rld>[\",\\w]+)\\),\\((?<ws>[\",\\w]+)\\)\\)");
            execTask(task);
            Match match = task.Matchs[0];
            rldStorages = match.Result("${rld}").Replace("\"", "").Split(',');
            wsStorages = match.Result("${ws}").Replace("\"", "").Split(',');
        }

        /// <summary>
        /// 获取GSMModem默认的短信存储空间
        /// </summary>
        /// <param name="rldStorage">读取,列出,删除操作目标存储空间</param>
        /// <param name="rldStoreUsed">读取,列出,删除操作存储空间已使用的短信条数</param>
        /// <param name="rldStoreTotal">读取,列出,删除操作存储空间可存储的短信总条数</param>
        /// <param name="wsStorage">写入与发送操作目标存储空间</param>
        /// <param name="wsStoreUsed">写入与发送操作存储空间已使用的短信条数</param>
        /// <param name="wsStoreTotal">写入与发送操作存储空间可存储的短信总条数</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void GetPreferredMessageStorage(out string rldStorage, out int rldStoreUsed,
            out int rldStoreTotal, out string wsStorage, out int wsStoreUsed, out int wsStoreTotal)
        {
            ModemTask task = new ModemTask("AT+CPMS?\r", "Modem未启动",
                "^\\+CPMS:\\s*\"(?<rld>\\w+)\",(?<rldu>\\d+),(?<rldt>\\d+),\"(?<ws>\\w+)\",(?<wsu>\\d+),(?<wst>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            rldStorage = match.Result("${rld}");
            rldStoreUsed = int.Parse(match.Result("${rldu}"));
            rldStoreTotal = int.Parse(match.Result("${rldt}"));
            wsStorage = match.Result("${ws}");
            wsStoreUsed = int.Parse(match.Result("${wsu}"));
            wsStoreTotal = int.Parse(match.Result("${wst}"));
        }

        /// <summary>
        /// 设置GSMModem默认的短信存储空间
        /// </summary>
        /// <param name="rldStorage">读取,列出,删除操作目标存储空间</param>
        /// <param name="rldStoreUsed">读取,列出,删除操作存储空间已使用的短信条数</param>
        /// <param name="rldStoreTotal">读取,列出,删除操作存储空间可存储的短信总条数</param>
        /// <param name="wsStoreUsed">写入与发送操作存储空间已使用的短信条数</param>
        /// <param name="wsStoreTotal">写入与发送操作存储空间可存储的短信总条数</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">设置了不支持的短信时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetPreferredMessageStorage(string rldStorage, out int rldStoreUsed,
            out int rldStoreTotal, out int wsStoreUsed, out int wsStoreTotal)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPMS=\"{0}\"\r", rldStorage), "Modem未启动",
                "^\\+CPMS:\\s*(?<rldu>\\d+),(?<rldt>\\d+),(?<wsu>\\d+),(?<wst>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            rldStoreUsed = int.Parse(match.Result("${rldu}"));
            rldStoreTotal = int.Parse(match.Result("${rldt}"));
            wsStoreUsed = int.Parse(match.Result("${wsu}"));
            wsStoreTotal = int.Parse(match.Result("${wst}"));
        }

        /// <summary>
        /// 设置GSMModem默认的短信存储空间
        /// </summary>
        /// <param name="rldStorage">读取,列出,删除操作目标存储空间</param>
        /// <param name="wsStorage">写入与发送操作目标存储空间, 不需要需要修改时, 值为null时不修改</param>
        /// <param name="rldStoreUsed">读取,列出,删除操作存储空间已使用的短信条数</param>
        /// <param name="rldStoreTotal">读取,列出,删除操作存储空间可存储的短信总条数</param>
        /// <param name="wsStoreUsed">写入与发送操作存储空间已使用的短信条数</param>
        /// <param name="wsStoreTotal">写入与发送操作存储空间可存储的短信总条数</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">设置了不支持的短信时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetPreferredMessageStorage(string rldStorage, string wsStorage,
            out int rldStoreUsed, out int rldStoreTotal, out int wsStoreUsed, out int wsStoreTotal)
        {
            ModemTask task = new ModemTask(string.Format("AT+CPMS=\"{0}\",\"{1}\"\r", rldStorage, wsStorage), "Modem未启动",
                "^\\+CPMS:\\s*(?<rldu>\\d+),(?<rldt>\\d+),(?<wsu>\\d+),(?<wst>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            rldStoreUsed = int.Parse(match.Result("${rldu}"));
            rldStoreTotal = int.Parse(match.Result("${rldt}"));
            wsStoreUsed = int.Parse(match.Result("${wsu}"));
            wsStoreTotal = int.Parse(match.Result("${wst}"));
        }

        /// <summary>
        /// 获取GSMModem短信格式
        /// </summary>
        /// <returns>GSMModem短信格式</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public MessageFormat GetMessageFormat()
        {
            ModemTask task = new ModemTask("AT+CMGF?\r", "Modem未启动", "^\\+CMGF:\\s*(?<ans>\\d+)");
            execTask(task);
            return (MessageFormat)int.Parse(task.Matchs[0].Result("${ans}"));
        }

        /// <summary>
        /// 设置GSMModem短信格式
        /// </summary>
        /// <param name="messageFormat">GSMModem短信格式</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        /// <returns></returns>
        public void SetMessageFormat(MessageFormat messageFormat)
        {
            ModemTask task = new ModemTask(string.Format("AT+CMGF={0}\r", (int)messageFormat), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 保存设置信息(短信中心号码和文本模式参数)到EEPROM(SIM卡Phase1)或SIM(SIM卡Phase2)
        /// </summary>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SaveSettings()
        {
            ModemTask task = new ModemTask("AT+CSAS\r", "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 恢复设置信息(短信中心号码和文本模式参数)从EEPROM(SIM卡Phase1)或SIM(SIM卡Phase2)
        /// </summary>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void RestoreSettings()
        {
            ModemTask task = new ModemTask("AT+CRES\r", "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 获取是否显示文本模式参数
        /// </summary>
        /// <returns>是否显示文本模式参数</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public bool GetIsShowTextModeParameters()
        {
            ModemTask task = new ModemTask("AT+CSDH?\r", "Modem未启动", "^\\+CSDH:\\s*(?<ans>\\d+)");
            execTask(task);
            return task.Matchs[0].Result("${ans}") == "1";
        }

        /// <summary>
        /// 设置是否显示文本模式参数
        /// 影响的指令+CMTI, +CMT, +CDS, +CMGR, +CMGL
        /// </summary>
        /// <param name="isShow">是否显示文本模式参数</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetIsShowTextModeParameters(bool isShow)
        {
            ModemTask task = new ModemTask(string.Format("AT+CSDH={0}\r", (isShow ? "1" : "0")), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 获取新短信提示模式
        /// </summary>
        /// <returns>新短信提示模式</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public NewMessageIndication GetNewMessageIndication()
        {
            ModemTask task = new ModemTask("AT+CNMI?\r", "Modem未启动",
                "^\\+CNMI:\\s*(?<mode>\\d+),(?<mt>\\d+),(?<bm>\\d+),(?<ds>\\d+),(?<bfr>\\d+)");
            execTask(task);
            Match match = task.Matchs[0];
            return new NewMessageIndication()
            {
                Mode = int.Parse(match.Result("${mode}")),
                MessageTreat = int.Parse(match.Result("${mt}")),
                BroadcaseTreat = int.Parse(match.Result("${bm}")),
                SMSStatusReport = int.Parse(match.Result("${ds}")),
                BufferTreat = int.Parse(match.Result("${bfr}"))
            };
        }

        /// <summary>
        /// 设置新短信提示模式
        /// </summary>
        /// <param name="indication">新短信提示模式</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetNewMessageIndication(NewMessageIndication indication)
        {
            string cmd = string.Format("AT+CNMI={0},{1},{2},{3},{4}\r",
                indication.Mode,
                indication.MessageTreat,
                indication.BroadcaseTreat,
                indication.SMSStatusReport,
                indication.BufferTreat);
            ModemTask task = new ModemTask(cmd, "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 读取短信
        /// </summary>
        /// <param name="index">短信所在存储空间的索引</param>
        /// <returns>读取的短信</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public Message ReadMessage(int index)
        {
            ModemTask task = new ModemTask(string.Format("AT+CMGR={0}\r", index),
                "Modem未启动或该位置没有短信", "(?<ans>[ \\w\\p{P}\\p{S}]+)");
            execTask(task);
            string line = task.Matchs[0].Result("${ans}");
            Match match = new Regex("^\\+CMGR:\\s*(?<stat>\\d+),(?<alpha>[ \"\\w\\d]*),(?<lenth>\\d+)").Match(line);
            if (!match.Success) throw new ModemDataException(line);
            MessageState messageState = (MessageState)int.Parse(match.Result("${stat}"));
            int lenth = int.Parse(match.Result("${lenth}"));
            return new Message(index, messageState, lenth, task.Matchs[1].Result("${ans}"));
        }

        /// <summary>
        /// 列出短信
        /// </summary>
        /// <param name="state">筛选短信的状态</param>
        /// <returns>读取的短信</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public Message[] ListMessage(MessageState state)
        {
            ModemTask task = new ModemTask(string.Format("AT+CMGL={0}\r", (int)state), "Modem未启动", "(?<ans>[ \\w\\p{P}\\p{S}]+)");
            execTask(task);
            List<Message> messages = new List<Message>();
            Regex regex = new Regex("^\\+CMGL:\\s*(?<index>\\d+),(?<stat>\\d+),(?<alpha>[ \"\\w\\d]*),(?<lenth>\\d+)");
            for (int i = 0; i < task.Matchs.Count; i++)
            {
                string line = task.Matchs[i].Result("${ans}");
                Match match = regex.Match(line);
                if (!match.Success) continue;
                int index = int.Parse(match.Result("${index}"));
                MessageState messageState = (MessageState)int.Parse(match.Result("${stat}"));
                int lenth = int.Parse(match.Result("${lenth}"));
                messages.Add(new Message(index, messageState, lenth, task.Matchs[++i].Result("${ans}")));
            }
            return messages.ToArray();
        }

        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="message">需要发送的短信</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SendMessage(Message message)
        {
            string tpdu = message.Tpdu;
            ModemTask task1 = new ModemTask(string.Format("AT+CMGS={0}\r", tpdu.Length / 2), "Modem未启动", "\u003E\u0020");
            ModemTask task2 = new ModemTask(string.Format("00{0}\u001A", tpdu), "短信未发送成功", "(?<ans>[ \\w\\p{P}\\p{S}]+)");
            task1.IsNonOKCmd = true;
            TaskGroup group = new TaskGroup(new ModemTask[] { task1, task2 });
            execTask(group);
        }

        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="mobileNum">目标号码</param>
        /// <param name="userData">短信内容(PDU串)</param>
        /// <param name="encode">编码</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SendMessage(string mobileNum, string userData, DataCodingScheme encode)
        {
            for (int i = 0; i < userData.Length; i += MAXNOTELETLENTH)
            {
                string str = userData.Length - i > MAXNOTELETLENTH ?
                    userData.Substring(i, MAXNOTELETLENTH) : userData.Substring(i, userData.Length - i);
                SendMessage(new Message(mobileNum, str, encode));
            }
        }

        /// <summary>
        /// 发送文本短信
        /// </summary>
        /// <param name="mobileNum">目标号码</param>
        /// <param name="text">短信内容(文本)</param>
        public void SendMessage(string mobileNum, string text)
        {
            SendMessage(mobileNum, PduString.GetPdustr(text), DataCodingScheme.USC2);
        }

        /// <summary>
        /// 删除短信
        /// </summary>
        /// <param name="index">短信在存储卡中的位置</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void DeleteMessage(int index)
        {
            ModemTask task = new ModemTask(string.Format("AT+CMGD={0}\r", index), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 删除短信
        /// </summary>
        /// <param name="index">短信在存储卡中的位置</param>
        /// <param name="flag">删除选项</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void DeleteMessage(int index, DeleteFlag flag)
        {
            ModemTask task = new ModemTask(string.Format("AT+CMGD={0},{1}\r", index, (int)flag), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 获取服务中心号码
        /// </summary>
        /// <returns>服务中心号码</returns>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public string GetServiceCenterAddress()
        {
            ModemTask task = new ModemTask("AT+CSCA?\r", "Modem未启动", "^\\+CSCA:\\s*\"(?<sca>[\\+\\d]+)\",(?<type>\\d+)");
            execTask(task);
            return task.Matchs[0].Result("${sca}");
        }

        /// <summary>
        /// 设置服务中心号码
        /// </summary>
        /// <param name="serviceCenterAddress">服务中心号码</param>
        /// <exception cref="ModemClosedException">Modem未打开时引发该异常</exception>
        /// <exception cref="TimeoutException">读取或发送未在超时时间到期之前完成引发该异常</exception>
        /// <exception cref="ModemUnsupportedException">Modem启动未完成或没有SIM卡时引发该异常</exception>
        /// <exception cref="ModemDataException">Modem接收到的数据异常</exception>
        public void SetServiceCenterAddress(string serviceCenterAddress)
        {
            ModemTask task = new ModemTask(string.Format("AT+CSCA=\"{0}\"\r", serviceCenterAddress), "Modem未启动", "");
            execTask(task);
        }

        #endregion

        #region SUPPLEMENTARY SERVICES COMMANDS
        #endregion

        #region DATA COMMANDS
        #endregion

        #region FAX COMMANDS
        #endregion

        #region FAX CLASS 2 COMMANDS
        #endregion

        #region V24-V25 COMMANDS

        /// <summary>
        /// 设置Modem是否回显
        /// </summary>
        /// <param name="echo">True回显, False不回显</param>
        public void SetEcho(bool echo)
        {
            ModemTask task = new ModemTask(string.Format("ATE{0}\r", echo ? "1" : "0"), "Modem未启动", "");
            execTask(task);
        }

        /// <summary>
        /// 恢复出厂设置
        /// </summary>
        public void RestoreFactorySetting()
        {
            ModemTask task = new ModemTask("AT&F\r", "Modem未启动", "");
            execTask(task);
        }

        #endregion

        #region SPECIFIC AT COMMANDS
        #endregion

        #region DATA COMMANDS
        #endregion

        #region SIM TOOLKIT COMMANDS
        #endregion
    }

    
}
