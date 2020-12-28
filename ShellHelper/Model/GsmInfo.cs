using System;
using System.Collections.Generic;
using System.Text;

namespace ShellHelper.Model
{
    public class GsmInfo
    {
        public string model { get; set; }
        public string signal{ get; set; }
        public string manufacturer { get; set; }
        public string serial_number { get; set; }
        public int operation_id { get; set; }
        public string operation_name { get; set; }
        public string phone_number { get; set; }
    }
}
