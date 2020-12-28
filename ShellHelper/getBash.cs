using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
namespace ShellHelper
{
    class getBash
    {
        static string userShare = "/usr/share/proxyppp/";
        string sPath3Proxy = userShare + "proxy_cfg";
        static string ipGateway = "192.168.1.1";
        public void setGateway()
        {
            Utils.Bash("route del default", 5000);
            Utils.Bash("route add default gw 192.168.1.1 wlan0", 5000);
        }

        public string getIPByInterface(int nId)
        {
            string sResult = Utils.Bash("sudo curl -s https://whoer.net/ip --interface ppp" + nId, 10000);
            sResult = sResult.RemoveSpecialChar();
            return sResult;
        }
        public string getIpLocal()
        {
            //ifconfig | sed -En 's/127.0.0.1//;s/.*inet (addr:)?(([0-9]*\.){3}[0-9]*).*/\2/p'
            string sResult = Utils.Bash("ifconfig | sed -En 's/127.0.0.1//;s/.*inet (addr:)?(([0-9]*\\.){3}[0-9]*).*/\\2/p'", 10000);
            if (string.IsNullOrEmpty(sResult) || !sResult.Contains("192.168"))
                return "";
            string[] arr = sResult.Split('\n');            
            return arr.First(x => x.Contains("192.168"));
        }
        public Model.ModemInfo RunModem(Model.ModemInfo info)
        {
            lblStart:
            if (info.procesId != 0)
                Utils.KillProceesById(info.procesId);
            if (info.pppdId != 0)
                Utils.KillProceesById(info.pppdId);
            //List<int> lsLastPppd = Utils.ProcessIdList("pppd");
            info.procesId = Utils.BashReadProcess($"wvdial modem_{info.device_id}", 0).ToInt();
            System.Threading.Thread.Sleep(1000);
            bool bDeviceNotResponding = false;
            for (int i = 0; i < 15; i++)
            {
                System.Threading.Thread.Sleep(2000);
                //Device or resource busy
                if (Utils.dicOutput.ContainsKey(info.procesId))
                {
                    if(Utils.dicOutput[info.procesId].Contains("Modem not responding"))
                    {
                        Console.WriteLine($"{info.path_dev} is not responding!");
                        bDeviceNotResponding = false;
                        break;
                    } else if(Utils.dicOutput[info.procesId].Contains("Device or resource busy"))
                    {
                        Console.WriteLine($"{info.path_dev} is not responding!");
                        bDeviceNotResponding = false;
                        break;
                    }                    
                }
                info.ip_address = getIPByInterface(info.device_id);
                if (!info.ip_address.Contains("."))
                    info.ip_address = "";
                if (!string.IsNullOrEmpty(info.ip_address))
                    break;
            }
            if(bDeviceNotResponding)
            {
                getAllDevice.UnbindDev(info.bus);
                System.Threading.Thread.Sleep(1000);
                getAllDevice.CheckCDRom();
                System.Threading.Thread.Sleep(1000);
                goto lblStart;
            }
            if (Utils.dicOutput.ContainsKey(info.procesId))
            {
                string sPid3d = Utils.dicOutput[info.procesId].Regx("Pid of pppd: \\d+");
                if (!string.IsNullOrEmpty(sPid3d))
                {
                    info.pppdId = sPid3d.Regx("\\d+").ToInt()+1;                   
                    Console.WriteLine($"pppd {info.pppdId} wvdial: {info.procesId}");
                }              
            }

            Utils.lsThreadOut.Add(info.procesId);
            //List<int> lsCurrent = Utils.ProcessIdList("pppd");
            //info.pppdId = lsCurrent.Find(x => !lsLastPppd.Contains(x));
            //Console.WriteLine($"info.pppdId {info.pppdId} wvdial: {info.procesId}");
            if (info.pppdId == 0 && info.procesId != 0)
            {
                Utils.KillProceesById(info.procesId);
                info.status = 0;
                info.time = 0;
            }
            else
            {
                info.time = Utils.getTimeMili(true);
                info.status = 1;
            }
            //setGateway();
            return info;
        }
        public void StopModem(Model.ModemInfo info)
        {
            if (info.procesId != 0)
                Utils.KillProceesById(info.procesId);
            if (info.pppdId != 0)
                Utils.KillProceesById(info.pppdId);
        }
        void Run3Proxy()
        {
            Utils.Bash($"3proxy {sPath3Proxy}", 0);
        }
        Model.GsmInfo getModem(string DeviceId)
        {
            Model.GsmInfo gsmInfo = new Model.GsmInfo();
            string sTest = Utils.Bash($"gsmctl -d {DeviceId} CURROP ME", 5000);
            if (sTest.Contains("Numeric name"))
            {
                //Console.WriteLine("Op: " + sTest.Regx("4\\d{4,}"));
                gsmInfo.operation_id = sTest.Regx("4\\d{4,}").ToInt();
                if (gsmInfo.operation_id != 0)
                {
                    var Opinfo = Utils.lsOperation.Find(x => x.Id == gsmInfo.operation_id);
                    if (Opinfo != null)
                        gsmInfo.operation_name = Opinfo.Name;
                }
            }
            if (sTest.Contains("Serial"))
            {
                string Serial = sTest.Regx("Serial Number: \\d+");
                if (!string.IsNullOrEmpty(Serial))
                    Serial = Serial.Replace("Serial Number: ", "");
                //Console.WriteLine("Serial Number: " + Serial);
                gsmInfo.serial_number = Serial;
            }
            if (sTest.Contains("Model"))
            {
                string Model = sTest.Regx("Model:.+\n");
                if (!string.IsNullOrEmpty(Model))
                    Model = Model.Replace("Model: ", "").Trim();
                //Console.WriteLine("Model: " + Model);
                gsmInfo.model = Model;
            }
            return gsmInfo;
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
                sDeviceId=arrLine.First(x=>x.Contains("/dev/"));
                if (string.IsNullOrEmpty(sDeviceId))
                    continue;                
                Model.ModemInfo modemInfo = new Model.ModemInfo();
                modemInfo.name_dev = arrLine.First(x => x.Contains("/tty"));
                modemInfo.path_dev = sDeviceId;
                lsDevice.Add(modemInfo);
                Console.WriteLine(modemInfo.path_dev);
            }
            Console.WriteLine($"lsDevice {lsDevice.Count}");
            return lsDevice;
        }
        
