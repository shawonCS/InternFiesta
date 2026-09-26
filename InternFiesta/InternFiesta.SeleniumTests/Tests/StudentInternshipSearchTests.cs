using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class StudentInternshipSearchTests
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

            options.AddArgument(
                "--disable-features=PasswordLeakDetection");

            driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(10));

            LoginAsStudent();
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
                d.Url.Contains("/Dashboard"));
        }

        [Test]
        public void Student_ShouldSeeActiveInternships()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships");

            wait.Until(d =>
                d.PageSource.Contains(
                    "Internship Opportunities"));

            Assert.That(
                driver.PageSource,
                Does.Contain("Internship Opportunities"));
        }

        [Test]
        public void Student_ShouldSearchAndFilterInternships()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships");

            var location =
                driver.FindElement(By.Name("location"));

            location.SendKeys("Dhaka");

            driver.FindElement(By.XPath("//button[@type='submit' and contains(normalize-space(.), 'Search')]"))
                .Click();

            wait.Until(d =>
                d.Url.Contains("location="));

            Assert.That(
                driver.Url,
                Does.Contain("location=Dhaka"));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}