using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using Raft;
using Raft.DTOs;
using System.Xml.Serialization;
using Xunit;

namespace RaftTests
{
    public class ElectionTests
    {
        // Testing #1
        [Fact]
        public async Task TestCase01_ActiveLeadersSendHeartbeatsWithin50ms()
        {
            // Arrange
            Node leaderNode = new Node([], null);
            leaderNode.BecomeLeader();

            var followerNode = Substitute.For<INode>();
            leaderNode.OtherNodes = [followerNode];

            // Act
            var atLeastTwoCyclesTime = 120;
            //Thread.Sleep(atLeastTwoCyclesTime);
            await Task.Delay(atLeastTwoCyclesTime);

            // Assert
            await followerNode.Received().RecieveAppendEntriesRPC(Arg.Is<AppendEntriesRPC>(actual =>
                actual.leaderId == leaderNode.NodeId));
        }

        // Testing #2
        [Fact]
        public void TestCase02_LeadersAreRememberedByFollowers()
        {
            // Arrange
            Node leaderNode = new Node([], null);
            leaderNode.BecomeLeader();

            var followerNode = new Node([leaderNode], null);
            leaderNode.OtherNodes = [followerNode];

            // Act
            leaderNode.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(followerNode.LeaderId, leaderNode.NodeId);
        }

        // Testing #3
        [Fact]
        public void TestCase03_NodesStartAsFollowers()
        {
            // Arrange
            Node newNode = new Node([], null);

            // Act
            var currentNodeState = newNode.State;

            // Assert
            Assert.Equal(Node.NodeState.Follower, currentNodeState);
        }

        // Testing #4
        [Fact]
        public void TestCase04_IgnoredFollowersStartElectionAfter300ms()
        {
            // Arrange
            var followerNode = new Node([], null);

            // Act
            var BiggestElectionTimoutTime = 600;
            Thread.Sleep(BiggestElectionTimoutTime);

            // Assert
            Assert.Equal(Node.NodeState.Candidate, followerNode.State);
        }

        // Testing #5 (part 1)
        [Fact]
        public void TestCase05_ElectionTimesAreBetween150And300()
        {
            // Arrange
            int n = 10;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node([], null);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            // Assert
            electionTimeouts.ForEach(x => Assert.True(x >= 150));
            electionTimeouts.ForEach(x => Assert.True(x < 300));
        }

        // Testing #5 (part 2)
        [Fact]
        public void TestCase05_ElectionTimesAreRandom()
        {
            // Arrange
            int n = 10;
            int threshold = 3;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node([], null);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            var numberOfRepeats = n - electionTimeouts.Distinct().Count();

            // Assert
            Assert.True(numberOfRepeats <= threshold);
        }

        // Testing #6
        [Fact]
        public void TestCase06_NewElectionBeginsAndTermIsGreater()
        {
            // Arrange
            Node n = new Node([], null);

            // Act
            Thread.Sleep(600);

            // Assert
            Assert.True(n.TermNumber > 0);
        }

        // Testing #7
        [Fact]
        public async Task TestCase07_WhenLeadersSendMessagesToMeThenIStayFollower()
        {
            // Arrange
            var followerNode = new Node([], null);
            followerNode.State = Node.NodeState.Follower;

            var followerElectionTimeBefore = followerNode.ElectionTimeout;
            // Act
            // Leader sends messages to me
            AppendEntriesRPC rpc = new();
            await followerNode.RecieveAppendEntriesRPC(rpc);
            Thread.Sleep(100);

            // Assert
            Assert.NotEqual(followerElectionTimeBefore, followerNode.ElectionTimeout);
            Assert.Equal(Node.NodeState.Follower, followerNode.State);
        }

        // Testing #8
        [Fact]
        public async Task TestCase08_MajorityVotesWins()
        {
            // 8. Given an election begins, when the candidate gets a majority of votes,
            // it becomes a leader.

            // Arrange
            var followerNode = Substitute.For<INode>();
            followerNode.NodeId = Guid.NewGuid();
			var followerNode2 = Substitute.For<INode>();
            followerNode2.NodeId = Guid.NewGuid();

			Node candidateNode = new([], null);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.OtherNodes = [followerNode, followerNode2];
			//followerNode.OtherNodes = [candidateNode, followerNode2];
			//followerNode2.OtherNodes = [candidateNode, followerNode];

			// Act
			await candidateNode.RecieveVoteResults(new VoteFromFollowerRpc(true, 100));
			await candidateNode.RecieveVoteResults(new VoteFromFollowerRpc(true, 100));

            Thread.Sleep(300);

            // Assert
			Assert.Equal(Node.NodeState.Leader, candidateNode.State);   
        }

