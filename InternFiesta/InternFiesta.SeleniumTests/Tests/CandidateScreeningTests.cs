using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class CandidateScreeningTests
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
        public void Company_ShouldSeeCandidateScreeningOptions()
        {
            LoginAsCompany();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/Internships");

            var internshipCard = wait.Until(d =>
                d.FindElement(By.XPath(
                    "//div[contains(@class,'card')]" +
                    "[.//*[contains(text(),'Application Status Test Internship')]]")));

            var applicantsButton = internshipCard.FindElement(
                By.LinkText("Applicants"));

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].click();",
                applicantsButton);

            wait.Until(d =>
                d.Url.Contains("CompanyApplications"));

            var pageText = driver.FindElement(
                By.TagName("body")).Text;

            Assert.That(pageText, Does.Contain("Shortlist"));
            Assert.That(pageText, Does.Contain("Select"));
            Assert.That(pageText, Does.Contain("Reject"));
            Assert.That(pageText, Does.Contain("Waitlist"));
        }

        private void LoginAsCompany()
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
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}