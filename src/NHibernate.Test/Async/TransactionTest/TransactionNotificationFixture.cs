﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Data;
using System.Data.Common;
using NHibernate.Transaction;
using NUnit.Framework;

namespace NHibernate.Test.TransactionTest
{
	using System.Threading.Tasks;
	using System.Threading;
	[TestFixture]
	public class TransactionNotificationFixtureAsync : TestCase
	{
		protected override string[] Mappings => Array.Empty<string>();

		[Test]
		public async Task CommitAsync()
		{
			var interceptor = new RecordingInterceptor();
			using (var session = Sfi.WithOptions().Interceptor(interceptor).OpenSession())
			using (var tx = session.BeginTransaction())
			{
				var synchronisation = new Synchronization();
				tx.RegisterSynchronization(synchronisation);
				await (tx.CommitAsync());
				Assert.That(interceptor.afterTransactionBeginCalled, Is.EqualTo(1), "interceptor begin");
				Assert.That(interceptor.beforeTransactionCompletionCalled, Is.EqualTo(1), "interceptor before");
				Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1), "interceptor after");
				Assert.That(synchronisation.BeforeExecutions, Is.EqualTo(1), "sync before");
				Assert.That(synchronisation.AfterExecutions, Is.EqualTo(1), "sync after");
			}
		}

		[Test]
		public async Task RollbackAsync()
		{
			var interceptor = new RecordingInterceptor();
			using (var session = Sfi.WithOptions().Interceptor(interceptor).OpenSession())
			using (var tx = session.BeginTransaction())
			{
				var synchronisation = new Synchronization();
				tx.RegisterSynchronization(synchronisation);
				await (tx.RollbackAsync());
				Assert.That(interceptor.afterTransactionBeginCalled, Is.EqualTo(1), "interceptor begin");
				Assert.That(interceptor.beforeTransactionCompletionCalled, Is.EqualTo(0), "interceptor before");
				Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1), "interceptor after");
				Assert.That(synchronisation.BeforeExecutions, Is.EqualTo(0), "sync before");
				Assert.That(synchronisation.AfterExecutions, Is.EqualTo(1), "sync after");
			}
		}

		[Theory]
		[Description("NH2128")]
		public async Task ShouldNotifyAfterTransactionAsync(bool usePrematureClose)
		{
			var interceptor = new RecordingInterceptor();
			var synchronisation = new Synchronization();
			ISession s;

			using (s = OpenSession(interceptor))
			using (var t = s.BeginTransaction())
			{
				t.RegisterSynchronization(synchronisation);
				await (s.CreateCriteria<object>().ListAsync<object>());

				// Call session close while still inside transaction?
				if (usePrematureClose)
					s.Close();
			}

			Assert.That(s.IsOpen, Is.False);
			Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1), "interceptor");
			Assert.That(synchronisation.AfterExecutions, Is.EqualTo(1), "sync");
		}

		[Description("NH2128")]
		[Theory]
		public async Task ShouldNotifyAfterTransactionWithOwnConnectionAsync(bool usePrematureClose)
		{
			var interceptor = new RecordingInterceptor();
			var synchronisation = new Synchronization();
			ISession s;

			using (var ownConnection = await (Sfi.ConnectionProvider.GetConnectionAsync(CancellationToken.None)))
			{
				using (s = Sfi.WithOptions().Connection(ownConnection).Interceptor(interceptor).OpenSession())
				using (var t = s.BeginTransaction())
				{
					t.RegisterSynchronization(synchronisation);
					await (s.CreateCriteria<object>().ListAsync<object>());

					// Call session close while still inside transaction?
					if (usePrematureClose)
						s.Close();
				}
			}

			Assert.That(s.IsOpen, Is.False);
			Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1), "interceptor");
			Assert.That(synchronisation.AfterExecutions, Is.EqualTo(1), "sync");
		}
	}

	#region Synchronization classes

	public partial class CustomTransaction : ITransaction
	{

		public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task RollbackAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}
	}

	public partial class Synchronization : ITransactionCompletionSynchronization
	{

		public Task ExecuteBeforeTransactionCompletionAsync(CancellationToken cancellationToken)
		{
			BeforeExecutions += 1;
			return Task.CompletedTask;
		}

		public Task ExecuteAfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
		{
			AfterExecutions += 1;
			return Task.CompletedTask;
		}
	}

	#endregion
}
