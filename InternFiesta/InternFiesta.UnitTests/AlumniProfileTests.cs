
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.ViewModels.Alumni;
using InternFiesta.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class AlumniProfileTests
    {
        private ApplicationDbContext _db = null!;
        private Mock<UserManager<ApplicationUser>> _users = null!;

        [SetUp]
        public void SetUp()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            _db = new ApplicationDbContext(options);

            _db.Users.AddRange(
                new ApplicationUser
                {
                    Id = "alumni-1",
                    UserName = "alumni1@test.local",
                    Email = "alumni1@test.local",
                    FullName = "First Alumni"
                },
                new ApplicationUser
                {
                    Id = "alumni-2",
                    UserName = "alumni2@test.local",
                    Email = "alumni2@test.local",
                    FullName = "Second Alumni"
                },
                new ApplicationUser
                {
                    Id = "alumni-3",
                    UserName = "alumni3@test.local",
                    Email = "alumni3@test.local",
                    FullName = "New Alumni"
                });

            _db.AlumniProfiles.AddRange(
                new AlumniProfile
                {
                    UserId = "alumni-1",
                    Headline = "",
                    Industry = "Technology",
                    JobTitle = "Software Engineer",
                    IsVisibleToStudents = false
                },
                new AlumniProfile
                {
                    UserId = "alumni-2",
                    Headline = "",
                    Industry = "Finance",
                    JobTitle = "Accountant",
                    IsVisibleToStudents = false
                });

            _db.SaveChanges();

            var store = new Mock<IUserStore<ApplicationUser>>();

            _users = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);
        }

        private AlumniProfilesController ControllerFor(
            string? signedInId)
        {
            ApplicationUser? account = signedInId == null
                ? null
                : _db.Users.Single(u => u.Id == signedInId);

            _users
                .Setup(x => x.GetUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(account);

            var httpContext = new DefaultHttpContext();

            if (signedInId != null)
            {
                httpContext.User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(
                                ClaimTypes.NameIdentifier,
                                signedInId),
                            new Claim(
                                ClaimTypes.Role,
                                "Alumni")
                        },
                        "TestAuthentication"));
            }

            var controller = new AlumniProfilesController(
                _db,
                _users.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        // TEST 1: Own profile loads in Edit page.
        [Test]
        public async Task EditGet_LoadsOnlySignedInAlumnusProfile()
        {
            var result =
                await ControllerFor("alumni-1").Edit();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var model =
                ((ViewResult)result).Model as AlumniProfileForm;

            Assert.That(model, Is.Not.Null);
            Assert.That(
                model!.JobTitle,
                Is.EqualTo("Software Engineer"));
            Assert.That(
                model.Industry,
                Is.EqualTo("Technology"));
            Assert.That(
                model.JobTitle,
                Is.Not.EqualTo("Accountant"));
        }

        // TEST 2: My Profile shows own data.
        [Test]
        public async Task IndexGet_ShowsOnlySignedInAlumnusProfile()
        {
            var result =
                await ControllerFor("alumni-2").Index();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var profile =
                ((ViewResult)result).Model as AlumniProfile;

            Assert.That(profile, Is.Not.Null);
            Assert.That(
                profile!.UserId,
                Is.EqualTo("alumni-2"));
        }

        // TEST 3: Own profile updates, other user's does not.
        [Test]
        public async Task EditPost_UpdatesOwnProfile_LeavesOtherProfileUntouched()
        {
            var controller = ControllerFor("alumni-1");

            var result = await controller.Edit(
                new AlumniProfileForm
                {
                    Industry = "  Software  ",
                    JobTitle = "  QA Engineer  ",
                    About = "  Learning testing  ",
                    IsVisibleToStudents = true,
                    IsAvailableForMentoring = true
                });

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            Assert.That(
                ((RedirectToActionResult)result).ActionName,
                Is.EqualTo("Index"));

            var own = await _db.AlumniProfiles
                .SingleAsync(p => p.UserId == "alumni-1");

            var other = await _db.AlumniProfiles
                .SingleAsync(p => p.UserId == "alumni-2");

            Assert.Multiple(() =>
            {
                Assert.That(
                    own.JobTitle,
                    Is.EqualTo("QA Engineer"));
                Assert.That(
                    own.Industry,
                    Is.EqualTo("Software"));
                Assert.That(
                    own.About,
                    Is.EqualTo("Learning testing"));
                Assert.That(
                    own.IsVisibleToStudents,
                    Is.True);
                Assert.That(
                    own.IsAvailableForMentoring,
                    Is.True);
                Assert.That(
                    other.JobTitle,
                    Is.EqualTo("Accountant"));
                Assert.That(
                    other.Industry,
                    Is.EqualTo("Finance"));
            });
        }

        // TEST 4: Profile can be created if missing.
        [Test]
        public async Task EditPost_CreatesProfileForCurrentAlumni_IfMissing()
        {
            var result = await ControllerFor("alumni-3")
                .Edit(new AlumniProfileForm
                {
                    JobTitle = "Junior Developer"
                });

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var created = await _db.AlumniProfiles
                .SingleAsync(p => p.UserId == "alumni-3");

            Assert.That(
                created.JobTitle,
                Is.EqualTo("Junior Developer"));

            Assert.That(
                created.Headline,
                Is.EqualTo(string.Empty));

            Assert.That(
                await _db.AlumniProfiles.CountAsync(
                    p => p.UserId == "alumni-3"),
                Is.EqualTo(1));
        }

        // TEST 5: Repeated saves do not duplicate profiles.
        [Test]
        public async Task EditPost_UpdatesExistingProfileWithoutDuplicate()
        {
            var controller = ControllerFor("alumni-1");

            await controller.Edit(new AlumniProfileForm
            {
                JobTitle = "Developer"
            });

            await controller.Edit(new AlumniProfileForm
            {
                JobTitle = "Senior Developer"
            });

            Assert.That(
                await _db.AlumniProfiles.CountAsync(
                    p => p.UserId == "alumni-1"),
                Is.EqualTo(1));

            var saved = await _db.AlumniProfiles
                .SingleAsync(p => p.UserId == "alumni-1");

            Assert.That(
                saved.JobTitle,
                Is.EqualTo("Senior Developer"));
        }

        // TEST 6: Invalid LinkedIn URL is rejected.
        [Test]
        public async Task EditPost_InvalidLinkedInUrl_DoesNotChangeDatabase()
        {
            var controller = ControllerFor("alumni-1");

            var result = await controller.Edit(
                new AlumniProfileForm
                {
                    JobTitle = "Should Not Be Saved",
                    LinkedInUrl = "https://example.com/fake"
                });

            Assert.That(
                result,
                Is.TypeOf<ViewResult>());

            Assert.That(
                controller.ModelState.IsValid,
                Is.False);

            var saved = await _db.AlumniProfiles
                .SingleAsync(p => p.UserId == "alumni-1");

            Assert.That(
                saved.JobTitle,
                Is.EqualTo("Software Engineer"));
        }

        // TEST 7: Invalid form cannot update data.
        [Test]
        public async Task EditPost_InvalidModelState_DoesNotSaveChanges()
        {
            var controller = ControllerFor("alumni-1");

            controller.ModelState.AddModelError(
                nameof(AlumniProfileForm.Industry),
                "Industry exceeds permitted length");

            var result = await controller.Edit(
                new AlumniProfileForm
                {
                    Industry = new string('X', 101),
                    JobTitle = "Should Not Be Saved"
                });

            Assert.That(
                result,
                Is.TypeOf<ViewResult>());

            var saved = await _db.AlumniProfiles
                .SingleAsync(p => p.UserId == "alumni-1");

            Assert.That(
                saved.JobTitle,
                Is.EqualTo("Software Engineer"));
        }

        // TEST 8: Industry max length validation.
        [Test]
        public void ProfileForm_OverlongIndustry_IsInvalid()
        {
            var form = new AlumniProfileForm
            {
                Industry = new string('X', 101)
            };

            var errors = new List<ValidationResult>();

            bool valid = Validator.TryValidateObject(
                form,
                new ValidationContext(form),
                errors,
                validateAllProperties: true);

            Assert.That(valid, Is.False);
            Assert.That(errors, Is.Not.Empty);
        }

        // TEST 9: Registration requires account fields.
        [Test]
        public void AlumniRegistration_MissingRequiredAccountFields_IsInvalid()
        {
            var form = new RegisterViewModel
            {
                Role = "Alumni",
                FullName = "",
                Email = "",
                Password = "",
                ConfirmPassword = ""
            };

            var errors = new List<ValidationResult>();

            bool valid = Validator.TryValidateObject(
                form,
                new ValidationContext(form),
                errors,
                validateAllProperties: true);

            Assert.That(valid, Is.False);

            Assert.That(
                errors.SelectMany(e => e.MemberNames),
                Does.Contain(nameof(RegisterViewModel.FullName)));

            Assert.That(
                errors.SelectMany(e => e.MemberNames),
                Does.Contain(nameof(RegisterViewModel.Email)));

            Assert.That(
                errors.SelectMany(e => e.MemberNames),
                Does.Contain(nameof(RegisterViewModel.Password)));
        }

        // TEST 10: Professional fields are optional.
        [Test]
        public void AlumniRegistration_OptionalProfessionalFieldsMayBeBlank()
        {
            var form = new RegisterViewModel
            {
                Role = "Alumni",
                FullName = "Test Alumni",
                Email = "test.alumni@example.com",
                Password = "TestSecure123!",
                ConfirmPassword = "TestSecure123!"
            };

            var errors = new List<ValidationResult>();

            bool valid = Validator.TryValidateObject(
                form,
                new ValidationContext(form),
                errors,
                validateAllProperties: true);

            Assert.That(
                valid,
                Is.True,
                "Professional fields should be optional.");
        }

        // TEST 11: Missing logged-in user cannot save.
        [Test]
        public async Task EditPost_WithoutSignedInUser_DoesNotSave()
        {
            var result = await ControllerFor(null)
                .Edit(new AlumniProfileForm
                {
                    JobTitle = "Unauthorized Update"
                });

            Assert.That(
                result,
                Is.TypeOf<ChallengeResult>());

            Assert.That(
                await _db.AlumniProfiles.CountAsync(),
                Is.EqualTo(2));
        }

        // TEST 12: Controller declares Alumni authorization.
        [Test]
        public void Controller_DeclaresAlumniAuthorization()
        {
            var attributes = typeof(AlumniProfilesController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>();

            Assert.That(
                attributes.Any(a => a.Roles == "Alumni"),
                Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }
    }
}
