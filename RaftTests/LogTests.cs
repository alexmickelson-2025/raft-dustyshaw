using Raft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftTests
{
	public class LogTests
	{
		// Testing #1
		[Fact]
		public void TestCase01_NodesHoldLogs()
		{
			// Arrange
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");
			n.Entries = [l];

			// Act
			Thread.Sleep(600);

			// Assert
			Assert.True(n.Entries.Count() > 0);
		}

		// Testing #2
		[Fact]
		public void TestCase02_NodesRecieveCommands()
		{
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");

			n.RecieveClientCommand(l.Command);


			Assert.True(n.Entries.Count() > 0);
			//Assert.Contains(l, n.Entries);
		}

		// Testing #3
		[Fact]
		public void TestCase02_NodesStartWithNoLogs()
		{
			Node n = new([], null, null);


			Assert.True(n.Entries.Count() == 0);
		}
	}
}
