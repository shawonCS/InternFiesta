using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class InterviewSchedulingTests
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
        public void Company_ShouldBeAbleToOpenInterviewSchedule()
        {
            LoginAsCompany();

            driver.Navigate().GoToUrl("http://localhost:5000/Internships");

            var internshipCard = wait.Until(d =>
                d.FindElement(By.XPath(
                    "//div[contains(@class,'card')]" +
                    "[.//*[contains(text(),'Application Status Test Internship')]]")));

            var applicantsButton = internshipCard.FindElement(By.LinkText("Applicants"));

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                applicantsButton);

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].click();",
                applicantsButton);

            wait.Until(d => d.Url.Contains("CompanyApplications"));

            var scheduleButtons = driver.FindElements(
                By.XPath("//a[contains(normalize-space(.),'Schedule')]"));

            if (scheduleButtons.Count == 0)
            {
                driver.FindElement(
                    By.XPath("//button[contains(normalize-space(.),'Shortlist')]"))
                    .Click();

                wait.Until(d =>
                    d.FindElements(
                        By.XPath("//a[contains(normalize-space(.),'Schedule')]"))
                    .Count > 0);
            }

            driver.FindElement(
                By.XPath("//a[contains(normalize-space(.),'Schedule')]"))
                .Click();

            wait.Until(d => d.Url.Contains("/Interview/Schedule"));

            var bodyText = driver.FindElement(By.TagName("body")).Text;

            Assert.That(bodyText, Does.Contain("Interview"));
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