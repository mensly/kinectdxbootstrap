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
    static class Conversions
    {
        public static NewKinect.CameraSpacePoint Convert(this OldKinect.SkeletonPoint point)
        {
            return new NewKinect.CameraSpacePoint()
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z
            };
        }
        public static NewKinect.TrackingState Convert(this OldKinect.JointTrackingState state)
        {
            switch (state)
            {
                case JointTrackingState.Tracked:
                    return NewKinect.TrackingState.Tracked;
                case JointTrackingState.Inferred:
                    return NewKinect.TrackingState.Inferred;
                case JointTrackingState.NotTracked:
                default:
                    return NewKinect.TrackingState.NotTracked;
            }
        }
        public static NewKinect.FrameEdges Convert(this OldKinect.FrameEdges edges)
        {
            NewKinect.FrameEdges newEdges = NewKinect.FrameEdges.None;
            if (0 != (edges & FrameEdges.Bottom))
            {
                newEdges |= NewKinect.FrameEdges.Bottom;
            }
            if (0 != (edges & FrameEdges.Left))
            {
                newEdges |= NewKinect.FrameEdges.Left;
            }
            if (0 != (edges & FrameEdges.Right))
            {
                newEdges |= NewKinect.FrameEdges.Right;
            }
            if (0 != (edges & FrameEdges.Top))
            {
                newEdges |= NewKinect.FrameEdges.Top;
            }
            return newEdges;
        }

        public static NewKinect.JointType Convert(this JointType type)
        {
            switch (type)
            {
                case JointType.AnkleLeft:
                    return NewKinect.JointType.AnkleLeft;
                case JointType.AnkleRight:
                    return NewKinect.JointType.AnkleRight;
                case JointType.ElbowLeft:
                    return NewKinect.JointType.ElbowLeft;
                case JointType.ElbowRight:
                    return NewKinect.JointType.ElbowRight;
                case JointType.FootLeft:
                    return NewKinect.JointType.FootLeft;
                case JointType.FootRight:
                    return NewKinect.JointType.FootRight;
                case JointType.HandLeft:
                    return NewKinect.JointType.HandLeft;
                case JointType.HandRight:
                    return NewKinect.JointType.HandRight;
                case JointType.Head:
                    return NewKinect.JointType.Head;
                case JointType.HipCenter:
                    return NewKinect.JointType.SpineBase;
                case JointType.HipLeft:
                    return NewKinect.JointType.HipLeft;
                case JointType.HipRight:
                    return NewKinect.JointType.HipRight;
                case JointType.KneeLeft:
                    return NewKinect.JointType.KneeLeft;
                case JointType.KneeRight:
                    return NewKinect.JointType.KneeRight;
                case JointType.ShoulderCenter:
                    return NewKinect.JointType.SpineShoulder;
                case JointType.ShoulderLeft:
                    return NewKinect.JointType.ShoulderLeft;
                case JointType.ShoulderRight:
                    return NewKinect.JointType.ShoulderRight;
                case JointType.Spine:
                    return NewKinect.JointType.SpineMid;
                case JointType.WristLeft:
                    return NewKinect.JointType.WristLeft;
                case JointType.WristRight:
                    return NewKinect.JointType.WristRight;
                default:
                    // Should not occur
                    return default(NewKinect.JointType);
            }
        }
    }

    

}
