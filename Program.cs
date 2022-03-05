using Newtonsoft.Json;
using PcarsUDP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;
using System.Text;
using System.Collections;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Linq;

namespace simDash
{
    class Program
    {

        private static Thread serial1Thread;
        private static Thread serial2Thread;
        private static Thread serial3Thread;

        private static Queue outputQueue1;    // From App to Arduino
        private static Queue outputQueue2;    // From App to Arduino
        private static Queue outputQueue3;    // From App to Arduino
        //private static Queue inputQueue1;    // From Arduino to App
        private static Configuration appConfig;

        private static int currentRPM;
        private static int lastRPM;

        public static void StartSerialThreads()
        {
            Console.WriteLine("Starting Serial Threads");
            outputQueue1 = Queue.Synchronized(new Queue());
            outputQueue2 = Queue.Synchronized(new Queue());
            outputQueue3 = Queue.Synchronized(new Queue());

            if (appConfig.dashBrakeCom != null)
            {
                serial1Thread = new Thread(StreamThreadLoop1);
                serial1Thread.Start();
            }

            if (appConfig.speedCom != null)
            {
                serial2Thread = new Thread(StreamThreadLoop2);
                serial2Thread.Start();
            }

            if (appConfig.rpmCom != null)
            { 
                serial3Thread = new Thread(StreamThreadLoop3);
                serial3Thread.Start();
            }
        }

        public static void SendToArduino(string command)
        {
            outputQueue1.Enqueue(command);
            outputQueue2.Enqueue(command);
            outputQueue3.Enqueue(command);
        }


        public static void StreamThreadLoop1()
        {
            // Opens the connection on the serial port
            SerialPort stream1 = new SerialPort(appConfig.dashBrakeCom, appConfig.baudRate, Parity.None);
            stream1.Open();
            Console.WriteLine("Dash Brake Controller COM Port Open on " + appConfig.dashBrakeCom);
            // Looping
            while (true)
            {
                // Send to Arduino
                if (outputQueue1.Count != 0)
                {
                    string command = (string)outputQueue1.Dequeue();
                    stream1.Write(command);
                }
            }
        }

        public static void StreamThreadLoop2()
        {
            // Opens the connection on the serial port
            SerialPort stream2 = new SerialPort(appConfig.speedCom, appConfig.baudRate, Parity.None);
            stream2.Open();
            Console.WriteLine("Speed Controller COM Port Open " + appConfig.speedCom);
            // Looping
            while (true)
            {
                // Send to Arduino
                if (outputQueue2.Count != 0)
                {
                    string command = (string)outputQueue2.Dequeue();
                    stream2.Write(command);
                }
            }
        }

        public static void StreamThreadLoop3()
        {
            // Opens the connection on the serial port
            SerialPort stream3 = new SerialPort(appConfig.rpmCom, appConfig.baudRate, Parity.None);
            stream3.Open();
            Console.WriteLine("RPM Controller COM Port Open on " + appConfig.rpmCom);
            // Looping
            while (true)
            {
                // Send to Arduino
                if (outputQueue3.Count != 0)
                {
                    string command = (string)outputQueue3.Dequeue();
                    stream3.Write(command);
                }
            }
        }


        static void Main(string[] args)
        {
            // Read the config file
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            appConfig = deserializer.Deserialize<Configuration>(File.ReadAllText("config.yaml"));


            UdpClient listener = new UdpClient(5606);                       //Create a UDPClient object
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 5606);       //Start recieving data from any IP listening on port 5606 (port for PCARS2)
            PCars2_UDP uDP = new PCars2_UDP(listener, groupEP);             //Create an UDP object that will retrieve telemetry values from in game.
            dashMessage myMsg = new dashMessage();

            Dictionary<int, string> gearDict = new Dictionary<int, string>();
            gearDict.Add(127, "R");
            gearDict.Add(112, "N");
            gearDict.Add(0, "N");
            gearDict.Add(113, "1");
            gearDict.Add(114, "2");
            gearDict.Add(115, "2");
            gearDict.Add(116, "3");
            gearDict.Add(117, "4");
            gearDict.Add(118, "5");
            gearDict.Add(119, "6");
            gearDict.Add(120, "7");
            gearDict.Add(121, "8");


