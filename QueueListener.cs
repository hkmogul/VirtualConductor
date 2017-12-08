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

        /// <summary>
        /// If the first beat has been collected
        /// </summary>
        private bool firstBeat;

        /// <summary>
        /// The time of the previous beat
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// Initialize the listener at the home IP and constant port
        /// </summary>
        public QueueListener()
        {
            Init(IPAddress.Parse("127.0.0.1"), 8001,true);
        }

        /// <summary>
        /// Initialize the QueueListener with a given server IP address, port number, and if the right hand should be used
        /// </summary>
        /// <param name="ip">The IP address to send OSC messages to</param>
        /// <param name="p">The port number to send OSC messages to</param>
        /// <param name="useRightHand">Whether or not the right hand should be detected.</param>
        public QueueListener(IPAddress ip, int p, bool useRightHand)
        {
            Init(ip, p, useRightHand);
        }

        /// <summary>
        /// Centralized initializer for constructor overloads
        /// </summary>
        /// <param name="ip">The IP address to send OSC messages to</param>
        /// <param name="p">The port number to send OSC messages to</param>
        /// <param name="useRightHand">Whether or not the right hand should be detected.</param>
        private void Init(IPAddress ip, int p, bool useRightHand)
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

        /// <summary>
        /// Event listener for initialization
        /// </summary>
        /// <param name="controller">the handle for the LM controller</param>
        public void OnInit(Controller controller)
        {
            Console.WriteLine("Initialized");
        }

        /// <summary>
        /// Event response to connecting to the leapmotion controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnConnect(object sender, DeviceEventArgs args)
        {
            Console.WriteLine("Connected");
        }

        /// <summary>
        /// Event response to disconnecting from the LM controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnDisconnect(object sender, DeviceEventArgs args)
        {
            oscHandler.Dispose();
            Console.WriteLine("Disconnected");
        }

        /// <summary>
        /// Event responder to receiving a new frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnFrame(object sender, FrameEventArgs args)
        {
            // Get the most recent frame and report some basic information
            Frame frame = args.frame;
            InteractionBox box = frame.InteractionBox;
            double xBounds = frame.InteractionBox.Width;
            Hand h;

            // search through detected hands
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
                // no matching hands were detected. just send the last played tempo
                oscHandler.SendTempo();
                return;
            }

            h = frame.Hands[matchIndex];
            
            // further stabilize this position by adding the average of the last three points
            xPositions.EnqueueMovingAverage(h.StabilizedPalmPosition.x,3);

            double xBackAvg = xPositions.BackAverage(.8);
            double xFrontAvg = xPositions.FrontAverage(.2);
            double averageXpos = 0;

            if ((xBackAvg * xFrontAvg < 0 && xPositions.Length() > 20))
            {
                Console.WriteLine("Beat!");
                averageXpos = xPositions.Average();
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
                    oscHandler.SendTempo((int)Math.Round(estimatedTempos.Average()/5)*5);

                    var initialNorm = box.NormalizePoint(initialPosition);
                    var currentNorm = box.NormalizePoint(h.StabilizedPalmPosition);
                    double xDist = Math.Abs(currentNorm.x - initialNorm.x);

                    // reset initial position
                    initialPosition = h.StabilizedPalmPosition;

                    // send volume based on scale of distance
                    oscHandler.SendVolume(xDist*10 > 1 ? 1: xDist*10);


                    double normAvgX = averageXpos / xBounds;
                    oscHandler.SendPan(normAvgX);
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
