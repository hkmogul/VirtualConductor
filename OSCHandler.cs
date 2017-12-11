using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rug.Osc;
using System.Net;
namespace LeapTempoC.Objects
{
    class OSCHandler :IDisposable
    {
        private IPAddress address;
        private int port;
        private readonly string tempoAddr = "/tempo";
        private readonly string panAddr = "/pan";
        private readonly string volumeAddr = "/volume";
        private int lastSentTempo;
        private double lastSentPan;
        private double lastSentVolume;
        public OSCHandler()
        {
            address = IPAddress.Parse("127.0.0.1");
            port = 8590;
            Initialize();
        }

        public OSCHandler(IPAddress addr, int p)
        {
            address = addr;
            port = p;
            Initialize();
        }

        public void Initialize()
        {
            lastSentPan = -1;
            lastSentTempo = -1;
            lastSentVolume = -1;
        }

        public void SendMessage(string str, int num)
        {
            var mess = new OscMessage(str, num);
            using (OscSender sender = new OscSender(address, port))
            {
                sender.Connect();
                sender.Send(mess);
            }
        }

        public void SendTempo()
        {
            SendTempo(lastSentTempo);
        }

        public void SendTempo(int t)
        {
  
            if (t != lastSentTempo)
            {
                var mess = new OscMessage(tempoAddr, t);
                Console.WriteLine(string.Format("Sending tempo {0}", t));
                using (OscSender sender = new OscSender(address, port))
                {
                    sender.Connect();
                    sender.Send(mess);
                }
                lastSentTempo = t;
            }
        }

        public void SendPan(double p)
        {   
            if (p != lastSentPan )
            {
                int val = (int)(p * 100);
                var mess = new OscMessage(panAddr, val);
                Console.WriteLine(string.Format("Sending pan {0}", val));

                using (OscSender sender = new OscSender(address, port))
                {
                    sender.Connect();
                    sender.Send(mess);
                }

                lastSentPan = p;
            }
        }

        public void SendVolume(double v)
        {
            if (v != lastSentVolume)
            {
                int val = (int)(v * 100);
                var mess = new OscMessage(volumeAddr, val);
                Console.WriteLine(string.Format("Sending volume {0}", val));

                using (OscSender sender = new OscSender(address, port))
                {
                    sender.Connect();
                    sender.Send(mess);
                }

                lastSentPan = v;
            }
        }

        public void SendFermata()
        {
            var mess = new OscMessage(tempoAddr, 0);
            Console.WriteLine(string.Format("Sending tempo {0}", 0));
            using (OscSender sender = new OscSender(address, port))
            {
                sender.Connect();
                sender.Send(mess);
            }

        }

        public void Dispose()
        {

        }
    }
}
