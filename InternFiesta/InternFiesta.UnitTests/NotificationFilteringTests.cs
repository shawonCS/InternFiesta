using System.Security.Claims;
using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    public class NotificationFilteringTests
    {
        [Test]
        public async Task Index_ShouldShowOnlyCurrentUsersNotifications()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            context.Notifications.Add(new Notification
            {
                UserId = "student-1",
                Title = "My Notification",
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            context.Notifications.Add(new Notification
            {
                UserId = "student-2",
                Title = "Other Notification",
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

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

            var viewResult = result as ViewResult;
            var model = viewResult!.Model as List<Notification>;

            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(1));
            Assert.That(model[0].UserId, Is.EqualTo("student-1"));
        }
    }
}