using System;


namespace simDash {
    public class Configuration
    {
        public int baudRate { get; set; }
        public string dashBrakeCom { get; set; }
        public string rpmCom { get; set; }
        public string speedCom { get; set; }
        public int messageInterval { get; set; }
        public bool verbose { get; set; }
        public double rpmScaleFactor { get; set; }
        public int maxRpmInc { get; set; }
        public int rpmBufferSize { get; set; } = 10;
        public int oilTempBufferSize { get; set; } = 10;
        public int oilTempOffset { get; set; } = 0;
        public int oilPresBufferSize { get; set; } = 10;
        public int oilPresOffset { get; set; } = 0;
    }
 }
