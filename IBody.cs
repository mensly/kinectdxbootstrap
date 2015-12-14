using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectDXBootstrap
{
    public interface IBody
    {
        //
        // Summary:
        //     Gets the edges of the field of view that clip the body.
        FrameEdges ClippedEdges { get; }
        //
        // Summary:
        //     Gets the confidence of the body's left hand tracking state.
        TrackingConfidence HandLeftConfidence { get; }
        //
        // Summary:
        //     Gets the status of the body's left hand state.
        HandState HandLeftState { get; }
        //
        // Summary:
        //     Gets the confidence of the body's right hand tracking state.
        TrackingConfidence HandRightConfidence { get; }
        //
        // Summary:
        //     Gets the status of the body's right hand state.
        HandState HandRightState { get; }
        //
        // Summary:
        //     Gets whether or not the body is restricted.
        bool IsRestricted { get; }
        //
        // Summary:
        //     Gets whether or not the body is tracked.
        bool IsTracked { get; }
        //
        // Summary:
        //     Gets the joint orientations of the body.
        IReadOnlyDictionary<JointType, JointOrientation> JointOrientations { get; }
        //
        // Summary:
        //     Gets the joint orientations of the body.
        IReadOnlyDictionary<JointType, Joint> Joints { get; }
        //
        // Summary:
        //     Gets the lean vector of the body.
        PointF Lean { get; }
        //
        // Summary:
        //     Gets the tracking state for the body lean.
        TrackingState LeanTrackingState { get; }
        //
        // Summary:
        //     Gets the tracking ID for the body.
        ulong TrackingId { get; }
    }

    class WrappedBody : IBody
    {
        public Body body;
        public WrappedBody(Body body)
        {
            this.body = body;
        }

        public FrameEdges ClippedEdges { get { return body.ClippedEdges; } }
        public TrackingConfidence HandLeftConfidence { get { return body.HandLeftConfidence; } }
        public HandState HandLeftState { get { return body.HandLeftState; } }
        public TrackingConfidence HandRightConfidence { get { return body.HandRightConfidence; } }
        public HandState HandRightState { get { return body.HandRightState; } }
        public bool IsRestricted { get { return body.IsRestricted; } }
        public bool IsTracked { get { return body.IsTracked; } }
        public IReadOnlyDictionary<JointType, JointOrientation> JointOrientations { get { return body.JointOrientations; } }
        public IReadOnlyDictionary<JointType, Joint> Joints { get { return body.Joints; } }
        public PointF Lean { get { return body.Lean; } }
        public TrackingState LeanTrackingState { get { return body.LeanTrackingState; } }
        public ulong TrackingId { get { return body.TrackingId; } }
    }
}
