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
    public class StudentJobFairRegistrationTests
    {
        private IWebDriver driver = null!;
        private WebDriverWait wait = null!;

        private const string BaseUrl =
            "http://localhost:5000";

        private const string EventTitle =
            "ST17 Student Registration Demo";

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver, TimeSpan.FromSeconds(15));
        }

        [Test]
        public void Student_CanRegisterAndSeeConfirmation()
        {
            // Step 1: Open Login page
            driver.Navigate().GoToUrl(
                BaseUrl + "/Account/Login");

            // Step 2: Student login
            wait.Until(d =>
                d.FindElement(By.Name("Email")))
                .SendKeys("student@test.com");

            driver.FindElement(By.Name("Password"))
                .SendKeys("Student@123");

            driver.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            // Step 3: Wait for successful login
            wait.Until(d =>
                d.Url.Contains("/Dashboard"));

            // Step 4: Open Job Fairs
            driver.Navigate().GoToUrl(
                BaseUrl + "/JobFairs");

            // Step 5: Find our demo event
            var eventCard = wait.Until(d =>
                d.FindElements(By.CssSelector(".card"))
                    .FirstOrDefault(card =>
                        card.Text.Contains(EventTitle)));

            Assert.That(
                eventCard,
                Is.Not.Null,
                "Demo Job Fair was not found. " +
                "Create an open event named: " + EventTitle);

            // Step 6: Open event details
            eventCard!.FindElement(
                By.LinkText("View Event"))
                .Click();

            wait.Until(d =>
                d.Url.Contains("/JobFairs/Details"));

            // Step 7: Find Register button
            var registerButton = driver.FindElements(
                By.XPath(
                    "//button[normalize-space()='Register for Job Fair']"))
                .FirstOrDefault();

            Assert.That(
                registerButton,
                Is.Not.Null,
                "Register button not found. " +
                "Check that the event is open and " +
                "the Student is not already registered.");

            // Step 8: Click Register
            registerButton!.Click();

            // Step 9: Verify confirmation
            wait.Until(d =>
                d.FindElement(By.TagName("body"))
                    .Text.Contains("Registration Confirmed!"));

            Assert.That(
                driver.FindElement(By.TagName("body")).Text,
                Does.Contain("Registration Confirmed!"));

            // Step 10: Open My Job Fairs
            driver.Navigate().GoToUrl(
                BaseUrl + "/JobFairs/MyEvents");

            // Step 11: Verify registered event appears
            var registeredEvent = wait.Until(d =>
                d.FindElements(By.TagName("h4"))
                    .FirstOrDefault(h =>
                        h.Text.Contains(EventTitle)));

            Assert.That(
                registeredEvent,
                Is.Not.Null,
                "The registered event was not found " +
                "in My Job Fairs.");

            // Step 12: Verify Registered badge
            var registeredCard = registeredEvent!
                .FindElement(
                    By.XPath(
                        "./ancestor::div[contains(@class,'card')][1]"));

            Assert.That(
                registeredCard.Text,
                Does.Contain("Registered"));
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            driver?.Dispose();
        }
    }
}