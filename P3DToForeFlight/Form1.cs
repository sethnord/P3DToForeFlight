using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using FSUIPC;
using System.Net.Sockets;
using System.Net;

namespace P3DToForeFlight
{
    public partial class Form1 : Form
    {
        //Global Vars
        Offset groundSpeed = new Offset(692, 4);
        FsPositionSnapshot playerPos = new FsPositionSnapshot();
        UdpClient udp = new UdpClient(); //Create a UDP client, but DON'T bind it.
        decimal lat;
        decimal lon;
        decimal alt;
        decimal hdg;
        UInt32 spd;

        public Form1()
        {
            InitializeComponent();
            label1.Hide();
            if (FSUIPCConnection.IsOpen)
            {
                label1.Show();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Connect to FSUIPC button
            FSUIPCConnection.Open();

            if (FSUIPCConnection.IsOpen)
            {
                label1.Show();
                BroadcastPosition();
            }
        }

        public void BroadcastPosition()
        {
            udp.Connect("192.168.1.103",49002);
            string basePacket = "XGPSSimulator,";
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                
                while (true)
                {
                    playerPos = FSUIPCConnection.GetPositionSnapshot();
                    //Populate positional variables
                    alt = (Decimal)playerPos.Altitude.Feet;
                    FsLatLonPoint location = playerPos.Location;
                    lat = (Decimal)location.Latitude.DecimalDegrees;
                    lon = (Decimal)location.Longitude.DecimalDegrees;
                    hdg = (Decimal)playerPos.HeadingDegreesTrue;
                    FSUIPCConnection.Process();
                    groundSpeed.Reconnect();
                    spd = groundSpeed.GetValue<UInt32>();
                    spd = spd / 65536;
                    spd = spd * 3600;
                    spd = spd / 1852;
                    

                    alt = Decimal.Round(alt, 1);
                    lat = Decimal.Round(lat, 3);
                    lon = Decimal.Round(lon, 3);
                    hdg = Decimal.Round(hdg, 2);

                    //Assemble a packet
                    string packetToSend = basePacket + lon.ToString() + "," + lat.ToString() + "," + alt.ToString() + "," + hdg.ToString() + "," + spd.ToString() + ".0";
                    byte[] sendMe = Encoding.ASCII.GetBytes(packetToSend);

                    //Broadcast via UDP

                    udp.Send(sendMe, sendMe.Length);

                    //Pause the thread to allow for the one second gap required by foreflight.
                    Thread.Sleep(990); //Only 990 to allow for processing time.
                }

            }).Start();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
