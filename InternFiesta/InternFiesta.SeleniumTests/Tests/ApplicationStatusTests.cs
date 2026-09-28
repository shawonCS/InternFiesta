using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class ApplicationStatusTests
    {
        private IWebDriver driver = null!;
        private WebDriverWait wait = null!;

        [SetUp]
        public void Setup()
        {
            var options = new ChromeOptions();

            options.AddUserProfilePreference(
                "credentials_enable_service",
                false);

            options.AddUserProfilePreference(
                "profile.password_manager_enabled",
                false);

            options.AddUserProfilePreference(
                "profile.password_manager_leak_detection",
                false);

            driver = new ChromeDriver(options);

            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(10));
        }

        private void LoginAsStudent()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/Account/Login");

            driver.FindElement(By.Name("Email"))
                .SendKeys("student@test.com");

            driver.FindElement(By.Name("Password"))
                .SendKeys("Student@123");

            driver.FindElement(
                By.CssSelector(
                    "main.public-content form button[type='submit']"))
                .Click();

            wait.Until(d =>
                !d.Url.Contains("/Account/Login"));
        }

        [Test]
        public void Student_ShouldOpenMyApplicationsPage()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships/MyApplications");

            wait.Until(d =>
                d.PageSource.Contains("My Applications"));

            Assert.That(
                driver.Url,
                Does.Contain(
                    "/StudentInternships/MyApplications"));
        }

        [Test]
        public void MyApplications_ShouldDisplayApplicationStatus()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships/MyApplications");

            wait.Until(d =>
                d.PageSource.Contains(
                    "Application Status Test Internship"));

            Assert.That(
                driver.PageSource,
                Does.Contain(
                    "Application Status Test Internship"));

            Assert.That(
                driver.PageSource,
                Does.Contain("Applied"));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}