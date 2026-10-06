using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class NotificationTests
    {
        private ChromeDriver driver = null!;
        private WebDriverWait wait = null!;

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        [Test]
        public void Student_ShouldBeAbleToOpenNotifications()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/Notifications");

            wait.Until(d =>
                d.FindElement(By.TagName("body")));

            var pageText = driver.FindElement(
                By.TagName("body")).Text;

            Assert.That(pageText, Does.Contain("My Notifications"));
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
                By.CssSelector("button[type='submit']"))
                .Click();

            wait.Until(d =>
                !d.Url.Contains("/Account/Login"));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}