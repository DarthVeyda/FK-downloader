using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace FK_Downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IWebDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl("file:///F:/!tmp/Diary/Diary.ru%20-%20Mobile%20v2.htm#mainPage");
                driver.FindElement(By.Id("username")).SendKeys("ФБ: Гость");
                driver.FindElement(By.Id("password")).SendKeys("zxcvbn");
                driver.FindElement(By.Id("loginbutton")).Click();
                driver.Quit();
            }
        }
    }
}
