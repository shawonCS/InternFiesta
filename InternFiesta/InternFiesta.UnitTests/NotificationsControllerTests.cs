using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace InternFiesta.UnitTests
{
    public class NotificationsControllerTests
    {
        [Test]
        public async Task Index_ShouldMarkNotificationsAsRead()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var notification = new Notification
            {
                UserId = "student-1",
                Title = "Test Notification",
                Message = "Test message",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var store = new Mock<IUserStore<ApplicationUser>>();

            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("student-1");

            var controller = new NotificationsController(
                context,
                userManager.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await controller.Index();

            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(notification.IsRead, Is.True);
        }
    }
}