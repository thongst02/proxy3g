using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ShellHelper
{
    public class getAllDevice
    {
        public static void CheckCDRom()
        {
            string sResult = Utils.Bash($"ls -l /dev/sr*", 5000);
            string[] arrResult = sResult.Split('\n');
            string sDeviceId = "";
            foreach (string sLine in arrResult)
            {
                if (string.IsNullOrEmpty(sLine) || !sLine.Contains("/dev/sr"))
                    continue;
                sDeviceId = sLine.Regx("/dev/sr\\d+");
                if (string.IsNullOrEmpty(sDeviceId))
                    continue;
                Utils.Bash($"eject {sDeviceId}");
                System.Threading.Thread.Sleep(1000);
            }
        }
        List<Model.ModemInfo> getDevices()
        {
            string sResult = Utils.Bash($"ls -l /dev/serial/by-path/*");
            string[] arrResult = sResult.Split('\n');
            string[] arrLine = null;
            string sDeviceId = "";
            List<Model.ModemInfo> lsDevice = new List<Model.ModemInfo>();
            foreach (string sLine in arrResult)
            {
                if (string.IsNullOrEmpty(sLine) || !sLine.Contains("/dev/"))
                    continue;
                arrLine = sLine.Split(' ');
                sDeviceId = arrLine.First(x => x.Contains("/dev/"));
                if (string.IsNullOrEmpty(sDeviceId))
                    continue;
                Model.ModemInfo modemInfo = new Model.ModemInfo();
                modemInfo.name_dev = arrLine.First(x => x.Contains("/tty"));
                modemInfo.path_dev = sDeviceId;
                modemInfo.bus = modemInfo.path_dev.Regx(":[\\d|\\.]+:");
                lsDevice.Add(modemInfo);
                //Console.WriteLine(modemInfo.path_dev);
            }
            Console.WriteLine($"Devices {lsDevice.Count}");
            return lsDevice;
        }
        Model.GsmInfo getModemInfo(string path_dev)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var atCommand = new ATCommand(path_dev);
            atCommand.SendCommand("AT");
            System.Threading.Thread.Sleep(50);
            atCommand.WaitForRead(1000);
            if (atCommand.isCanRead)
            {               
                atCommand.SendCommand("AT+CMGF=1");
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CIMI");
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CGSN");
                System.Threading.Thread.Sleep(50);
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CGMI");
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CUSD=2");
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CGMR");
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CGMM");
                System.Threading.Thread.Sleep(50);
                atCommand.SendCommand("AT+CSCS");
                System.Threading.Thread.Sleep(50);

                atCommand.SendCommand("AT+CSQ");
                System.Threading.Thread.Sleep(50);
                //atCommand.getPhonenumber();
                //atCommand.WaitForPhone(5000);
                //atCommand.SendCommand("AT+CUSD=2");
                //if (atCommand.gsmInfo != null)
                //{
                //    Console.WriteLine($"serialNumber:\t{atCommand.gsmInfo.serial_number}");
                //    Console.WriteLine($"manufacturer:\t{atCommand.gsmInfo.manufacturer}");
                //    Console.WriteLine($"model:\t{atCommand.gsmInfo.model}");
                //    Console.WriteLine($"signal:\t{atCommand.gsmInfo.signal}");
                //    Console.WriteLine($"operation_name:\t{atCommand.gsmInfo.operation_name}");
                //    Console.WriteLine($"phoneNumber :\t{atCommand.gsmInfo.phone_number}");
                //}
            }
            //else
            //    Console.WriteLine("Port close!");
            atCommand.Disconnect();
            stopwatch.Stop();
            //Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalMilliseconds}");
            return atCommand.gsmInfo;
        }
        public static void UnbindDev(string idUsb)
        {
            //Console.WriteLine("File: " + File.Exists("/sys/bus/usb/drivers/usb/1-1.7.4/"));
            //Console.WriteLine("Directory: " + Directory.Exists("/sys/bus/usb/drivers/usb/1-1.7.4/"));
            if (string.IsNullOrEmpty(idUsb))
                return;
            string sBusDevice = Utils.Bash("lsusb", 5000);
            if (string.IsNullOrEmpty(sBusDevice))
                return;
            List<string> lsBus = new List<string>();
            System.Text.RegularExpressions.Regex regx = new System.Text.RegularExpressions.Regex("Bus \\d+");
            System.Text.RegularExpressions.MatchCollection mts = regx.Matches(sBusDevice);

            if (mts != null && mts.Count > 0)
            {
                foreach (System.Text.RegularExpressions.Match mat in mts)
                {
                    if (!mat.Success)
                        continue;
                    string buss = mat.Value.Replace("Bus ", "");
                    if (!lsBus.Contains(buss))
                        lsBus.Add(buss);
                }
            }
            idUsb = idUsb.Replace(":", "");
            string sPathSysDev = "";
            List<string> lsDevicePath = new List<string>();
            foreach (string sBus in lsBus)
            {
                sPathSysDev = $"/sys/bus/usb/drivers/usb/{sBus.ToInt()}-{idUsb}/";
                Console.WriteLine(sPathSysDev);
                if (!Directory.Exists(sPathSysDev))
                {
                    sPathSysDev = "";
                    continue;
                }
                lsDevicePath.Add($"{sBus.ToInt()}-{idUsb}");
            }
            if (lsDevicePath == null || lsDevicePath.Count == 0)
            {
                Console.WriteLine("not found path sys");
                return;
            }
            foreach (string sPathSys in lsDevicePath)
            {
                Console.WriteLine($"found path {sPathSys}");
                Utils.Bash($"echo '{sPathSys}' > \"/sys/bus/usb/drivers/usb/unbind\"", 5000);
                Utils.Bash($"echo '{sPathSys}' > \"/sys/bus/usb/drivers/usb/bind\"", 5000);
            }
            return;
        }
        public List<Model.ModemInfo> getDeviecsAvaliable()
        {
            var devices = getDevices();
            if (devices == null || devices.Count == 0)
                return null;
            List<System.Threading.Thread> threads = new List<System.Threading.Thread>();
            List<Model.ModemInfo> deviceAvaliables = new List<Model.ModemInfo>();
            List<string> lsBus = devices.Select(x => x.bus).Distinct().ToList();
            for(int i=0;i< devices.Count;i++)
            {
                var t = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart((object obj) =>
                {
                    var device= ((Model.ModemInfo)obj);
                    device.gsmInfo = getModemInfo(device.path_dev);
                    if (device.gsmInfo == null)
                        return;
                    lock (deviceAvaliables)
                    {
                        deviceAvaliables.Add(device);
                        if (lsBus.Contains(device.bus))
                            lsBus.Remove(device.bus);
                    }
                }));
                t.Start(devices[i]);
                threads.Add(t);
            }            
            foreach (var t in threads)
                t.Join(15000);
            System.Threading.Thread.Sleep(500);            
            if (deviceAvaliables == null || deviceAvaliables.Count == 0)
            {
                Console.WriteLine("not found devices");
                return null;
            }
            deviceAvaliables = deviceAvaliables.OrderBy(a => a.gsmInfo.serial_number).ThenBy(a => a.name_dev).ToList();
            if (lsBus.Count > 0)
            {
                Console.WriteLine($"bus not connect {lsBus.Count}");
                foreach (string bus in lsBus)
                {
                    Console.WriteLine(bus);
                    UnbindDev(bus);
                }
            }
            Utils.SaveConfig(deviceAvaliables, "deviceAvaliables.json");
            Console.WriteLine($"deviceAvaliables {deviceAvaliables.Count}");
            return deviceAvaliables;
        }
    }
}
