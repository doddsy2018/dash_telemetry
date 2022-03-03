using System;


namespace simDash {
    public class dashMessage
    {

        /*
        '{"speed":' + [SpeedMph]
        + ',"rpm":' + [Rpms]
        + ',"fuel":' + [Fuel]
        + ',"oiltemp":' + [OilTemperature]
        + ',"oilpres":' + [OilPressure]
        + ',"brake":' + [Brake]
        + ',"gear":"' + [Gear] + '"'
        + '}\n'
        */

        public double speed { get; set; } = 0;
        public ushort rpm { get; set; } = 0;
        public double fuel { get; set; } = 0;
        public double oiltemp { get; set; } = 0;
        public double oilpres { get; set; } = 0;
        public int brake { get; set; } = 0;
        public string gear { get; set; } = "N";
    
    }
 }