            StartSerialThreads();

            List<int> rpmBuffer = new List<int>(new int[appConfig.rpmBufferSize]);
            List<double> oilPressureBuffer = new List<double>(new double[appConfig.oilPresBufferSize]);
            List<double> oilTempBuffer = new List<double>(new double[appConfig.oilTempBufferSize]);
            //Console.WriteLine(string.Join(", ", rpmBuffer));
            //Console.WriteLine(rpmBuffer.Average().ToString());

            int msgCount = 0;
            while (true)
            {
                uDP.readPackets();  //Read Packets ever loop iteration
                //Console.WriteLine(uDP.Rpm + " " + uDP.Speed + " " + uDP.GearNumGears);

                rpmBuffer.Add(uDP.Rpm);
                rpmBuffer.RemoveAt(0);
                currentRPM = Convert.ToInt32(rpmBuffer.Average()*appConfig.rpmScaleFactor);
                // Case where RPM goes up to much for increment
                if ((currentRPM - lastRPM) > appConfig.maxRpmInc)
                {
                    currentRPM = lastRPM + appConfig.maxRpmInc;
                }
                // Case where RPM goes up to much for increment
                else if ((currentRPM - lastRPM) < (-1 * appConfig.maxRpmInc))
                {
                    currentRPM = lastRPM - appConfig.maxRpmInc;
                }
                lastRPM = currentRPM;
                myMsg.rpm = (ushort)currentRPM;

                //Console.WriteLine(string.Join(", ", rpmBuffer));
                //Console.WriteLine(rpmBuffer.Average().ToString());

                myMsg.speed = Math.Round(uDP.Speed * 2.23694,2);  //mps to mph
                myMsg.fuel = Math.Round(uDP.FuelLevel*100,2);
                myMsg.brake =  uDP.Brake*4;

                //double oilTemp= (uDP.OilTempCelsius * 9 / 5) + 32 + appConfig.oilTempOffset; // Celsius to F
                double oilTemp = uDP.OilTempCelsius + appConfig.oilTempOffset; // Celsius to F
                oilTempBuffer.Add(oilTemp);
                oilTempBuffer.RemoveAt(0);
                myMsg.oiltemp = Math.Round(oilTemp, 2);  

                double oilPressure = Math.Round((uDP.OilPressureKPa * 0.145038)+ appConfig.oilPresOffset, 1); //KPa to PSI
                oilPressureBuffer.Add(oilPressure);
                oilPressureBuffer.RemoveAt(0);
                oilPressure= Math.Round(oilPressureBuffer.Average(), 2);
                if (oilPressure < 20)
                {
                    oilPressure = 20;
                }
                myMsg.oilpres = oilPressure;

                int currentGear = uDP.GearNumGears;
                myMsg.gear = gearDict[currentGear];

                string output = JsonConvert.SerializeObject(myMsg);

                if (appConfig.verbose)
                {
                    //Console.WriteLine(myMsg.rpm + " " + myMsg.speed + " " + myMsg.fuel + " " + myMsg.oiltemp + " " + myMsg.oilpres + " " + myMsg.gear + " " + myMsg.brake);
                    Console.WriteLine(output);
                }


                if (msgCount > appConfig.messageInterval)
                {
                    output = output + "\n";
                    SendToArduino(output);
                    msgCount = 0;
                }

                msgCount++;


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

                //Console.WriteLine(uDP.ParticipantInfo[uDP.ViewedParticipantIndex, 0] + " " + uDP.ParticipantInfo[uDP.ViewedParticipantIndex, 1] + " " + uDP.ParticipantInfo[uDP.ViewedParticipantIndex, 2]);
                //For Wheel Arrays 0 = Front Left, 1 = Front Right, 2 = Rear Left, 3 = Rear Right.
            }


        }
    }
}
