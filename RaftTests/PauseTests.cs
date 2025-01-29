using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Raft;

namespace RaftTests;

public class PauseTests
{
    // Test 1
    [Fact]
    public void PausingLeadersInElectionLoops()
    {
        // when node is a leader with an election loop, then they get paused, other nodes do not get heartbeats for 400 ms
        var followerNode = Substitute.For<INode>();
        followerNode.State = Node.NodeState.Follower;

        var leaderNode = new Node([], null);
        leaderNode.BecomeLeader();

        leaderNode.OtherNodes = [followerNode];
        followerNode.OtherNodes = [leaderNode];

        // act
        leaderNode.PauseNode();

        Thread.Sleep(400);

        // assert
        followerNode.Received(0).RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<List<Entry>>(), Arg.Any<AppendEntriesRPC>());
    }

    [Fact]
    public void PausedLeadersStayLeadersForever()
    {
        // when node is a leader with an election loop, then they get paused, other nodes do not get heartbeats for 400 ms
        var leaderNode = new Node([], null);
        leaderNode.BecomeLeader();

        // act
        leaderNode.PauseNode();
        Thread.Sleep(400);

        // assert
        Assert.Equal(Node.NodeState.Leader, leaderNode.State);
    }

    // Test 2
    [Fact]
    public void LeadersCanUnPause()
    {
        // when node is a leader with an election loop, then they get paused, other nodes do not get heartbeats for 400 ms
        var followerNode = Substitute.For<INode>();
        followerNode.State = Node.NodeState.Follower;

        var leaderNode = new Node([], null);
        leaderNode.BecomeLeader();

        leaderNode.OtherNodes = [followerNode];
        followerNode.OtherNodes = [leaderNode];

        // act
        leaderNode.PauseNode();
        Thread.Sleep(400);

        leaderNode.UnpauseNode();
        Thread.Sleep(35);

        // assert
        followerNode.Received(1).RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<List<Entry>>(), Arg.Any<AppendEntriesRPC>());

    }

    // Test 3
    [Fact]
    public void PausedFollowersDontBecomeCandidates()
    {
        var candidateNode = new Node([], null);
        candidateNode.State = Node.NodeState.Follower;

        // act
        candidateNode.PauseNode();
        Thread.Sleep(300);
        
        // assert
        Assert.Equal(Node.NodeState.Follower, candidateNode.State);
    }

    // Test 4
    [Fact]
    public void UnpausedFollowersDontBecomeCandidates()
    {
        // arrange
        var candidateNode = new Node([], null);
        candidateNode.State = Node.NodeState.Follower;

        // act
        candidateNode.PauseNode();
        Thread.Sleep(300);

        candidateNode.UnpauseNode();
        Thread.Sleep(300);

        // assert
        Assert.Equal(Node.NodeState.Candidate, candidateNode.State);
    }

	[Fact]
	public void PauseAFollowerThenUnpauseAFollower()
	{
        // arrange
		var followerNode = new Node([], null);
		followerNode.State = Node.NodeState.Follower;

		var leaderNode = new Node([], null);
		leaderNode.BecomeLeader();

		leaderNode.OtherNodes = [followerNode];
		followerNode.OtherNodes = [leaderNode];

		// act
		followerNode.PauseNode();
		Thread.Sleep(400);

        // assert
        Assert.True(followerNode.State == Node.NodeState.Follower);

        followerNode.UnpauseNode();

        Thread.Sleep(300);

        Assert.True(followerNode.State == Node.NodeState.Follower); // is a leader for some reason 


	}
}
