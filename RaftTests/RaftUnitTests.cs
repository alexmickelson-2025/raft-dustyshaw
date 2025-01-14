using NSubstitute;
using Raft;

namespace RaftTests
{
    public class RaftUnitTests
    {
        // Testing #3
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

        // Testing #4
        [Fact]
        public void TestCase4_FollowersStartElections()
        {
            // Arrange
            Node newNode = new Node(true, []);

            // Act
            

            // Assert
        }

        // Testing #5 (part 1)
        [Fact]
        public void TestCase5_ElectionTimesAreBetween150And300()
        {
            // Arrange
            int n = 10;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node(true, []);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            // Assert
            electionTimeouts.ForEach(x => Assert.True(x > 150));
            electionTimeouts.ForEach(x => Assert.True(x < 300));
        }

        // Testing #5 (part 2)
        [Fact]
        public void TestCase5_ElectionTimesAreRandom()
        {
            // Arrange
            int n = 10;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node(true, []);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            // Assert
            Assert.Equal(n, electionTimeouts.Distinct().Count());
        }


        // Testing #17
        [Fact]
        public void TestCase17_FollowersSendResponses()
        {
            // Arrange
            var followerNode = Substitute.For<INode>();
            followerNode.State = Node.NodeState.Follower;

            var leaderNode = new Node(true, []);
            leaderNode.State = Node.NodeState.Leader;

            leaderNode.OtherNodes = [followerNode];
            followerNode.OtherNodes = [leaderNode];

            // Act
            leaderNode.SendAppendEntriesRPC(); // Send heartbeat

            // Assert
            followerNode.Received(1).RespondToAppendEntriesRPC();
        }

    }
}