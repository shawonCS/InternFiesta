
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace InternFiesta.SeleniumTests.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class FacultyProgressMonitoringTests
    {
        private IWebDriver driver = null!;
        private WebDriverWait wait = null!;

        private const string BaseUrl = "http://localhost:5000";

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(15));
        }

        private void LoginAsFaculty()
        {
            driver.Navigate().GoToUrl(
                BaseUrl + "/Account/Login");

            wait.Until(d =>
                d.FindElement(By.Name("Email")))
                .SendKeys("faculty@internfiesta.test");

            driver.FindElement(By.Name("Password"))
                .SendKeys("Test@12345678");

            driver.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            // Wait until login finishes
            wait.Until(d =>
                !d.Url.Contains("/Account/Login"));

            Assert.That(
                driver.Url,
                Does.Not.Contain("AccessDenied"),
                "Faculty login was not authorized.");
        }

        [Test]
        public void Faculty_ShouldViewAssignedStudentProgress()
        {
            // Step 1: Faculty login
            LoginAsFaculty();

            // Step 2: Open Assigned Students page
            driver.Navigate().GoToUrl(
                BaseUrl + "/AcademicProgress/FacultyStudents");

            wait.Until(d =>
                d.FindElement(By.TagName("body")));

            // Step 3: Verify Faculty Students page opened
            Assert.That(
                driver.Url,
                Does.Contain(
                    "/AcademicProgress/FacultyStudents"),
                "Faculty Students page did not open.");

            Assert.That(
                driver.Url,
                Does.Not.Contain("AccessDenied"));

            // Step 4: Find assigned Student details link
            var studentLink = driver.FindElements(
                By.CssSelector(
                    "a[href*='FacultyStudentDetails']"))
                .FirstOrDefault();

            Assert.That(
                studentLink,
                Is.Not.Null,
                "No assigned student found. " +
                "Assign a selected internship application " +
                "to this faculty before running the test.");

            // Step 5: Open Student progress details
            studentLink!.Click();

            wait.Until(d =>
                d.Url.Contains("FacultyStudentDetails"));

            // Step 6: Verify Student progress page
            string pageText = driver.FindElement(
                By.TagName("body")).Text;

            Assert.That(
                pageText,
                Does.Contain("Progress").IgnoreCase,
                "Student progress information was not displayed.");
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            driver?.Dispose();
        }
    }
}
