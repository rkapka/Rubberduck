﻿using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Rubberduck.Parsing.Rewriter;
using Rubberduck.Parsing.VBA.Parsing;

namespace RubberduckTests.Rewriter
{
    [TestFixture]
    public class RewriteManagerTests
    {
        [Test]
        [Category("Rewriter")]
        public void ReturnsValidCodePaneSessions()
        {
            var rewritingManager = RewritingManager(out _);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            Assert.AreEqual(RewriteSessionState.Valid, codePaneSession.Status);
        }


        [Test]
        [Category("Rewriter")]
        public void ReturnsValidAttributesSessions()
        {
            var rewritingManager = RewritingManager(out _);
            var attributesSession = rewritingManager.CheckOutAttributesSession();
            Assert.AreEqual(RewriteSessionState.Valid, attributesSession.Status);
        }


        [Test]
        [Category("Rewriter")]
        public void InvalidateAllSessionsSetsTheStatusToOtherSessionsRewriteAppliedForAllActiveSessions()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();
            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            rewritingManager.InvalidateAllSessions();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions().Concat(mockFactory.RequestedAttributesSessions()))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Once);
            }
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromAnActiveCodePaneSessionSetsItsStatusToRewriteApplied()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();
            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            Assert.AreEqual(RewriteSessionState.RewriteApplied, codePaneSession.Status);
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromAnActiveAttributesSessionSetsItsStatusToRewriteApplied()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            rewritingManager.CheckOutCodePaneSession();
            var attributesSession = rewritingManager.CheckOutAttributesSession();
            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            Assert.AreEqual(RewriteSessionState.RewriteApplied, attributesSession.Status);
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromAnActiveCodePaneSessionSetsTheStatusToOtherSessionsRewriteAppliedForAllActiveSessions()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();
            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions().Concat(mockFactory.RequestedAttributesSessions()))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Once);
            }
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromAnActiveAttributesSessionSetsTheStatusToOtherSessionsRewriteAppliedForAllActiveSessions()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            rewritingManager.CheckOutCodePaneSession();
            var attributesSession =  rewritingManager.CheckOutAttributesSession();
            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions().Concat(mockFactory.RequestedAttributesSessions()))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Once);
            }
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveCodePaneSessionDoesNotSetItsStatusToRewriteApplied_InactiveDueToInvalidateAll()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();

            rewritingManager.InvalidateAllSessions();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            var mockSession = mockFactory.RequestedCodePaneSessions()
                .First(mockedSession => codePaneSession.Equals(mockedSession.Object));

            mockSession.VerifySet(m => m.Status = RewriteSessionState.RewriteApplied, Times.Never);
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveAttributesSessionDoesNotSetItsStatusToRewriteApplied_InactiveDueToInvalidateAll()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var attributesSession = rewritingManager.CheckOutAttributesSession();

            rewritingManager.InvalidateAllSessions();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            var mockSession = mockFactory.RequestedAttributesSessions()
                .First(mockedSession => attributesSession == mockedSession.Object);

            mockSession.VerifySet(m => m.Status = RewriteSessionState.RewriteApplied, Times.Never);
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveCodePaneSessionDoesNotSetItsStatusToRewriteApplied_InactiveDueToRewrite()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            var attributesSession = rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            var mockSession = mockFactory.RequestedCodePaneSessions()
                .First(mockedSession => codePaneSession == mockedSession.Object);

            mockSession.VerifySet(m => m.Status = RewriteSessionState.RewriteApplied, Times.Never);
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveAttributesSessionDoesNotSetItsStatusToRewriteApplied_InactiveDueToRewrite()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            var attributesSession = rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            var mockSession = mockFactory.RequestedAttributesSessions()
                .First(mockedSession => attributesSession.Equals(mockedSession.Object));

            mockSession.VerifySet(m => m.Status = RewriteSessionState.RewriteApplied, Times.Never);
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveCodePaneSessionDoesNotSetTheStatusToOtherSessionsRewriteAppliedForAnyActiveSession_InactiveDueToInvalidateAll()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();

            rewritingManager.InvalidateAllSessions();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions()
                .Concat(mockFactory.RequestedAttributesSessions())
                .Where(session => session.Object != codePaneSession))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Never);
            }
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveAttributesSessionDoesNotSetTheStatusToOtherSessionsRewriteAppliedForAnyActiveSession_InactiveDueToInvalidateAll()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var attributesSession = rewritingManager.CheckOutAttributesSession();

            rewritingManager.InvalidateAllSessions();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions()
                .Concat(mockFactory.RequestedAttributesSessions())
                .Where(session => session.Object != attributesSession))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Never);
            }
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveCodePaneSessionDoesNotSetTheStatusToOtherSessionsRewriteAppliedForAnyActiveSession_InactiveDueToRewrite()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            var attributesSession = rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions()
                .Concat(mockFactory.RequestedAttributesSessions())
                .Where(session => session.Object != codePaneSession && session.Object != attributesSession))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Never);
            }
        }

        [Test]
        [Category("Rewriter")]
        public void CallingTheRewritingAllowedCallbackFromANoLongerActiveAttributesSessionDoesNotSetTheStatusToOtherSessionsRewriteAppliedForAnyActiveSession_InactiveDueToRewrite()
        {
            var rewritingManager = RewritingManager(out var mockFactory);
            var codePaneSession = rewritingManager.CheckOutCodePaneSession();
            var attributesSession = rewritingManager.CheckOutAttributesSession();

            codePaneSession.TryRewrite();

            rewritingManager.CheckOutCodePaneSession();
            rewritingManager.CheckOutAttributesSession();

            attributesSession.TryRewrite();

            foreach (var mockSession in mockFactory.RequestedCodePaneSessions()
                .Concat(mockFactory.RequestedAttributesSessions())
                .Where( session => session.Object != codePaneSession && session.Object != attributesSession))
            {
                mockSession.VerifySet(m => m.Status = RewriteSessionState.OtherSessionsRewriteApplied, Times.Never);
            }
        }

        private IRewritingManager RewritingManager(out MockRewriteSessionFactory mockFactory)
        {
            mockFactory = new MockRewriteSessionFactory();
            return new RewritingManager(mockFactory);
        }
    }

    public class MockRewriteSessionFactory : IRewriteSessionFactory
    {
        private readonly List<Mock<IRewriteSession>> _requestedCodePaneSessions = new List<Mock<IRewriteSession>>();
        private readonly List<Mock<IRewriteSession>> _requestedAttributesSessions = new List<Mock<IRewriteSession>>();

        public IEnumerable<Mock<IRewriteSession>> RequestedCodePaneSessions()
        {
            return _requestedCodePaneSessions;
        }

        public IEnumerable<Mock<IRewriteSession>> RequestedAttributesSessions()
        {
            return _requestedAttributesSessions;
        }

        public IRewriteSession CodePaneSession(Func<IRewriteSession, bool> rewritingAllowed)
        {
            var mockSession = MockSession(rewritingAllowed, CodeKind.CodePaneCode);
            _requestedCodePaneSessions.Add(mockSession);
            return mockSession.Object;
        }

        private Mock<IRewriteSession> MockSession(Func<IRewriteSession, bool> rewritingAllowed, CodeKind targetCodeKind)
        {
            var mockSession = new Mock<IRewriteSession>();
            mockSession.Setup(m => m.TryRewrite()).Callback(() => rewritingAllowed.Invoke(mockSession.Object));
            var status = RewriteSessionState.Valid;
            mockSession.SetupGet(m => m.Status).Returns(status);
            mockSession.SetupSet(m => m.Status = It.IsAny<RewriteSessionState>())
                .Callback<RewriteSessionState>( value =>
                {
                    if (status == RewriteSessionState.Valid)
                    {
                        status = value;
                        mockSession.SetupGet(m => m.Status).Returns(status);
                    }
                }); 
            mockSession.Setup(m => m.TargetCodeKind).Returns(targetCodeKind);

            return mockSession;
        }

        public IRewriteSession AttributesSession(Func<IRewriteSession, bool> rewritingAllowed)
        {
            var mockSession = MockSession(rewritingAllowed, CodeKind.AttributesCode);
            _requestedAttributesSessions.Add(mockSession);
            return mockSession.Object;
        }
    }
}