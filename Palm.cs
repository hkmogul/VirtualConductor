using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Leap;
namespace LeapTempoC.Objects
{
    class Palm
    {
        /// <summary>
        /// Maximum magnitude threshold to consider a stop
        /// </summary>
        private static readonly float velocityStopThreshold = 10;

        private static readonly float velocityStartThreshold = 40;

        /// <summary>
        /// Maximum magnitude of dot product of previous and current velocity to consider a stop
        /// </summary>
        private static readonly double velocityDotThreshold = -0.5;

        /// <summary>
        /// Minimum time the hand had to be moving for a stop to be considered
        /// </summary>
        private static readonly TimeSpan MinimumMotionDuration = new TimeSpan(0, 0, 0, 0, 500);

        private static readonly float DistanceThreshold = 500;
        /// <summary>
        /// If the hand is currently stopped
        /// </summary>
        public bool Stopped
        {
            get;
            private set;
        }

        /// <summary>
        /// The distance traveled so far in the current beat. Used to denote volume
        /// </summary>
        public float DistanceTraveled
        {
            get;
            private set;
        }

        /// <summary>
        /// The last velocity of the palm
        /// </summary>
        public Vector PreviousVelocity
        {
            get;
            private set;
        }

        public Vector StartPosition
        {
            get;
            private set;
        }

        public Vector StopPosition
        {
            get;
            private set;
        }
        /// <summary>
        /// Average position of the hand (while moving)
        /// </summary>
        public Vector Centroid
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of elements to average over when doing the weighted average
        /// </summary>
        private int nElements;

        /// <summary>
        /// How long the hand has stopped. Used to check if a fermata message should send
        /// </summary>
        public TimeSpan DurationStopped
        {
            get
            {
                if (Stopped)
                {
                    return DateTime.Now - StopTimeInit;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }


        public TimeSpan DurationMoving
        {
            get
            {
                if (!Stopped)
                {
                    return DateTime.Now - StartTimeInit;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }
        /// <summary>
        /// The previous distance of the hand
        /// </summary>
        public float PreviousDistance
        {
            get;
            private set;
        }
        
        /// <summary>
        /// How long the hand was in motion previously
        /// </summary>
        public TimeSpan PreviousDuration
        {
            get;
            private set;
        }

        /// <summary>
        /// Time that the hand stopped moving
        /// </summary>
        private DateTime StopTimeInit;

        /// <summary>
        /// Last detected position of the hand
        /// </summary>
        private Vector PreviousPosition;
        
        /// <summary>
        /// Time that the hand started moving
        /// </summary>
        private DateTime StartTimeInit;

        /// <summary>
        /// Represents the palm from the LeapMotion readings
        /// Checks if the hand has approximately stopped by checking if it has moved a very small distance,
        /// if the dot product of the previous and current velocity is negative, 
        /// or if the magnitude of the velocity is very low
        /// </summary>
        public Palm()
        {
            Stopped = false;
            DistanceTraveled = 0;
            StopTimeInit = DateTime.Now;
            StartTimeInit = DateTime.Now;
            PreviousPosition = new Vector(0, 0, 0);
        }

        /// <summary>
        /// Move the hand. Checks if the hand should be stopped at this point
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        public void Move(Vector position, Vector velocity)
        {

            if (!this.Stopped)
            {
                this.DistanceTraveled += Math.Abs(position.DistanceTo(this.PreviousPosition));
            }


            var magVelocity = velocity.Magnitude;
            var dotProduct = this.PreviousVelocity.Dot(velocity);
            if (!Stopped &&
                ((magVelocity <= Palm.velocityStopThreshold) &&
                DurationMoving >= MinimumMotionDuration &&
                DistanceTraveled > DistanceThreshold))
            {
                Console.WriteLine(string.Format("Mag is {0}, dot is {1}, duration is {2}. Distance traveled is {3}", magVelocity, dotProduct, DurationMoving, DistanceTraveled));
                Stop(position);
                return;
            }
            else if (
                Stopped 
                && magVelocity > Palm.velocityStartThreshold)
            {
                Start(velocity, position);
            }

            if (!Stopped)
            {
                UpdateCentroid(position);
            }

            // in either case, set the last position
            PreviousPosition = averageVector(PreviousPosition, position);
            PreviousVelocity = averageVector(PreviousVelocity,velocity);
            // Console.WriteLine(string.Format("Velocity was {0}, dot product with previous was {1}, duration moving was {2}", magVelocity, dotProduct, DurationMoving));
        }

        private Vector averageVector(Vector v1, Vector v2)
        {
            return new Vector((v1.x + v2.x) / 2, (v1.y + v2.y) / 2, (v1.z + v2.z) / 2);
        }

        private void Start(Vector position, Vector velocity)
        {
            Stopped = false;
            this.StartTimeInit = DateTime.Now;
            this.DistanceTraveled = Math.Abs(position.DistanceTo(this.PreviousPosition));
            UpdateCentroid(position);
            StartPosition = position;
        }

        private void Stop(Vector position)
        {
            this.PreviousDistance = this.DistanceTraveled;
            this.PreviousDuration = DateTime.Now - this.StartTimeInit;
            this.DistanceTraveled = 0;
            this.StopTimeInit = DateTime.Now;
            this.Stopped = true;
            this.nElements = 0;
            StopPosition = position;
        }

        private void UpdateCentroid(Vector newV)
        {
            if (this.nElements == 0)
            {
                Centroid = newV;
                nElements++;
                return;
            }

            nElements++;
            var dx = this.Centroid.x + newV.x / nElements;
            var dy = this.Centroid.y + newV.y / nElements;
            var dz = this.Centroid.z + newV.z / nElements;
            Centroid = new Vector(dx, dy, dz);
        }

    }
}