        // Testing #8
        [Fact]
		public async Task TestCase08_MajorityVotesWinWithSubstitutes()
		{
			// 8. Given an election begins, when the candidate gets a majority of votes,
			// it becomes a leader.

			// Arrange
			Node candidateNode = new([], null);
			candidateNode.TermNumber = 100;
			candidateNode.State = Node.NodeState.Candidate;
            candidateNode.votesRecieved = [true]; // say he has already recieved one vote

			// Act
			await candidateNode.RecieveVoteResults(new VoteFromFollowerRpc(true, 100));

			// Assert
			Assert.Equal(Node.NodeState.Leader, candidateNode.State);
            Assert.True(candidateNode.votesRecieved.Count() == 0);
		}

		// Testing #9
		[Fact]
        public async Task TestCase09_MajorityVotesEvenWithUnresponsiveStillBecomeLeader()
        {
            var followerNode1 = Substitute.For<INode>();
			var followerNode2 = Substitute.For<INode>();
			var followerNode3 = Substitute.For<INode>();
			var followerNode4 = Substitute.For<INode>();

            Node candidateNode = new([], null);
            candidateNode.TermNumber = 100;
            candidateNode.ElectionTimeout = 9999999;
            candidateNode.State = Node.NodeState.Candidate;
            
            // 5 node system...
            candidateNode.OtherNodes = [followerNode1, followerNode2, followerNode3, followerNode4];

            // Act
            // Say in a 5 system I recieve only 2 votes, and have one server unresponsive
            List<bool> votesRecieved = [true, true];
            candidateNode.votesRecieved = votesRecieved;
            await candidateNode.RecieveVoteResults(new VoteFromFollowerRpc(true, 100)); // third vote should push over the edge

			// Assert
			Assert.Equal(Node.NodeState.Leader, candidateNode.State);
        }

        // Testing #10
        [Fact]
        public async Task TestCase10_FollowersRespondeYesToVotes()
        {
            // A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with "yes".

            // Arrange
            var follower = new Node([], null);
            follower.VoteForId = Guid.Empty; // given a follower has not voted
            follower.TermNumber = -30; // and is in an earlier term

            var candidateNode = Substitute.For<INode>();
            int candidateTermNumber = 100;

            // Act
            await follower.RecieveAVoteRequestFromCandidate(new VoteRequestFromCandidateRpc( candidateNode.NodeId, candidateTermNumber));

            // Assert
            Assert.Equal(follower.VoteForId, candidateNode.NodeId); // Node recorded that they have voted for candidate
            Assert.Equal(follower.VotedForTermNumber, candidateTermNumber);
        }

        // Testing #11
        [Fact]
        public void TestCase11_NewCandidateNodesVoteForThemselves()
        {
            // Arrange
            Node n = new Node([], null);
            Thread.Sleep(100);
            var thisNodesId = n.NodeId;

            // Act
            Thread.Sleep(375);

            // Assert
            Assert.Equal(n.VoteForId, thisNodesId); // It votes for itself
            Assert.Equal(Node.NodeState.Candidate, n.State);   // And it is a candidate now
        }

		// Testing #12
		[Fact]
		public async Task TestCase12_CandidatesBecomeFollowersWhenRecieveLaterTermRPC()
		{
			// 12.Given a candidate, when it receives an AppendEntries message from a node with a later term,
			// then the candidate loses and becomes a follower.

			Node candidateNode = new Node([], null);
			candidateNode.State = Node.NodeState.Candidate;
			candidateNode.TermNumber = 1;

			// Act
			int LeaderTermNumber = 100;
			AppendEntriesRPC rpc = new(LeaderTermNumber, Guid.NewGuid(), 0, new List<Entry>(), -1);
			await candidateNode.RecieveAppendEntriesRPC(rpc);

			// Assert
			Assert.Equal(Node.NodeState.Follower, candidateNode.State); // Candidate reverts to follower
		}

		// Testing #12 (but opposite)
		[Fact]
        public void TestCase12_CandidatesStaysCandidateWhenRecieveLesserTermRPC()
        {
            // Arrange
            Node node1 = new Node([], null);
            node1.TermNumber = 1;

            Node candidateNode = new Node([node1], null);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 100;

            node1.OtherNodes = [candidateNode];

            // Act
            node1.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(Node.NodeState.Candidate, candidateNode.State); // Candidate stays a candidate
        }

		// Testing #13
		[Fact]
		public async Task TestCase13_CandidatesBecomeFollowersWhenRecieveEqualTermRPC()
		{
			// Arrange
			Node candidateNode = new Node([], null);
			candidateNode.State = Node.NodeState.Candidate;
			candidateNode.TermNumber = 100;  // equal terms
			int LeaderTermNumber = 100; // equal terms

			// Act
			AppendEntriesRPC rpc = new(LeaderTermNumber, Guid.NewGuid(), 0, new List<Entry>(), -1);
			await candidateNode.RecieveAppendEntriesRPC(rpc);

			// Assert
			Assert.Equal(Node.NodeState.Follower, candidateNode.State); // Converts to Follower
		}