        public List<Model.ModemInfo> getDeviecsAvaliable()
        {
            var lsDevice = getDevices();
            if (lsDevice == null || lsDevice.Count == 0)
                return null;
            List<Model.ModemInfo> lsAvaliable = new List<Model.ModemInfo>();
            List<Model.ModemInfo> lsAvaliableOld = Utils.LoadConfig<List<Model.ModemInfo>>("avaliable.json");
            if (lsAvaliableOld == null)
                lsAvaliableOld = new List<Model.ModemInfo>();
            string sPathTemp = "";
            foreach (var modem in lsDevice)
            {
                if (string.IsNullOrEmpty(modem.path_dev))
                    continue;
                sPathTemp = modem.path_dev.ReplaceRegx("\\d+-port\\d+", "");               
                if(!string.IsNullOrEmpty(sPathTemp))
                {
                    var avaliabOld = lsAvaliableOld.Find(x => !string.IsNullOrEmpty(x.path_dev) && x.path_dev.Contains(sPathTemp));
                    if (avaliabOld != null && avaliabOld.path_dev != modem.path_dev)
                    {
                        Console.WriteLine($"Find Old {modem.path_dev}");
                        continue;
                    }
                }
                var gsmInfo = getModem(modem.path_dev);
                System.Threading.Thread.Sleep(500);
                if (gsmInfo == null || string.IsNullOrEmpty(gsmInfo.serial_number))
                    continue;                
                modem.name_dev = modem.name_dev.Replace("../../", "/dev/");
                modem.gsmInfo = gsmInfo;
                Console.WriteLine("============================");
                Console.WriteLine(modem.path_dev);
                Console.WriteLine(gsmInfo.model);
                Console.WriteLine(gsmInfo.serial_number);
                Console.WriteLine("============================");
                lsAvaliable.Add(modem);
            }
            lsAvaliable = lsAvaliable.OrderBy(a => a.name_dev).ThenBy(a => a.path_dev).ToList();
            Utils.SaveConfig(lsAvaliable, "avaliable.json");
            //System.IO.File.WriteAllText("avaliable.json", Newtonsoft.Json.JsonConvert.SerializeObject(lsAvaliable));


            //int iCount = 3000;
            //string ipLocal = getIpLocal();
            //lsAvaliable.ForEach(x => x.proxy_address = ipLocal + ":" + iCount++);
            //iCount = 0;
            //lsAvaliable.ForEach(x => x.device_id = iCount++);

            //CreateWvdialProfile(lsAvaliable);
            //CreateConfig3proxy(ipLocal, lsAvaliable);
            //Run3Proxy();
            return lsAvaliable;
        }
        

