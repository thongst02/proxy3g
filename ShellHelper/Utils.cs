using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Linq;
namespace ShellHelper
{
    class Utils
    {
        static readonly DateTime dt1970 = new DateTime(1970, 1, 1);
        public static List<Operation> lsOperation = new List<Operation>() {
        new Operation() { Name = "Vinaphone", Id = 45202, PhoneList = new List<string>() { "091", "094", "088", "083", "084", "085", "081", "082" },
            ATGetPhone="AT+CUSD=1,\"*111#\",15" },
        new Operation() { Name = "Indochina", Id = 45208, PhoneList = new List<string>() { "087" },
            ATGetPhone="AT+CUSD=1,\"*111#\",15" },
        new Operation() { Name = "Viettel", Id = 45204, PhoneList = new List<string>() { "086", "096", "097", "098", "032", "033", "034", "035", "036", "037", "038", "039" } ,
             ATGetPhone="AT+CUSD=1,\"*101#\",15"
        },
        new Operation() { Name = "Mobifone", Id = 45201, PhoneList = new List<string>() { "090", "093", "070", "076", "077", "078", "079" } ,
            ATGetPhone="AT+CUSD=1,\"*0#\",15"},
        new Operation() { Name = "VietnaMobile", Id = 45205, PhoneList = new List<string>() { "092", "052", "058" },
            ATGetPhone="AT+CUSD=1,\"*123#\",15"}
        };
        public static List<System.Threading.Thread> lsThread = new List<System.Threading.Thread>();
        public static Dictionary<int, string> dicOutput = new Dictionary<int, string>();
        public static List<int> lsThreadOut = new List<int>();
        static byte[] SaveStreamToFile(Stream stream)
        {
            if (stream.Length == 0) return new byte[] { 0 };
            // Create a FileStream object to write a stream to a file
            byte[] bytesInStream = new byte[stream.Length];
            stream.Read(bytesInStream, 0, (int)bytesInStream.Length);
            // Use FileStream object to write to the specified file
            //fileStream.Write(bytesInStream, 0, bytesInStream.Length);
            return bytesInStream;
        }
        public static byte[] GetFileResource(string sResourcePath)
        {
            try
            {
                System.Reflection.Assembly _assembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (_assembly == null)
                    return new byte[] { 0 };
                Stream fs = _assembly.GetManifestResourceStream(sResourcePath);
                if (fs == null)
                    return new byte[] { 0 };
                return SaveStreamToFile(fs);
            }
            catch (Exception exc)
            {
            }
            return new byte[] { 0 };
        }
        public static long getTimeMili(bool isUtc = false)
        {
            if (isUtc)
                return Convert.ToInt64((Math.Round((DateTime.UtcNow - dt1970).TotalMilliseconds)));
            return Convert.ToInt64((Math.Round((DateTime.Now - dt1970).TotalMilliseconds)));
        }
        public static List<int> ProcessIdList(string sProcessName)
        {
            var proWvs = Process.GetProcessesByName(sProcessName);
            if (proWvs == null || proWvs.Length == 0)
                return new List<int>();
            return proWvs.Select(x => x.Id).ToList();
        }
        
        public static string Bash(string cmd, int timeout = 3000000)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = timeout>0,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            DateTime dtStart = DateTime.Now;
            process.Start();
            string result = "";
           
            if (timeout > 0)
            {
                new System.Threading.Thread(new System.Threading.ThreadStart(() =>
                {
                    result = process.StandardOutput.ReadToEnd();
                })).Start();
                while (!process.HasExited && process.Responding && (DateTime.Now - dtStart).TotalMilliseconds < timeout)
                {
                    System.Threading.Thread.Sleep(100);
                }
                //process.WaitForExit(timeout);
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch { }
                }
            }
            
            if (string.IsNullOrEmpty(result))
                result = process.Id.ToString();
            return result;
        }
        public static string BashReadProcess(string cmd, int timeout = 3000000)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError=true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                }
            };          
            process.Start();
            if (!dicOutput.ContainsKey(process.Id))
                dicOutput.Add(process.Id, "");           
            var t = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {                
                string standard_output;
                while ((standard_output = process.StandardOutput.ReadLine()) != null)
                {
                    if (lsThreadOut.Contains(process.Id))
                        break;
                    dicOutput[process.Id] += standard_output + Environment.NewLine;
                }
            }));
            t.Start();
            lsThread.Add(t);

            var t2 = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {               
                string standard_output;
                while ((standard_output = process.StandardError.ReadLine()) != null)
                {
                    if (lsThreadOut.Contains(process.Id))
                        break;
                    dicOutput[process.Id] += standard_output + Environment.NewLine;
                    Console.WriteLine($"{process.Id} {standard_output}");
                }
            }));
            t2.Start();
            lsThread.Add(t2);
            return process.Id.ToString();
        }
        public static void KillAll(string sProcess= "wvdial")
        {
            var proWvs = Process.GetProcessesByName(sProcess);
            if (proWvs == null || proWvs.Length == 0)
                return;
            foreach (Process proc in proWvs)
            {
                try
                {
                    proc.Kill();
                }
                catch { }
            }
        }
        public static void KillProceesById(int id)
        {
            try
            {
                var proc = Process.GetProcessById(id);
            if (proc == null)
                return;
            
                proc.Kill();
            }
            catch { }
        }
        public static void SaveConfig(object config, string sConfigName)
        {
            try
            {
                if (string.IsNullOrEmpty(Path.GetDirectoryName(sConfigName)))
                    sConfigName = AppDomain.CurrentDomain.BaseDirectory + sConfigName;
                if (!Directory.Exists(Path.GetDirectoryName(sConfigName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(sConfigName));
                string sText = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                if (!sConfigName.Contains("json"))
                    sConfigName = $"{sConfigName}.json";
                File.WriteAllText(sConfigName, sText);
            }
            catch (Exception exc)
            {
                Console.WriteLine("error save config :" + exc.Message);
            }
        }
        public static T LoadConfig<T>(string sConfigName)
        {
            object parsedValue = default(T);
            if (!sConfigName.Contains(".json"))
                sConfigName = $"{sConfigName}.json";
            if (string.IsNullOrEmpty(sConfigName) || !File.Exists(sConfigName))
                return (T)parsedValue;
            try
            {
                string sText = File.ReadAllText(sConfigName);
                if (string.IsNullOrEmpty(sText))
                    return (T)parsedValue;
                parsedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(sText);
                //parsedValue = Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                parsedValue = null;
            }
            return (T)parsedValue;
        }
    }
    public class Operation
    {
        [Newtonsoft.Json.JsonProperty("PhoneList ")]
        public List<string> PhoneList { get; set; }
        [Newtonsoft.Json.JsonProperty("Id")]
        public int Id { get; set; }
        [Newtonsoft.Json.JsonProperty("Name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("ATGetPhone")]
        public string ATGetPhone { get; set; }
    }
}
