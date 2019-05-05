using System;

namespace RBS.Messages.control_msgs
{
    [System.Serializable]
    public class SingleJointPositionActionResult : ExtendMessage
    {
        public RBS.Messages.std_msgs.Header header;
        public RBS.Messages.actionlib_msgs.GoalStatus status;
        public RBS.Messages.control_msgs.SingleJointPositionResult result;
        public override string Type() { return "control_msgs/SingleJointPositionActionResult"; }
        public SingleJointPositionActionResult()
        {
            header = new RBS.Messages.std_msgs.Header();
            status = new RBS.Messages.actionlib_msgs.GoalStatus();
            result = new RBS.Messages.control_msgs.SingleJointPositionResult();
        }
    }
}