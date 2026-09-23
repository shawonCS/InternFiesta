using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace InternFiesta.SeleniumTests.Tests
{
    public class InternshipTests
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

        private void CreateInternship(string title)
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5000/Internships/Create");

            var titleField = wait.Until(d =>
                d.FindElement(By.Id("Title")));

            titleField.SendKeys(title);

            driver.FindElement(By.Id("Description"))
                .SendKeys("Created automatically using Selenium.");

            driver.FindElement(By.Id("RequiredSkills"))
                .SendKeys("C#, ASP.NET Core, SQL");

            driver.FindElement(By.Id("Location"))
                .SendKeys("Dhaka");

            var positions =
                driver.FindElement(By.Id("Positions"));

            positions.Clear();
            positions.SendKeys("2");

            var deadline =
                driver.FindElement(By.Id("Deadline"));

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].value='2027-12-31';",
                deadline);

            driver.FindElement(
                By.XPath(
                    "//button[@type='submit' and contains(text(),'Create Internship')]"))
                .Click();

            wait.Until(d =>
                !d.Url.Contains("/Internships/Create"));
        }

        [Test]
        public void Company_ShouldCreateInternship()
        {
            LoginAsCompany();

            string title =
                "Selenium Create Test " +
                Guid.NewGuid().ToString("N")[..6];

            CreateInternship(title);

            Assert.Multiple(() =>
            {
                Assert.That(
                    driver.Url,
                    Does.Contain("/Internships"));

                Assert.That(
                    driver.PageSource,
                    Does.Contain(title));
            });
        }

        [Test]
        public void Company_ShouldEditInternship()
        {
            LoginAsCompany();

            string title =
                "Selenium Edit Test " +
                Guid.NewGuid().ToString("N")[..6];

            CreateInternship(title);

            var card = wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        $"//div[contains(@class,'dashboard-card')][.//h5[normalize-space()='{title}']]")));

            card.FindElement(
                By.XPath(".//a[normalize-space()='Edit']"))
                .Click();

            wait.Until(d =>
                d.Url.Contains("/Internships/Edit"));

            var location =
                driver.FindElement(By.Id("Location"));

            location.Clear();
            location.SendKeys("Dhaka / Hybrid");

            driver.FindElement(
                By.XPath(
                    "//button[@type='submit' and contains(text(),'Save Changes')]"))
                .Click();

            wait.Until(d =>
                !d.Url.Contains("/Internships/Edit"));

            var updatedCard = wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        $"//div[contains(@class,'dashboard-card')][.//h5[normalize-space()='{title}']]")));

            Assert.That(
                updatedCard.Text,
                Does.Contain("Dhaka / Hybrid"));
        }

        [Test]
        public void Company_ShouldCloseInternship()
        {
            LoginAsCompany();

            string title =
                "Selenium Close Test " +
                Guid.NewGuid().ToString("N")[..6];

            CreateInternship(title);

            var card = wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        $"//div[contains(@class,'dashboard-card')][.//h5[normalize-space()='{title}']]")));

            card.FindElement(
                By.XPath(".//button[normalize-space()='Close']"))
                .Click();

            driver.SwitchTo()
                .Alert()
                .Accept();

            wait.Until(d =>
                d.Url.Contains("/Internships"));

            var closedCard = wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        $"//div[contains(@class,'dashboard-card')][.//h5[normalize-space()='{title}']]")));

            Assert.That(
                closedCard.Text,
                Does.Contain("Closed"));
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}