using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Turret
{
    public class ArduinoControllerMain
    {

        SerialPort currentPort;
        bool portFound;
        public event EventHandler OnCommLink;
        public event EventHandler<StringEventArgs> OnData;

        

        public void SetComPort(string comPort)
        {
            try
            {
                currentPort = new SerialPort(comPort, 57600);
                currentPort.Encoding = Encoding.ASCII;
                currentPort.DataReceived += CurrentPort_DataReceived;
                currentPort.Open();

                if (OnCommLink != null)
                    OnCommLink(this, new EventArgs());
                if (OnData != null)
                    OnData(this, new StringEventArgs() { Data = "Online!" });
            }
            catch (Exception e)
            {
                if (OnData != null)
                    OnData(this, new StringEventArgs() { Data = e.Message });
            }
        }

        public void SetComPort(int comPort)
        {
            SetComPort("COM" + comPort);
        }

        public class StringEventArgs : EventArgs
        {
            public string Data { get; set; }
        }

        
        private void CurrentPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();

            if (OnData != null)
                OnData(this, new StringEventArgs { Data = "Data Received: " + indata });
        }

        public void SendMove(int servoPos)
        {        
            // using a signed char means values 0-127, so 
            // we scale down our servo values by 2 and up by 2 on the arduino
            var data = new char[2];
            data[0] = 'M';
            data[1] = Convert.ToChar(servoPos / 2); 
            currentPort.Write(data, 0, 2);
                     
        }

        internal void SendQuit()
        {
            var data = new char[2];
            data[0] = 'Q';
            data[1] = (char)Convert.ToByte(0);
            currentPort.Write(data, 0, 2);
        }

        public void SendFirePrep()
        {
            var data = new char[2];
            data[0] = 'S';
            data[1] = (char)Convert.ToByte(0);
            currentPort.Write(data, 0, 2);
         
        }

        public void SendFire()
        {
            var data = new char[2];
            data[0] = 'F';
            data[1] = (char)Convert.ToByte(0);
            currentPort.Write(data, 0, 2);
         
        }

        internal void Shutdown()
        {
            currentPort.Close();
            currentPort.Dispose();
        }
    }
}
