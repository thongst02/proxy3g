
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
namespace ShellHelper
{
    public static class Program
    {
        static List<Model.ModemInfo> lsModem = null;

        static void Main(string[] args)
        {                                                
            var getbashObj = new getBash();
            Utils.KillAll("wvdial");
            Utils.KillAll("3proxy");
            Utils.KillAll("pppd");
            getAllDevice.CheckCDRom();
            Stopwatch st = new Stopwatch();           
            st.Start();
            lsModem = new getAllDevice().getDeviecsAvaliable();
            st.Stop();            
            Console.WriteLine($"getAllDevice.getDeviecsAvaliable: {st.Elapsed.TotalMilliseconds}");

            getbashObj.CreateProfile(lsModem);
            WebServer ws;
            try
            {
                ws = new WebServer(SendResponse, new System.Collections.Generic.List<string>() {"http://*/" });
                ws.Run();
                new System.Threading.Thread(new System.Threading.ThreadStart(()=>
                {
                    ThreadMain();
                })).Start();
                Console.WriteLine("Server is start.");
            }
            catch (Exception exc)
            {
                Console.WriteLine("Web: " + exc.Message);
            }
            
            Console.ReadLine();
        }
        static string SendResponse(HttpListenerRequest request)
        {
            string Head = "";
            Regex regular = new Regex("jQuery(\\w+)");
            Match matchcfg = regular.Match(request.RawUrl);
            if (matchcfg.Success)
                Head = matchcfg.Value;
            if (request.RawUrl.Contains("favicon.ico") || 
                request.RawUrl.Contains("index.html") || 
                request.RawUrl.EndsWith("app.js")||
                request.RawUrl.EndsWith("logo_default.png") ||
                request.RawUrl.EndsWith("logo_default@2x.png") ||
                request.RawUrl.EndsWith("logo_default.png") ||
                request.RawUrl.EndsWith("home.html") ||
                request.RawUrl.EndsWith("jquery.min.js") ||
                request.RawUrl.EndsWith("style.css") ||
                request.RawUrl.EndsWith("logo_default.png"))
                return "";
            //logo_default.png
            if (request.RawUrl == "/data/sim.json")
            {
                //Changed = false;
                //var ls = GetDataSim();
                //return JsonConvert.SerializeObject(new { total = ls.Count, header = "SIM INFO", records = ls });
                return "{\"data\":1}";
            }            
            string _Data = "", _Command;
            try
            {
                //"/reset?id=1"
                _Command = request.RawUrl.Regx("/[\\w|_|\\.]+\\??");
                if (string.IsNullOrEmpty(_Command))
                    return "{\"message\":\"not found\"}";
                _Command = _Command.Replace("/", "").Replace("?", "");
                _Command = _Command.ToLower();

                switch (_Command)
                {
                    case "reset":
                        if (!request.QueryString.HasKeys() || request.QueryString["id"] == null)
                            return "{\"message\":\"not found id\"}";
                        return ResetModem(request.QueryString["id"]);
                    case "stop":
                        if (!request.QueryString.HasKeys() || request.QueryString["id"] == null)
                            return "{\"message\":\"not found id\"}";
                        return StopModem(request.QueryString["id"]);
                        
                    case "data":
                        return Newtonsoft.Json.JsonConvert.SerializeObject(lsModem);
                    case "data.json":
                        return Newtonsoft.Json.JsonConvert.SerializeObject(lsModem);
                }
            }
            catch (Exception exe) {
                Console.WriteLine("exc: " + exe.Message);
                return "{\"json\":1}"; }
            if (lsModem == null)
                return "{\"message\":\"not found\"}";
            else
                return Newtonsoft.Json.JsonConvert.SerializeObject(lsModem);
        }
       static string ResetModem(string id)
        {
            if (lsModem == null || lsModem.Count == 0)
                return "{\"message\":\"not found modem\", \"success\":false}";
            int nDeviceId = id.ToInt();
            var modem = lsModem.Find(x => x.device_id == nDeviceId);
            if (modem == null)
                return "{\"message\":\"not found device id "+ id + "\", \"success\":false}";
            modem.ip_address = "";
            modem.status = 0;
            System.Diagnostics.Stopwatch st = new Stopwatch();
            st.Start();
            modem = new getBash().RunModem(modem);
            st.Stop();
            if (modem.status == 0)
                return "{\"message\":\"connect fail\", \"success\":false,\"time\":" + Math.Round(st.Elapsed.TotalMilliseconds) + "}";
            return "{\"message\":\"connect success\" ,\"ip\":\"" + modem.ip_address + "\", \"success\":true,\"time\":" + Math.Round(st.Elapsed.TotalMilliseconds) + "}";
        }
        static string StopModem(string id)
        {
            if (lsModem == null || lsModem.Count == 0)
                return "{\"message\":\"not found modem\", \"success\":false}";
            int nDeviceId = id.ToInt();
            var modem = lsModem.Find(x => x.device_id == nDeviceId);
            if (modem == null)
                return "{\"message\":\"not found modem\", \"success\":false}";
            modem.ip_address = "";
            modem.status = 0;
            modem.procesId = 0;
            modem.time = 0;
            new getBash().StopModem(modem);
            return "{\"message\":\"stoped device id " + id + "\", \"success\":true}";
            
        }
        static void ThreadMain()
        {
            var getB = new getBash();
            while(true)
            {
                System.Threading.Thread.Sleep(30000);
                if (lsModem == null)
                    continue;
                foreach(var modem in lsModem)
                {
                    if (modem.status == 0)
                        continue;
                    modem.ip_address = getB.getIPByInterface(modem.device_id);
                }
            }
        }
    }
}
