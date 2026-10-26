
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
    public class AlumniProfileFlowTests
    {
        private ChromeDriver _driver = null!;
        private WebDriverWait _wait = null!;

        private string _baseUrl = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;

        // ==========================================
        // SETUP
        // ==========================================

        [SetUp]
        public void SetUp()
        {
            _baseUrl = (
                Environment.GetEnvironmentVariable(
                    "INTERNFIESTA_TEST_BASE_URL")
                ?? "http://localhost:5000"
            ).TrimEnd('/');

            _email = Environment.GetEnvironmentVariable(
                "INTERNFIESTA_TEST_ALUMNI_EMAIL")
                ?? string.Empty;

            _password = Environment.GetEnvironmentVariable(
                "INTERNFIESTA_TEST_ALUMNI_PASSWORD")
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_email) ||
                string.IsNullOrWhiteSpace(_password))
            {
                Assert.Fail(
                    "Alumni test credentials are missing. " +
                    "Configure them before running dotnet test.");
            }

            var options = new ChromeOptions
            {
                AcceptInsecureCertificates = true
            };

            options.AddArgument("--start-maximized");

            _driver = new ChromeDriver(options);

            _wait = new WebDriverWait(
                _driver,
                TimeSpan.FromSeconds(20));

            _wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException));
        }

        // ==========================================
        // MAIN SELENIUM TEST
        // ==========================================

        [Test]
        public void Alumni_Login_EditProfile_Save_SeeSavedValues_AndReopenEdit()
        {
            // STEP 1: Open Login page.

            _driver.Navigate().GoToUrl(
                _baseUrl + "/Account/Login");

            var emailInput = WaitForElement("Email");
            var passwordInput = WaitForElement("Password");

            // STEP 2: Enter credentials.

            emailInput.Clear();
            emailInput.SendKeys(_email);

            passwordInput.Clear();
            passwordInput.SendKeys(_password);

            var loginForm = passwordInput.FindElement(
                By.XPath("./ancestor::form[1]"));

            var loginButton = loginForm.FindElement(
                By.CssSelector("button[type='submit']"));

            loginButton.Click();

            // STEP 3: Wait for Login result.

            try
            {
                _wait.Until(d =>
                    !IsLoginUrl(d.Url) ||
                    HasValidationErrors(d));
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail(Diagnostic(
                    "Login timed out."));
            }

            if (IsLoginUrl(_driver.Url))
            {
                Assert.Fail(Diagnostic(
                    "Alumni Login failed. " +
                    "Check the registered email and password."));
            }

            // STEP 4: Verify My Profile access.

            _driver.Navigate().GoToUrl(
                _baseUrl + "/AlumniProfiles/Index");

            WaitForProtectedElement(
                By.CssSelector(
                    "a[href*='/AlumniProfiles/Edit']"),
                "Alumni My Profile");

            // STEP 5: Open Edit Profile.

            _driver.Navigate().GoToUrl(
                _baseUrl + "/AlumniProfiles/Edit");

            WaitForProtectedElement(
                By.Name("JobTitle"),
                "Alumni Edit Profile");

            // STEP 6: Generate unique values.

            string jobTitle =
                "Meem QA " +
                Guid.NewGuid().ToString("N")[..8];

            string industry = "Software Engineering";

            string company = "InternFiesta Test Company";

            string about =
                "Automated Alumni profile save test.";

            // STEP 7: Fill Edit Profile fields.

            ReplaceValue("Industry", industry);
            ReplaceValue("JobTitle", jobTitle);
            ReplaceValue("CompanyName", company);
            ReplaceValue("About", about);

            // STEP 8: Submit Save Changes.

            var jobTitleInput = WaitForElement("JobTitle");

            var editForm = jobTitleInput.FindElement(
                By.XPath("./ancestor::form[1]"));

            var saveButton = editForm.FindElement(
                By.CssSelector("button[type='submit']"));

            SubmitProfileForm(editForm, saveButton);

            // STEP 9: Wait for the Save result.

            try
            {
                _wait.Until(d =>
                    IsLoginUrl(d.Url) ||
                    IsAccessDeniedUrl(d.Url) ||
                    !IsEditUrl(d.Url) ||
                    HasValidationErrors(d));
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail(Diagnostic(
                    "Profile Save timed out. " +
                    "The Edit page did not finish saving."));
            }

            CheckAuthentication("Profile Save");

            if (IsEditUrl(_driver.Url))
            {
                Assert.Fail(Diagnostic(
                    "Profile Save was unsuccessful. " +
                    "The form remained on Edit Profile."));
            }

            // STEP 10: Open My Profile again.

            _driver.Navigate().GoToUrl(
                _baseUrl + "/AlumniProfiles/Index");

            // STEP 11: Verify updated Job Title.

            try
            {
                _wait.Until(d =>
                {
                    if (IsLoginUrl(d.Url) ||
                        IsAccessDeniedUrl(d.Url))
                    {
                        return true;
                    }

                    return d.FindElements(
                        By.TagName("body"))
                        .Any(body => body.Text.Contains(
                            jobTitle,
                            StringComparison.Ordinal));
                });
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail(Diagnostic(
                    "Saved Job Title was not visible " +
                    "on My Profile. Expected: " + jobTitle));
            }

            CheckAuthentication("My Profile verification");

            string profileText = _driver.FindElement(
                By.TagName("body")).Text;

            Assert.Multiple(() =>
            {
                Assert.That(
                    profileText,
                    Does.Contain(jobTitle),
                    "Saved Job Title is missing.");

                Assert.That(
                    profileText,
                    Does.Contain(company),
                    "Saved Company is missing.");
            });

            // STEP 12: Reopen Edit Profile.

            _driver.Navigate().GoToUrl(
                _baseUrl + "/AlumniProfiles/Edit");

            WaitForProtectedElement(
                By.Name("JobTitle"),
                "Reopened Edit Profile");

            // STEP 13: Verify persisted values.

            string savedJobTitle =
                GetInputValue("JobTitle");

            string savedIndustry =
                GetInputValue("Industry");

            string savedCompany =
                GetInputValue("CompanyName");

            Assert.Multiple(() =>
            {
                Assert.That(
                    savedJobTitle,
                    Is.EqualTo(jobTitle),
                    "Job Title was not persisted.");

                Assert.That(
                    savedIndustry,
                    Is.EqualTo(industry),
                    "Industry was not persisted.");

                Assert.That(
                    savedCompany,
                    Is.EqualTo(company),
                    "Company was not persisted.");
            });
        }

        // ==========================================
        // FORM SUBMISSION
        // ==========================================

        private void SubmitProfileForm(
            IWebElement form,
            IWebElement saveButton)
        {
            // Bring the button into the viewport.

            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView(" +
                "{block:'center', inline:'nearest'});",
                saveButton);

            // Use keyboard submission to avoid
            // mouse-coordinate click interception.

            try
            {
                saveButton.SendKeys(Keys.Enter);
            }
            catch (WebDriverException ex) when (
                ex is ElementNotInteractableException ||
                ex is ElementClickInterceptedException)
            {
                // Fallback: native browser form submission.
                // requestSubmit preserves HTML validation
                // and dispatches the submit event.

                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].requestSubmit(arguments[1]);",
                    form,
                    saveButton);
            }
        }

        // ==========================================
        // WAIT FOR PROTECTED PAGE
        // ==========================================

        private void WaitForProtectedElement(
            By locator,
            string pageName)
        {
            try
            {
                _wait.Until(d =>
                    IsLoginUrl(d.Url) ||
                    IsAccessDeniedUrl(d.Url) ||
                    d.FindElements(locator)
                        .Any(element => element.Displayed));
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail(Diagnostic(
                    pageName + " did not load."));
            }

            CheckAuthentication(pageName);

            Assert.That(
                _driver.FindElements(locator)
                    .Any(element => element.Displayed),
                Is.True,
                Diagnostic(
                    pageName + " expected element missing."));
        }

        // ==========================================
        // AUTHENTICATION CHECK
        // ==========================================

        private void CheckAuthentication(string pageName)
        {
            if (IsLoginUrl(_driver.Url))
            {
                Assert.Fail(Diagnostic(
                    pageName + " redirected to Login. " +
                    "Selenium is not authenticated."));
            }

            if (IsAccessDeniedUrl(_driver.Url))
            {
                Assert.Fail(Diagnostic(
                    pageName + " returned Access Denied. " +
                    "Check the Alumni role."));
            }
        }

        // ==========================================
        // INPUT HELPERS
        // ==========================================

        private IWebElement WaitForElement(string name)
        {
            return _wait.Until(d =>
                d.FindElement(By.Name(name)));
        }

        private void ReplaceValue(
            string name,
            string value)
        {
            var input = WaitForElement(name);

            input.Clear();
            input.SendKeys(value);
        }

        private string GetInputValue(string name)
        {
            return WaitForElement(name)
                .GetAttribute("value")
                ?? string.Empty;
        }

        // ==========================================
        // URL HELPERS
        // ==========================================

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

        private static bool IsEditUrl(string url)
        {
            return url.Contains(
                "/AlumniProfiles/Edit",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAccessDeniedUrl(string url)
        {
            return url.Contains(
                "AccessDenied",
                StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // VALIDATION CHECK
        // ==========================================

        private static bool HasValidationErrors(
            IWebDriver driver)
        {
            return driver.FindElements(
                By.CssSelector(
                    ".field-validation-error, " +
                    ".validation-summary-errors, " +
                    ".alert-danger"))
                .Any(element =>
                    element.Displayed &&
                    !string.IsNullOrWhiteSpace(element.Text));
        }

        // ==========================================
        // FAILURE DIAGNOSTICS
        // ==========================================

        private string Diagnostic(string message)
        {
            string url = _driver.Url;

            var errors = _driver.FindElements(
                By.CssSelector(
                    ".field-validation-error, " +
                    ".validation-summary-errors, " +
                    ".alert-danger"));

            string errorText = string.Join(
                " | ",
                errors
                    .Where(element => element.Displayed)
                    .Select(element => element.Text)
                    .Where(text =>
                        !string.IsNullOrWhiteSpace(text)));

            var invalidFields = _driver.FindElements(
                By.CssSelector(
                    "input:invalid, " +
                    "textarea:invalid, " +
                    "select:invalid"));

            string invalidNames = string.Join(
                ", ",
                invalidFields.Select(element =>
                    element.GetAttribute("name")
                    ?? "(unnamed)"));

            return
                message +
                "\nCurrent URL: " + url +
                "\nValidation errors: " +
                (string.IsNullOrWhiteSpace(errorText)
                    ? "None displayed"
                    : errorText) +
                "\nInvalid fields: " +
                (string.IsNullOrWhiteSpace(invalidNames)
                    ? "None detected"
                    : invalidNames);
        }

        // ==========================================
        // CLEANUP
        // ==========================================

        [TearDown]
        public void TearDown()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
