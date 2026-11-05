
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Globalization;
using System.Linq;

namespace InternFiesta.SeleniumTests.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class CoordinatorJobFairCreationTests
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
                driver, TimeSpan.FromSeconds(15));
        }

        private void LoginAsCoordinator()
        {
            driver.Navigate().GoToUrl(
                BaseUrl + "/Account/Login");

            driver.FindElement(By.Id("Email"))
                .SendKeys("coordinator@internfiesta.test");

            driver.FindElement(By.Id("Password"))
                .SendKeys("Test@12345678");

            driver.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            wait.Until(d =>
                d.Url.Contains("/Dashboard"));
        }

        private void SetDateTime(string elementId, DateTime value)
        {
            var element = driver.FindElement(
                By.Id(elementId));

            var formattedDate = value.ToString(
                "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture);

            var javascript = (IJavaScriptExecutor)driver;

            javascript.ExecuteScript(
                "arguments[0].value = arguments[1];" +
                "arguments[0].dispatchEvent(new Event('input', { bubbles: true }));" +
                "arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
                element,
                formattedDate);
        }

        [Test]
        public void Coordinator_ShouldCreateJobFair_AndSeeItInList()
        {
            // Step 1: Login as Coordinator
            LoginAsCoordinator();

            // Step 2: Open Create Job Fair page
            driver.Navigate().GoToUrl(
                BaseUrl + "/CoordinatorJobFairs/Create");

            wait.Until(d =>
                d.FindElement(By.Id("Title")));

            // Step 3: Prepare test data
            string fairTitle =
                "Selenium Coordinator Fair " +
                Guid.NewGuid().ToString("N")[..8];

            DateTime startDate = DateTime.UtcNow
                .AddHours(6)
                .AddDays(20)
                .Date
                .AddHours(10);

            DateTime endDate = startDate.AddHours(5);

            // Step 4: Fill up the form
            driver.FindElement(By.Id("Title"))
                .SendKeys(fairTitle);

            driver.FindElement(By.Id("Venue"))
                .SendKeys("UAP Campus Auditorium, Dhaka");

            driver.FindElement(By.Id("Description"))
                .SendKeys(
                    "A university career fair for student internship " +
                    "opportunities and employer networking.");

            SetDateTime("StartsAt", startDate);
            SetDateTime("EndsAt", endDate);

            // Step 5: Submit the form
            driver.FindElement(
                By.XPath(
                    "//button[normalize-space()='Create Job Fair']"))
                .Click();

            // Step 6: Verify created event appears in the list
            var createdEvent = wait.Until(d =>
                d.FindElements(By.TagName("h4"))
                    .FirstOrDefault(e =>
                        e.Text.Contains(fairTitle)));

            Assert.That(
                createdEvent,
                Is.Not.Null,
                "The newly created Job Fair was not found in the event list.");

            Assert.That(
                driver.Url,
                Does.Contain("/CoordinatorJobFairs"));

            // Step 7: Verify venue is displayed
            var eventCard = createdEvent!.FindElement(
                By.XPath(
                    "./ancestor::div[contains(@class,'card')][1]"));

            Assert.That(
                eventCard.Text,
                Does.Contain("UAP Campus Auditorium, Dhaka"));
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            driver?.Dispose();
        }
    }
}
