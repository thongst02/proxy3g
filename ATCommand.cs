using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.IO;


namespace ShellHelper
{
    class ATCommand
    {
        SerialPort portSend { get; set; }
        public bool isCanRead {get;set;}= false;
        public bool bReady { get;set;}= false;
        public Model.GsmInfo gsmInfo { get; set; }

        public ATCommand(string sPortName)
        {
            Connect(sPortName);
        }
        string LastCommand = "";
        public void SendCommand(string sCommand)
        {
            try
            {
                if (!portSend.IsOpen)
                    return;
                LastCommand = sCommand;
                portSend.Write(sCommand+"\u000D");
            }
            catch (Exception exc)
            {
                //Console.WriteLine($"SendCommand {exc.Message}");
            }
        }
        public void Disconnect()
        {
            try
            {
                if (portSend.IsOpen)
                    portSend.Close();
                portSend.Dispose();
            }
            catch (Exception exc)
            {
                //Console.WriteLine($"DisConnect {exc.Message}");
            }
        }
        public void WaitForRead(int nTime)
        {
            DateTime dttime = DateTime.Now;
            while ((DateTime.Now - dttime).TotalMilliseconds < nTime && !isCanRead)
                System.Threading.Thread.Sleep(10);
        }
        public void WaitForPhone(int nTime)
        {
            if (gsmInfo == null || gsmInfo.operation_id == 0)
                return;
            DateTime dttime = DateTime.Now;
            while ((DateTime.Now - dttime).TotalMilliseconds < nTime && string.IsNullOrEmpty(gsmInfo.phone_number))
                System.Threading.Thread.Sleep(10);
        }
        void Connect(string sPortName)
        {
            try
            {
                Parity parity = Parity.None;
                StopBits stopBit = StopBits.One;
                //38400
                portSend = new SerialPort(sPortName, 38400, parity, 8, stopBit);
                portSend.Open();
                portSend.DataReceived += new SerialDataReceivedEventHandler(portOnDataReceived);
                portSend.WriteTimeout = 5000;
            }
            catch (Exception exc)
            {
                //Console.WriteLine($"Connect {exc.Message}");
            }
        }
        string sCurrentStr = "";
        void portOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            RecivceData1();
        }
        void RecivceData1()
        {            
            sCurrentStr = portSend.ReadExisting(); //current use
            sCurrentStr = sCurrentStr.Replace("\r", "");            
            string[] sArr = sCurrentStr.Split('\n');
            foreach (string sText in sArr)
            {
                if (string.IsNullOrEmpty(sText))
                    continue;
                if (sText.StartsWith("AT+"))
                    LastCommand = sText;
                Console.WriteLine(sText);
                ProcessCommand(sText);
            }
        }
        void RecivceData()
        {
            int c;
            sCurrentStr = "";
            if (!portSend.IsOpen)
                return;
            do
            {               
                c = portSend.ReadByte();
                if (c != 0xA)
                    sCurrentStr += (char)c;
            } while (c != 0xA);
            //Console.WriteLine($"R: {sCurrentStr} Last {LastCommand}");
            sCurrentStr = sCurrentStr.Replace("\r", "");
            sCurrentStr = sCurrentStr.Replace("\n", "");
            //string[] sArr = sCurrentStr.Split('\n');            
            ProcessCommand(sCurrentStr);
        }
        void ProcessCommand(string sText)
        {
            if (string.IsNullOrEmpty(sText))
                return;
            if (LastCommand == sText)
                return;
            if (sCurrentStr.ToLower().Contains("ready") && !sCurrentStr.Contains("NOT READY"))
                bReady = true;
            //Console.WriteLine($"R: {sText} Last {LastCommand}");
            if (sText == "OK")
                isCanRead = true;
            if (!isCanRead)
                return;
            if (gsmInfo == null)
                gsmInfo = new Model.GsmInfo();
            if (string.IsNullOrEmpty(gsmInfo.serial_number) && LastCommand.Contains("CGSN"))
                gsmInfo.serial_number = sText.Regx("\\d+");
            if (gsmInfo.operation_id == 0 && LastCommand == "AT+CIMI")
            {
                gsmInfo.operation_id = sText.Regx("\\d{5}").ToInt();
                if (gsmInfo.operation_id != 0)
                {
                    var Opinfo = Utils.lsOperation.Find(x => x.Id == gsmInfo.operation_id);
                    if (Opinfo != null)
                        gsmInfo.operation_name = Opinfo.Name;
                }
            }
            if (string.IsNullOrEmpty(gsmInfo.manufacturer) && LastCommand.Contains("CGMI"))
                gsmInfo.manufacturer = sText.Replace("+CGMM:","").Trim();
            if (string.IsNullOrEmpty(gsmInfo.model) && LastCommand.Contains("CGMM"))
                gsmInfo.model = sText.Replace("+CGMM:", "").Trim();
            if (string.IsNullOrEmpty(gsmInfo.signal) && sText.Contains("CSQ"))
            {
                gsmInfo.signal = sText.Replace("+CSQ:", "").Trim();
                gsmInfo.signal = gsmInfo.signal.Regx("\\d+");
                int nSignal = gsmInfo.signal.ToInt();
                if (nSignal < 10)
                    gsmInfo.signal = $"Marginal {nSignal}";
                else if (nSignal >= 10 && nSignal < 15)
                    gsmInfo.signal = $"OK {nSignal}";
                else if (nSignal >= 15 && nSignal < 20)
                    gsmInfo.signal = $"Good {nSignal}";
                else if (nSignal >= 20 )
                    gsmInfo.signal = $"Excellent {nSignal}";
            }
            if (string.IsNullOrEmpty(gsmInfo.phone_number) && LastCommand.Contains("+CUSD"))
                gsmInfo.phone_number=sText.Regx("\\d{9,}");
        }
        public void getPhonenumber()
        {
            if (gsmInfo == null)
                return;
            if (gsmInfo.operation_id == 0)
                SendCommand("AT+CIMI");
            System.Threading.Thread.Sleep(50);

            var operation = Utils.lsOperation.Find(x => x.Id == gsmInfo.operation_id);
            if (operation == null)
                return;
            SendCommand(operation.ATGetPhone);
            /*
            if (gsmInfo.operation_id == 45205)
                SendCommand("AT+CUSD=1,\"*123#\",15");
            else if (gsmInfo.operation_id == 45201)
                SendCommand("AT+CUSD=1,\"*0#\",15");
            else if (gsmInfo.operation_id == 45202 || gsmInfo.operation_id == 45208)
                SendCommand("AT+CUSD=1,\"*111#\",15");
            else if (gsmInfo.operation_id == 45204)
                SendCommand("AT+CUSD=1,\"*101#\",15");
            */
          
        }

    }
}
