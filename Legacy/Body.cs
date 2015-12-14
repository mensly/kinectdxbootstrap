extern alias oldKinect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NewKinect = Microsoft.Kinect;
using oldKinect.Microsoft.Kinect;
using OldKinect = oldKinect.Microsoft.Kinect;

namespace KinectDXBootstrap.Legacy
{
    class Body : IBody
    {
        public NewKinect.FrameEdges ClippedEdges { get; private set; }
        public NewKinect.TrackingConfidence HandLeftConfidence { get { return NewKinect.TrackingConfidence.Low; } }
        public NewKinect.HandState HandLeftState { get { return NewKinect.HandState.Unknown; } }
        public NewKinect.TrackingConfidence HandRightConfidence { get { return NewKinect.TrackingConfidence.Low; } }
        public NewKinect.HandState HandRightState { get { return NewKinect.HandState.Unknown; } }
        public bool IsRestricted { get { return false; } }
        public bool IsTracked { get; private set; }
        public IReadOnlyDictionary<NewKinect.JointType, NewKinect.JointOrientation> JointOrientations { get { return jointOrientations; } }
        private static readonly Dictionary<NewKinect.JointType, NewKinect.JointOrientation> jointOrientations = new Dictionary<NewKinect.JointType, NewKinect.JointOrientation>();
        public IReadOnlyDictionary<NewKinect.JointType, NewKinect.Joint> Joints { get { return joints; } }
        private Dictionary<NewKinect.JointType, NewKinect.Joint> joints = new Dictionary<NewKinect.JointType, NewKinect.Joint>();
        // TODO: Can Lean be replicated from joints?
        public NewKinect.PointF Lean { get { return new NewKinect.PointF() { X = 0, Y = 0 }; } }
        public NewKinect.TrackingState LeanTrackingState { get { return NewKinect.TrackingState.NotTracked; } }
        public ulong TrackingId { get; private set; }

        public void Update(Skeleton skeleton)
        {
            foreach (Joint j in skeleton.Joints)
            {
                NewKinect.JointType type = j.JointType.Convert();
                joints[type] = new NewKinect.Joint()
                {
                    JointType = type,
                    Position = j.Position.Convert(),
                    TrackingState = j.TrackingState.Convert()
                };
            }
            ClippedEdges = skeleton.ClippedEdges.Convert();
            IsTracked = skeleton.TrackingState == OldKinect.SkeletonTrackingState.Tracked;
            TrackingId = (ulong)skeleton.TrackingId;
        }
    }
}
