using Microsoft.Playwright;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JDPriceProtection
{
    class Program
    {
        private const string RunRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static async Task Main()
        {
            try
            {
                // 等待网络连接
                await WaitForNetworkConnectionAsync();

                // 确保浏览器已安装
                await EnsureBrowserInstalled();

                // 执行价格保护操作
                await ExecutePriceProtectionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序发生错误: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static async Task WaitForNetworkConnectionAsync()
        {
            while (true)
            {
                try
                {
                    var response = await httpClient.GetAsync("https://www.baidu.com");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("网络正常");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"网络异常：{ex.Message}");
                }
                await Task.Delay(1000);
            }
        }

        private static async Task EnsureBrowserInstalled()
        {
            try
            {
                using var playwrightTemp = await Playwright.CreateAsync();
                await playwrightTemp.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                Console.WriteLine("浏览器已安装，跳过安装步骤。");
            }
            catch
            {
                Console.WriteLine("检测到浏览器未安装，开始安装 Chromium...");
                var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });

                if (exitCode != 0)
                {
                    throw new Exception("无法安装 Chromium 浏览器。请手动安装谷歌浏览器");
                }

                Console.WriteLine("Chromium 安装完成！");
            }
        }

        private static async Task ExecutePriceProtectionAsync()
        {
            var playwright = await Playwright.CreateAsync();
            string userDataDir = @"C:\JDPriceProtection";

            await using var browser = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false
            });

            var page = await browser.NewPageAsync();

            try
            {
                page.Dialog += async (sender, dialog) =>
                {
                    await dialog.DismissAsync();
                };

                await page.GotoAsync("https://msitepp-fm.jd.com/rest/priceprophone/priceProPhoneMenu");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                if (await page.Locator("#one-btn").IsVisibleAsync())
                {
                    await page.ClickAsync("#one-btn");
                    await page.WaitForTimeoutAsync(2000);

                    var successText = await page.QuerySelectorAsync("text='恭喜您价保成功'");
                    if (successText != null)
                    {
                        Console.WriteLine("价保成功！按任意键退出...");
                        Console.ReadLine();
                    }
                }
                else
                {
                    if (!await page.Locator("#one-btn-dis").IsVisibleAsync())
                    {
                        await page.EvaluateAsync("confirm('需要登录京东账户，请先登录，登录完成后请关闭浏览器，后续不用重复登录。')");
                        Console.WriteLine("请登录京东账户...");
                        Console.ReadLine();
                    }
                }
            }
            finally
            {
                await browser.CloseAsync();
            }
        }
    }
}
