using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class CandidateMatchTests
    {
        private ChromeDriver driver = null!;
        private WebDriverWait wait = null!;

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(10));
        }

        [Test]
        public void Company_ShouldSeeCandidateMatch()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/Account/Login");

            driver.FindElement(By.Name("Email"))
                .SendKeys("company@test.com");

            driver.FindElement(By.Name("Password"))
                .SendKeys("Company@123");

            driver.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            wait.Until(d =>
                !d.Url.Contains("/Account/Login"));

            driver.Navigate().GoToUrl(
                "http://localhost:5000/Internships");

            var internshipCard = wait.Until(d =>
                d.FindElement(By.XPath(
                    "//div[contains(@class,'card')]" +
                    "[.//*[contains(text(),'Application Status Test Internship')]]")));

            internshipCard
                .FindElement(By.LinkText("Applicants"))
                .Click();

            wait.Until(d =>
                d.Url.Contains("CompanyApplications"));

            var pageText = driver.FindElement(
                By.TagName("body")).Text;

            Assert.That(pageText, Does.Contain("Match"));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}