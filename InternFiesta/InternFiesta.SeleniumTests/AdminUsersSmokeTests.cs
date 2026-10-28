
using System;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class AdminUsersSmokeTests
    {
        private const string DemoEmail =
            "admin.smoke.demo@example.test";

        private const string DemoName =
            "T-ST-20 Disposable Demo";

        private ChromeDriver _driver = null!;
        private WebDriverWait _wait = null!;

        private string _baseUrl = string.Empty;
        private string _adminEmail = string.Empty;
        private string _adminPassword = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _baseUrl = (
                Environment.GetEnvironmentVariable(
                    "INTERNFIESTA_TEST_BASE_URL")
                ?? "http://localhost:5000"
            ).TrimEnd('/');

            _adminEmail =
                Environment.GetEnvironmentVariable(
                    "INTERNFIESTA_TEST_ADMIN_EMAIL")
                ?? string.Empty;

            _adminPassword =
                Environment.GetEnvironmentVariable(
                    "INTERNFIESTA_TEST_ADMIN_PASSWORD")
                ?? string.Empty;

            Assert.That(
                Uri.TryCreate(_baseUrl, UriKind.Absolute, out var url)
                && url!.IsLoopback
                && (url.Scheme == "http" || url.Scheme == "https"),
                Is.True,
                "Use only a local test website.");

            Assert.That(_adminEmail, Is.Not.Empty);
            Assert.That(_adminPassword, Is.Not.Empty);

            Assert.That(
                _adminEmail,
                Is.Not.EqualTo(DemoEmail).IgnoreCase);

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
        public void Administrator_SearchAndRestoreDemoAccount()
        {
            // STEP 1: Administrator Login
            _driver.Navigate().GoToUrl(
                _baseUrl + "/Account/Login");

            _wait.Until(d =>
                d.FindElement(By.Name("Email")))
                .SendKeys(_adminEmail);

            var passwordInput =
                _driver.FindElement(By.Name("Password"));

            passwordInput.SendKeys(_adminPassword);

            passwordInput.FindElement(
                By.XPath("./ancestor::form[1]"))
                .FindElement(
                    By.CssSelector("button[type='submit']"))
                .Click();

            _wait.Until(d =>
                !d.Url.Contains(
                    "/Account/Login",
                    StringComparison.OrdinalIgnoreCase));

            // STEP 2: Open Manage Users
            _driver.Navigate().GoToUrl(
                _baseUrl + "/Admin/Users");

            var search = _wait.Until(d =>
                d.FindElement(By.Id("userSearch")));

            // STEP 3: Search exact demo email
            search.Clear();
            search.SendKeys(DemoEmail);

            search.FindElement(
                By.XPath("./ancestor::form[1]"))
                .FindElement(
                    By.CssSelector("button[type='submit']"))
                .Click();


            // STEP 4: Wait for the correct search result
            var rowLocator = By.XPath(
                "//table//tbody/tr[td[normalize-space(.)='" +
                DemoEmail + "']]");

            _wait.Until(d =>
            {
                try
                {
                    var rows = d.FindElements(rowLocator);

                    if (rows.Count != 1)
                        return false;

                    string text = rows[0].Text;

                    return text.Contains(DemoName) &&
                           text.Contains("Student");
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });

            // STEP 5: Open the demo account details
            _wait.Until(d =>
            {
                try
                {
                    var detailsLink = d.FindElement(
                        By.XPath(
                            "//table//tbody/tr[td[normalize-space(.)='" +
                            DemoEmail +
                            "']]//a[normalize-space()='View Details']"));

                    detailsLink.Click();
                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            });


            // STEP 5: View Details

            _wait.Until(d =>
                d.Url.Contains(
                    "/Admin/Users/Details/",
                    StringComparison.OrdinalIgnoreCase));

            string targetId = new Uri(_driver.Url)
                .AbsolutePath.TrimEnd('/')
                .Split('/').Last();

            // STEP 6: Safety verification
            var name = _driver.FindElement(
                By.CssSelector(".card-body h4.mb-1"));

            Assert.That(name.Text.Trim(), Is.EqualTo(DemoName));

            var email = name.FindElement(
                By.XPath("./following-sibling::p")).Text.Trim();

            Assert.That(
                email,
                Is.EqualTo(DemoEmail).IgnoreCase);

            var role = _driver.FindElement(
                By.XPath(
                    "//dt[normalize-space()='Assigned Role(s)']" +
                    "/following-sibling::dd[1]"));

            Assert.That(role.Text.Trim(), Is.EqualTo("Student"));

            Assert.That(
                _driver.FindElements(
                    By.CssSelector(".card-body .badge"))
                    .Any(e => e.Text.Trim() == "Disabled"),
                Is.True,
                "Demo account must initially be Disabled.");

            // STEP 7: Open Restore confirmation
            var restoreButton = _wait.Until(d =>
                d.FindElements(
                    By.CssSelector(
                        "button[data-bs-target='#confirmStatusModal']"))
                 .FirstOrDefault(b =>
                     b.Displayed &&
                     b.Enabled &&
                     b.Text.Contains("Restore")));

            Assert.That(restoreButton, Is.Not.Null);

            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                restoreButton);

            restoreButton!.Click();

            // STEP 8: Verify confirmation targets demo
            var modal = _wait.Until(d =>
                d.FindElement(
                    By.CssSelector("#confirmStatusModal.show")));

            var form = modal.FindElement(
                By.CssSelector("form[action*='SetStatus']"));


            // Verify the Restore form targets the correct demo account
            string actionUrl =
                form.GetDomAttribute("action") ?? string.Empty;

            TestContext.Progress.WriteLine(
                "Restore form action: " + actionUrl);

            Assert.That(actionUrl, Is.Not.Empty,
                "Restore form action is missing.");

            // Works with both relative and absolute URLs
            var submitUrl = new Uri(
                new Uri(_driver.Url),
                actionUrl);

            var baseAddress = new Uri(_baseUrl);

            Assert.That(
                submitUrl.GetLeftPart(UriPartial.Authority),
                Is.EqualTo(
                    baseAddress.GetLeftPart(UriPartial.Authority)),
                "Safety check: form points to another website.");

            Assert.That(
                submitUrl.AbsolutePath.TrimEnd('/'),
                Does.EndWith("/SetStatus/" + targetId).IgnoreCase,
                "Safety check: wrong target account.");

            Assert.That(
                form.FindElement(By.Name("action"))
                    .GetAttribute("value"),
                Is.EqualTo("Restore"));

            // STEP 9: Confirm Restore
            form.FindElement(
                By.CssSelector("button[type='submit']"))
                .Click();

            // STEP 10: Verify Active status
            _wait.Until(d =>
                d.Url.Contains(
                    "/Admin/Users/Details/",
                    StringComparison.OrdinalIgnoreCase)
                &&
                d.FindElements(
                    By.CssSelector(".card-body .badge"))
                 .Any(b => b.Text.Trim() == "Active"));

            Assert.That(
                _driver.FindElement(
                    By.CssSelector(".card-body h4.mb-1"))
                    .Text.Trim(),
                Is.EqualTo(DemoName));
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
