using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class InternshipUiTests
    {
        private IWebDriver driver = null!;
        private WebDriverWait wait = null!;

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(5));
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
                d.Url.Contains("/Dashboard"));
        }

        [Test]
        public void InternshipPage_ShouldShowCreateButton()
        {
            LoginAsCompany();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/Internships");

            var createButton = wait.Until(d =>
                d.FindElement(By.LinkText("Create Internship")));

            Assert.That(
                createButton.Displayed,
                Is.True);
        }

        [Test]
        public void CreateInternshipPage_ShouldShowFormFields()
        {
            LoginAsCompany();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/Internships/Create");

            var title = wait.Until(d =>
                d.FindElement(By.Id("Title")));

            var description =
                driver.FindElement(By.Id("Description"));

            var location =
                driver.FindElement(By.Id("Location"));

            Assert.Multiple(() =>
            {
                Assert.That(title.Displayed, Is.True);
                Assert.That(description.Displayed, Is.True);
                Assert.That(location.Displayed, Is.True);
            });
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}