using NSubstitute;
using Raft;

namespace RaftTests
{
    public class RaftUnitTests
    {

        // Testing #4
        [Fact]
        public void TestCase4_FollowersStartElections()
        {
            // Arrange
            Node newNode = new Node(true, []);

            // Act
            

            // Assert
        }

        // Testing #5
        [Fact]
        public void TestCase3_NodesStartAsFollowers()
        {
            // Arrange
            Node newNode = new Node(true, []);

            // Act
            var currentNodeState = newNode.State;

            // Assert
            Assert.Equal(Node.NodeState.Follower, currentNodeState);
        }

        // Testing #17
        [Fact]
        public void TestCase17_FollowersSendResponses()
        {
            // Arrange
            var followerNode = Substitute.For<INode>();
            followerNode.State = Node.NodeState.Follower;

            var leaderNode = Substitute.For<INode>();
            leaderNode.State = Node.NodeState.Leader;

            leaderNode.OtherNodes = new[] { followerNode };
            followerNode.OtherNodes = new[] { leaderNode };

            // Act
            leaderNode.SendAppendEntriesRPC(); // Send heartbeat

            // Assert
            followerNode.Received(1).RespondToAppendEntriesRPC();
        }
    }
}