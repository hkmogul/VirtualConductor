using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leap;
using System.Threading;
using LeapTempoC.Objects;
using System.Net;
namespace LeapTempoC
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Leap.IController controller = new Leap.Controller())
            {
                controller.SetPolicy(Leap.Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);
                IPAddress ip;
                int port;
                bool useRightHand;
                Console.WriteLine(string.Join("--", args));
                if (args.Count() >= 3)
                {
                    ip = IPAddress.Parse(args[0]);
                    port = int.Parse(args[1]);
                    useRightHand = bool.Parse(args[2]);
                }
                else
                {
                    ip = IPAddress.Parse("127.0.0.1");
                    port = 8001;
                    useRightHand = true;
                }

                Console.WriteLine(string.Format("IP address is {0}, port is {1}, using {2} hand", ip, port, useRightHand ? "right" : "left"));

                // Set up our listener:
                QueueListener listener = new QueueListener(ip, port, useRightHand);
                controller.Connect += listener.OnServiceConnect;
                controller.Disconnect += listener.OnServiceDisconnect;
                controller.FrameReady += listener.OnFrame;
                controller.Device += listener.OnConnect;
                controller.DeviceLost += listener.OnDisconnect;
                controller.DeviceFailure += listener.OnDeviceFailure;
                controller.LogMessage += listener.OnLogMessage;

                // Keep this process running until Enter is pressed
                Console.WriteLine("Press any key to quit...");
                Console.ReadLine();
            }
        }
    }
}
