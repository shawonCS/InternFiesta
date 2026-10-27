
using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class CoordinatorOverviewBrowserTests
    {
        private ChromeDriver _driver = null!;
        private WebDriverWait _wait = null!;

        private string _baseUrl = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _baseUrl = (
                Environment.GetEnvironmentVariable(
                    "INTERNFIESTA_TEST_BASE_URL")
                ?? "http://localhost:5000"
            ).TrimEnd('/');

            _email = Environment.GetEnvironmentVariable(
                "INTERNFIESTA_TEST_COORDINATOR_EMAIL")
                ?? string.Empty;

            _password = Environment.GetEnvironmentVariable(
                "INTERNFIESTA_TEST_COORDINATOR_PASSWORD")
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_email) ||
                string.IsNullOrWhiteSpace(_password))
            {
                Assert.Fail(
                    "Coordinator test credentials missing. " +
                    "Configure the local runsettings file " +
                    "before running from Test Explorer.");
            }

            var options = new ChromeOptions
            {
                AcceptInsecureCertificates = true
            };

            options.AddArgument("--window-size=1440,900");

            _driver = new ChromeDriver(options);

            _wait = new WebDriverWait(
                _driver,
                TimeSpan.FromSeconds(20));

            _wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException));
        }

        [Test]
        public void Coordinator_Login_Overview_ShowsSixCardsAndAcademicStatus()
        {
            // STEP 1: Open Login page.
            _driver.Navigate().GoToUrl(
                _baseUrl + "/Account/Login");

            var emailField = _wait.Until(d =>
                d.FindElement(By.Name("Email")));

            var passwordField = _driver.FindElement(
                By.Name("Password"));

            emailField.SendKeys(_email);
            passwordField.SendKeys(_password);

            var loginForm = passwordField.FindElement(
                By.XPath("./ancestor::form[1]"));

            loginForm.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            // STEP 2: Wait for login.
            try
            {
                _wait.Until(d =>
                    !IsLoginUrl(d.Url) ||
                    HasLoginError(d));
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail(
                    "Coordinator Login timed out. URL: " +
                    _driver.Url);
            }

            if (IsLoginUrl(_driver.Url))
            {
                Assert.Fail(
                    "Coordinator Login failed. " +
                    "Check local test credentials. URL: " +
                    _driver.Url);
            }

            // STEP 3: Open Coordinator Overview HTML page.
            // Do not open the Summary JSON endpoint here.
            _driver.Navigate().GoToUrl(
                _baseUrl + "/Coordinator/Overview");

            try
            {
                _wait.Until(d =>
                    IsLoginUrl(d.Url) ||
                    IsAccessDeniedUrl(d.Url) ||
                    d.FindElements(
                        By.CssSelector(
                            ".row.g-3.mb-4 .card"))
                        .Count >= 6);
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail(
                    "Coordinator Overview did not show " +
                    "six summary cards. URL: " +
                    _driver.Url);
            }

            Assert.That(
                IsLoginUrl(_driver.Url),
                Is.False,
                "Overview redirected to Login.");

            Assert.That(
                IsAccessDeniedUrl(_driver.Url),
                Is.False,
                "Coordinator received Access Denied.");

            Assert.That(
                _driver.Url,
                Does.Contain("/Coordinator/Overview"));

            // STEP 4: Verify six cards.
            var cards = _driver.FindElements(
                By.CssSelector(
                    ".row.g-3.mb-4 .card"));

            Assert.That(
                cards.Count,
                Is.EqualTo(6));

            string[] expectedLabels =
            {
                "Students",
                "Companies",
                "Internships",
                "Applications",
                "Job Fairs",
                "Academic Participants"
            };

            foreach (var label in expectedLabels)
            {
                var card = cards.SingleOrDefault(c =>
                    c.FindElements(
                        By.CssSelector(".text-muted.small"))
                        .Any(x => x.Text.Trim() == label));

                Assert.That(
                    card,
                    Is.Not.Null,
                    "Missing summary card: " + label);

                var displayedCount = card!.FindElement(
                    By.CssSelector(".fs-2.fw-bold"))
                    .Text.Trim();

                bool validNumber = int.TryParse(
                    displayedCount,
                    NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out int numericCount);

                Assert.That(
                    validNumber && numericCount >= 0,
                    Is.True,
                    "Invalid count in card: " + label);
            }

            // STEP 5: Verify academic status section.
            string pageText = _driver.FindElement(
                By.TagName("body")).Text;

            Assert.That(
                pageText,
                Does.Contain(
                    "Academic Participation & Status"));

            var tables = _driver.FindElements(
                By.CssSelector("table.table"));

            if (tables.Count > 0)
            {
                // Existing academic records: table must show.
                var table = tables[0];

                var headers = table.FindElements(
                    By.CssSelector("thead th"))
                    .Select(h => h.Text.Trim())
                    .ToArray();

                string[] expectedHeaders =
                {
                    "Student",
                    "Internship",
                    "Faculty",
                    "Application",
                    "Academic Tracking",
                    "Updates",
                    "Milestones",
                    "Assigned"
                };

                foreach (string heading in expectedHeaders)
                {
                    Assert.That(
                        headers,
                        Does.Contain(heading),
                        "Missing table heading: " + heading);
                }

                var rows = table.FindElements(
                    By.CssSelector("tbody tr"));

                Assert.That(
                    rows.Count,
                    Is.GreaterThan(0));

                foreach (var row in rows)
                {
                    var cells = row.FindElements(
                        By.TagName("td"));

                    Assert.That(
                        cells.Count,
                        Is.EqualTo(8));

                    Assert.That(
                        cells[0].Text,
                        Is.Not.Empty,
                        "Student name missing.");

                    Assert.That(
                        cells[4].Text,
                        Is.Not.Empty,
                        "Academic tracking status missing.");
                }
            }
            else
            {
                // Empty academic database: show a helpful message.
                Assert.That(
                    pageText,
                    Does.Contain(
                        "No academic participation records yet"),
                    "No academic table or empty-state message.");
            }

            // STEP 6: Check navigation links.
            Assert.That(
                _driver.FindElements(
                    By.CssSelector(
                        "a[href*='CoordinatorJobFairs']"))
                    .Count,
                Is.GreaterThan(0),
                "Manage Job Fairs link missing.");

            Assert.That(
                _driver.FindElements(
                    By.CssSelector(
                        "a[href*='AssignFaculty']"))
                    .Count,
                Is.GreaterThan(0),
                "Assign Faculty link missing.");
        }

        private static bool IsLoginUrl(string url)
        {
            return url.Contains(
                       "/Account/Login",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   url.Contains(
                       "/Identity/Account/Login",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAccessDeniedUrl(string url)
        {
            return url.Contains(
                "AccessDenied",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasLoginError(
            IWebDriver driver)
        {
            return driver.FindElements(
                By.CssSelector(
                    ".validation-summary-errors, " +
                    ".field-validation-error, " +
                    ".alert-danger"))
                .Any(e =>
                    e.Displayed &&
                    !string.IsNullOrWhiteSpace(e.Text));
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