        public void CreateProfile(List<Model.ModemInfo> deviceAvaliables)
        {
            if (deviceAvaliables == null || deviceAvaliables.Count == 0)
                return;
            int iCount = 3000;
            string ipLocal = getIpLocal();
            deviceAvaliables.ForEach(x => x.proxy_address = ipLocal + ":" + iCount++);
            iCount = 0;
            deviceAvaliables.ForEach(x => x.device_id = iCount++);

            CreateWvdialProfile(deviceAvaliables);
            CreateConfig3proxy(ipLocal, deviceAvaliables);
            Run3Proxy();
        }

        bool CreateMypppD(int id)
        {            
            if (!Directory.Exists(userShare))
                Directory.CreateDirectory(userShare);
            string sPath = $"{userShare}myppd{id}";
            File.WriteAllText(sPath, "/usr/sbin/pppd $@ unit " + id);
            Utils.Bash($"chmod 777 {sPath}",5000);
            return true;
        }
        string CreateWvdialProfile(Model.ModemInfo modem)
        {
            CreateMypppD(modem.device_id);
            string sProfile = $"[Dialer modem_{modem.device_id}]{Environment.NewLine}";
            sProfile += $"PPPD Path = {userShare}myppd{modem.device_id}{Environment.NewLine}";
            sProfile += $"Modem = {modem.path_dev}{Environment.NewLine}";
            return sProfile;
        }
        bool CreateWvdialProfile(List<Model.ModemInfo> modemInfos)
        {
            if (modemInfos == null || modemInfos.Count == 0)
                return false;
            string sProfile = "";
            sProfile += "[Dialer Defaults]" + Environment.NewLine;
            sProfile += "Init1 = ATZ " + Environment.NewLine;
            sProfile += "Init2 = ATQ0 V1 E1 S0=0 &C1 &D2 +FCLASS=0" + Environment.NewLine;
            sProfile += "Init3 = AT+CGDCONT=1,\"IP\",\"internet\"" + Environment.NewLine;
            sProfile += "Stupid Mode =1" + Environment.NewLine;
            sProfile += "Modem Type = Analog Modem" + Environment.NewLine;
            sProfile += "ISDN = 0" + Environment.NewLine;
            sProfile += "Phone = *99#" + Environment.NewLine;
            sProfile += "Username = { }" + Environment.NewLine;
            sProfile += "Password = { }" + Environment.NewLine;
            sProfile += "Baud = 460800" + Environment.NewLine;
            sProfile += "Auto DNS = off" + Environment.NewLine;
            sProfile += "check DNS = no" + Environment.NewLine;
            sProfile += "New PPPD = yes" + Environment.NewLine;
            sProfile += "Auto Reconnect =on" + Environment.NewLine;
            sProfile += "Check Def Route =off" + Environment.NewLine;
            foreach (var modems in modemInfos)
            {
                sProfile += CreateWvdialProfile(modems);
            }
            File.WriteAllText("/etc/wvdial.conf", sProfile);
            return true;
        }
        
        bool CreateConfig3proxy(string ipLocal, List<Model.ModemInfo> modemInfos)
        {
            if (!Directory.Exists(userShare))
                Directory.CreateDirectory(userShare);            
            string sConfig = "";
            int nCount = 3000;
            //sConfig += $"proxy -a -i{ipLocal} -p80 -Dewlan0";
            foreach (var modem in modemInfos)
            {
                if (!string.IsNullOrEmpty(sConfig))
                    sConfig += Environment.NewLine;
                sConfig += $"proxy -a -i{ipLocal} -p{modem.device_id + nCount} -Deppp{modem.device_id}";
            }
            
            File.WriteAllText(sPath3Proxy, sConfig);
            System.Threading.Thread.Sleep(100);
            Utils.Bash($"chmod 777 {sPath3Proxy}", 5000);
            return true;
        }
    }
}
