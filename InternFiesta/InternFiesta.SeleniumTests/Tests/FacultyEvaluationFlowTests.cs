
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace InternFiesta.SeleniumTests.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class FacultyEvaluationFlowTests
    {
        private ChromeDriver driver = null!;
        private WebDriverWait wait = null!;

        private const string BaseUrl = "http://localhost:5000";

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();

            wait = new WebDriverWait(
                driver, TimeSpan.FromSeconds(15));
        }

        private void LoginAsFaculty()
        {
            driver.Navigate().GoToUrl(
                BaseUrl + "/Account/Login");

            wait.Until(d =>
                d.FindElement(By.Name("Email")))
                .SendKeys("faculty@internfiesta.test");

            driver.FindElement(By.Name("Password"))
                .SendKeys("Test@12345678");

            driver.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            wait.Until(d =>
                !d.Url.Contains("/Account/Login"));

            Assert.That(
                driver.Url,
                Does.Not.Contain("AccessDenied"));
        }

        [Test]
        public void Faculty_ShouldSaveGrade_AndViewSummary()
        {
            // Step 1: Faculty login
            LoginAsFaculty();

            // Step 2: Open Performance Matrix
            driver.Navigate().GoToUrl(
                BaseUrl + "/AcademicProgress/PerformanceMatrix");

            // Step 3: Find evaluation button
            var evaluateLink = wait.Until(d =>
                d.FindElements(
                    By.CssSelector(
                        "a[href*='FacultyEvaluations'][href*='Edit']"))
                    .FirstOrDefault());

            Assert.That(
                evaluateLink,
                Is.Not.Null,
                "No assigned internship found for evaluation.");

            // Step 4: Open evaluation form
            evaluateLink!.Click();

            var gradeField = wait.Until(d =>
                d.FindElement(By.Name("Grade")));

            // Step 5: Select Grade B+
            if (gradeField.TagName.Equals(
                "select", StringComparison.OrdinalIgnoreCase))
            {
                new SelectElement(gradeField)
                    .SelectByText("B+");
            }
            else
            {
                gradeField.Clear();
                gradeField.SendKeys("B+");
            }

            // Step 6: Write evaluation comments
            string comments =
                "Selenium test evaluation: The student completed " +
                "the internship tasks successfully.";

            var commentsField = driver.FindElement(
                By.Name("Comments"));

            commentsField.Clear();
            commentsField.SendKeys(comments);

            // Step 7: Save evaluation
            var form = gradeField.FindElement(
                By.XPath("./ancestor::form"));

            form.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            // Step 8: Wait for Summary page
            wait.Until(d =>
                d.Url.Contains("/FacultyEvaluations/Summary"));

            // Step 9: Verify saved result
            string summaryText = driver.FindElement(
                By.TagName("body")).Text;

            Assert.That(
                summaryText,
                Does.Contain("B+"),
                "Saved grade was not found in summary.");

            Assert.That(
                summaryText,
                Does.Contain(comments),
                "Saved comments were not found in summary.");
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            driver?.Dispose();
        }
    }
}
