using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class AuthTests
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

        [Test]
        public void InvalidPassword_ShouldShowError()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/Account/Login");

            driver.FindElement(By.Name("Email"))
                .SendKeys("company@test.com");

            driver.FindElement(By.Name("Password"))
                .SendKeys("WrongPassword123");

            driver.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            Assert.That(
                driver.PageSource,
                Does.Contain("Invalid email or password."));
        }

        [Test]
        public void Logout_ShouldReturnToLoginPage()
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

            var logoutButton = wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        "//form[contains(@action,'/Account/Logout')]//button[@type='submit']")));

            logoutButton.Click();

            wait.Until(d =>
                d.Url.Contains("/Account/Login"));

            Assert.That(
                driver.Url,
                Does.Contain("/Account/Login"));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}