		// Testing #14
		[Fact]
		public async Task TestCase14_SecondVoteRequestInSameTermRespondNo()
		{
			// 14. If a node receives a second request for a vote for the same term, it should respond "no". 

			// Arrange
			Node node = new([], null);

			var c1 = Substitute.For<INode>();
            c1.NodeId = Guid.NewGuid();

			var c2 = Substitute.For<INode>();
            c2.NodeId = Guid.NewGuid();

            node.OtherNodes = [c1, c2];

            // Act
            await node.RecieveAVoteRequestFromCandidate(new VoteRequestFromCandidateRpc(c1.NodeId, 100));
            await node.RecieveAVoteRequestFromCandidate(new VoteRequestFromCandidateRpc(c2.NodeId, 100));

            // Assert
            await c1.Received(1).RecieveVoteResults(Arg.Is<VoteFromFollowerRpc>(actual => 
                actual.result == true));
            await c2.Received(1).RecieveVoteResults(Arg.Is<VoteFromFollowerRpc>(actual =>
                actual.result == false));
		}

		// Testing #15
		[Fact]
        public async Task TestCase15_SecondVoteRequestFromNodeInFutureTermVotesYes()
        {
            // 15.If a node receives a second request for a vote for a future term, it should vote for that node.
            // Arrange
            Node node = new Node([], null);
            node.TermNumber = 1;

            Node candidateNode = new Node([node], null);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 50;

            Node candidateNode2 = new Node( [node, candidateNode], null);
            candidateNode2.State = Node.NodeState.Candidate;
            candidateNode2.TermNumber = 100;

            candidateNode.OtherNodes = [node, candidateNode2];

            // Act
            await candidateNode.SendVoteRequestRPCsToOtherNodes();    // follower says yes
            await candidateNode2.SendVoteRequestRPCsToOtherNodes();   // second vote request for greater term is accepted

            // Assert
            Assert.Equal(node.VoteForId, candidateNode2.NodeId);
            Assert.NotEqual(candidateNode.NodeId, node.VoteForId);

            Assert.Equal(100, node.VotedForTermNumber);
            Assert.Contains(true, candidateNode2.votesRecieved);
            Assert.Contains(false, candidateNode.votesRecieved);
        }

        // Testing #16
        [Fact]
        public void TestCase16_ElectionTimersRestartDuringElection()
        {
            // Arrange
            Node n = new Node([], null);
            n.State = Node.NodeState.Candidate;
            var termBefore = n.TermNumber;

            // Act
            Thread.Sleep(600); // When election timer runs out

            // Assert
            // In my eyes, these are indicators that a new election began
            Assert.True(termBefore < n.TermNumber);
            Assert.Equal(Node.NodeState.Candidate, n.State);
        }

		// Testing #17
		[Fact]
		public async Task TestCase17_FollowersSendResponses()
		{
			// 17. When a follower node receives an AppendEntries request, it sends a response.

			// Arrange
			var leader = Substitute.For<INode>();
			var follower = new Node([leader], null);

			// Act
			AppendEntriesRPC rpc = new AppendEntriesRPC();
			await follower.RecieveAppendEntriesRPC(rpc); // Send heartbeat

			// Assert
			await leader.Received(1).RespondBackToLeader(Arg.Any<ResponseBackToLeader>());
		}

		// Testing #18
		[Fact]
		public async Task TestCase18_AppendEntriesFromPreviousTermsAreRejected()
		{
			// Given a candidate receives an AppendEntries from a previous term, then it rejects.

			// Arrange
			var leader = Substitute.For<INode>();

			Node candidateNode = new([], null);
			candidateNode.TermNumber = 100;

			candidateNode.OtherNodes = [leader];

			// Act
			AppendEntriesRPC rpc = new(2, leader.NodeId, -1, new List<Entry>(), 0); // #
			rpc.prevLogIndex = 2;
			await candidateNode.RecieveAppendEntriesRPC(rpc);

			// Assert
            await leader.Received().RespondBackToLeader(Arg.Is<ResponseBackToLeader>(actual =>
                actual.response == false &&
                actual.fTermNumber == 100));
        }

        // Testing #19
        [Fact]
		public void TestCase19_NewLeadersSendRPC()
		{
			// 19. When a candidate wins an election, it immediately sends a heartbeat.
			var leaderNode = new Node([], null);
			var followerNode = Substitute.For<INode>();
			leaderNode.OtherNodes = [followerNode];

			// Act
			leaderNode.BecomeLeader();

			// Assert
			followerNode.Received(1).RecieveAppendEntriesRPC(Arg.Any<AppendEntriesRPC>());
		}

        [Fact]
        public void CandidateResetsTimeout()
        {
			var f1 = Substitute.For<INode>();
			var f2 = Substitute.For<INode>();

			Node n = new([f1, f2], null);

            var oldTimer = n.aTimer;

            n.TimeoutHasPassed();

            Assert.NotEqual(n.aTimer, oldTimer);
        }

	}
}