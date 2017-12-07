using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Leap;
using LeapTempoC.Objects;
namespace LeapTempoC
{
    /// <summary>
    /// Class for reading frames and detecting beats by means of a moving average queue
    /// </summary>
    class QueueListener
    {
        /// <summary>
        /// Threshold of time before we send a fermata
        /// </summary>
        static readonly TimeSpan fermataThreshold = new TimeSpan(0, 0, 0, 5, 0);

        /// <summary>
        /// Handler for sending OSC messages
        /// </summary>
        private OSCHandler oscHandler;

        /// <summary>
        /// Fixed length queue of stabilized positions of X
        /// </summary>
        private FixedQueue xPositions;

        /// <summary>
        /// Starting position of a beat.
        /// </summary>
        private Vector initialPosition;

        /// <summary>
        /// Fixed length queue of estimated tempos, for smoothing the actual values
        /// </summary>
        private FixedQueue estimatedTempos;

        /// <summary>
        /// IP address to send messages to
        /// </summary>
        private IPAddress serverIP;

        /// <summary>
        /// Port number to send messages to
        /// </summary>
        private int port;

        /// <summary>
        /// If the right hand should be used
        /// </summary>
        private bool UseRightHand;
        private bool firstBeat;
        private DateTime startTime;
        public QueueListener()
        {
            init(IPAddress.Parse("127.0.0.1"), 8001,true);
        }

        public QueueListener(IPAddress ip, int p, bool useRightHand)
        {
            init(ip, p, useRightHand);
        }

        private void init(IPAddress ip, int p, bool useRightHand)
        {
            serverIP = ip;
            port = p;
            
            xPositions = new FixedQueue(1000);
            initialPosition = new Vector();
            UseRightHand = useRightHand;
            firstBeat = true;
            oscHandler = new OSCHandler(serverIP, port);
            estimatedTempos = new FixedQueue(5);
        }
        public void OnInit(Controller controller)
        {
            Console.WriteLine("Initialized");
        }

        public void OnConnect(object sender, DeviceEventArgs args)
        {
            Console.WriteLine("Connected");
        }

        public void OnDisconnect(object sender, DeviceEventArgs args)
        {
            oscHandler.Dispose();
            Console.WriteLine("Disconnected");
        }

        public void OnFrame(object sender, FrameEventArgs args)
        {
            Random r = new Random();
            // Get the most recent frame and report some basic information
            Frame frame = args.frame;
            double xBounds = frame.InteractionBox.Width;
            Hand h;
            if (frame.Hands.Count == 0)
            {
                return;
            }
            int matchIndex = -1;
            for (int i = 0; i < frame.Hands.Count; i++)
            {
                if (frame.Hands[i].IsRight && UseRightHand)
                {
                    matchIndex = i;
                    break;
                }
                else if (frame.Hands[i].IsLeft && !UseRightHand)
                {
                    matchIndex = i;
                    break;
                }
            }

            if (matchIndex < 0)
            {
                return;
            }

            h = frame.Hands[matchIndex];
            //Console.WriteLine(h.StabilizedPalmPosition.x);

            xPositions.EnqueueMovingAverage(h.StabilizedPalmPosition.x,3);

            double xBackAvg = xPositions.BackAverage(.8);
            double xFrontAvg = xPositions.FrontAverage(.2);

            double averageXpos = 0;
            if ((xBackAvg * xFrontAvg < 0 && xPositions.Length() > 20))// || (zBackAvg * zFrontAvg < 0 && zPositions.Length() > 20))
            {
                Console.WriteLine("Beat!");
                averageXpos = xPositions.Average();
                Console.WriteLine(string.Format("Num points read before this was {0}", xPositions.Length()));
                xPositions.Clear();
                if (firstBeat)
                {
                    // not enough data to create a tempo. we will use this as a starting point
                    startTime = DateTime.Now;
                    initialPosition = h.PalmPosition;
                    firstBeat = false;
                    return;
                }
                else
                {
                    TimeSpan span = DateTime.Now - startTime;
                    startTime = DateTime.Now;

                    double durSeconds = span.Seconds + (span.Milliseconds / 1000.0);
                    int estTempo =(int) Math.Round(60 / durSeconds);
                    estimatedTempos.Enqueue(estTempo);
                    oscHandler.SendTempo((int)estimatedTempos.Average());

                    double xDist = Math.Abs(h.StabilizedPalmPosition.x - initialPosition.x);

                    // reset initial position
                    initialPosition = h.StabilizedPalmPosition;
                    oscHandler.SendVolume(xDist*10/xBounds);
                    double normAvgX = averageXpos / xBounds;
                    Console.WriteLine(normAvgX);
                    double sumAvgX = (averageXpos + xBounds) / xBounds;
                    Console.WriteLine(sumAvgX);
                    Console.WriteLine("-----");
                }
            }
            else
            {
                // check if the time between the last beat time and the current time is greater than the threshold for a fermata
            }

        }

        public void OnServiceConnect(object sender, ConnectionEventArgs args)
        {
            Console.WriteLine("Service Connected");
        }

        public void OnServiceDisconnect(object sender, ConnectionLostEventArgs args)
        {
            Console.WriteLine("Service Disconnected");
        }

        public void OnServiceChange(Controller controller)
        {
            Console.WriteLine("Service Changed");
        }

        public void OnDeviceFailure(object sender, DeviceFailureEventArgs args)
        {
            Console.WriteLine("Device Error");
            Console.WriteLine("  PNP ID:" + args.DeviceSerialNumber);
            Console.WriteLine("  Failure message:" + args.ErrorMessage);
        }

        public void OnLogMessage(object sender, LogEventArgs args)
        {
            switch (args.severity)
            {
                case Leap.MessageSeverity.MESSAGE_CRITICAL:
                    Console.WriteLine("[Critical]");
                    break;
                case Leap.MessageSeverity.MESSAGE_WARNING:
                    Console.WriteLine("[Warning]");
                    break;
                case Leap.MessageSeverity.MESSAGE_INFORMATION:
                    Console.WriteLine("[Info]");
                    break;
                case Leap.MessageSeverity.MESSAGE_UNKNOWN:
                    Console.WriteLine("[Unknown]");
                    break;
            }
            Console.WriteLine("[{0}] {1}", args.timestamp, args.message);
        }
    }

}
