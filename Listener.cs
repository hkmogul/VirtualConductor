using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leap;
using LeapTempoC.Objects;
namespace LeapTempoC
{
    /// <summary>
    /// Class for reading frames and handling the Palm object. TODO: OSC messages and moving this to its own file
    /// </summary>
    class Listener
    {
        Palm palm;
        static readonly TimeSpan fermataThreshold = new TimeSpan(0, 0, 0, 5, 0);
        bool handStopped;
        private InteractionBox bounder;
        private OSCHandler oscHandler;

        public Listener()
        {
            palm = new Palm();
            handStopped = false;
            bounder = new InteractionBox();
        }
        public void OnInit(Controller controller)
        {
            Console.WriteLine("Initialized");
        }

        public void OnConnect(object sender, DeviceEventArgs args)
        {
            oscHandler = new OSCHandler(); // todo: this needs to pass in any optional address or port
            Console.WriteLine("Connected");
        }

        public void OnDisconnect(object sender, DeviceEventArgs args)
        {
            oscHandler.Dispose();
            Console.WriteLine("Disconnected");
        }

        public void OnFrame(object sender, FrameEventArgs args)
        {
            // Get the most recent frame and report some basic information
            Frame frame = args.frame;
            Hand h;
            if (frame.Hands.Count == 0)
            {
                return;
            }

            h = frame.Hands[0];
            Console.WriteLine(string.Format("Velocity is {0}", h.PalmVelocity));
            palm.Move(h.PalmPosition, h.PalmVelocity);
            if (palm.Stopped)
            {
                // check if this is the first time we're reading that the hand is stopped
                if (!handStopped)
                {
                    Console.WriteLine("Beat!");
                    handStopped = true;
                    // possible beat. get volume data, centroid of movement, and append the tempo to the queue
                    TimeSpan dur = palm.PreviousDuration;
                    Vector centroid = palm.Centroid;
                    Vector startPos = palm.StartPosition;
                    Vector stopPos = palm.StopPosition;
                    oscHandler = new OSCHandler();
                    float estTempo = 60 * 1000 / dur.Milliseconds;
                    oscHandler.SendTempo((int)Math.Round(estTempo));

                    // X centroid of hand during movement = pan
                    Vector normCentroid = bounder.NormalizePoint(centroid);
                    oscHandler.SendPan((double)normCentroid.x);

                    // get normalized distance by normalizing the points
                    Vector startNorm = bounder.NormalizePoint(startPos);
                    Vector stopNorm = bounder.NormalizePoint(stopPos);
                    oscHandler.SendVolume(stopNorm.DistanceTo(startNorm));
                }

                // otherwise, check if the hand meets fermata threshold
                else if (palm.DurationStopped > Listener.fermataThreshold)
                {
                    Console.WriteLine(string.Format("Duration stopped is {0}, threshold is {1}", palm.DurationStopped, Listener.fermataThreshold));
                    oscHandler = new OSCHandler();
                    oscHandler.SendFermata();
                }
            }
            else
            {
                // check if it was stopped previously
                if (handStopped)
                {
                    oscHandler = new OSCHandler();
                    oscHandler.SendTempo();
                }

                handStopped = false;
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
