using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace InternFiesta.SeleniumTests.Tests
{
    public class LoginTests
    {
        private IWebDriver driver=null!;

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
        }

        [Test]
        public void LoginPage_ShouldOpen()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/Account/Login");

            Assert.That(driver.Title, Does.Contain("Login"));
        }
        [Test]
        public void Company_ShouldBeAbleToLogin()
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

            Assert.That(
                driver.Url,
                Does.Contain("/Dashboard"));
        }
        [Test]
        public void Student_ShouldBeAbleToLogin()
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

            Assert.That(
                driver.Url,
                Does.Contain("/Dashboard"));
        }
        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}