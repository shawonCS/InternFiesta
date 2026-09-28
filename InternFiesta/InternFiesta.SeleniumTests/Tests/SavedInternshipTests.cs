using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class SavedInternshipTests
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

        private IWebElement GetFirstSaveButton()
        {
            return wait.Until(d =>
            {
                var buttons = d.FindElements(
                    By.XPath(
                        "//form[contains(@action,'StudentInternships/Save')]//button[@type='submit']"));

                return buttons.Count > 0
                    ? buttons[0]
                    : null;
            })!;
        }

        private void ClickSafely(IWebElement element)
        {
            ((IJavaScriptExecutor)driver)
                .ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    element);

            ((IJavaScriptExecutor)driver)
                .ExecuteScript(
                    "arguments[0].click();",
                    element);
        }

        [Test]
        public void Student_ShouldBeAbleToSaveInternship()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships");

            var saveButton =
                GetFirstSaveButton();

            ClickSafely(saveButton);

            wait.Until(d =>
                d.Url.Contains("/StudentInternships"));

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships/Saved");

            wait.Until(d =>
                d.PageSource.Contains("Saved Internships"));

            var removeButtons =
                driver.FindElements(
                    By.XPath(
                        "//form[contains(@action,'RemoveSaved')]//button[@type='submit']"));

            Assert.That(
                removeButtons.Count,
                Is.GreaterThan(0));
        }

        [Test]
        public void Student_ShouldBeAbleToRemoveSavedInternship()
        {
            LoginAsStudent();

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships");

            var saveButton =
                GetFirstSaveButton();

            ClickSafely(saveButton);

            driver.Navigate().GoToUrl(
                "http://localhost:5000/StudentInternships/Saved");

            var removeButtonsBefore =
                wait.Until(d =>
                {
                    var buttons = d.FindElements(
                        By.XPath(
                            "//form[contains(@action,'RemoveSaved')]//button[@type='submit']"));

                    return buttons.Count > 0
                        ? buttons
                        : null;
                })!;

            var countBefore =
                removeButtonsBefore.Count;

            ClickSafely(
                removeButtonsBefore[0]);

            wait.Until(d =>
                d.Url.Contains(
                    "/StudentInternships/Saved"));

            wait.Until(d =>
            {
                var currentButtons =
                    d.FindElements(
                        By.XPath(
                            "//form[contains(@action,'RemoveSaved')]//button[@type='submit']"));

                return currentButtons.Count
                    < countBefore;
            });

            var removeButtonsAfter =
                driver.FindElements(
                    By.XPath(
                        "//form[contains(@action,'RemoveSaved')]//button[@type='submit']"));

            Assert.That(
                removeButtonsAfter.Count,
                Is.EqualTo(countBefore - 1));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}