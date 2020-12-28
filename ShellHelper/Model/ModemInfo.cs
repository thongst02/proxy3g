using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
namespace ShellHelper.Model
{
    public class ModemInfo
    {
        //public string model { get; set; }
        //public string operation_name { get; set; }
        //public string serial_number { get; set; }
        public int device_id { get; set; }
        public GsmInfo gsmInfo { get; set; }
        //[JsonIgnore]
        public string path_dev { get; set; }
        //[JsonIgnore]
        public string name_dev { get; set; }
        public string proxy_address { get; set; }
        public string ip_address { get; set; }
        [JsonIgnore]
        public string name { get; set; }
        //[JsonIgnore]
        public string bus { get; set; }
        public long time { get; set; }
        public int status { get; set; }
        [JsonIgnore]
        public int procesId { get; set; }
        [JsonIgnore]
        public int pppdId { get; set; }
    }
}
