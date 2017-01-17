using Microsoft.Bot.Connector;
using NUnit.Framework;
using Moq;
using GraceBot.Models;
using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace GraceBot.Tests
{
    [TestFixture]
    class DbTests
    {
        private Mock<DbSet<ActivityModel>> _mockActivities;
        private Mock<DbSet<ChannelAccount>> _mockChannelAccounts;
        private Mock<DbSet<ConversationAccount>> _mockConversationAccounts;
        private Mock<GraceBotContext> _mockContext;
        private DbManager _dbManager;

        [SetUp]
        public void Setup()
        {
            _mockActivities = new Mock<DbSet<ActivityModel>>();
            _mockChannelAccounts = new Mock<DbSet<ChannelAccount>>();
            _mockConversationAccounts = new Mock<DbSet<ConversationAccount>>();
            _mockContext = new Mock<GraceBotContext>();

            _mockContext.Setup(m => m.Activities).Returns(_mockActivities.Object);
            _mockContext.Setup(m => m.ChannelAccounts).Returns(_mockChannelAccounts.Object);
            _mockContext.Setup(m => m.ConversationAccounts).Returns(_mockConversationAccounts.Object);

            _dbManager = new DbManager(_mockContext.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _mockActivities = null;
            _mockChannelAccounts = null;
            _mockConversationAccounts = null;
        }

        /// <summary>
        /// Tests the data integrity within conversion process between <see cref="Activity>"/> and <see cref="ActivityModel>"/>. 
        /// 
        /// The latter is used as an Entity Framework Model. The <see cref="ActivityModel>"/> instance should 
        /// hold identical data of the necessary properties of the <see cref="Activity>"/> instance. Moreover,
        /// data integrity should maintain after an <see cref="ActivityModel>"/> is converted back to a <see cref="Activity>"/>
        /// object.
        /// </summary>
        [Test]
        public void ConversionDataIntegrityTest()
        {
            var activity = MakeActivity();
            var model = _dbManager.ConvertToModel(activity);
            var converted = _dbManager.ConvertToActivity(model);

            Assert.AreEqual(activity.Id, converted.Id);
            Assert.AreEqual(activity.Text, converted.Text);
            Assert.AreEqual(activity.Type, converted.Type);
            Assert.AreEqual(activity.ServiceUrl, converted.ServiceUrl);
            Assert.AreEqual(activity.Timestamp, converted.Timestamp);
            Assert.AreEqual(activity.ChannelId, converted.ChannelId);
            Assert.AreEqual(activity.From, converted.From);
            Assert.AreEqual(activity.Conversation, converted.Conversation);
            Assert.AreEqual(activity.Recipient, converted.Recipient);
            Assert.AreEqual(activity.ReplyToId, converted.ReplyToId);
        }

        [Test]
        public async Task AddActivityTest()
        {
            var activity = MakeActivity();
            await _dbManager.AddActivity(activity);

            _mockActivities.Verify(m => m.Add(It.IsAny<ActivityModel>()), Times.Once(), "Activities failed to add");
            _mockContext.Verify(m => m.SaveChangesAsync(), Times.Once(), "Context failed to save changes");
        }


        [Test]
        public void FindActivityTest()
        {
            var a1 = MakeActivity();
            var activities = new List<ActivityModel>
            {
                _dbManager.ConvertToModel(a1),
            }.AsQueryable();

            _mockActivities.As<IQueryable<ActivityModel>>().Setup(m => m.Provider).Returns(activities.Provider);
            _mockActivities.As<IQueryable<ActivityModel>>().Setup(m => m.Expression).Returns(activities.Expression);
            _mockActivities.As<IQueryable<ActivityModel>>().Setup(m => m.ElementType).Returns(activities.ElementType);
            _mockActivities.As<IQueryable<ActivityModel>>().Setup(m => m.GetEnumerator()).Returns(activities.GetEnumerator());
            _mockActivities.Setup(m => m.Include(It.IsAny<string>())).Returns(_mockActivities.Object);

            Assert.AreEqual(a1.Id, _dbManager.FindActivity(a1.Id).Id);
        }

        private Activity MakeActivity(string replyToActivityId = null)
        {
            var guid = Guid.NewGuid().ToString();

            var from = new ChannelAccount()
            {
                Id = "FromId",
                Name = "FromName",
            };

            var conversation = new ConversationAccount()
            {
                Id = "ConversationAccountId",
                IsGroup = false,
                Name = "ConversationAccountName"
            };

            var recipient = new ChannelAccount()
            {
                Id = "RecipientId",
                Name = "RecipientName"
            };

            return new Activity()
            {
                Text = "Text",
                Type = ActivityTypes.Message,
                Id = $"{guid}",
                ServiceUrl = "ServiceUrl",
                Timestamp = DateTime.Now,
                ChannelId = "ChannelId",
                From = from,
                Conversation = conversation,
                Recipient = recipient,
                ReplyToId = replyToActivityId,
            };
        }
    }
}
