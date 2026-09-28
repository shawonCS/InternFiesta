using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class InternshipApplicationTests
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

        private IWebElement GetFirstApplyButton()
        {
            return wait.Until(d =>
            {
                var buttons = d.FindElements(
                    By.XPath(
                        "//form[contains(@action,'StudentInternships/Apply')]//button[@type='submit']"));

                return buttons.Count > 0
                    ? buttons[0]
                    : null;
            })!;
        }

        private void AcceptConfirmation()
        {
            var alert = wait.Until(d =>
            {
                try
                {
                    return d.SwitchTo().Alert();
                }
                catch (NoAlertPresentException)
                {
                    return null;
                }
            });

            alert!.Accept();
        }

        [Test]
        public void Student_ShouldBeAbleToApplyForInternship()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships");

            var applyButton = GetFirstApplyButton();

            applyButton.Click();

            AcceptConfirmation();

            wait.Until(d =>
                d.Url.Contains("/StudentInternships"));

            Assert.That(
                driver.Url,
                Does.Contain("/StudentInternships"));
        }

        [Test]
        public void DuplicateApplication_ShouldShowWarning()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships");

            var firstApplyButton =
                GetFirstApplyButton();

            firstApplyButton.Click();

            AcceptConfirmation();

            wait.Until(d =>
                d.Url.Contains("/StudentInternships"));

            var secondApplyButton =
               GetFirstApplyButton();

            ((IJavaScriptExecutor)driver)
                .ExecuteScript(
                    "arguments[0].scrollIntoView({block: 'center'});",
                    secondApplyButton);

            ((IJavaScriptExecutor)driver)
                .ExecuteScript(
                    "arguments[0].click();",
                    secondApplyButton);

            AcceptConfirmation();

            wait.Until(d =>
                d.PageSource.Contains(
                    "You have already applied for this internship."));

            Assert.That(
                driver.PageSource,
                Does.Contain(
                    "You have already applied for this internship."));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}